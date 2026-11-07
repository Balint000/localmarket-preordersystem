using System.ComponentModel.DataAnnotations;

namespace localmarket_preordersystem.api.Contracts.Common;

public sealed class PageRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
