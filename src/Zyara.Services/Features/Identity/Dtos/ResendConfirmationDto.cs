using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

public class ResendConfirmationDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
