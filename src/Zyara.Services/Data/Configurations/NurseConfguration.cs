using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data.Configurations
{
    internal class NurseConfguration : IEntityTypeConfiguration<Nurse>
    {
        public void Configure(EntityTypeBuilder<Nurse> builder)
        {
            // Inherits from Provider under TPH (shared properties are configured in User and Provider configurations)
            builder.HasBaseType<Provider>();
        }
    }
}

