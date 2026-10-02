using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.ToTable("Markets");
        builder.HasKey(market => market.Id);

        builder.Property(market => market.Name).HasMaxLength(200).IsRequired();
        builder.Property(market => market.Address).HasMaxLength(500).IsRequired();
        builder.Property<List<DayOfWeek>>("_openingDays")
            .HasColumnName("OpeningDays")
            .HasColumnType("integer[]")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}