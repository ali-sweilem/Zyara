using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class NurseConfguration : IEntityTypeConfiguration<Nurse>
    {
        public void Configure(EntityTypeBuilder<Nurse> builder)
        {
          builder.HasBaseType<Provider>();
         
            builder.HasKey(n => n.Id);
            builder.Property(n=>n.averageRating)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            builder.Property(n => n.Email)
                .IsRequired();
            builder.Property(n => n.Phone)
                .IsRequired();
            builder.Property(n => n.averageRating)
                .IsRequired();
            builder.Property(n => n.experienceYears)
                .IsRequired();
            builder.Property(n => n.IsActive)
                .IsRequired();
            builder.Property(n => n.VerificationStatus)
                .IsRequired();
            builder.Property(n => n.CreatedAt)
                .IsRequired();
            builder.Property(n=>n.TimeSlots)
                .IsRequired();
        }
    }
}
