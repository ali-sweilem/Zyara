using System;
using System.Collections.Generic;
using Zyara.Services.Common;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Web.ViewModels
{
    public class HomeIndexViewModel
    {
        public ProviderSearchCriteria Criteria { get; set; } = new();
        public PagedResult<ProviderCardDto> Results { get; set; } = new();
        public IReadOnlyList<string> Specialties { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Cities { get; set; } = Array.Empty<string>();
    }
}

