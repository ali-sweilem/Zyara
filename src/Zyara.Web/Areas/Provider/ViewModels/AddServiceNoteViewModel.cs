using System.ComponentModel.DataAnnotations;

namespace Zyara.Web.Areas.Provider.ViewModels;

public class AddServiceNoteViewModel
{
    [Required(ErrorMessage = "Note content is required.")]
    [MaxLength(2000, ErrorMessage = "Note too long.")]
    public string Content { get; set; } = string.Empty;

    public int BookingId { get; set; }
}
