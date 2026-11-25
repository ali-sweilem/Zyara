namespace Zyara.Services.Features.Providers.Dtos;

public sealed class ProviderServiceItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Fee { get; init; }

    /// <summary>True for home-visit services (the patient must pick an address); false for remote consultations.</summary>
    public bool IsHomeVisit { get; init; }
}

