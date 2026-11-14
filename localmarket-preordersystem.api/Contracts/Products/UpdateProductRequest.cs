using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.api.Contracts.Products;

public sealed record UpdateProductRequest(string Name, string? Description, IReadOnlyList<int> CategoryIds, IReadOnlyList<Allergy>? Allergies);
