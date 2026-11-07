using localmarket_preordersystem.api.Contracts.Common;

namespace localmarket_preordersystem.api.Extensions;

public static class PagingExtensions
{
    public static PagedResult<T> ToPaged<T>(this IReadOnlyList<T> source, PageRequest paging)
    {
        var items = source
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToList();
        return new PagedResult<T>(items, source.Count, paging.Page, paging.PageSize);
    }
}
