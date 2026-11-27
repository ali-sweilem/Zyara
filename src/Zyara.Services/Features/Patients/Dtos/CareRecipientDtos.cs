using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Patients.Dtos;

public sealed class CareRecipientDto
{
    public int Id { get; init; }
    public int PatientId { get; init; }
    public int Slot { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public Relationship Relationship { get; init; }
    public BloodType? BloodType { get; init; }
    public string? ChronicConditions { get; init; }
    public string? Allergies { get; init; }
    public string? CurrentMedications { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }

    public bool IsSelf => Relationship == Relationship.Self;

    public int? Age
    {
        get
        {
            if (!DateOfBirth.HasValue) return null;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value > today.AddYears(-age)) age--;
            return age;
        }
    }

    public string BloodTypeDisplay => BloodType?.ToDisplayString() ?? "Unknown";
}

public sealed class AddCareRecipientDto
{
    /// <summary>Step 1: Is this patient the account owner?</summary>
    public bool IsAccountOwner { get; set; }

    public Relationship? Relationship { get; set; }

    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string? FullName { get; set; }

    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    [Required(ErrorMessage = "Please choose a blood type.")]
    public BloodType? BloodType { get; set; }

    [Required(ErrorMessage = "Chronic conditions are required. Enter 'None' if not applicable.")]
    [StringLength(500, ErrorMessage = "Chronic conditions cannot exceed 500 characters.")]
    public string ChronicConditions { get; set; } = string.Empty;

    [Required(ErrorMessage = "Allergies are required. Enter 'None' if not applicable.")]
    [StringLength(500, ErrorMessage = "Allergies cannot exceed 500 characters.")]
    public string Allergies { get; set; } = string.Empty;

    [Required(ErrorMessage = "Current medications are required. Enter 'None' if not applicable.")]
    [StringLength(500, ErrorMessage = "Current medications cannot exceed 500 characters.")]
    public string CurrentMedications { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}

public sealed class EditCareRecipientDto
{
    public int Id { get; set; }

    public Relationship? Relationship { get; set; }

    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string? FullName { get; set; }

    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public Gender? Gender { get; set; }

    [Required(ErrorMessage = "Please choose a blood type.")]
    public BloodType? BloodType { get; set; }

    [Required(ErrorMessage = "Chronic conditions are required. Enter 'None' if not applicable.")]
    [StringLength(500, ErrorMessage = "Chronic conditions cannot exceed 500 characters.")]
    public string ChronicConditions { get; set; } = string.Empty;

    [Required(ErrorMessage = "Allergies are required. Enter 'None' if not applicable.")]
    [StringLength(500, ErrorMessage = "Allergies cannot exceed 500 characters.")]
    public string Allergies { get; set; } = string.Empty;

    [Required(ErrorMessage = "Current medications are required. Enter 'None' if not applicable.")]
    [StringLength(500, ErrorMessage = "Current medications cannot exceed 500 characters.")]
    public string CurrentMedications { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}

