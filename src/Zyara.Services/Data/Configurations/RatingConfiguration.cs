
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    public class RatingConfiguration
        : IEntityTypeConfiguration<Rating>
    {
        public void Configure(EntityTypeBuilder<Rating> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Stars)
                .IsRequired();

            builder.ToTable("Ratings", table =>
            {
                table.HasCheckConstraint(
                    "CK_Ratings_Stars",
                    "[Stars] >= 1 AND [Stars] <= 5");
            });

            builder.Property(r => r.Comment)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.HasIndex(r => r.BookingId)
                .IsUnique();
        }
    }
}

