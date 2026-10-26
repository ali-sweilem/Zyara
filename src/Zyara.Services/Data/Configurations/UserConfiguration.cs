using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Services.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("Users");
        // PK
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Gender)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(u => u.Role)
        .HasConversion<string>()
        .HasMaxLength(20);

        builder.HasDiscriminator(u => u.Role)
            .HasValue<Patient>(UserRole.Patient)
            .HasValue<Doctor>(UserRole.Doctor)
            .HasValue<Nurse>(UserRole.Nurse)
            .HasValue<Admin>(UserRole.Admin);
    }
}