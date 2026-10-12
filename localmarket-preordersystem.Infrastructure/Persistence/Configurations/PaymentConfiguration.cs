using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
            table.HasCheckConstraint("CK_Payments_Amount", "\"Amount\" > 0"));
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Amount).HasPrecision(12, 2);
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(payment => payment.Date).HasColumnType("timestamp with time zone");
        builder.HasIndex(payment => payment.OrderId).IsUnique();
    }
}