using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.ToTable("Addresses");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Label).IsRequired().HasMaxLength(50);
            builder.Property(a => a.City).IsRequired().HasMaxLength(100);
            builder.Property(a => a.District).IsRequired().HasMaxLength(100);
            builder.Property(a => a.StreetAndBuilding).IsRequired().HasMaxLength(200);
        }
    }
}
