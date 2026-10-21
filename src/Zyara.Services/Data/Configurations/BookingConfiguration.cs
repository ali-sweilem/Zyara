using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Services.Data.Configurations
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
            // TODO(M2): Change User to Provider entity once it exists
            builder.HasOne(b => b.Provider)
                .WithMany()
                .HasForeignKey(b => b.ProviderId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // Booking (many) -> Service (1): required, Restrict
            builder.HasOne(b => b.Service)
                .WithMany()
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // Booking (0..1) <-> TimeSlot (1): one-to-one, FK on Booking, unique index, Restrict
            builder.HasOne(b => b.TimeSlot)
                .WithOne()
                .HasForeignKey<Booking>(b => b.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasIndex(b => b.TimeSlotId).IsUnique();

            // Booking (1) <-> Payment (1): one-to-one, FK on Payment, Cascade
            builder.HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Booking (1) <-> Rating (0..1): one-to-one optional
            builder.HasOne(b => b.Rating)
                .WithOne()
                .HasForeignKey<Rating>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // ServiceNote (0..*) -> Booking (1): one-to-many
            // TODO(M4): Add navigation to ServiceNotes once ServiceNote entity is created

            // Prescription (0..1) <-> Booking (1): one-to-one optional
            // TODO(M4): Add navigation to Prescription once Prescription entity is created

            builder.HasIndex(b => b.PatientId);
            builder.HasIndex(b => b.ProviderId);
            builder.HasIndex(b => b.Status);
        }
    }
}