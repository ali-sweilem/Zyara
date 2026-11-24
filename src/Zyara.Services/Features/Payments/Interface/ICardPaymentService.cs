using Zyara.Services.Features.Payments.Dtos;

namespace Zyara.Services.Features.Payments.Interface;

public interface ICardPaymentService
{
    /// <summary>Creates (or restarts) the online payment for a booking. The amount comes from the database. Null = not possible.</summary>
    Task<GatewayCheckoutDto?> StartCheckoutAsync(int bookingId, string returnUrl);

    Task<CardCheckoutDto?> GetCheckoutAsync(string reference);

    /// <summary>Idempotent: the same webhook twice changes nothing the second time.</summary>
    Task<WebhookOutcome> HandleWebhookAsync(string body, string? signature);

    Task<CardResultDto?> GetResultAsync(int bookingId);
}
