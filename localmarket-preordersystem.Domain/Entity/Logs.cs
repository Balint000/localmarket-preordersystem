namespace localmarket_preordersystem.Domain.Entity
{
    public class Logs
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;      // pl. "OrderCancelled"
        public string EntityName { get; set; } = string.Empty;  // pl. "Order"
        public int EntityId { get; set; }
        public string? OldValue { get; set; }  // JSON snapshot
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
