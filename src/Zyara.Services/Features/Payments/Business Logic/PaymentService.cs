using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data;
using Zyara.Services.Common;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;
using Zyara.Services.Features.Providers;

namespace Zyara.Services.Features.Payments;

public class PaymentService : IPaymentService
{
    private readonly ZyaraDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly IConfiguration _config;
    private readonly TimeProvider _timeProvider;

    public PaymentService(
        ZyaraDbContext db,
        IPaymentGateway? gateway = null,
        IConfiguration? config = null,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _gateway = gateway!;
        _config = config!;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<BookingConfirmationDto?> GetBookingConfirmationAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Provider)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null) return null;

        var egyptNow = EgyptTime.Now(_timeProvider);
        var isHomeVisit = booking.Service != null && booking.Service.Type != ServiceType.RemoteConsultation;

        return new BookingConfirmationDto
        {
            BookingId = booking.Id,
            PatientId = booking.PatientId,
            Status = booking.Status,
            TotalPrice = booking.TotalPrice,
            OtpCode = isHomeVisit ? booking.OtpCode : null,
            IsHomeVisit = isHomeVisit,
            ProviderFullName = booking.Provider?.FullName ?? "Healthcare Provider",
            ProviderSpecialty = booking.Provider?.Specialty,
            ServiceName = booking.Service?.Name ?? "Service",
            Date = booking.TimeSlot?.Date ?? EgyptTime.Now(_timeProvider).Date,
            StartTime = booking.TimeSlot?.StartTime ?? TimeSpan.Zero,
            EndTime = booking.TimeSlot?.EndTime ?? TimeSpan.FromHours(2),
            VisitAddress = booking.VisitAddress,
            PaymentMethod = booking.Payment?.Method ?? PaymentMethod.CashOnArrival,
            PaymentStatus = booking.Payment?.Status ?? PaymentStatus.Pending,
            FormattedWindow = booking.TimeSlot != null ? SlotFormatting.FormatWindow(booking.TimeSlot.StartTime, booking.TimeSlot.EndTime) : string.Empty,
            DayLabel = booking.TimeSlot != null ? SlotFormatting.FormatDayLabel(booking.TimeSlot.Date, egyptNow) : string.Empty
        };
    }

    public async Task<BookingPaymentSummaryDto?> GetBookingPaymentSummaryAsync(
        int bookingId,
        int patientId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Provider)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null || booking.PatientId != patientId) return null;

        var egyptNow = EgyptTime.Now(_timeProvider);
        var providerConfig = _config["Payment:Provider"] ?? "Fake";
        var isFake = providerConfig.Equals("Fake", StringComparison.OrdinalIgnoreCase) ||
                     providerConfig.Equals("FakeVisa", StringComparison.OrdinalIgnoreCase);

        return new BookingPaymentSummaryDto
        {
            BookingId = booking.Id,
            PatientId = booking.PatientId,
            ProviderFullName = booking.Provider?.FullName ?? "Healthcare Provider",
            ProviderSpecialty = booking.Provider?.Specialty,
            ServiceName = booking.Service?.Name ?? "Service",
            Date = booking.TimeSlot?.Date ?? EgyptTime.Now(_timeProvider).Date,
            StartTime = booking.TimeSlot?.StartTime ?? TimeSpan.Zero,
            EndTime = booking.TimeSlot?.EndTime ?? TimeSpan.FromHours(2),
            TotalPrice = booking.TotalPrice,
            ServiceFee = booking.ServiceFee,
            VisitAddress = booking.VisitAddress,
            PaymentStatus = booking.Payment?.Status ?? PaymentStatus.Pending,
            IsFakeGatewayActive = isFake,
            FormattedWindow = booking.TimeSlot != null ? SlotFormatting.FormatWindow(booking.TimeSlot.StartTime, booking.TimeSlot.EndTime) : string.Empty,
            DayLabel = booking.TimeSlot != null ? SlotFormatting.FormatDayLabel(booking.TimeSlot.Date, egyptNow) : string.Empty
        };
    }

    public async Task<ServiceResult<bool>> PayWithCardAsync(
        int patientId,
        int bookingId,
        CardInput card,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Payment)
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null || booking.PatientId != patientId)
        {
            return ServiceResult<bool>.Fail("Booking not found or access denied.");
        }

        if (booking.Status != BookingStatus.PendingPayment)
        {
            return ServiceResult<bool>.Fail("This booking is not currently awaiting payment.");
        }

        var expiryMinutes = _config.GetValue<int>("Booking:PendingPaymentExpiryMinutes", BookingRules.DefaultPendingPaymentExpiryMinutes);
        if (DateTime.UtcNow - booking.CreatedAt > TimeSpan.FromMinutes(expiryMinutes))
        {
            booking.Expire(DateTime.UtcNow);
            if (booking.TimeSlot != null)
            {
                booking.TimeSlot.Status = TimeSlotStatus.Available;
            }
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<bool>.Fail("This payment session has expired. The time slot has been released.");
        }

        var gatewayResult = await _gateway.AuthorizeAsync(booking.TotalPrice, booking.Id, card);
        if (!gatewayResult.Succeeded)
        {
            return ServiceResult<bool>.Fail(gatewayResult.FailureReason ?? "Payment authorization failed. Please check your card information and try again.");
        }

        if (booking.Payment == null)
        {
            booking.Payment = new Payment
            {
                BookingId = booking.Id,
                Method = PaymentMethod.OnlinePayment,
                Status = PaymentStatus.Pending,
                Amount = booking.TotalPrice,
                RefundedAmount = 0m
            };
            _db.Payments.Add(booking.Payment);
        }

        booking.Payment.Authorize();
        booking.Payment.GatewayReference = gatewayResult.Reference;

        var cleaned = (card.CardNumber ?? "").Replace(" ", "").Replace("-", "").Trim();
        booking.Payment.CardLast4 = cleaned.Length >= 4 ? cleaned[^4..] : "4242";
        booking.Payment.CardBrand = cleaned.StartsWith("4") ? "Visa" : "Mastercard";

        booking.MarkPaymentAuthorized();

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    // --- Backward compatibility methods for tests & webhooks ---
    public async Task<Payment?> GetPaymentByReferenceAsync(string gatewayReference)
    {
        return await _db.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.GatewayReference == gatewayReference);
    }

    public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
    {
        return await _db.Payments.FindAsync(paymentId);
    }

    public async Task<Payment> CreateOrGetOnlinePaymentAsync(int bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null) throw new InvalidOperationException("Booking not found");

        if (booking.Payment == null)
        {
            var payment = new Payment
            {
                BookingId = booking.Id,
                Method = PaymentMethod.OnlinePayment,
                Status = PaymentStatus.Pending,
                Amount = booking.TotalPrice,
                RefundedAmount = 0m
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            return payment;
        }

        booking.Payment.Method = PaymentMethod.OnlinePayment;
        await _db.SaveChangesAsync();
        return booking.Payment;
    }

    public async Task<bool> AuthorizePaymentAsync(string gatewayReference, string cardBrand, string cardLast4)
    {
        var payment = await _db.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.GatewayReference == gatewayReference);

        if (payment == null) return false;

        if (payment.Status == PaymentStatus.Pending)
        {
            payment.Authorize();
            payment.CardBrand = cardBrand;
            payment.CardLast4 = cardLast4;

            if (payment.Booking != null && payment.Booking.Status == BookingStatus.PendingPayment)
            {
                payment.Booking.MarkPaymentAuthorized();
            }

            await _db.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> CaptureAsync(int paymentId)
    {
        var payment = await _db.Payments.FindAsync(paymentId);
        if (payment == null) return false;
        payment.Capture();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReleaseAsync(int paymentId)
    {
        var payment = await _db.Payments.FindAsync(paymentId);
        if (payment == null) return false;
        payment.Release();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RefundAsync(int paymentId, decimal amount)
    {
        var payment = await _db.Payments.FindAsync(paymentId);
        if (payment == null) return false;
        payment.Refund(amount);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateGatewayReferenceAsync(int paymentId, string gatewayReference)
    {
        var payment = await _db.Payments.FindAsync(paymentId);
        if (payment == null) return false;
        payment.GatewayReference = gatewayReference;
        await _db.SaveChangesAsync();
        return true;
    }
}
