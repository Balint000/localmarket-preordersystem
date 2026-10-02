using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class LogsConfiguration : IEntityTypeConfiguration<Logs>
{
    public void Configure(EntityTypeBuilder<Logs> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Action).HasMaxLength(100).IsRequired();
        builder.Property(log => log.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(log => log.OldValue).HasColumnType("jsonb");
        builder.Property(log => log.NewValue).HasColumnType("jsonb");
        builder.Property(log => log.CreatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(log => new { log.EntityName, log.EntityId });
        builder.HasIndex(log => log.CreatedAt);
    }
}