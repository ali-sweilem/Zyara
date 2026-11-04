using System;
using System.Collections.Generic;
using Zyara.Services.Features.Providers;

namespace Zyara.Web.Models
{
    public class HomeIndexViewModel
    {
        public ProviderSearchCriteria Criteria { get; set; } = new();
        public PagedResult<ProviderCardDto> Results { get; set; } = new();
        public IReadOnlyList<string> Specialties { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Cities { get; set; } = Array.Empty<string>();
    }
}

