using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Repositories.Data.Seed;

/// <summary>
/// Creates the Admin account from user-secrets (Seed:Admin:Email / Seed:Admin:Password). Idempotent.
/// Admins are never created through public registration (SRS 3.3.1).
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(
        UserManager<User> userManager,
        string? email,
        string? password,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Admin seeding skipped: set Seed:Admin:Email and Seed:Admin:Password with 'dotnet user-secrets'.");
            return;
        }

        var roleName = nameof(UserRole.Admin);
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Make sure the account still has the Admin role; never touch its password.
            if (!await userManager.IsInRoleAsync(existing, roleName))
            {
                await userManager.AddToRoleAsync(existing, roleName);
            }
            return;
        }

        var admin = new Admin
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "System Admin",
            PhoneNumber = "01000000000",
            Gender = Gender.Male,
            Role = UserRole.Admin,
            IsActive = true
        };

        var create = await userManager.CreateAsync(admin, password);
        if (!create.Succeeded)
        {
            // Never log the password.
            throw new InvalidOperationException(
                $"Failed to seed admin: {string.Join("; ", create.Errors.Select(e => e.Description))}");
        }

        var addRole = await userManager.AddToRoleAsync(admin, roleName);
        if (!addRole.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to assign Admin role: {string.Join("; ", addRole.Errors.Select(e => e.Description))}");
        }
    }
}
