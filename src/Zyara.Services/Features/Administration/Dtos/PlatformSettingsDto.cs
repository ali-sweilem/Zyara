using System.ComponentModel.DataAnnotations;

namespace Zyara.Services.Features.Administration.Dtos
{
    public class PlatformSettingsDto
    {
        [Display(Name = "Service fee (% of the visit price)")]
        [Range(0, 100, ErrorMessage = "Service fee must be between 0 and 100.")]
        public decimal ServiceFeePercentage { get; set; }

        [Display(Name = "Cancellation commission (% of the visit price)")]
        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100.")]
        public decimal CommissionPercentage { get; set; }
    }
}
