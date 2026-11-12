using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Payments.Dtos
{
    public class BookingConfirmationDto
    {
        public int BookingId { get; set; }
        public BookingStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public string? OtpCode { get; set; }
        public bool IsHomeVisit { get; set; }
    }
}