using System;
using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels;

public class ProviderProfileViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsDoctor => Role == UserRole.Doctor;
    public string Specialty { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string BaseCity { get; set; } = string.Empty;
    public VerificationStatus VerificationStatus { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public decimal WalletBalance { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }

    public List<CredentialItemViewModel> Credentials { get; set; } = new();
}

public class CredentialItemViewModel
{
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsVerified { get; set; }
}

