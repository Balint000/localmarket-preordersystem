using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name).HasMaxLength(100).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(500).IsRequired();
        builder.Property(product => product.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property<List<Allergy>>("_allergies")
            .HasColumnName("Allergies")
            .HasColumnType("integer[]")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(product => product.Producer)
            .WithMany(producer => producer.Products)
            .HasForeignKey(product => product.ProducerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(product => product.Categories)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "ProductCategories",
                right => right.HasOne<Category>().WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Product>().WithMany().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("ProductCategories");
                    join.HasKey("ProductId", "CategoryId");
                });

        builder.Navigation(product => product.Categories).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(product => product.WeeklyStocks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(product => new { product.ProducerId, product.Name }).IsUnique();
    }
}