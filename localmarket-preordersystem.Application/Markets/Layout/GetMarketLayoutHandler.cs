using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Markets.Layout
{
    /// <summary>
    /// A piac felülnézeti térképe a standokkal. Nyilvános: a vevő is látja, hol az árus standja. A piacvezető
    /// a még jóváhagyásra váró árusok kiosztását is látja, mindenki más csak a jóváhagyottakét.
    /// </summary>
    public sealed class GetMarketLayoutHandler(IMarketRepository markets, IProducerRepository producers, ICurrentUserService currentUser)
    {
        public async Task<MarketLayoutDto> HandleAsync(int marketId, CancellationToken ct = default)
        {
            var market = await markets.GetByIdAsync(marketId, ct)
                ?? throw new NotFoundException($"Nem található piac: {marketId}.");
            var status = currentUser.IsManager() ? (ProducerStatus?)null : ProducerStatus.Approved;
            var list = await producers.ListAsync(status, marketId, ct);
            return MarketLayoutDto.From(market, list.Where(p => p.Status != ProducerStatus.Rejected));
        }
    }
}
