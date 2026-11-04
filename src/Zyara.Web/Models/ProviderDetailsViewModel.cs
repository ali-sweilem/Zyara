using Zyara.Services.Features.Providers;

namespace Zyara.Web.Models;

public sealed class ProviderDetailsViewModel
{
    public ProviderDetailsDto Provider { get; init; } = null!;
    public PagedResult<ProviderReviewDto> Reviews { get; init; } = null!;
}
