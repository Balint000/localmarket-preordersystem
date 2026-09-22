using localmarket_preordersystem.Domain.Exceptions;

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

        private Logs()
        {
            // EF Core-nak kell
        }

        public static Logs Record(int? userId, string action, string entityName, int entityId, string? oldValue, string? newValue)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new DomainException("A napló akció megnevezése kötelező.");
            if (string.IsNullOrWhiteSpace(entityName))
                throw new DomainException("A napló entitás-neve kötelező.");

            return new Logs
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
