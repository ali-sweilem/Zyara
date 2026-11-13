using System;
using System.Collections.Generic;

namespace Zyara.Services.Common
{
    public class PagedResult<T> : IPagedResult
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }

        /// <summary>Alias of <see cref="PageNumber"/>.</summary>
        public int Page => PageNumber;

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
