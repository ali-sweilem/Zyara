using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
    {
        public void Configure(EntityTypeBuilder<TimeSlot> builder)
        {
            builder.HasKey(ts => ts.Id);

            builder.Property(ts => ts.ProviderId)
                .IsRequired();

            builder.Property(ts => ts.Date)
                .IsRequired();

            builder.Property(ts => ts.StartTime)
                .IsRequired();

            builder.Property(ts => ts.EndTime)
                .IsRequired();

            builder.Property(ts => ts.Status)
                .IsRequired();

            // Relationships
            builder.HasOne(ts => ts.Provider)
                .WithMany(p => p.TimeSlots)
                .HasForeignKey(ts => ts.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ts => ts.Booking)
                .WithOne(b => b.TimeSlot)
                .HasForeignKey<TimeSlot>(ts => ts.BookingId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}