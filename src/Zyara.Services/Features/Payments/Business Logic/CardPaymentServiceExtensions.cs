using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zyara.Services.Features.Payments;

public static class CardPaymentServiceExtensions
{
    public static IServiceCollection AddZyaraCardPayments(this IServiceCollection services, IConfiguration config)
    {
        return services;
    }
}