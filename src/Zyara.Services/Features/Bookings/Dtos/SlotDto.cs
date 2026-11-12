namespace Zyara.Services.Features.Bookings.Dtos;

public sealed class SlotDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
