using Zyara.Domain.Entities;
using Zyara.Services.Features.Payments.Dtos;

namespace Zyara.Services.Features.Payments.Interface;

/// <summary>
/// The card gateway behind a small interface (T12). The card number is typed on the GATEWAY's page,
/// so it never reaches our server; we only keep the reference, brand and last 4 digits.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Creates a checkout for this payment and returns where to send the patient.</summary>
    Task<GatewayCheckoutDto> CreateCheckoutAsync(Payment payment, string returnUrl);

    /// <summary>Checks the webhook signature and parses it. Returns null if the signature is wrong or the body is bad.</summary>
    GatewayWebhookEvent? VerifyWebhook(string body, string? signature);
}

/// <summary>
/// Only the demo gateway implements this: it plays the part of the gateway's hosted page
/// and produces the signed webhook a real gateway would send.
/// </summary>
public interface IHostedCheckoutSimulator
{
    (string Body, string Signature) Charge(string reference, string cardNumber);
}
