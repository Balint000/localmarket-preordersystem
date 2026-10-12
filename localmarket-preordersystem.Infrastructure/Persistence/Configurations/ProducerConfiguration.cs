using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class ProducerConfiguration : IEntityTypeConfiguration<Producer>
{
    public void Configure(EntityTypeBuilder<Producer> builder)
    {
        builder.ToTable("Producers", table =>
            table.HasCheckConstraint("CK_Producers_StallNumber", "\"StallNumber\" >= 0"));
        builder.HasKey(producer => producer.Id);

        builder.Property(producer => producer.Name).HasMaxLength(200).IsRequired();
        builder.Property(producer => producer.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(producer => producer.RejectionReason).HasMaxLength(1000);
        builder.Property(producer => producer.RegisteredAt).HasColumnType("timestamp with time zone");

        builder.HasOne(producer => producer.Market)
            .WithMany(market => market.Producers)
            .HasForeignKey(producer => producer.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(producer => new { producer.MarketId, producer.StallNumber });
    }
}