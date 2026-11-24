namespace Zyara.Services.Features.Payments.Dtos;

/// <summary>What the gateway gives back when a checkout is created: where to send the patient.</summary>
public sealed record GatewayCheckoutDto(string Reference, string RedirectUrl);

/// <summary>A verified webhook: the gateway telling us the result of a checkout.</summary>
public sealed record GatewayWebhookEvent(string Reference, bool Succeeded, string? CardBrand, string? CardLast4);
