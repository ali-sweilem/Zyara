using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Zyara.Domain.Entities;

namespace Zyara.Services.Features.Authorization.Handlers;

public sealed class CredentialAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, ProviderCredential>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        ProviderCredential credential)
    {
        var claim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId))
        {
            return Task.CompletedTask;
        }

        var isAdmin = context.User.IsInRole("Admin");
        var isOwner = credential.ProviderID == userId;

        if ((requirement.Name == Operations.View.Name || requirement.Name == Operations.Download.Name)
            && (isOwner || isAdmin))
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
