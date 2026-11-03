using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Markets.CreateMarket
{
    /// <summary>Új piac felvétele. Csak piacvezető.</summary>
    public sealed class CreateMarketHandler(IMarketRepository markets, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<MarketDto> HandleAsync(CreateMarketCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            CreateMarketValidator.Validate(command);

            var market = Market.Create(command.Name, command.Address, command.OpeningDays);
            market.UpdateLocation(command.Latitude, command.Longitude);

            await markets.AddAsync(market, ct);
            await uow.SaveChangesAsync(ct);
            return MarketDto.From(market);
        }
    }
}
