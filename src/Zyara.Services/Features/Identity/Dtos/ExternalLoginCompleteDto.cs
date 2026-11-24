using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Identity.Dtos;

/// <summary>
/// Shown after the first Google sign-in to finish creating a Patient account.
/// Email is NEVER trusted from the form: the controller overwrites it with the verified Google email.
/// Rules mirror RegisterPatientDto so Google patients end up with the same data as normal patients.
/// </summary>
public class ExternalLoginCompleteDto : IValidatableObject
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^01[0125]\d{8}$", ErrorMessage = "Enter a valid Egyptian mobile number (e.g. 01012345678).")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gender is required.")]
    public Gender? Gender { get; set; }

    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    public string? ReturnUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth is { } dob)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dob > today || dob.Year < 1900)
            {
                yield return new ValidationResult("Enter a valid date of birth.", new[] { nameof(DateOfBirth) });
            }
        }
    }
}
