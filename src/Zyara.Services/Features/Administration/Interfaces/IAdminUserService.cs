using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminUserService
    {
        /// <summary>Users page, optionally filtered by role and search query.</summary>
        Task<PagedResult<UserListDto>> GetUsersAsync(UserRole? role, string? search = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

        /// <summary>Flips IsActive. Prevents self-deactivation when currentAdminId matches.</summary>
        Task<ServiceResult> ToggleUserActiveAsync(int id, int currentAdminId, CancellationToken cancellationToken = default);
    }
}

