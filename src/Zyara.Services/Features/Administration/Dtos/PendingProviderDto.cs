using Zyara.Domain.Enums;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Administration.Dtos
{
    public class PendingProviderDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Specialty { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string? BaseCity { get; set; }
        public List<ProviderCredentialDto> Credentials { get; set; } = new();
    }
}