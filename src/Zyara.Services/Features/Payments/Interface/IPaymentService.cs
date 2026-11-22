using System.Threading;
using System.Threading.Tasks;
using Zyara.Domain.Entities;
using Zyara.Services.Common;
using Zyara.Services.Features.Payments.Dtos;

namespace Zyara.Services.Features.Payments.Interface;

public interface IPaymentService
{
    Task<BookingConfirmationDto?> GetBookingConfirmationAsync(int bookingId, CancellationToken cancellationToken = default);
    Task<BookingPaymentSummaryDto?> GetBookingPaymentSummaryAsync(int bookingId, int patientId, CancellationToken cancellationToken = default);
    Task<ServiceResult<bool>> PayWithCardAsync(int patientId, int bookingId, CardInput card, CancellationToken cancellationToken = default);

    // Backward compatibility methods for tests and webhooks
    Task<Payment?> GetPaymentByReferenceAsync(string gatewayReference);
    Task<Payment?> GetPaymentByIdAsync(int paymentId);
    Task<Payment> CreateOrGetOnlinePaymentAsync(int bookingId);
    Task<bool> AuthorizePaymentAsync(string gatewayReference, string cardBrand, string cardLast4);
    Task<bool> CaptureAsync(int paymentId);
    Task<bool> ReleaseAsync(int paymentId);
    Task<bool> RefundAsync(int paymentId, decimal amount);
    Task<bool> UpdateGatewayReferenceAsync(int paymentId, string gatewayReference);
}