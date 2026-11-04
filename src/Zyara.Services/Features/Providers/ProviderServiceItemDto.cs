namespace Zyara.Services.Features.Providers;

public sealed class ProviderServiceItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Fee { get; init; }
}
