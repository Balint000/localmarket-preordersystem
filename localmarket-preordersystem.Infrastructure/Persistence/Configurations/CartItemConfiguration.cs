using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", table =>
            table.HasCheckConstraint("CK_CartItems_Quantity", "\"Quantity\" > 0"));
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Quantity).HasPrecision(12, 3);
        builder.Property(item => item.UnitPriceSnapshot).HasPrecision(12, 2);

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.CartId, item.ProductId }).IsUnique();
    }
}