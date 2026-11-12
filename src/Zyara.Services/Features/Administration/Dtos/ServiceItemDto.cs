using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Administration.Dtos
{
    public class ServiceItemDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public ServiceType Type { get; init; }
        public bool IsActive { get; init; }
    }
}
