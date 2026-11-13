using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Zyara.Services.Features.Payments.Dtos;

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
                    Method = PaymentMethod.CashOnArrival, // 0 in SQL
                    Status = PaymentStatus.Pending,       // 0 in SQL
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
    }
}