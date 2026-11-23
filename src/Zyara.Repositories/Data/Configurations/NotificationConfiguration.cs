using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Repositories.Data.Configurations
{
    internal class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(40)
                .HasDefaultValue(NotificationType.General);

            builder.Property(n => n.Link)
                .HasMaxLength(300);

            builder.Property(n => n.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // "my notifications, unread first / newest first" + the unread badge count
            builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
        }
    }
}
