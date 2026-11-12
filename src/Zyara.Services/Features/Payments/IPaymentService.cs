using System.Threading.Tasks;
using Zyara.Services.Features.Payments.Dtos;

namespace Zyara.Services.Features.Payments
{
    public interface IPaymentService
    {
        Task<PaymentDto?> GetPaymentDetailsAsync(int bookingId);
        Task<bool> AddPaymentAsync(int bookingId);
        Task<string?> GenerateOtpAsync(int bookingId);
        Task<BookingConfirmationDto?> GetBookingConfirmationAsync(int bookingId);
    }
}