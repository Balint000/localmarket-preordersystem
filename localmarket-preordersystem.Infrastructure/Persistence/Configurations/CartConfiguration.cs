using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(cart => cart.Id);

        builder.HasOne(cart => cart.User)
            .WithOne()
            .HasForeignKey<Cart>(cart => cart.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cart => cart.Items)
            .WithOne(item => item.Cart)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(cart => cart.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(cart => cart.UserId).IsUnique();
    }
}