using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Patients.Dtos;

public sealed class PatientAddressItemDto
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string StreetAndBuilding { get; init; } = string.Empty;
}

/// <summary>Everything the profile page shows (the editable values are the first block).</summary>
public sealed class PatientProfileDto
{
    // editable
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public Gender Gender { get; init; }

    // read-only
    public string? Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public string? PhotoUrl { get; init; }
    public DateTime MemberSince { get; init; }          // UTC
    public bool TwoFactorEnabled { get; init; }
    public bool HasPassword { get; init; }              // false for accounts created with Google

    public int TotalBookings { get; init; }
    public int CompletedVisits { get; init; }
    public int ReviewsWritten { get; init; }

    public IReadOnlyList<PatientAddressItemDto> Addresses { get; init; } = Array.Empty<PatientAddressItemDto>();
    public int MaxAddresses { get; init; }
}

public sealed class EditPatientProfileDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Please choose your gender.")]
    public Gender? Gender { get; set; }
}

public sealed class AddAddressDto
{
    [Required(ErrorMessage = "Give the address a name (e.g. Home).")]
    [StringLength(50, ErrorMessage = "The address name cannot exceed 50 characters.")]
    public string Label { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(60, ErrorMessage = "City cannot exceed 60 characters.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "District is required.")]
    [StringLength(60, ErrorMessage = "District cannot exceed 60 characters.")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Street and building are required.")]
    [StringLength(200, ErrorMessage = "Street and building cannot exceed 200 characters.")]
    public string StreetAndBuilding { get; set; } = string.Empty;
}

public sealed class ChangePasswordDto
{
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the new password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The new password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm the new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and its confirmation do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
