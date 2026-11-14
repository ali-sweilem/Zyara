using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Services;

public sealed class UpdateServiceDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ServiceType Type { get; init; }
}