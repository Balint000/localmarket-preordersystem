using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Common.Extensions;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Application.Common.Validation;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Application.Producers.ApproveProducer
{
    public sealed record ApproveProducerCommand(int ProducerId);

    /// <summary>Árus jóváhagyása — csak piacvezető. A jóváhagyás nélküli árus nem hirdethet terméket.</summary>
    public sealed class ApproveProducerHandler(IProducerRepository producers, IAuditLogRepository audit, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        public async Task<ProducerDto> HandleAsync(ApproveProducerCommand command, CancellationToken ct = default)
        {
            currentUser.RequireRole(Role.Admin);
            var producer = await producers.GetByIdAsync(command.ProducerId, ct)
                ?? throw new NotFoundException($"Nem található árus: {command.ProducerId}.");

            var before = producer.Status;
            producer.Approve();
            await audit.RecordAsync(currentUser, "ProducerApproved", "Producer", producer.Id, new { Status = before }, new { Status = producer.Status }, ct);
            await uow.SaveChangesAsync(ct);
            return ProducerDto.From(producer);
        }
    }
}
