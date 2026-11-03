using System.Text.Json;
using System.Text.Json.Serialization;
using localmarket_preordersystem.Application.Common.Interfaces;
using localmarket_preordersystem.Domain.Entity;

namespace localmarket_preordersystem.Application.Common.Extensions
{
    public static class AuditLogRepositoryExtensions
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = false
        };

        /// <summary>Kritikus változás naplózása; az értékek JSON-ként kerülnek a jsonb oszlopba. A mentést a hívó végzi.</summary>
        public static Task RecordAsync(
            this IAuditLogRepository repository,
            ICurrentUserService user,
            string action,
            string entityName,
            int entityId,
            object? oldValue,
            object? newValue,
            CancellationToken cancellationToken = default)
        {
            var log = Logs.Record(
                user.UserId,
                action,
                entityName,
                entityId,
                oldValue is null ? null : JsonSerializer.Serialize(oldValue, Options),
                newValue is null ? null : JsonSerializer.Serialize(newValue, Options));
            return repository.AddAsync(log, cancellationToken);
        }
    }
}
