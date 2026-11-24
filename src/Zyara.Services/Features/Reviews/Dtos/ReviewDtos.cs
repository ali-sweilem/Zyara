using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Reviews.Dtos;

/// <summary>A completed visit the patient can talk about.</summary>
public class ReviewableVisitDto
{
    public int BookingId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderPhotoUrl { get; set; }
    public string ProviderSpecialty { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

/// <summary>A visit the patient already reviewed.</summary>
public sealed class MyReviewDto : ReviewableVisitDto
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewedAt { get; set; }   // UTC
}

/// <summary>"My reviews" page: visits waiting for a review + the reviews already written.</summary>
public sealed class PatientReviewsPageDto
{
    public List<ReviewableVisitDto> Pending { get; set; } = new();
    public List<MyReviewDto> Reviewed { get; set; } = new();
}

/// <summary>The "Rate your visit" page (the visit summary + what the patient typed so far).</summary>
public sealed class ReviewFormDto : ReviewableVisitDto
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
}

/// <summary>What the patient posts. The patient id is NEVER posted: it comes from ICurrentUser.</summary>
public sealed class CreateReviewDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Visit not found.")]
    public int BookingId { get; set; }

    [Range(1, 5, ErrorMessage = "Please choose a rating from 1 to 5 stars.")]
    public int Stars { get; set; }

    [StringLength(1000, ErrorMessage = "The comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }
}
