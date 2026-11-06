using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_OrderItems_Fulfilled", "\"FulfilledQuantity\" >= 0 AND \"FulfilledQuantity\" <= \"Quantity\"");
        });
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductName).HasMaxLength(100).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(12, 2);
        builder.Property(item => item.Quantity).HasPrecision(12, 3);
        builder.Property(item => item.FulfilledQuantity).HasPrecision(12, 3);
        builder.Property(item => item.Unit).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Ignore(item => item.Total);

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.OrderId);
        builder.HasIndex(item => item.ProductId);
    }
}
