using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;
namespace Zyara.Services.Data.Configurations
{
    internal class ProviderConfiguration:IEntityTypeConfiguration<Provider>
    {
        public void Configure(EntityTypeBuilder<Provider> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.licenser)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(d => d.Name)
                .IsRequired(true)
                .HasMaxLength(100);
            builder.Property(d => d.Email)
                .IsRequired(false)
                .HasMaxLength(100);
            builder.Property(d => d.PhoneNumber)
                .IsRequired(true)
                .HasMaxLength(11);
            builder.Property(d => d.Specilty)
                .IsRequired(true)
                .HasMaxLength(100);
            builder.Property(d => d.experienceYears)
                .IsRequired(true);
            builder.Property(d => d.VerificationStatus)
                .IsRequired(true);
            builder.Property(d => d.averageRating)
                .IsRequired(true)
                .HasColumnType("decimal(18,2)");
            //relationships
            builder.HasMany(d => d.Booking)
                .WithOne(d => d.Provider)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(d => d.TimeSlots)
                .WithOne(d => d.Provider)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(d => d.ProviderCredentials)
                .WithOne(d => d.Provider)
                .HasForeignKey(d => d.ProviderID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Admin)
                .WithMany(d => d.Providers)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
