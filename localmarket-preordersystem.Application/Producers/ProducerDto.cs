using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Producers
{
    public sealed record ProducerDto(int Id, string Name, int MarketId, int StallNumber, string Status, string? RejectionReason, DateTime RegisteredAt)
    {
        public static ProducerDto From(Producer p) => new(p.Id, p.Name, p.MarketId, p.StallNumber, p.Status.ToString(), p.RejectionReason, p.RegisteredAt);
    }
}
