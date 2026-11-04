using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", table => table.HasCheckConstraint("CK_Review_Stars", "[Stars] BETWEEN 1 AND 5"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.BookingId).IsRequired();
        builder.Property(r => r.Stars).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.HasIndex(r => r.BookingId).IsUnique();
        builder.HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
