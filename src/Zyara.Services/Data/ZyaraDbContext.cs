using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Entities;
using Zyara.Services.Data.Configurations;

namespace Zyara.Services.Data
{
    internal class ZyaraDbContext(DbContextOptions<ZyaraDbContext> options) : DbContext(options)
    {
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Nurse> Nurses { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<ProviderCredential> ProviderCredentials { get; set; }
        override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZyaraDbContext).Assembly);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           modelBuilder.ApplyConfiguration(new DoctorConfiguration());
            modelBuilder.ApplyConfiguration(new NurseConfguration());
            modelBuilder.ApplyConfiguration(new ProviderConfiguration());
            modelBuilder.ApplyConfiguration(new ProviderCredentialConfiguration());
        }

        }
    }
