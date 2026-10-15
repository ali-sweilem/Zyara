using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.licenser)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(d => d.Name)
                .IsRequired(true)
                .HasMaxLength(100);
            builder.Property(d => d.Email)
                .IsRequired(false)
                .HasMaxLength(100);
            builder.Property(d => d.PhoneNumber)
                .IsRequired(true)
                .HasMaxLength(11);
            builder.Property(d => d.averageRating)
               .IsRequired(true)
               .HasColumnType("decimal(18,2)");
            builder.Property(d => d.experienceYears)
                .IsRequired(true);
        builder.Property(d => d.Specilty)
                .IsRequired(true)
                .HasMaxLength(100);
            // Configure the relationships with Prescription
            builder.HasMany(d => d.Prescriptions)
                .WithOne(p => p.doctor)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
