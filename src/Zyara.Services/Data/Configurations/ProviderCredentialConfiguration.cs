using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class ProviderCredentialConfiguration : IEntityTypeConfiguration<ProviderCredential>
    {
        public void Configure(EntityTypeBuilder<ProviderCredential> builder)
        {
            builder.ToTable("ProviderCredentials");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.DocumentType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.FileUrl)
                .IsRequired()
                .HasMaxLength(300);

            // Relationships
            builder.HasOne(d => d.Provider)
                .WithMany(d => d.ProviderCredentials)
                .HasForeignKey(d => d.ProviderID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

