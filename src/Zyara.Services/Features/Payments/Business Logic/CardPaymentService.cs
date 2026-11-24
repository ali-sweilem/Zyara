using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments;

public sealed class CardPaymentService : ICardPaymentService
{
    private readonly ZyaraDbContext _db;
    private readonly IPaymentGateway _gateway;

    public CardPaymentService(ZyaraDbContext db, IPaymentGateway gateway)
    {
        _db = db;
        _gateway = gateway;
    }

    public async Task<GatewayCheckoutDto?> StartCheckoutAsync(int bookingId, string returnUrl)
    {
        var booking = await _db.Bookings
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return null;

        var payment = booking.Payment;
        if (payment is null)
        {
            // The amount is read from the database, never from the form.
            payment = new Payment
            {
                BookingId = booking.Id,
                Method = PaymentMethod.OnlinePayment,
                Status = PaymentStatus.Pending,
                Amount = booking.TotalPrice,
                RefundedAmount = 0m
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
        }
        else if (payment.Method != PaymentMethod.OnlinePayment || payment.Status != PaymentStatus.Pending)
        {
            // Already paid, or the patient already chose cash: nothing to start.
            return null;
        }

        var checkout = await _gateway.CreateCheckoutAsync(payment, returnUrl);
        payment.GatewayReference = checkout.Reference;
        await _db.SaveChangesAsync();
        return checkout;
    }

    public async Task<CardCheckoutDto?> GetCheckoutAsync(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;

        var payment = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GatewayReference == reference
                                      && p.Method == PaymentMethod.OnlinePayment
                                      && p.Status == PaymentStatus.Pending);

        return payment is null ? null : new CardCheckoutDto(payment.BookingId, payment.Amount, reference);
    }

    public async Task<WebhookOutcome> HandleWebhookAsync(string body, string? signature)
    {
        var ev = _gateway.VerifyWebhook(body, signature);
        if (ev is null) return WebhookOutcome.InvalidSignature;

        var payment = await _db.Payments
            .Include(p => p.Booking).ThenInclude(b => b!.Service)
            .FirstOrDefaultAsync(p => p.GatewayReference == ev.Reference);
        if (payment is null) return WebhookOutcome.UnknownReference;

        var outcome = Apply(payment, ev);
        if (outcome != WebhookOutcome.Applied) return outcome;

        // Same as the cash flow: home visits get an OTP the provider asks for at the door.
        var booking = payment.Booking;
        var isHomeVisit = booking?.Service is not null && booking.Service.Type != ServiceType.RemoteConsultation;
        if (booking is not null && isHomeVisit && string.IsNullOrEmpty(booking.OtpCode))
        {
            booking.OtpCode = Random.Shared.Next(1000, 10000).ToString();
        }

        await _db.SaveChangesAsync();
        return WebhookOutcome.Applied;
    }

    public async Task<CardResultDto?> GetResultAsync(int bookingId)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Method == PaymentMethod.OnlinePayment);

        return payment is null
            ? null
            : new CardResultDto(payment.BookingId, payment.Status, payment.Amount, payment.CardBrand, payment.CardLast4);
    }

    /// <summary>
    /// The idempotency rule, kept pure so it can be unit-tested: only a Pending online payment can change.
    /// A repeated webhook finds it already Authorized and does nothing.
    /// </summary>
    public static WebhookOutcome Apply(Payment payment, GatewayWebhookEvent ev)
    {
        if (payment.Method != PaymentMethod.OnlinePayment || payment.Status != PaymentStatus.Pending)
            return WebhookOutcome.Duplicate;

        if (!ev.Succeeded) return WebhookOutcome.Failed;

        payment.CardBrand = ev.CardBrand;
        payment.CardLast4 = IsLast4(ev.CardLast4) ? ev.CardLast4 : null;
        payment.Authorize();
        return WebhookOutcome.Applied;
    }

    private static bool IsLast4(string? v) => v is { Length: 4 } && v.All(char.IsDigit);
}
