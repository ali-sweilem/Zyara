using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;

namespace Zyara.Services.Data
{
    public class ZyaraDbContext(DbContextOptions<ZyaraDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Nurse> Nurses { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<ProviderCredential> ProviderCredentials { get; set; }
        public DbSet<ProviderService> ProviderServices { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<ServiceNote> ServiceNotes { get; set; }
        public DbSet<PlatformSettings> PlatformSettings { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Configured via Dependency Injection
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply all entity configurations defined in this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZyaraDbContext).Assembly);
        }
    }
}