using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Payments.Dtos;

/// <summary>Data for the (demo) hosted payment page.</summary>
public sealed record CardCheckoutDto(int BookingId, decimal Amount, string Reference);

/// <summary>Data for the result page. Always read from the database, never from the URL.</summary>
public sealed record CardResultDto(int BookingId, PaymentStatus Status, decimal Amount, string? CardBrand, string? CardLast4);

public enum WebhookOutcome
{
    Applied,           // payment moved Pending -> Authorized
    Duplicate,         // already processed (or not a pending online payment): nothing changed
    Failed,            // the gateway says the charge failed: payment stays Pending
    UnknownReference,  // no payment with this reference
    InvalidSignature   // not from the gateway: rejected
}
