namespace Zyara.Web.Areas.Provider.ViewModels;

public class BookingDetailsViewModel
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ServiceFee { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Commission { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string Address { get; set; } = "N/A";
    public int? RatingStars { get; set; }
    public string? RatingComment { get; set; }
    public List<ServiceNoteViewModel> ServiceNotes { get; set; } = new();
    public bool CanAccept { get; set; }
    public bool CanReject { get; set; }
    public bool CanStart { get; set; }
    public bool CanComplete { get; set; }
    public bool CanCancel { get; set; }
    public bool CanAddNote { get; set; }
    public bool IsHomeVisit { get; set; }
    public string? OtpCode { get; set; } // not shown to provider per spec
}
