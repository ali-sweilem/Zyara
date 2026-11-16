using Zyara.Domain.Enums;
using Zyara.Services.Common;

namespace Zyara.Services.Features.Administration.Dtos;

public sealed class AdminUsersIndexDto
{
    public PagedResult<UserListDto> Users { get; init; } = new();
    public UserRole? Role { get; init; }
}

