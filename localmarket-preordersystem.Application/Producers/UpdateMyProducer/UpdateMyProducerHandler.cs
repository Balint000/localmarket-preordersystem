using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Producers.UpdateMyProducer
{
    public sealed record UpdateMyProducerCommand(string Name, int StallNumber);

    public static class UpdateMyProducerValidator
    {
        public static void Validate(UpdateMyProducerCommand c) => new ValidationBuilder()
            .Required(nameof(c.Name), c.Name, "Az árus nevének megadása kötelező.")
            .MaxLength(nameof(c.Name), c.Name, 100)
            .Must(nameof(c.StallNumber), c.StallNumber >= 0, "A standszám nem lehet negatív.")
            .ThrowIfInvalid();
    }

    /// <summary>Az árus a SAJÁT profilját szerkeszti (az azonosítót a tokenből kapjuk, nem a kérésből).</summary>
    public sealed class UpdateMyProducerHandler(IProducerRepository producers, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<ProducerDto> HandleAsync(UpdateMyProducerCommand command, CancellationToken ct = default)
        {
            var producerId = currentUser.RequireProducerId();
            UpdateMyProducerValidator.Validate(command);
            var producer = await producers.GetByIdAsync(producerId, ct)
                ?? throw new NotFoundException("Az árus-profil nem található.");

            producer.Rename(command.Name);
            producer.UpdateStallNumber(command.StallNumber);
            await uow.SaveChangesAsync(ct);
            return ProducerDto.From(producer);
        }
    }
}
