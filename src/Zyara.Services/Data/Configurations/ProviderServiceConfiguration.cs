using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    public class ProviderServiceConfiguration : IEntityTypeConfiguration<ProviderService>
    {
        public void Configure(EntityTypeBuilder<ProviderService> builder)
        {
            builder.HasKey(ps => ps.Id);

            builder.Property(ps => ps.ProviderId)
                .IsRequired();

            builder.Property(ps => ps.ServiceId)
                .IsRequired();

            
            builder.Property(ps => ps.Fee)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(ps => ps.CommissionPercentage)
                .HasPrecision(5, 2)
                .IsRequired();

            // Relationships
            builder.HasOne(ps => ps.Service)
                .WithMany(s => s.ProviderServices)
                .HasForeignKey(ps => ps.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ps => ps.Provider)
                .WithMany() 
                .HasForeignKey(ps => ps.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}