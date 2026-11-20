using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.MedicalRecords.Dtos;

/// <summary>One medication row. Everything is optional here: blank rows are ignored and required fields are checked in the service.</summary>
public sealed class PrescriptionItemInput
{
    [StringLength(150)] public string? MedicationName { get; set; }
    [StringLength(100)] public string? Dosage { get; set; }
    [StringLength(100)] public string? Frequency { get; set; }
    [StringLength(100)] public string? Duration { get; set; }
    [StringLength(500)] public string? Instructions { get; set; }
}

public sealed class SavePrescriptionDto
{
    public int BookingId { get; set; }
    public List<PrescriptionItemInput> Items { get; set; } = new();
}

public sealed class SaveServiceNoteDto
{
    public int BookingId { get; set; }

    [Required(ErrorMessage = "Write a note first.")]
    [StringLength(2000, ErrorMessage = "The note is too long (2000 characters max).")]
    public string? Content { get; set; }
}

/// <summary>What the provider sees/edits for one booking.</summary>
public sealed class VisitRecordDto
{
    public int BookingId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public BookingStatus Status { get; set; }
    public bool CanPrescribe { get; set; }          // doctors only
    public DateTime? PrescriptionIssuedAt { get; set; }
    public List<PrescriptionItemInput> Items { get; set; } = new();
    public string? Note { get; set; }
}

/// <summary>Read-only view for the patient who owns the booking.</summary>
public sealed class PatientVisitRecordDto
{
    public int BookingId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public DateTime? PrescriptionIssuedAt { get; set; }
    public List<PrescriptionItemInput> Items { get; set; } = new();
    public string? Note { get; set; }
}
