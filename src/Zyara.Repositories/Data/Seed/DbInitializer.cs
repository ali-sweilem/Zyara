using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zyara.Domain.Entities;

namespace Zyara.Repositories.Data.Seed;

/// <summary>Migrate -> RoleSeeder -> AdminSeeder. Called from Program.cs in every environment.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        string? adminEmail,
        string? adminPassword,
        CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        var db = services.GetRequiredService<ZyaraDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await RoleSeeder.SeedAsync(services.GetRequiredService<RoleManager<IdentityRole<int>>>());

        await AdminSeeder.SeedAsync(
            services.GetRequiredService<UserManager<User>>(),
            adminEmail,
            adminPassword,
            logger);
    }
}
