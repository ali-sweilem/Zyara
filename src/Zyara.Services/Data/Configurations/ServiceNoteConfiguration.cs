using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class ServiceNoteConfiguration : IEntityTypeConfiguration<ServiceNote>
    {
        public void Configure(EntityTypeBuilder<ServiceNote> builder)
        {
            builder.ToTable("ServiceNotes");
            builder.HasKey(sn => sn.Id);

            builder.Property(sn => sn.Content)
                .IsRequired()
                .HasMaxLength(2000);

            builder.HasOne(sn => sn.Booking)
                .WithMany()
                .HasForeignKey(sn => sn.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

