using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments;

/// <summary>
/// Demo gateway (Payment:Provider = Fake). Accepts the test card 4242 4242 4242 4242 and nothing else.
/// Webhooks are signed with HMAC-SHA256 using Payment:WebhookSecret (user-secrets); without a secret a
/// random per-process one is used, which is fine for the demo because the signing happens in the same process.
/// </summary>
public sealed class FakeVisaGateway : IPaymentGateway, IHostedCheckoutSimulator
{
    public const string TestCard = "4242424242424242";

    private static readonly byte[] ProcessSecret = RandomNumberGenerator.GetBytes(32);
    private readonly byte[] _secret;

    public FakeVisaGateway(string? webhookSecret = null)
    {
        _secret = string.IsNullOrWhiteSpace(webhookSecret) ? ProcessSecret : Encoding.UTF8.GetBytes(webhookSecret);
    }

    public Task<GatewayCheckoutDto> CreateCheckoutAsync(Payment payment, string returnUrl)
    {
        var reference = "FAKE-" + Guid.NewGuid().ToString("N");
        // Our own page plays the role of the gateway's hosted page.
        return Task.FromResult(new GatewayCheckoutDto(reference, "/CardPayment/HostedPage?reference=" + reference));
    }

    public GatewayWebhookEvent? VerifyWebhook(string body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(signature)) return null;

        byte[] given;
        try { given = Convert.FromHexString(signature.Trim()); }
        catch (FormatException) { return null; }

        var expected = HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(body));
        if (!CryptographicOperations.FixedTimeEquals(expected, given)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var reference = root.GetProperty("reference").GetString();
            if (string.IsNullOrWhiteSpace(reference)) return null;

            var succeeded = root.GetProperty("succeeded").GetBoolean();
            var brand = root.TryGetProperty("cardBrand", out var b) ? b.GetString() : null;
            var last4 = root.TryGetProperty("cardLast4", out var l) ? l.GetString() : null;
            return new GatewayWebhookEvent(reference, succeeded, brand, last4);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    public (string Body, string Signature) Charge(string reference, string cardNumber)
    {
        // The number lives only in this method: it is compared and dropped, never stored or logged.
        var digits = new string((cardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        var ok = digits == TestCard;

        var body = JsonSerializer.Serialize(new
        {
            reference,
            succeeded = ok,
            cardBrand = ok ? "Visa" : null,
            cardLast4 = ok ? digits[^4..] : null
        });

        return (body, Sign(body));
    }

    public string Sign(string body) =>
        Convert.ToHexString(HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
}
