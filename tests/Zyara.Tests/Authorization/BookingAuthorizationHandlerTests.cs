using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Authorization.Handlers;

namespace Zyara.Tests.Authorization;

public class BookingAuthorizationHandlerTests
{
    private static readonly Booking Booking = new()
    {
        PatientId = 1,
        ProviderId = 2
    };

    [Fact]
    public async Task Owner_patient_can_view_booking()
    {
        Assert.True(await IsAuthorizedAsync(Operations.View, 1));
    }

    [Fact]
    public async Task Owner_provider_can_view_booking()
    {
        Assert.True(await IsAuthorizedAsync(Operations.View, 2));
    }

    [Fact]
    public async Task Other_user_cannot_view_booking()
    {
        Assert.False(await IsAuthorizedAsync(Operations.View, 3));
    }

    [Fact]
    public async Task Admin_can_view_booking()
    {
        Assert.True(await IsAuthorizedAsync(Operations.View, 3, "Admin"));
    }

    [Fact]
    public async Task Admin_cannot_cancel_booking()
    {
        Assert.False(await IsAuthorizedAsync(Operations.Cancel, 3, "Admin"));
    }

    [Fact]
    public async Task Owner_can_cancel_booking()
    {
        Assert.True(await IsAuthorizedAsync(Operations.Cancel, 1));
    }

    [Fact]
    public async Task Anonymous_user_cannot_view_booking()
    {
        Assert.False(await IsAuthorizedAsync(Operations.View));
    }

    private static async Task<bool> IsAuthorizedAsync(
        OperationAuthorizationRequirement operation,
        int? userId = null,
        string? role = null)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var context = new AuthorizationHandlerContext(new[] { operation }, user, Booking);
        var handler = new BookingAuthorizationHandler();

        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }
}
