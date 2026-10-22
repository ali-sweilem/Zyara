using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Entities;
using Zyara.Services.Data.Configurations;

namespace Zyara.Services.Data
{
    public class ZyaraDbContext(DbContextOptions<ZyaraDbContext> options) : DbContext(options)
    {
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Nurse> Nurses { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<ProviderCredential> ProviderCredentials { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ProviderService> ProviderServices { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Already configured via DI / constructor
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new DoctorConfiguration());
            modelBuilder.ApplyConfiguration(new NurseConfguration());
            modelBuilder.ApplyConfiguration(new ProviderConfiguration());
            modelBuilder.ApplyConfiguration(new ProviderCredentialConfiguration());
            modelBuilder.ApplyConfiguration(new BookingConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceConfiguration());
            modelBuilder.ApplyConfiguration(new ProviderServiceConfiguration());
            modelBuilder.ApplyConfiguration(new TimeSlotConfiguration());
        }
    }
}