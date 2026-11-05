namespace Zyara.Web.Areas.Provider.ViewModels;

public class BookingListViewModel
{
    public BookingFilterViewModel Filter { get; set; } = new();
    public List<BookingListItemViewModel> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
