using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments
{
    public class PaymentService : IPaymentService
    {
        private readonly ZyaraDbContext _db;

        public PaymentService(ZyaraDbContext db)
        {
            _db = db;
        }

        public async Task<PaymentDto?> GetPaymentDetailsAsync(int bookingId)
        {
            var booking = await _db.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return null;

            return new PaymentDto
            {
                BookingId = booking.Id,
                TotalPrice = booking.TotalPrice,
                Method = "Cash on Arrival"
            };
        }

        public async Task<bool> AddPaymentAsync(int bookingId)
        {
            var booking = await _db.Bookings
                .Include(b => b.Payment)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return false;

            // 1. إنشاء سجل الدفع إن لم يكن موجوداً
            if (booking.Payment == null)
            {
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Method = PaymentMethod.CashOnArrival,
                    Status = PaymentStatus.Pending,
                    Amount = booking.TotalPrice,
                    RefundedAmount = 0m
                };

                _db.Payments.Add(payment);
            }

            // 2. توليد OTP إذا كانت الخدمة زيارة منزلية
            bool isHomeVisit = booking.Service != null &&
                               booking.Service.Type != ServiceType.RemoteConsultation;

            if (isHomeVisit && string.IsNullOrEmpty(booking.OtpCode))
            {
                booking.OtpCode = Random.Shared.Next(1000, 10000).ToString();
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<string?> GenerateOtpAsync(int bookingId)
        {
            var booking = await _db.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return null;

            bool isHomeVisit = booking.Service != null &&
                               booking.Service.Type != ServiceType.RemoteConsultation;

            if (isHomeVisit)
            {
                string otp = Random.Shared.Next(1000, 10000).ToString();
                booking.OtpCode = otp;
                await _db.SaveChangesAsync();
                return otp;
            }

            booking.OtpCode = string.Empty;
            await _db.SaveChangesAsync();
            return null;
        }

        public async Task<BookingConfirmationDto?> GetBookingConfirmationAsync(int bookingId)
        {
            var booking = await _db.Bookings
                .Include(b => b.Service)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return null;

            bool isHomeVisit = booking.Service != null &&
                               booking.Service.Type != ServiceType.RemoteConsultation;

            return new BookingConfirmationDto
            {
                BookingId = booking.Id,
                Status = booking.Status,
                TotalPrice = booking.TotalPrice,
                OtpCode = booking.OtpCode,
                IsHomeVisit = isHomeVisit
            };
        }

        // ==========================================
        //         VISA & GATEWAY LOGIC
        // ==========================================

        public async Task<Payment?> GetPaymentByReferenceAsync(string gatewayReference)
        {
            if (string.IsNullOrWhiteSpace(gatewayReference)) return null;

            return await _db.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.GatewayReference == gatewayReference);
        }

        public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
        {
            return await _db.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }

        public async Task<Payment> CreateOrGetOnlinePaymentAsync(int bookingId)
        {
            var booking = await _db.Bookings
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId)
                ?? throw new InvalidOperationException($"Booking #{bookingId} not found.");

            if (booking.Payment != null)
            {
                booking.Payment.Method = PaymentMethod.OnlinePayment;
                if (booking.Payment.Status != PaymentStatus.Authorized && booking.Payment.Status != PaymentStatus.Captured)
                {
                    booking.Payment.Status = PaymentStatus.Pending;
                }
                booking.Payment.Amount = booking.TotalPrice;
                await _db.SaveChangesAsync();
                return booking.Payment;
            }

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

        public async Task<bool> AuthorizePaymentAsync(string gatewayReference, string cardBrand, string cardLast4)
        {
            var payment = await GetPaymentByReferenceAsync(gatewayReference);
            if (payment == null) return false;

            // Idempotent: إذا كانت العملية معتمدة بالفعل، نرجع true دون أي تكرار
            if (payment.Status == PaymentStatus.Authorized || payment.Status == PaymentStatus.Captured)
            {
                return true;
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return false;
            }

            payment.CardBrand = cardBrand;
            payment.CardLast4 = cardLast4;
            payment.Authorize();

            await _db.SaveChangesAsync();
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
}