using System.Collections.Generic;

namespace Zyara.Web.Models;

public sealed class LandingIndexViewModel
{
    public IReadOnlyList<ServiceOption> Services { get; init; } = [];
}
