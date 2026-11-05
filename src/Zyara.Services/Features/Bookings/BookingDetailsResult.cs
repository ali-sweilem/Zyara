using Zyara.Domain.Entities;

namespace Zyara.Services.Features.Bookings;

public class BookingDetailsResult
{
    public Booking Booking { get; set; } = null!;
    public IReadOnlyList<ServiceNote> ServiceNotes { get; set; } = new List<ServiceNote>();
}
