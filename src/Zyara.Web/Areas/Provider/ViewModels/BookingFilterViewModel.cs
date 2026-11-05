namespace Zyara.Web.Areas.Provider.ViewModels;

public class BookingFilterViewModel
{
    public Domain.Enums.BookingStatus? Status { get; set; }
    public Domain.Enums.PaymentMethod? PaymentMethod { get; set; }
    public Domain.Enums.ServiceType? ServiceType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
