using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Administration.Dtos
{
    public class PlatformSettingsDto
    {
        [Display(Name = "Service fee (EGP)")]
        [Range(0, 1_000_000, ErrorMessage = "Service fee must be 0 or more.")]
        public decimal ServiceFee { get; set; }

        [Display(Name = "Commission (%)")]
        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100.")]
        public decimal CommissionPercentage { get; set; }
    }
}
