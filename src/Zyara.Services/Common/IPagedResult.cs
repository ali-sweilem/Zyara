namespace Zyara.Services.Common
{

    public interface IPagedResult
    {
        int Page { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }
}
