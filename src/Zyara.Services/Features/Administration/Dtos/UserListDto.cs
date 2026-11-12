using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Administration.Dtos
{
    public class UserListDto
    {
        public int Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public UserRole Role { get; init; }
        public bool IsActive { get; init; }
    }
}
