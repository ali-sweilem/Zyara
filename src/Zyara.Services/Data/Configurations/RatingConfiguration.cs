using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class RatingConfiguration : IEntityTypeConfiguration<Rating>
    {
        public void Configure(EntityTypeBuilder<Rating> builder)
        {
            builder.ToTable("Ratings");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Comment)
                .HasMaxLength(1000);

            builder.HasIndex(r => r.BookingId).IsUnique();
        }
    }
}

