using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Type)
                .IsRequired();

            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Relationships 
            builder.HasMany(s => s.ProviderServices)
                .WithOne(ps => ps.Service)
                .HasForeignKey(ps => ps.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Bookings)
                .WithOne(b => b.Service)
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}