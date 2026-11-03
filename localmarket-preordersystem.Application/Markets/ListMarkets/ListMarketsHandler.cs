using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.Markets.ListMarkets
{
    /// <summary>Piacok listája; nyilvános (regisztráció előtt is kell az árusnak).</summary>
    public sealed class ListMarketsHandler(IMarketRepository markets)
    {
        public async Task<IReadOnlyList<MarketDto>> HandleAsync(CancellationToken ct = default)
            => (await markets.ListAsync(ct)).Select(MarketDto.From).ToList();
    }
}
