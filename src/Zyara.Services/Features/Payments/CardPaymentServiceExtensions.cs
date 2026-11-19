using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments;

public static class CardPaymentServiceExtensions
{
    public static IServiceCollection AddZyaraCardPayments(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Payment:Provider"] ?? "FakeVisa";

        if (provider.Equals("Paymob", System.StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IPaymentGateway, PaymobGateway>();
        }
        else
        {
            services.AddScoped<IPaymentGateway, FakeVisaGateway>();
        }

        return services;
    }
}