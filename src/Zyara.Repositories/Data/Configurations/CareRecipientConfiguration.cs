using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Repositories.Data.Configurations;

public sealed class CareRecipientConfiguration : IEntityTypeConfiguration<CareRecipient>
{
    public void Configure(EntityTypeBuilder<CareRecipient> builder)
    {
        builder.ToTable("CareRecipients", t =>
        {
            t.HasCheckConstraint("CK_CareRecipients_Slot", "[Slot] BETWEEN 1 AND 3");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Slot)
            .IsRequired();

        builder.Property(c => c.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.DateOfBirth)
            .IsRequired(false);

        builder.Property(c => c.Gender)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(c => c.Relationship)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.BloodType)
            .IsRequired(false)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.ChronicConditions)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(c => c.Allergies)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(c => c.CurrentMedications)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(c => c.Notes)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasOne(c => c.Patient)
            .WithMany(p => p.CareRecipients)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Hard cap of 3 active recipients per account holder
        builder.HasIndex(c => new { c.PatientId, c.Slot })
            .IsUnique()
            .HasFilter("[IsArchived] = 0");

        // Only one active 'Self' patient per account holder
        builder.HasIndex(c => c.PatientId)
            .IsUnique()
            .HasFilter("[Relationship] = 'Self' AND [IsArchived] = 0");
    }
}

