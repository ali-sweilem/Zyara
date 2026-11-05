using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Bookings;

public class BookingFilter
{
    public BookingStatus? Status { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public ServiceType? ServiceType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
