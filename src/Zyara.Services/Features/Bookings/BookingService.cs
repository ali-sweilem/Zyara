using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Services.Common;
using Zyara.Services.Data;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Providers;

namespace Zyara.Services.Features.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly ZyaraDbContext _db;
    private readonly TimeProvider _timeProvider;

    public BookingService(ZyaraDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    // ─────────────────────────────────────────────────────────────────
    // GetAvailableSlotsAsync
    // ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<BookingSlotDto>> GetAvailableSlotsAsync(
        int providerId,
        CancellationToken cancellationToken = default)
    {
        var egyptNow = EgyptTime.Now(_timeProvider);
        var today = egyptNow.Date;
        var nowTime = egyptNow.TimeOfDay;

        return await _db.TimeSlots
            .AsNoTracking()
            .Where(ts =>
                ts.ProviderId == providerId &&
                ts.Status == TimeSlotStatus.Available &&
                // future slots only: either a future date, or today but start time hasn't passed yet
                (ts.Date > today || (ts.Date == today && ts.StartTime > nowTime)))
            .OrderBy(ts => ts.Date)
            .ThenBy(ts => ts.StartTime)
            .Select(ts => new BookingSlotDto
            {
                Id = ts.Id,
                Date = ts.Date,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime
            })
            .ToListAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────
    // CreateBookingAsync
    // ─────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<int>> CreateBookingAsync(
        BookAppointmentDto dto,
        CancellationToken cancellationToken = default)
    {
        // --- 1. Validate provider exists and is publicly visible ------------------
        var providerExists = await _db.Providers
            .AsNoTracking()
            .PubliclyVisible()
            .AnyAsync(p => p.Id == dto.ProviderId, cancellationToken);

        if (!providerExists)
        {
            return ServiceResult<int>.Fail("Provider not found or is not currently accepting bookings.");
        }

        // --- 2. Validate patient exists -------------------------------------------
        var patientExists = await _db.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == dto.PatientId, cancellationToken);

        if (!patientExists)
        {
            return ServiceResult<int>.Fail("Patient account not found. Please log in and try again.");
        }

        // --- 3. Load the chosen time slot with a tracking query (we will update it) -
        var slot = await _db.TimeSlots
            .FirstOrDefaultAsync(ts =>
                ts.Id == dto.TimeSlotId &&
                ts.ProviderId == dto.ProviderId,
                cancellationToken);

        if (slot is null)
        {
            return ServiceResult<int>.Fail("The selected time slot does not exist for this provider.");
        }

        // --- 4. Validate the slot is still in the future -------------------------
        var egyptNow = EgyptTime.Now(_timeProvider);
        var slotDateTime = slot.Date.Date + slot.StartTime;
        if (slotDateTime <= egyptNow)
        {
            return ServiceResult<int>.Fail("The selected time slot is in the past. Please choose a future slot.");
        }

        // --- 5. Validate the slot is still available (re-check inside save) ------
        if (slot.Status != TimeSlotStatus.Available)
        {
            return ServiceResult<int>.Fail("Sorry, this slot is no longer available. Please choose another slot.");
        }

        // --- 6. Validate the chosen ProviderService belongs to this provider ------
        var providerService = await _db.ProviderServices
            .AsNoTracking()
            .Include(ps => ps.Service)
            .FirstOrDefaultAsync(ps =>
                ps.Id == dto.ProviderServiceId &&
                ps.ProviderId == dto.ProviderId &&
                ps.Service != null &&
                ps.Service.IsActive,
                cancellationToken);

        if (providerService is null)
        {
            return ServiceResult<int>.Fail("The selected service is not offered by this provider.");
        }

        // --- 7. Load platform settings for commission calculation -----------------
        var settings = await _db.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            ?? new PlatformSettings(); // fallback to defaults if none seeded

        var commission = CommissionCalculator.Calculate(
            providerService.Fee,
            settings.CommissionPercentage / 100m);

        var totalPrice = providerService.Fee + settings.ServiceFee;

        // --- 8. Create the booking and mark the slot as Booked in a single SaveChanges ---
        var booking = new Booking
        {
            PatientId = dto.PatientId,
            ProviderId = dto.ProviderId,
            ServiceId = providerService.ServiceId,
            TimeSlotId = slot.Id,
            Status = BookingStatus.PendingProviderApproval,
            ServiceFee = settings.ServiceFee,
            TotalPrice = totalPrice,
            Commission = commission,
            OtpCode = string.Empty
        };

        slot.Status = TimeSlotStatus.Booked;

        _db.Bookings.Add(booking);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Unique constraint on Bookings.TimeSlotId was violated — slot was booked
            // by another patient between our availability check and our save.
            return ServiceResult<int>.Fail(
                "This slot was just taken by another patient. Please refresh and choose a different slot.");
        }

        return ServiceResult<int>.Ok(booking.Id);
    }
}

