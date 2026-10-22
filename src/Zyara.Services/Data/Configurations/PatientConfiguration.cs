using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            // DateOnly? is mapped to a nullable SQL "date" column automatically.
            // Do NOT call ToTable() here: Patient shares the Users table (TPH).

            builder.HasMany(p => p.Addresses)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
