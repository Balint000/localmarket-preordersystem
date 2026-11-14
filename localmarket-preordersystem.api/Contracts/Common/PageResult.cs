namespace localmarket_preordersystem.api.Contracts.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}
