using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Markets.Layout
{
    public sealed record UpdateMarketLayoutCommand(int MarketId, int Columns, int Rows, IReadOnlyList<StallInput> Stalls);

    /// <summary>
    /// A piac elrendezésének mentése (piacvezető). A standok a rácson belül, átfedés nélkül, egyedi számmal lehetnek.
    /// Ha egy kiosztott stand megszűnik (vagy régi adatban két árus ugyanazt kapta), az árus kiosztás nélkül marad,
    /// és a válasz felsorolja őket, hogy a piacvezető újra kioszthassa.
    /// </summary>
    public sealed class UpdateMarketLayoutHandler(
        IMarketRepository markets,
        IProducerRepository producers,
        IAuditLogRepository audit,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        public async Task<MarketLayoutDto> HandleAsync(UpdateMarketLayoutCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            new ValidationBuilder()
                .Must(nameof(command.Stalls), command.Stalls is not null, "A standok listája kötelező (lehet üres).")
                .ThrowIfInvalid();

            var market = await markets.GetByIdAsync(command.MarketId, ct)
                ?? throw new NotFoundException($"Nem található piac: {command.MarketId}.");
            var layout = MarketLayout.Create(command.Columns, command.Rows, command.Stalls.Select(s => s.ToDomain()));
            var before = new { market.Layout.Columns, market.Layout.Rows, Stalls = market.Layout.Stalls.Count };

            market.SetLayout(layout);

            var all = await producers.ListByMarketForUpdateAsync(market.Id, ct);
            var unassigned = new List<UnassignedProducerDto>();
            var kept = new HashSet<int>();
            foreach (var p in all.Where(p => p.StallNumber > 0).OrderBy(p => p.Id))
            {
                var number = p.StallNumber;
                string? reason = !layout.HasStall(number) ? "A stand megszűnt az új elrendezésben."
                    : !kept.Add(number) ? "A standot már egy korábban regisztrált árus kapta."
                    : null;
                if (reason is null) continue;
                p.UnassignStall();
                unassigned.Add(new UnassignedProducerDto(p.Id, p.Name, number, reason));
            }

            await audit.RecordAsync(currentUser, "MarketLayoutUpdated", "Market", market.Id, before,
                new { layout.Columns, layout.Rows, Stalls = layout.Stalls.Count, Unassigned = unassigned.Select(u => u.ProducerId) }, ct);
            await uow.SaveChangesAsync(ct);
            return MarketLayoutDto.From(market, all.Where(p => p.Status != ProducerStatus.Rejected), unassigned);
        }
    }
}
