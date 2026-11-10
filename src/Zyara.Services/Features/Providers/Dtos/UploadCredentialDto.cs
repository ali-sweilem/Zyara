using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Zyara.Services.Features.Providers.Dtos
{
    public class UploadCredentialDto
    {
        [Required(ErrorMessage = "Document type is required")]
        [StringLength(100, ErrorMessage = "Document type cannot exceed 100 characters")]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a file")]
        public IFormFile? File { get; set; }
    }
}