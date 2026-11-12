using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Zyara.Services.Common
{
    public static class PagingExtensions
    {
        /// <summary>
        /// Counts the query, then loads one page. The query must already be ordered.
        /// page &lt; 1 becomes 1; page past the end becomes the last page; pageSize is clamped to 1..maxPageSize.
        /// </summary>
        public static Task<PagedResult<T>> ToPagedAsync<T>(
            this IQueryable<T> query,
            int page,
            int pageSize,
            int maxPageSize = 50,
            CancellationToken cancellationToken = default)
            => query.ToPagedAsync(x => x, page, pageSize, maxPageSize, cancellationToken);

        /// <summary>
        /// Same as above, but projects each row with <paramref name="selector"/> in the database.
        /// </summary>
        public static async Task<PagedResult<TResult>> ToPagedAsync<TSource, TResult>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, TResult>> selector,
            int page,
            int pageSize,
            int maxPageSize = 50,
            CancellationToken cancellationToken = default)
        {
            pageSize = Math.Clamp(pageSize, 1, maxPageSize);

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            page = totalPages > 0 ? Math.Clamp(page, 1, totalPages) : 1;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new PagedResult<TResult>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }
    }
}
