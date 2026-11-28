using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Producers.UpdateMyProducer
{
    /// <param name="StallNumber">Elhagyható; a standot a piacvezető osztja ki, ezért csak a jelenlegi értékkel egyezhet.</param>
    public sealed record UpdateMyProducerCommand(string Name, int? StallNumber = null);

    public static class UpdateMyProducerValidator
    {
        public static void Validate(UpdateMyProducerCommand c) => new ValidationBuilder()
            .Required(nameof(c.Name), c.Name, "Az árus nevének megadása kötelező.")
            .MaxLength(nameof(c.Name), c.Name, 100)
            .ThrowIfInvalid();
    }

    /// <summary>Az árus a SAJÁT profilját szerkeszti (az azonosítót a tokenből kapjuk, nem a kérésből). A standot nem módosíthatja.</summary>
    public sealed class UpdateMyProducerHandler(IProducerRepository producers, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<ProducerDto> HandleAsync(UpdateMyProducerCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            UpdateMyProducerValidator.Validate(command);
            var producer = await producers.GetByIdAsync(producerId, ct)
                ?? throw new NotFoundException("Az árus-profil nem található.");

            if (command.StallNumber is { } stall && stall != producer.StallNumber)
                throw new ForbiddenAccessException("A standszámot a piacvezető osztja ki a piac elrendezése alapján; ha módosítás kell, kérd tőle.");

            producer.Rename(command.Name);
            await uow.SaveChangesAsync(ct);
            return ProducerDto.From(producer);
        }
    }
}
