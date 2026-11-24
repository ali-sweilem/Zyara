using System.Threading.Tasks;
using Zyara.Services.Features.Payments.Dtos;

namespace Zyara.Services.Features.Payments.Interface
{
    public interface IPaymentService
    {
        Task<PaymentDto?> GetPaymentDetailsAsync(int bookingId);
        Task<bool> AddPaymentAsync(int bookingId);
        Task<string?> GenerateOtpAsync(int bookingId);
        Task<BookingConfirmationDto?> GetBookingConfirmationAsync(int bookingId);

        // Online (card) payments only. Not wired to accept/reject yet; each returns false if the payment
        // is not an online payment or is in the wrong state.
        /// <summary>Authorized -> Captured (the visit happened, the money is ours to settle).</summary>
        Task<bool> CaptureAsync(int bookingId);

        /// <summary>Authorized -> Released (rejected / expired / cancelled before the visit).</summary>
        Task<bool> ReleaseAsync(int bookingId);

        /// <summary>Captured -> Refunded / PartiallyRefunded.</summary>
        Task<bool> RefundAsync(int bookingId, decimal amount);
    }
}
