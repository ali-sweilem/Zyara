using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Web.Models;

public sealed class ProviderDetailsViewModel
{
    public ProviderDetailsDto Provider { get; init; } = null!;
    public PagedResult<ProviderReviewDto> Reviews { get; init; } = null!;
}
