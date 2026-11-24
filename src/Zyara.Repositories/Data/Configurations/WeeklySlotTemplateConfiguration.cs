using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Repositories.Data.Configurations
{
    internal class WeeklySlotTemplateConfiguration : IEntityTypeConfiguration<WeeklySlotTemplate>
    {
        public void Configure(EntityTypeBuilder<WeeklySlotTemplate> builder)
        {
            builder.ToTable("WeeklySlotTemplates");
            builder.HasKey(t => t.Id);

            builder.HasOne(t => t.Provider)
                .WithMany()
                .HasForeignKey(t => t.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // the same provider cannot repeat the same day + start time twice
            builder.HasIndex(t => new { t.ProviderId, t.DayOfWeek, t.StartTime }).IsUnique();
        }
    }
}
