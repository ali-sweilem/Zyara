using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Patients.Dtos;

namespace Zyara.Services.Features.Patients;

public sealed class PatientPortalService(
    IRepository<Patient> patients,
    IRepository<Booking> bookings,
    IRepository<Address> addresses,
    IRepository<Review> reviews,
    IUnitOfWork unitOfWork) : IPatientPortalService
{
    public async Task<PatientDashboardDto?> GetDashboardAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var patient = await patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null) return null;

        var patientBookings = await bookings.Query()
            .AsNoTracking()
            .Include(b => b.Provider)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Where(b => b.PatientId == patientId)
            .OrderByDescending(b => b.TimeSlot!.Date)
            .Take(20)
            .Select(b => new PatientBookingDto
            {
                Id = b.Id,
                ProviderName = b.Provider!.FullName,
                ServiceName = b.Service!.Name,
                Status = b.Status,
                Date = DateOnly.FromDateTime(b.TimeSlot!.Date),
                StartTime = b.TimeSlot.StartTime
            })
            .ToListAsync(cancellationToken);

        var patientAddresses = await addresses.Query()
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.Id)
            .Select(a => new PatientAddressDto
            {
                Id = a.Id,
                Label = a.Label,
                FullAddress = a.StreetAndBuilding + ", " + a.District + ", " + a.City
            })
            .ToListAsync(cancellationToken);

        return new PatientDashboardDto
        {
            PatientName = patient.FullName,
            Bookings = patientBookings,
            Addresses = patientAddresses
        };
    }

    public async Task<ServiceResult<bool>> CancelBookingAsync(int patientId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await bookings.Query()
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.PatientId == patientId, cancellationToken);

        if (booking is null) return ServiceResult<bool>.Fail("Booking not found.");
        if (booking.Status is not (BookingStatus.PendingProviderApproval or BookingStatus.Confirmed))
            return ServiceResult<bool>.Fail("This booking cannot be cancelled at its current status.");

        booking.Status = BookingStatus.Cancelled;
        if (booking.TimeSlot is not null) booking.TimeSlot.Status = TimeSlotStatus.Available;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> AddReviewAsync(int patientId, int bookingId, int stars, string? comment, CancellationToken cancellationToken = default)
    {
        if (stars is < 1 or > 5) return ServiceResult<bool>.Fail("Rating must be between 1 and 5 stars.");

        var booking = await bookings.Query()
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.PatientId == patientId, cancellationToken);
        if (booking is null || booking.Status != BookingStatus.Completed)
            return ServiceResult<bool>.Fail("Only completed visits can be reviewed.");
        if (booking.Review is not null) return ServiceResult<bool>.Fail("This visit has already been reviewed.");

        reviews.Add(new Review { BookingId = bookingId, Stars = stars, Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim() });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }
}
