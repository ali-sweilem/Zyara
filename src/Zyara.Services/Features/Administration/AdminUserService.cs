using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Data;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminUserService : IAdminUserService
    {
        private readonly ZyaraDbContext _db;

        public AdminUserService(ZyaraDbContext db)
        {
            _db = db;
        }

        public Task<PagedResult<UserListDto>> GetUsersAsync(UserRole? role,int page,int pageSize = 10,CancellationToken cancellationToken = default)
        {
            var query = _db.Users.AsNoTracking();

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
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user is null)
            {
                return false;
            }

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
