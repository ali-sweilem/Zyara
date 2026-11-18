using Microsoft.AspNetCore.Identity;
using Zyara.Domain.Enums;

namespace Zyara.Repositories.Data.Seed;

/// <summary>Creates one Identity role per <see cref="UserRole"/> value. Idempotent.</summary>
public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        foreach (var name in Enum.GetNames<UserRole>())
        {
            if (await roleManager.RoleExistsAsync(name))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<int>(name));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed role '{name}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
