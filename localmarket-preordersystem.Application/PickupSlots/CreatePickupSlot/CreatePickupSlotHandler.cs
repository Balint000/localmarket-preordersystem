using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Models;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using localmarket_preordersystem.Application.Categories;

namespace localmarket_preordersystem.Application.PickupSlots.CreatePickupSlot
{
    public sealed record CreatePickupSlotCommand(DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity);

    public static class CreatePickupSlotValidator
    {
        public static void Validate(CreatePickupSlotCommand c) => new ValidationBuilder()
            .Must(nameof(c.EndTime), c.EndTime > c.StartTime, "A végének későbbinek kell lennie a kezdésnél.")
            .Positive(nameof(c.Capacity), c.Capacity, "A kapacitásnak pozitívnak kell lennie.")
            .ThrowIfInvalid();
    }

    /// <summary>Átvételi idősáv hirdetése — jóváhagyott árus, a piac nyitvatartási napjára, nem múltbeli napra.</summary>
    public sealed class CreatePickupSlotHandler(IProducerRepository producers, IPickupSlotRepository slots, IUnitOfWork uow, ICurrentUserService currentUser, TimeProvider time)
    {
        public async Task<PickupSlotDto> HandleAsync(CreatePickupSlotCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            CreatePickupSlotValidator.Validate(command);

            var now = time.GetLocalNow().DateTime;
            if (command.Date.Date.Add(command.EndTime.ToTimeSpan()) <= now)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    [nameof(command.Date)] = new[] { "Múltbeli időpont nem hirdethető meg." }
                });

            var producer = await producers.GetByIdAsync(producerId, ct)
                ?? throw new NotFoundException("Az árus-profil nem található.");

            var slot = PickupSlot.Create(producer, command.Date, command.StartTime, command.EndTime, command.Capacity);
            await slots.AddAsync(slot, ct);
            await uow.SaveChangesAsync(ct);
            return PickupSlotDto.From(slot);
        }
    }
}
