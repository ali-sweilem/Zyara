using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments;

public class FakeVisaGateway : IPaymentGateway
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public FakeVisaGateway(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
    }

    public bool CollectsCardDetailsInApp => true;

    public Task<GatewayResult> AuthorizeAsync(decimal amount, int bookingId, CardInput? card = null)
    {
        var allowFake = _config.GetValue<bool>("Payment:AllowFakeGateway");
        if (!_env.IsDevelopment() && !allowFake)
        {
            throw new InvalidOperationException("FakeVisaGateway is only permitted in Development environment unless Payment:AllowFakeGateway is true.");
        }

        if (card == null || string.IsNullOrWhiteSpace(card.CardNumber))
        {
            return Task.FromResult(GatewayResult.Failure("Card number is required."));
        }

        var cleaned = card.CardNumber.Replace(" ", "").Replace("-", "").Trim();
        if (!PassesLuhn(cleaned))
        {
            return Task.FromResult(GatewayResult.Failure("Invalid card number format or checksum."));
        }

        if (cleaned == "4000000000000002")
        {
            return Task.FromResult(GatewayResult.Failure("Your card was declined by the bank."));
        }

        if (cleaned == "4000000000009995")
        {
            return Task.FromResult(GatewayResult.Failure("Insufficient funds on this card."));
        }

        if (cleaned == "4242424242424242")
        {
            var reference = $"FAKE-{Guid.NewGuid():N}";
            return Task.FromResult(GatewayResult.Success(reference));
        }

        return Task.FromResult(GatewayResult.Failure("Invalid card number or unsupported test card."));
    }

    public Task<GatewayResult> CaptureAsync(string gatewayReference, decimal amount)
    {
        return Task.FromResult(GatewayResult.Success(gatewayReference));
    }

    public Task<GatewayResult> ReleaseAsync(string gatewayReference)
    {
        return Task.FromResult(GatewayResult.Success(gatewayReference));
    }

    private static bool PassesLuhn(string number)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Length < 12)
            return false;

        int sum = 0;
        bool alternate = false;
        for (int i = number.Length - 1; i >= 0; i--)
        {
            char c = number[i];
            if (!char.IsDigit(c)) return false;
            int n = c - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }
        return (sum % 10 == 0);
    }
}