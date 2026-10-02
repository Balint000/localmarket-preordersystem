using localmarket_preordersystem.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace localmarket_preordersystem.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(user => user.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasIndex(user => user.ProducerId).IsUnique();

        builder.HasOne(user => user.Producer)
            .WithOne()
            .HasForeignKey<User>(user => user.ProducerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}