using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Services.Features.Shared;
using Zyara.Services.Common;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IRepository<Zyara.Domain.Entities.User> _users;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISessionService _sessions;
        private readonly IRepository<Provider> _providers;
        private readonly TimeProvider _timeProvider;

        public AdminUserService(
            IRepository<Zyara.Domain.Entities.User> users,
            IUnitOfWork unitOfWork,
            ISessionService sessions,
            IRepository<Provider> providers,
            TimeProvider timeProvider)
        {
            _users = users;
            _unitOfWork = unitOfWork;
            _sessions = sessions;
            _providers = providers;
            _timeProvider = timeProvider;
        }

        public async Task<PagedResult<UserListDto>> GetUsersAsync(UserRole? role,int page,int pageSize = 10,CancellationToken cancellationToken = default)
        {
            var query = _users.Query().AsNoTracking();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            var result = await query
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .ToPagedAsync(
                    u => new UserListDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Email = u.Email,
                        Role = u.Role,
                        IsActive = u.IsActive
                    },
                    page,
                    pageSize,
                    maxPageSize: 50,
                    cancellationToken: cancellationToken);

            // providers: this week's same-day cancellations and whether new bookings are paused
            var providerIds = result.Items
                .Where(u => u.Role == UserRole.Doctor || u.Role == UserRole.Nurse)
                .Select(u => u.Id)
                .ToList();

            if (providerIds.Count > 0)
            {
                var stats = await _providers.Query()
                    .AsNoTracking()
                    .Where(p => providerIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.SameDayCancellationCount, p.CancellationWeekStart, p.BookingsSuspendedUntilUtc })
                    .ToListAsync(cancellationToken);

                var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
                var egyptNow = EgyptTime.Now(_timeProvider);

                foreach (var stat in stats)
                {
                    var item = result.Items.First(u => u.Id == stat.Id);
                    item.SameDayCancellations = CancellationRules.EffectiveWeeklyCount(
                        stat.SameDayCancellationCount, stat.CancellationWeekStart, egyptNow);
                    item.BookingsSuspended = stat.BookingsSuspendedUntilUtc.HasValue && stat.BookingsSuspendedUntilUtc.Value > utcNow;
                }
            }

            return result;
        }

        public async Task<bool> ResetProviderCancellationsAsync(int providerId, CancellationToken cancellationToken = default)
        {
            var provider = await _providers.FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);
            if (provider is null)
            {
                return false;
            }

            provider.SameDayCancellationCount = 0;
            provider.CancellationWeekStart = null;
            provider.BookingsSuspendedUntilUtc = null;

            await _unitOfWork.ExecuteInTransactionAsync(
                ct => _unitOfWork.SaveChangesAsync(ct), cancellationToken);
            return true;
        }

        public async Task<bool> ToggleUserActiveAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user is null)
            {
                return false;
            }

            user.IsActive = !user.IsActive;
            await _unitOfWork.ExecuteInTransactionAsync(
                ct => _unitOfWork.SaveChangesAsync(ct), cancellationToken);

            // A deactivated user must not stay signed in: kill their cookies (T9).
            if (!user.IsActive)
            {
                await _sessions.InvalidateAsync(user.Id);
            }

            return true;
        }
    }
}

