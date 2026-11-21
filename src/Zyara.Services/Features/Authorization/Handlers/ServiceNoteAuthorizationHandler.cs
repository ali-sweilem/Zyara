using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Zyara.Domain.Entities;

namespace Zyara.Services.Features.Authorization.Handlers;

public sealed class ServiceNoteAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, ServiceNote>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        ServiceNote note)
    {
        var claim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId) || note.Booking is null)
        {
            return Task.CompletedTask;
        }

        var booking = note.Booking;
        var isAdmin = context.User.IsInRole("Admin");
        var canView = booking.PatientId == userId || booking.ProviderId == userId || isAdmin;
        var canManage = booking.ProviderId == userId;

        if (requirement.Name == Operations.View.Name && canView)
        {
            context.Succeed(requirement);
        }
        else if (requirement.Name == Operations.Manage.Name && canManage)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
