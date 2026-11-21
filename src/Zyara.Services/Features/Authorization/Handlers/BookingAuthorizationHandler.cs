using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Zyara.Domain.Entities;

namespace Zyara.Services.Features.Authorization.Handlers;

public sealed class BookingAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Booking>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Booking booking)
    {
        var claim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId))
        {
            return Task.CompletedTask;
        }

        var isAdmin = context.User.IsInRole("Admin");
        var isOwner = booking.PatientId == userId || booking.ProviderId == userId;

        if (requirement.Name == Operations.View.Name && (isOwner || isAdmin))
        {
            context.Succeed(requirement);
        }
        else if (requirement.Name == Operations.Cancel.Name && isOwner)
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
