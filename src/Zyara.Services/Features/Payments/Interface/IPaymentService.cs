using System.Threading.Tasks;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Payments.Dtos;

namespace Zyara.Services.Features.Payments.Interface
{
    public interface IPaymentService
    {
        Task<PaymentDto?> GetPaymentDetailsAsync(int bookingId);
        Task<bool> AddPaymentAsync(int bookingId);
        Task<string?> GenerateOtpAsync(int bookingId);
        Task<BookingConfirmationDto?> GetBookingConfirmationAsync(int bookingId);

        // --- Visa & Gateway Methods ---
        Task<Payment?> GetPaymentByReferenceAsync(string gatewayReference);
        Task<Payment?> GetPaymentByIdAsync(int paymentId);
        Task<Payment> CreateOrGetOnlinePaymentAsync(int bookingId);
        Task<bool> AuthorizePaymentAsync(string gatewayReference, string cardBrand, string cardLast4);
        Task<bool> CaptureAsync(int paymentId);
        Task<bool> ReleaseAsync(int paymentId);
        Task<bool> RefundAsync(int paymentId, decimal amount);
        Task<bool> UpdateGatewayReferenceAsync(int paymentId, string gatewayReference);
    }
}