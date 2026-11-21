using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Repositories.Data.Configurations
{
    internal class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.ToTable("WalletTransactions");
            builder.HasKey(w => w.Id);

            builder.Property(w => w.Amount).HasColumnType("decimal(18,2)");

            builder.Property(w => w.Type)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(w => w.Description)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(w => w.Provider)
                .WithMany()
                .HasForeignKey(w => w.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(w => w.Booking)
                .WithMany()
                .HasForeignKey(w => w.BookingId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // A booking can be settled into the wallet only ONCE (protects against double credit).
            builder.HasIndex(w => w.BookingId)
                .IsUnique()
                .HasFilter("[BookingId] IS NOT NULL");

            builder.HasIndex(w => new { w.ProviderId, w.CreatedAt });
        }
    }
}
