using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Producers.RejectProducer
{
    public sealed record RejectProducerCommand(int ProducerId, string Reason);

    public static class RejectProducerValidator
    {
        public static void Validate(RejectProducerCommand c) => new ValidationBuilder()
            .Required(nameof(c.Reason), c.Reason, "Az elutasítás indoklása kötelező.")
            .MaxLength(nameof(c.Reason), c.Reason, 500)
            .ThrowIfInvalid();
    }

    public sealed class RejectProducerHandler(IProducerRepository producers, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<ProducerDto> HandleAsync(RejectProducerCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            RejectProducerValidator.Validate(command);
            var producer = await producers.GetByIdAsync(command.ProducerId, ct)
                ?? throw new NotFoundException($"Nem található árus: {command.ProducerId}.");

            var before = producer.Status;
            producer.Reject(command.Reason);
            await audit.RecordAsync(currentUser, "ProducerRejected", "Producer", producer.Id, new { Status = before }, new { Status = producer.Status, command.Reason }, ct);
            await uow.SaveChangesAsync(ct);
            return ProducerDto.From(producer);
        }
    }
}
