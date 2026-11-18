using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Identity.Dtos;

public class ResetPasswordDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

