using System.Collections.Generic;
using Zyara.Services.Features.Services;


namespace Zyara.Web.Models
{
    public class ServiceIndexViewModel
    {
        public IReadOnlyList<ServiceDto> Services { get; init; }
        = new List<ServiceDto>();
    }
}
