using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
    {
        public void Configure(EntityTypeBuilder<Prescription> builder)
        {
            builder.ToTable("Prescriptions");
            builder.HasKey(p => p.Id);

            builder.HasOne(p => p.Booking)
                .WithOne()
                .HasForeignKey<Prescription>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(p => p.Doctor)
                .WithMany(d => d.prescriptions)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.PrescriptionItems)
                .WithOne(pi => pi.Prescription)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

