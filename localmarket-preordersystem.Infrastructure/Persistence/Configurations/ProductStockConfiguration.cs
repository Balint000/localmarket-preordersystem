using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class ProductStockConfiguration : IEntityTypeConfiguration<ProductStock>
{
    public void Configure(EntityTypeBuilder<ProductStock> builder)
    {
        builder.ToTable("ProductStocks", table =>
        {
            table.HasCheckConstraint("CK_ProductStocks_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_ProductStocks_Reserved", "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"Quantity\"");
        });
        builder.HasKey(stock => stock.Id);

        builder.Property(stock => stock.WeekStartDate).HasColumnType("date");
        builder.Property(stock => stock.Unit).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(stock => stock.LastUpdated).HasColumnType("timestamp with time zone");
        builder.Ignore(stock => stock.AvailableQuantity);

        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasOne(stock => stock.Product)
            .WithMany(product => product.WeeklyStocks)
            .HasForeignKey(stock => stock.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(stock => new { stock.ProductId, stock.WeekStartDate }).IsUnique();
    }
}
