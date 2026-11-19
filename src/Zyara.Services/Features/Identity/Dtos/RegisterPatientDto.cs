using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

public class RegisterPatientDto : RegisterBaseDto, IValidatableObject
{
    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth is { } dob)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dob > today || dob.Year < 1900)
            {
                yield return new ValidationResult(
                    "Enter a valid date of birth.", new[] { nameof(DateOfBirth) });
            }
        }
    }
}
