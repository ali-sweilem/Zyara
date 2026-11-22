using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Repositories.Data.Configurations
{
    internal class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion<string>();

            builder.Property(b => b.ServiceFee)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(b => b.TotalPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(b => b.Commission)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(b => b.OtpCode)
                .IsRequired(false)
                .HasMaxLength(10);

            builder.Property(b => b.Notes)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(b => b.VisitAddress)
                .IsRequired(false)
                .HasMaxLength(400);

            builder.Property(b => b.VisitLatitude)
                .IsRequired(false);

            builder.Property(b => b.VisitLongitude)
                .IsRequired(false);

            builder.Property(b => b.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(b => b.RespondedAt)
                .IsRequired(false);

            builder.Property(b => b.StartedAt)
                .IsRequired(false);

            builder.Property(b => b.CompletedAt)
                .IsRequired(false);

            builder.Property(b => b.StatusReason)
                .IsRequired(false)
                .HasMaxLength(300);

            builder.Property(b => b.OtpFailedAttempts)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(b => b.RowVersion)
                .IsRowVersion();

            builder.Property(b => b.PatientId).IsRequired();
            builder.Property(b => b.ProviderId).IsRequired();
            builder.Property(b => b.ServiceId).IsRequired();
            builder.Property(b => b.TimeSlotId).IsRequired();

            // Booking (many) -> Patient (1): required, Restrict
            builder.HasOne(b => b.Patient)
                .WithMany()
                .HasForeignKey(b => b.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // Booking (many) -> Provider (1): required, Restrict
            builder.HasOne(b => b.Provider)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.ProviderId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // Booking (many) -> Service (1): required, Restrict
            builder.HasOne(b => b.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // TimeSlot relationship: HasOne(b => b.TimeSlot).WithMany().HasForeignKey(b => b.TimeSlotId).OnDelete(DeleteBehavior.Restrict).IsRequired()
            builder.HasOne(b => b.TimeSlot)
                .WithMany()
                .HasForeignKey(b => b.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasIndex(b => b.TimeSlotId)
                .IsUnique()
                .HasFilter("[Status] <> 'Cancelled' AND [Status] <> 'Rejected' AND [Status] <> 'Expired'");

            // Booking (1) <-> Payment (1): one-to-one, FK on Payment, Cascade
            builder.HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasIndex(b => b.PatientId);
            builder.HasIndex(b => b.ProviderId);
            builder.HasIndex(b => b.Status);
        }
    }
}
