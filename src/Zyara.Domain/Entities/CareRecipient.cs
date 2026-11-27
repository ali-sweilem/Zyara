using System;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities;

public class CareRecipient
{
    public int Id { get; set; }

    /// <summary>FK to the account holder (Patient).</summary>
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>Slot number between 1 and 3 on the account.</summary>
    public int Slot { get; set; }

    /// <summary>Soft delete flag.</summary>
    public bool IsArchived { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public Relationship Relationship { get; set; }
    public BloodType? BloodType { get; set; }

    public string? ChronicConditions { get; set; }
    public string? Allergies { get; set; }
    public string? CurrentMedications { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

