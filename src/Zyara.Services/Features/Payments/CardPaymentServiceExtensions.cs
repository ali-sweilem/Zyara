using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zyara.Services.Features.Payments;

/// <summary>STUB (T1 - Ali). Adham fills it in T12 (IPaymentGateway: FakeVisaGateway / PaymobGateway by "Payment:Provider").</summary>
public static class CardPaymentServiceExtensions
{
    public static IServiceCollection AddZyaraCardPayments(this IServiceCollection services, IConfiguration config)
    {
        return services;
    }
}
