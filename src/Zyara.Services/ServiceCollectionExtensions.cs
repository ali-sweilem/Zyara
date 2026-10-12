using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Data;

namespace Zyara.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZyaraServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ZyaraDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default")));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}