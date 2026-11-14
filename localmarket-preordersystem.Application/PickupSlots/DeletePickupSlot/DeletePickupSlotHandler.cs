using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;

namespace localmarket_preordersystem.Application.PickupSlots.DeletePickupSlot
{
    public sealed record DeletePickupSlotCommand(int SlotId);

    /// <summary>
    /// Átvételi idősáv törlése — az árus csak a SAJÁTJÁT törölheti. Foglalt idősáv nem törölhető,
    /// és az sem, amelyre valaha rendelés hivatkozott (rendelési előzmények épsége).
    /// </summary>
    public sealed class DeletePickupSlotHandler(IPickupSlotRepository slots, IOrderRepository orders, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task HandleAsync(DeletePickupSlotCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();

            var slot = await slots.GetByIdAsync(command.SlotId, ct)
                ?? throw new NotFoundException($"Nem található átvételi időpont: {command.SlotId}.");
            if (slot.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját átvételi időpontodat törölheted.");

            slot.EnsureCanBeRemoved();
            if (await orders.AnyForPickupSlotAsync(slot.Id, ct))
                throw new ConflictException("Az időponthoz tartozik (akár lemondott) rendelés, ezért nem törölhető.");

            var before = PickupSlotDto.From(slot);
            slots.Remove(slot);
            await audit.RecordAsync(currentUser, "PickupSlotDeleted", "PickupSlot", before.Id, before, null, ct);
            await uow.SaveChangesAsync(ct);
        }
    }
}
