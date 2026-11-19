using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Zyara.Services.Features.Authorization;

public static class Operations
{
    public static readonly OperationAuthorizationRequirement View = new() { Name = "View" };
    public static readonly OperationAuthorizationRequirement Cancel = new() { Name = "Cancel" };
    public static readonly OperationAuthorizationRequirement Manage = new() { Name = "Manage" };
    public static readonly OperationAuthorizationRequirement Download = new() { Name = "Download" };
}
