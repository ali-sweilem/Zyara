using System.Collections.Generic;
using Zyara.Services.Common;

namespace Zyara.Web.ViewModels;

public sealed class PaginationViewModel
{
    public PaginationViewModel(
        IPagedResult paged,
        string action,
        string? controller = null,
        IDictionary<string, string>? routeValues = null,
        string pageParameter = "PageNumber",
        string? fragment = null,
        string ariaLabel = "Pagination")
    {
        Page = paged.Page;
        TotalPages = paged.TotalPages;
        HasPreviousPage = paged.HasPreviousPage;
        HasNextPage = paged.HasNextPage;
        Action = action;
        Controller = controller;
        RouteValues = routeValues is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(routeValues);
        PageParameter = pageParameter;
        Fragment = fragment;
        AriaLabel = ariaLabel;
    }

    public int Page { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }
    public string Action { get; }
    public string? Controller { get; }
    public IReadOnlyDictionary<string, string> RouteValues { get; }
    public string PageParameter { get; }
    public string? Fragment { get; }
    public string AriaLabel { get; }
}