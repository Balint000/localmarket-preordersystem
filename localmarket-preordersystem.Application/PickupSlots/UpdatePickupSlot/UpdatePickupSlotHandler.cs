using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;

namespace localmarket_preordersystem.Application.PickupSlots.UpdatePickupSlot
{
    public sealed record UpdatePickupSlotCommand(int SlotId, DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity);

    public static class UpdatePickupSlotValidator
    {
        public static void Validate(UpdatePickupSlotCommand c) => new ValidationBuilder()
            .Positive(nameof(c.SlotId), c.SlotId, "Érvénytelen időpont azonosító.")
            .Must(nameof(c.EndTime), c.EndTime > c.StartTime, "A végének későbbinek kell lennie a kezdésnél.")
            .Positive(nameof(c.Capacity), c.Capacity, "A kapacitásnak pozitívnak kell lennie.")
            .ThrowIfInvalid();
    }

    /// <summary>
    /// Átvételi idősáv módosítása — az árus csak a SAJÁTJÁT módosíthatja. A szabályok (lejárt nem
    /// módosítható, foglalt nem mozgatható, a kapacitás nem mehet a foglalások alá) a PickupSlot.Update-ben vannak.
    /// </summary>
    public sealed class UpdatePickupSlotHandler(IPickupSlotRepository slots, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser, TimeProvider time)
    {
        public async Task<PickupSlotDto> HandleAsync(UpdatePickupSlotCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            UpdatePickupSlotValidator.Validate(command);

            var slot = await slots.GetForUpdateAsync(command.SlotId, ct)
                ?? throw new NotFoundException($"Nem található átvételi időpont: {command.SlotId}.");
            if (slot.ProducerId != producerId)
                throw new ForbiddenAccessException("Csak a saját átvételi időpontodat módosíthatod.");

            var before = PickupSlotDto.From(slot);
            slot.Update(command.Date, command.StartTime, command.EndTime, command.Capacity, time.GetLocalNow().DateTime);
            var after = PickupSlotDto.From(slot);

            await audit.RecordAsync(currentUser, "PickupSlotUpdated", "PickupSlot", slot.Id, before, after, ct);
            await uow.SaveChangesAsync(ct);
            return after;
        }
    }
}
