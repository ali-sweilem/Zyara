using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Common;

namespace Zyara.Web.ViewModels
{

    public sealed class ProviderDetailsViewModel
    {
        public ProviderDetailsDto Provider { get; init; } = null!;
        public PagedResult<ProviderReviewDto> Reviews { get; init; } = null!;
    }
}