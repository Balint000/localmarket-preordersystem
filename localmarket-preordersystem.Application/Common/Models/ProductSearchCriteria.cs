using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Common.Models
{
    /// <summary>Terméklista-szűrők. Mind opcionális; az üres szűrő mindent visszaad.</summary>
    public sealed record ProductSearchCriteria(
        string? Text = null,
        int? CategoryId = null,
        int? ProducerId = null,
        IReadOnlyCollection<Allergy>? ExcludeAllergies = null);
}
