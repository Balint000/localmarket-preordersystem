using localmarket_preordersystem.Application.Markets.Layout;

namespace localmarket_preordersystem.api.Contracts.Markets;

/// <summary>A piac elrendezése: rácsméret (cellákban) és a standok.</summary>
public sealed record MarketLayoutRequest(int Columns, int Rows, IReadOnlyList<StallInput> Stalls);
