using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class PickupSlotConfiguration : IEntityTypeConfiguration<PickupSlot>
{
    public void Configure(EntityTypeBuilder<PickupSlot> builder)
    {
        builder.ToTable("PickupSlots", table =>
        {
            table.HasCheckConstraint("CK_PickupSlots_Capacity", "\"Capacity\" > 0");
            table.HasCheckConstraint("CK_PickupSlots_BookedCount", "\"BookedCount\" >= 0 AND \"BookedCount\" <= \"Capacity\"");
            table.HasCheckConstraint("CK_PickupSlots_TimeRange", "\"EndTime\" > \"StartTime\"");
        });
        builder.HasKey(slot => slot.Id);

        builder.Property(slot => slot.Date).HasColumnType("date");
        builder.Property(slot => slot.StartTime).HasColumnType("time without time zone");
        builder.Property(slot => slot.EndTime).HasColumnType("time without time zone");
        builder.Property(slot => slot.BookedCount).IsConcurrencyToken();

        builder.HasOne(slot => slot.Producer)
            .WithMany(producer => producer.PickupSlots)
            .HasForeignKey(slot => slot.ProducerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(slot => new { slot.ProducerId, slot.Date, slot.StartTime, slot.EndTime }).IsUnique();
    }
}