using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Administration.Dtos
{
    public class AdminBookingDto
    {
        public int Id { get; init; }
        public string PatientName { get; init; } = string.Empty;
        public string ProviderName { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public DateTime Date { get; init; }
        public TimeSpan StartTime { get; init; }
        public decimal TotalPrice { get; init; }
        public BookingStatus Status { get; init; }
        public PaymentStatus? PaymentStatus { get; init; }
    }
}