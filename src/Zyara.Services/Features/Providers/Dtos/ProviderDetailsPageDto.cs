using Zyara.Services.Common;

namespace Zyara.Services.Features.Providers.Dtos;

public sealed class ProviderDetailsPageDto
{
    public ProviderDetailsDto Provider { get; init; } = null!;
    public PagedResult<ProviderReviewDto> Reviews { get; init; } = null!;
}

