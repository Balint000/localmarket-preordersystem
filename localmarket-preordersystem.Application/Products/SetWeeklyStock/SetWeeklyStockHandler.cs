using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Application.Categories;

namespace localmarket_preordersystem.Application.Products.SetWeeklyStock
{
    public sealed class SetWeeklyStockHandler(
        IProductRepository products,
        IAuditLogRepository audit,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        TimeProvider time)
    {
        public async Task<ProductDto> HandleAsync(SetWeeklyStockCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            SetWeeklyStockValidator.Validate(command);

            var today = time.GetLocalNow().Date;
            if (ProductStock.GetWeekStart(command.WeekDate) < ProductStock.GetWeekStart(today))
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    [nameof(command.WeekDate)] = new[] { "Múltbeli hétre nem rögzíthető kínálat." }
                });

            var product = await products.GetByIdAsync(command.ProductId, ct)
                ?? throw new NotFoundException($"Nem található termék: {command.ProductId}.");
            if (product.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját terméked kínálatát módosíthatod.");

            var existing = product.GetStockForWeek(command.WeekDate);
            var before = existing is null ? null : ProductStockDto.From(existing);

            if (existing is null)
                product.AddWeeklyStock(command.WeekDate, command.Quantity, command.Unit, command.UnitPrice);
            else
                product.UpdateWeeklyStock(command.WeekDate, command.Quantity, command.Unit, command.UnitPrice);

            var after = ProductStockDto.From(product.GetStockForWeek(command.WeekDate)!);
            await audit.RecordAsync(currentUser, existing is null ? "WeeklyStockCreated" : "WeeklyStockUpdated", "Product", product.Id, before, after, ct);
            await uow.SaveChangesAsync(ct);
            return ProductDto.From(product, today);
        }
    }
}
