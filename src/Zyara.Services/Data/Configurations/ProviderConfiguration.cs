using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations;

internal sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        // Table name and primary key are defined in UserConfiguration (TPH hierarchy).

        // String length constraints
        builder.Property(p => p.Specialty).HasMaxLength(100);
        builder.Property(p => p.BaseCity).HasMaxLength(100);
        builder.Property(p => p.RejectionReason).HasMaxLength(500);

        // Verification status enum stored as string
        builder.Property(p => p.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Financial balance precision
        builder.Property(p => p.WalletBalance).HasPrecision(18, 2);

        // Indexes for search and filtering
        builder.HasIndex(p => p.BaseCity);
        builder.HasIndex(p => p.VerificationStatus);

        // Admin verification relationship (Restrict delete so reviewing admin cannot be removed if referenced)
        builder.HasOne(p => p.VerifiedByAdmin)
            .WithMany()
            .HasForeignKey(p => p.VerifiedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Available schedule time slots
        builder.HasMany(p => p.TimeSlots)
            .WithOne(t => t.Provider)
            .HasForeignKey(t => t.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Services offered by the provider
        builder.HasMany(p => p.ProviderServices)
            .WithOne(ps => ps.Provider)
            .HasForeignKey(ps => ps.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

