using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
    {
        public void Configure(EntityTypeBuilder<PlatformSettings> builder)
        {
            builder.ToTable("PlatformSettings");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.ServiceFee)
                .HasPrecision(18, 2);

            builder.Property(p => p.CommissionPercentage)
                .HasPrecision(5, 2);
        }
    }
}

