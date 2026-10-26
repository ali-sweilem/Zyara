using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            // Inherits from Provider under TPH
            builder.HasBaseType<Provider>();

            // Doctor-specific properties (shared properties are configured in User and Provider configurations)
            builder.Property(d => d.licenser)
                .IsRequired()
                .HasMaxLength(100);

            // Relationship with Prescriptions
            builder.HasMany(d => d.prescriptions)
                .WithOne(p => p.Doctor)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

