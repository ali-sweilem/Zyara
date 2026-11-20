using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Repositories.Data.Configurations
{
    internal class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Method)
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion<string>();

            builder.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion<string>();

            builder.Property(p => p.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(p => p.RefundedAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(p => p.BookingId).IsRequired();

            builder.HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            //---------------
            builder.HasIndex(p => p.BookingId).IsUnique();

            builder.Property(p => p.GatewayReference).HasMaxLength(100);
            builder.Property(p => p.CardBrand).HasMaxLength(20);
            builder.Property(p => p.CardLast4).HasMaxLength(4);

            builder.HasIndex(p => p.GatewayReference)
                   .IsUnique()
                   .HasFilter("[GatewayReference] IS NOT NULL");
            //---------------
        }
    }
}

