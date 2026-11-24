using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments;

public static class CardPaymentServiceExtensions
{
    /// <summary>T12: registers the card gateway chosen by Payment:Provider (default "Fake") and the card payment service.</summary>
    public static IServiceCollection AddZyaraCardPayments(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Payment:Provider"];
        if (string.IsNullOrWhiteSpace(provider)) provider = "Fake";

        switch (provider.Trim().ToLowerInvariant())
        {
            case "fake":
                var gateway = new FakeVisaGateway(config["Payment:WebhookSecret"]);
                services.AddSingleton(gateway);
                services.AddSingleton<IPaymentGateway>(gateway);
                services.AddSingleton<IHostedCheckoutSimulator>(gateway);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown Payment:Provider '{provider}'. Supported values: Fake.");
        }

        services.AddScoped<ICardPaymentService, CardPaymentService>();
        return services;
    }
}
