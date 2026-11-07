using System;

namespace Zyara.Services.Features.Providers.Dtos;

public sealed class ProviderReviewDto
{
    public string ReviewerName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
