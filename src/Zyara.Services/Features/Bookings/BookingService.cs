using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;

namespace Zyara.Services.Features.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly ZyaraDbContext _db;

    public BookingService(ZyaraDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Succeeded, int? BookingId, string? Error)> CreateBookingAsync(
        int patientId, CreateBookingDto dto, CancellationToken cancellationToken = default)
    {
        var slot = await _db.TimeSlots
            .FirstOrDefaultAsync(s => s.Id == dto.SlotId && s.ProviderId == dto.ProviderId, cancellationToken);

        if (slot is null || slot.Status != TimeSlotStatus.Available)
        {
            return (false, null, "This time slot is no longer available.");
        }

        var offer = await _db.ProviderServices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderId == dto.ProviderId && p.ServiceId == dto.ServiceId, cancellationToken);

        if (offer is null)
        {
            return (false, null, "This provider does not offer the selected service.");
        }
        var patient = await _db.Patients
                         .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null)
        {
            return (false, null, "Patient not found.");
        }

        patient.FullName = dto.PatientName;
        patient.Phone = dto.PatientPhone;

        var settings = await _db.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? new PlatformSettings();

        var price = offer.Fee;

        var booking = new Booking
        {
            PatientId = patientId,
            ProviderId = dto.ProviderId,
            ServiceId = dto.ServiceId,
            TimeSlotId = slot.Id,
            Status = BookingStatus.PendingProviderApproval,
            ServiceFee = settings.ServiceFee,
            Commission = Math.Round(price * settings.CommissionPercentage / 100m, 2),
            TotalPrice = price + settings.ServiceFee
        };

        var address = new Address
        {
            PatientId = patientId,
            Label = dto.AddressLabel,
            City = dto.City ?? string.Empty,
            District = dto.District,
            StreetAndBuilding = dto.Street ?? string.Empty
        };
        _db.Addresses.Add(address);

        slot.Status = TimeSlotStatus.Booked;
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, booking.Id, null);
    }
}
