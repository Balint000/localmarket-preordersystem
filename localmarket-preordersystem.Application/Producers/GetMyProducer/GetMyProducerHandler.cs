using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.Producers.GetMyProducer
{
    /// <summary>
    /// Az árus a SAJÁT profilját kérdezi le (az azonosító a tokenből jön). Státusztól függetlenül működik:
    /// a jóváhagyásra váró (Pending) vagy elutasított árus is látja az adatait és az elutasítás indoklását,
    /// szemben a ListProducers-szel, ami a nem-piacvezetőnek csak a jóváhagyottakat adja.
    /// </summary>
    public sealed class GetMyProducerHandler(IProducerRepository producers, ICurrentUserService currentUser)
    {
        public async Task<ProducerDto> HandleAsync(CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            var producer = await producers.GetByIdAsync(producerId, ct)
                ?? throw new NotFoundException("Az árus-profil nem található.");
            return ProducerDto.From(producer);
        }
    }
}
