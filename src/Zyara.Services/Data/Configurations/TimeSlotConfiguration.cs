using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
    {
        public void Configure(EntityTypeBuilder<TimeSlot> builder)
        {
            builder.ToTable("TimeSlots");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.HasOne(t => t.Provider)
                .WithMany(p => p.TimeSlots)
                .HasForeignKey(t => t.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => new { t.ProviderId, t.Date });
        }
    }
}

