using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Producers.ListProducers
{
    public sealed record ListProducersQuery(ProducerStatus? Status = null, int? MarketId = null);

    /// <summary>Piacvezető minden státuszt lát (jóváhagyási sor); mindenki más csak a jóváhagyott árusokat.</summary>
    public sealed class ListProducersHandler(IProducerRepository producers, ICurrentUserService currentUser)
    {
        public async Task<IReadOnlyList<ProducerDto>> HandleAsync(ListProducersQuery query, CancellationToken ct = default)
        {
            var status = currentUser.IsManager() ? query.Status : ProducerStatus.Approved;
            return (await producers.ListAsync(status, query.MarketId, ct)).Select(ProducerDto.From).ToList();
        }
    }
}
