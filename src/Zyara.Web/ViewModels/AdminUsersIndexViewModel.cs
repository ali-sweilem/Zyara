using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Web.ViewModels
{
    public class AdminUsersIndexViewModel
    {
        public PagedResult<UserListDto> Users { get; init; } = new();
        public UserRole? Role { get; init; }
    }
}
