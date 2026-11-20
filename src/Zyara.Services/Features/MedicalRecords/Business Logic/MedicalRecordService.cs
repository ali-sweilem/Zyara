using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.MedicalRecords.Dtos;
using Zyara.Services.Features.MedicalRecords.Interfaces;
using Zyara.Services.Features.Notifications.Interfaces;

namespace Zyara.Services.Features.MedicalRecords;

public sealed class MedicalRecordService : IMedicalRecordService
{
    private const int MaxItems = 20;

    private readonly IRepository<Booking> _bookings;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly IRepository<ServiceNote> _notes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notifications;
    private readonly TimeProvider _timeProvider;

    public MedicalRecordService(
        IRepository<Booking> bookings,
        IRepository<Prescription> prescriptions,
        IRepository<ServiceNote> notes,
        IUnitOfWork unitOfWork,
        INotificationService notifications,
        TimeProvider timeProvider)
    {
        _bookings = bookings;
        _prescriptions = prescriptions;
        _notes = notes;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _timeProvider = timeProvider;
    }

    // ───────────────────────── provider side ─────────────────────────

    public async Task<ServiceResult<VisitRecordDto>> GetForProviderAsync(int providerId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await LoadProviderBookingAsync(providerId, bookingId, cancellationToken);
        if (booking is null) return ServiceResult<VisitRecordDto>.Fail("Booking not found.");

        if (!IsRecordable(booking))
            return ServiceResult<VisitRecordDto>.Fail("You can write the visit record once the visit has started.");

        var prescription = await LoadPrescriptionAsync(bookingId, cancellationToken);
        var note = await LoadNoteAsync(bookingId, cancellationToken);

        return ServiceResult<VisitRecordDto>.Ok(new VisitRecordDto
        {
            BookingId = booking.Id,
            PatientName = booking.Patient!.FullName,
            ServiceName = booking.Service!.Name,
            Date = booking.TimeSlot!.Date,
            StartTime = booking.TimeSlot.StartTime,
            Status = booking.Status,
            CanPrescribe = booking.Provider is Doctor,
            PrescriptionIssuedAt = prescription?.IssuedAt,
            Items = ToInputs(prescription),
            Note = note?.Content
        });
    }

    public async Task<ServiceResult> SavePrescriptionAsync(int providerId, SavePrescriptionDto dto, CancellationToken cancellationToken = default)
    {
        var booking = await LoadProviderBookingAsync(providerId, dto.BookingId, cancellationToken);
        if (booking is null) return ServiceResult.Fail("Booking not found.");

        if (!IsRecordable(booking))
            return ServiceResult.Fail("You can write the prescription once the visit has started.");

        if (booking.Provider is not Doctor)
            return ServiceResult.Fail("Only doctors can write prescriptions.");

        // drop completely blank rows, then validate the rest
        var rows = (dto.Items ?? new List<PrescriptionItemInput>())
            .Select(i => new PrescriptionItemInput
            {
                MedicationName = i.MedicationName?.Trim(),
                Dosage = i.Dosage?.Trim(),
                Frequency = i.Frequency?.Trim(),
                Duration = i.Duration?.Trim(),
                Instructions = i.Instructions?.Trim()
            })
            .Where(i => !string.IsNullOrEmpty(i.MedicationName) || !string.IsNullOrEmpty(i.Dosage)
                        || !string.IsNullOrEmpty(i.Frequency) || !string.IsNullOrEmpty(i.Duration)
                        || !string.IsNullOrEmpty(i.Instructions))
            .ToList();

        if (rows.Count == 0) return ServiceResult.Fail("Add at least one medication.");
        if (rows.Count > MaxItems) return ServiceResult.Fail($"A prescription can have at most {MaxItems} medications.");
        if (rows.Any(r => string.IsNullOrEmpty(r.MedicationName) || string.IsNullOrEmpty(r.Dosage)))
            return ServiceResult.Fail("Every medication needs a name and a dosage.");

        var prescription = await LoadPrescriptionAsync(dto.BookingId, cancellationToken);
        var isNew = prescription is null;

        if (prescription is null)
        {
            prescription = new Prescription
            {
                BookingId = booking.Id,
                DoctorId = booking.ProviderId,
                IssuedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            _prescriptions.Add(prescription);
        }
        else
        {
            prescription.PrescriptionItems.Clear();   // old rows are deleted (orphans), the form is the new truth
        }

        foreach (var r in rows)
        {
            prescription.PrescriptionItems.Add(new PrescriptionItem
            {
                MedicationName = r.MedicationName!,
                Dosage = r.Dosage!,
                Frequency = r.Frequency ?? string.Empty,
                Duration = r.Duration ?? string.Empty,
                Instructions = r.Instructions ?? string.Empty
            });
        }

        if (isNew)
        {
            _notifications.Notify(booking.PatientId, NotificationType.General, "Your prescription is ready",
                $"Your doctor wrote a prescription for booking #{booking.Id}.", $"/MedicalRecords/Details?bookingId={booking.Id}");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SaveNoteAsync(int providerId, SaveServiceNoteDto dto, CancellationToken cancellationToken = default)
    {
        var booking = await LoadProviderBookingAsync(providerId, dto.BookingId, cancellationToken);
        if (booking is null) return ServiceResult.Fail("Booking not found.");

        if (!IsRecordable(booking))
            return ServiceResult.Fail("You can write a note once the visit has started.");

        var content = dto.Content?.Trim();
        if (string.IsNullOrEmpty(content)) return ServiceResult.Fail("Write a note first.");
        if (content.Length > 2000) return ServiceResult.Fail("The note is too long (2000 characters max).");

        var note = await LoadNoteAsync(dto.BookingId, cancellationToken);
        if (note is null)
        {
            _notes.Add(new ServiceNote { BookingId = booking.Id, Content = content });
            _notifications.Notify(booking.PatientId, NotificationType.General, "A visit note was added",
                $"Your provider added a note to booking #{booking.Id}.", $"/MedicalRecords/Details?bookingId={booking.Id}");
        }
        else
        {
            note.Content = content;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    // ───────────────────────── patient side ─────────────────────────

    public async Task<ServiceResult<PatientVisitRecordDto>> GetForPatientAsync(int patientId, int bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookings.Query()
            .AsNoTracking()
            .Include(b => b.Provider)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.PatientId == patientId, cancellationToken);

        if (booking is null) return ServiceResult<PatientVisitRecordDto>.Fail("Booking not found.");

        var prescription = await _prescriptions.Query()
            .AsNoTracking()
            .Include(p => p.PrescriptionItems)
            .FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken);

        var note = await _notes.Query()
            .AsNoTracking()
            .Where(n => n.BookingId == bookingId)
            .OrderBy(n => n.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return ServiceResult<PatientVisitRecordDto>.Ok(new PatientVisitRecordDto
        {
            BookingId = booking.Id,
            ProviderName = booking.Provider!.FullName,
            ServiceName = booking.Service!.Name,
            Date = booking.TimeSlot!.Date,
            StartTime = booking.TimeSlot.StartTime,
            PrescriptionIssuedAt = prescription?.IssuedAt,
            Items = ToInputs(prescription),
            Note = note?.Content
        });
    }

    // ───────────────────────── helpers ─────────────────────────

    /// <summary>Only the booking's own provider gets it (anything else = null = "not found").</summary>
    private Task<Booking?> LoadProviderBookingAsync(int providerId, int bookingId, CancellationToken ct) =>
        _bookings.Query()
            .Include(b => b.Patient)
            .Include(b => b.Provider)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId, ct);

    private static bool IsRecordable(Booking booking) =>
        booking.Status is BookingStatus.Started or BookingStatus.Completed;

    private Task<Prescription?> LoadPrescriptionAsync(int bookingId, CancellationToken ct) =>
        _prescriptions.Query()
            .Include(p => p.PrescriptionItems)
            .FirstOrDefaultAsync(p => p.BookingId == bookingId, ct);

    private Task<ServiceNote?> LoadNoteAsync(int bookingId, CancellationToken ct) =>
        _notes.Query()
            .Where(n => n.BookingId == bookingId)
            .OrderBy(n => n.Id)
            .FirstOrDefaultAsync(ct);

    private static List<PrescriptionItemInput> ToInputs(Prescription? prescription) =>
        prescription is null
            ? new List<PrescriptionItemInput>()
            : prescription.PrescriptionItems
                .OrderBy(i => i.Id)
                .Select(i => new PrescriptionItemInput
                {
                    MedicationName = i.MedicationName,
                    Dosage = i.Dosage,
                    Frequency = i.Frequency,
                    Duration = i.Duration,
                    Instructions = i.Instructions
                })
                .ToList();
}
