using System.Text.Json;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

/// <summary>A <see cref="MarketLayout"/> értékobjektum jsonb-be (de)szerializálása és összehasonlítása az EF számára.</summary>
internal static class MarketLayoutJson
{
    private sealed record StallJson(int Number, int X, int Y, int Width, int Height, string? Label);
    private sealed record LayoutJson(int Columns, int Rows, List<StallJson>? Stalls);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(MarketLayout layout) => JsonSerializer.Serialize(
        new LayoutJson(layout.Columns, layout.Rows,
            layout.Stalls.Select(s => new StallJson(s.Number, s.X, s.Y, s.Width, s.Height, s.Label)).ToList()),
        Options);

    public static MarketLayout Deserialize(string json)
    {
        var data = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<LayoutJson>(json, Options);
        if (data is null)
            return MarketLayout.Empty;
        return MarketLayout.Create(data.Columns, data.Rows,
            (data.Stalls ?? []).Select(s => new MarketStall(s.Number, s.X, s.Y, s.Width, s.Height, s.Label)));
    }

    public static readonly ValueConverter<MarketLayout, string> Converter =
        new(layout => Serialize(layout), json => Deserialize(json));

    // az értékobjektum változatlan (immutable): a pillanatkép lehet maga a példány, az egyenlőség a JSON-tartalom szerint
    public static readonly ValueComparer<MarketLayout> Comparer = new(
        (a, b) => ReferenceEquals(a, b) || (a != null && b != null && Serialize(a) == Serialize(b)),
        layout => Serialize(layout).GetHashCode(),
        layout => layout);
}
