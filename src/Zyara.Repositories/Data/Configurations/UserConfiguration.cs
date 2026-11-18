using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Repositories.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name (kept as "Users" so the existing FKs keep pointing at it)
        builder.ToTable("Users");
        // PK (Id comes from IdentityUser<int>)
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

        // T1: Phone -> PhoneNumber (IdentityUser)
        builder.Property(u => u.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        // PasswordHash is owned by Identity and MUST stay nullable
        // (Google-only accounts are created without a password).

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
