using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Data;
using Zyara.Services.Features.Providers;
using Zyara.Services.Features.Services;

namespace Zyara.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZyaraServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ZyaraDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default")));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IProviderSearchService, ProviderSearchService>();
        services.AddScoped<IProviderDetailsService, ProviderDetailsService>();
        services.AddScoped<IServiceManagementService, ServiceManagementService>();
        return services;
    }
}