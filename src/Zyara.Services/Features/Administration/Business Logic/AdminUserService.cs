using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
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

        public AdminUserService(
            IRepository<Zyara.Domain.Entities.User> users,
            IUnitOfWork unitOfWork,
            ISessionService sessions)
        {
            _users = users;
            _unitOfWork = unitOfWork;
            _sessions = sessions;
        }

        public Task<PagedResult<UserListDto>> GetUsersAsync(UserRole? role,int page,int pageSize = 10,CancellationToken cancellationToken = default)
        {
            var query = _users.Query().AsNoTracking();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            return query
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

