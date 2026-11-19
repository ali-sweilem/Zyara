using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Zyara.Domain.Entities;

namespace Zyara.Services.Features.Authorization.Handlers;

public sealed class ProviderProfileAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Provider>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Provider provider)
    {
        var claim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId))
        {
            return Task.CompletedTask;
        }

        var isAdmin = context.User.IsInRole("Admin");
        var isOwner = provider.Id == userId;

        if (requirement.Name == Operations.View.Name && (isOwner || isAdmin))
        {
            context.Succeed(requirement);
        }
        else if (requirement.Name == Operations.Manage.Name && isOwner)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
