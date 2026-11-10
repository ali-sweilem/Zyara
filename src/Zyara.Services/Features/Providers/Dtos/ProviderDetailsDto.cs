using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers.Dtos;

public sealed class ProviderDetailsDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public string Specialty { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public int ExperienceYears { get; init; }
    public string BaseCity { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public double AverageRating { get; init; }
    public int RatingCount { get; init; }
    public string? PhotoUrl { get; init; }
    public decimal? MinFee { get; init; }
    public bool IsAvailable { get; set; }
    public IReadOnlyList<ProviderServiceItemDto> Services { get; init; } = [];
    public IReadOnlyList<UpcomingSlotDto> UpcomingSlots { get; init; } = [];
}
