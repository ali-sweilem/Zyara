using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Zyara.Services.Features.Authorization;

namespace Zyara.Web.Infrastructure;

public sealed class AreaAuthorizationConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.Attributes.OfType<AuthorizeAttribute>().Any())
        {
            return;
        }

        if (!controller.RouteValues.TryGetValue("area", out var area))
        {
            return;
        }

        var policyName = area switch
        {
            "Admin" => PolicyNames.AdminOnly,
            "Provider" => PolicyNames.ProviderOnly,
            _ => null
        };

        if (policyName is not null)
        {
            controller.Filters.Add(new AuthorizeFilter(policyName));
        }
    }
}
