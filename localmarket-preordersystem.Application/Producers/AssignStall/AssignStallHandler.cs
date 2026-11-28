using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Producers.AssignStall
{
    /// <param name="StallNumber">A kiosztandó stand; 0 = a kiosztás törlése.</param>
    /// <param name="Swap">Ha a stand foglalt: true esetén a két árus helyet cserél, különben 409.</param>
    public sealed record AssignStallCommand(int ProducerId, int StallNumber, bool Swap = false);

    /// <summary>
    /// Fix stand kiosztása egy árusnak (piacvezető). A stand a piac elrendezésében létezzen, és piaconként
    /// egy standon egy árus lehet. Csere esetén a stand korábbi árusa az áthelyezett árus régi standját kapja.
    /// </summary>
    public sealed class AssignStallHandler(IProducerRepository producers, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<ProducerDto> HandleAsync(AssignStallCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            var producer = await producers.GetByIdAsync(command.ProducerId, ct)
                ?? throw new NotFoundException($"Nem található árus: {command.ProducerId}.");
            if (command.StallNumber > 0 && producer.Status == ProducerStatus.Rejected)
                throw new ConflictException("Elutasított árusnak nem osztható ki stand.");

            var previous = producer.StallNumber;
            if (previous == command.StallNumber)
                return ProducerDto.From(producer);

            if (command.StallNumber > 0)
            {
                if (producer.Market.Layout.Stalls.Count == 0)
                    throw new ConflictException("A piacnak még nincs elrendezése: előbb rendezd be a standokat.");
                if (!producer.Market.Layout.HasStall(command.StallNumber))
                    throw new ConflictException($"A(z) {command.StallNumber}. stand nem szerepel a piac elrendezésében.");

                var marketProducers = await producers.ListByMarketForUpdateAsync(producer.MarketId, ct);
                var occupant = marketProducers.FirstOrDefault(p => p.Id != producer.Id && p.StallNumber == command.StallNumber);
                if (occupant is not null)
                {
                    if (!command.Swap)
                        throw new ConflictException($"A(z) {command.StallNumber}. standot már {occupant.Name} kapta. Válaszd a cserét, vagy előbb vedd el tőle.");
                    occupant.AssignStall(previous); // a régi stand (vagy 0) az előző árusé lesz
                    await audit.RecordAsync(currentUser, "ProducerStallAssigned", "Producer", occupant.Id,
                        new { StallNumber = command.StallNumber }, new { StallNumber = previous, SwappedWith = producer.Id }, ct);
                }
                // a követett példányt módosítjuk (a lista ugyanazt a példányt adja vissza, ha már be volt töltve)
                (marketProducers.FirstOrDefault(p => p.Id == producer.Id) ?? producer).AssignStall(command.StallNumber);
            }
            else
            {
                producer.UnassignStall();
            }

            await audit.RecordAsync(currentUser, "ProducerStallAssigned", "Producer", producer.Id,
                new { StallNumber = previous }, new { StallNumber = command.StallNumber }, ct);
            await uow.SaveChangesAsync(ct);
            return ProducerDto.From(producer);
        }
    }
}
