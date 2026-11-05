using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Services;

public sealed class ServiceDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ServiceType Type { get; init; }
    public bool IsActive { get; init; }
}