using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Markets.UpdateMarket
{
    /// <summary>Piac adatainak és nyitvatartási napjainak módosítása. Csak piacvezető; naplózott.</summary>
    public sealed class UpdateMarketHandler(IMarketRepository markets, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<MarketDto> HandleAsync(UpdateMarketCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            UpdateMarketValidator.Validate(command);

            var market = await markets.GetByIdAsync(command.MarketId, ct)
                ?? throw new NotFoundException($"Nem található piac: {command.MarketId}.");

            var before = MarketDto.From(market);
            market.UpdateDetails(command.Name, command.Address);
            market.SetOpeningDays(command.OpeningDays);
            market.UpdateLocation(command.Latitude, command.Longitude);
            if (command.IsActive) market.Activate(); else market.Deactivate();

            var after = MarketDto.From(market);
            await audit.RecordAsync(currentUser, "MarketUpdated", "Market", market.Id, before, after, ct);
            await uow.SaveChangesAsync(ct);
            return after;
        }
    }
}
