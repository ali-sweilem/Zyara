using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminUserService
    {
        /// <summary>Users page, optionally filtered by role (null = all roles).</summary>
        Task<PagedResult<UserListDto>> GetUsersAsync(UserRole? role, int page, int pageSize = 10, CancellationToken cancellationToken = default);

        /// <summary>Flips IsActive. Returns false if the user does not exist.</summary>
        Task<bool> ToggleUserActiveAsync(int id, CancellationToken cancellationToken = default);
    }
}
