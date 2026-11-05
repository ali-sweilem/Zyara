namespace Zyara.Web.Areas.Provider.ViewModels;

public class ServiceNoteViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
