using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Patients.Dtos;
using Zyara.Services.Features.Shared;

namespace Zyara.Services.Features.Patients;

/// <summary>The patient's bookings page. Cancelling lives in BookingCancellationService (it moves money).</summary>
public sealed class PatientPortalService(
    IRepository<Patient> patients,
    IRepository<Booking> bookings,
    IRepository<Address> addresses,
    TimeProvider timeProvider) : IPatientPortalService
{
    public async Task<PatientDashboardDto?> GetDashboardAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var patient = await patients.Query()
            .AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new { p.FullName, p.OutstandingPenalty })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null) return null;

        var rows = await bookings.Query()
            .AsNoTracking()
            .Where(b => b.PatientId == patientId)
            .OrderByDescending(b => b.TimeSlot!.Date)
            .Take(20)
            .Select(b => new
            {
                b.Id,
                ProviderName = b.Provider!.FullName,
                ServiceName = b.Service!.Name,
                b.Status,
                HasReview = b.Review != null,
                SlotDate = b.TimeSlot!.Date,
                b.TimeSlot.StartTime,
                b.Commission
            })
            .ToListAsync(cancellationToken);

        // what cancelling would cost right now: only a CONFIRMED booking, only in the last hour before the visit
        var now = EgyptTime.Now(timeProvider);
        var patientBookings = rows
            .Select(r => new PatientBookingDto
            {
                Id = r.Id,
                ProviderName = r.ProviderName,
                ServiceName = r.ServiceName,
                Status = r.Status,
                HasReview = r.HasReview,
                Date = DateOnly.FromDateTime(r.SlotDate),
                StartTime = r.StartTime,
                CancelPenalty = (r.Status is BookingStatus.Confirmed or BookingStatus.ConfirmedCollectionPending)
                                && CancellationRules.IsLatePatientCancel(r.SlotDate.Date + r.StartTime, now)
                    ? r.Commission
                    : 0m
            })
            .ToList();

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
            OutstandingPenalty = patient.OutstandingPenalty,
            Bookings = patientBookings,
            Addresses = patientAddresses
        };
    }
}
