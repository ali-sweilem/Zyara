using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;
namespace Zyara.Services.Data.Configurations
{
    internal class ProviderCredentialConfiguration :IEntityTypeConfiguration<ProviderCredential>
    {
        public void Configure(EntityTypeBuilder<ProviderCredential> builder)
        {
            builder.HasKey(d => d.Id);
          builder.Property(d=>d.DocumentType)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(d => d.FileUrl)
                .IsRequired()
                .HasMaxLength(200);
            builder.Property(d => d.ProviderID)
                .IsRequired(false);
            builder.Property(d=>d.Provider)
                .IsRequired(false);
            //relationships
            builder.HasOne(d => d.Provider)
                .WithMany(d => d.ProviderCredentials)
                .HasForeignKey(d => d.ProviderID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    
    }
}
