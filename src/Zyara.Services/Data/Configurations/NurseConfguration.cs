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
            builder.Property(n => n.Id)
                .ValueGeneratedOnAdd();
            builder.Property(n => n.experienceYears)
                .IsRequired();
            builder.Property(n => n.averageRating)
                .IsRequired();
            builder.Property(n => n.Specilty)
                .IsRequired();
            builder.Property(d => d.averageRating)
             .IsRequired(true)
             .HasColumnType("decimal(18,2)");
        }
    }
}
