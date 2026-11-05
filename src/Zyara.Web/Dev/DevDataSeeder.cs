// TEMP-SEED: delete this file and the Program.cs call after database is seeded
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace Zyara.Web.Dev;

public static class DevDataSeeder
{
    public static void Seed(ZyaraDbContext db)
    {
        if (db.Bookings.Any()) return; // idempotent

        // Platform settings
        db.PlatformSettings.Add(new PlatformSettings { Id = 1, ServiceFee = 50m, CommissionPercentage = 15m });

        // Services
        db.Services.AddRange(
            new Service { Name = "Doctor Home Visit", Type = ServiceType.DoctorHomeVisit, IsActive = true },
            new Service { Name = "Nurse Home Visit", Type = ServiceType.NurseHomeVisit, IsActive = true },
            new Service { Name = "Remote Consultation", Type = ServiceType.RemoteConsultation, IsActive = true }
        );

        // Doctor (verified)
        var doctor = new Doctor
        {
            FullName = "Dr. Dev User",
            Email = "dev.doctor@zyara.test",
            Phone = "0123456789",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123"),
            Gender = Gender.Male,
            Role = UserRole.Doctor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Specialty = "General",
            ExperienceYears = 5,
            licenser = "LIC-001",
            BaseCity = "Cairo",
            VerificationStatus = VerificationStatus.Verified,
            VerifiedAt = DateTime.UtcNow
        };
        db.Doctors.Add(doctor);
        db.SaveChanges();

        // Provider services
        var services = db.Services.ToList();
        db.ProviderServices.AddRange(
            new ProviderService { ProviderId = doctor.Id, ServiceId = services[0].Id, Fee = 200m, CommissionPercentage = 15m },
            new ProviderService { ProviderId = doctor.Id, ServiceId = services[1].Id, Fee = 150m, CommissionPercentage = 15m },
            new ProviderService { ProviderId = doctor.Id, ServiceId = services[2].Id, Fee = 100m, CommissionPercentage = 15m }
        );

        // Patients + addresses
        var p1 = new Patient { FullName = "Patient One", Email = "p1@test.com", Phone = "111", PasswordHash = "x", Role = UserRole.Patient, IsActive = true, CreatedAt = DateTime.UtcNow, Gender = Gender.Female };
        var p2 = new Patient { FullName = "Patient Two", Email = "p2@test.com", Phone = "222", PasswordHash = "x", Role = UserRole.Patient, IsActive = true, CreatedAt = DateTime.UtcNow, Gender = Gender.Male };
        db.Patients.AddRange(p1, p2);
        db.SaveChanges();
        db.Addresses.AddRange(
            new Address { PatientId = p1.Id, Label = "Home", City = "Cairo", District = "Nasr", StreetAndBuilding = "123 St" },
            new Address { PatientId = p2.Id, Label = "Home", City = "Cairo", District = "Heliopolis", StreetAndBuilding = "456 St" }
        );

        // TimeSlots
        var now = DateTime.Now;
        db.TimeSlots.AddRange(
            new TimeSlot { ProviderId = doctor.Id, Date = now.AddDays(3), StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 0, 0), Status = TimeSlotStatus.Locked },
            new TimeSlot { ProviderId = doctor.Id, Date = now.AddDays(3).AddHours(2), StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(13, 0, 0), Status = TimeSlotStatus.Locked },
            new TimeSlot { ProviderId = doctor.Id, Date = now.AddHours(12), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0), Status = TimeSlotStatus.Available }
        );
        db.SaveChanges();

        var slots = db.TimeSlots.ToList();

        // Bookings ~8 covering statuses
        db.Bookings.Add(new Booking
        {
            Status = BookingStatus.PendingProviderApproval,
            ServiceFee = 50m, TotalPrice = 200m, Commission = 0m, OtpCode = "123456",
            PatientId = p1.Id, ProviderId = doctor.Id, ServiceId = services[0].Id, TimeSlotId = slots[0].Id,
            Payment = new Payment { Method = PaymentMethod.OnlinePayment, Status = PaymentStatus.Authorized, Amount = 200m, BookingId = 0 }
        });
        // Note: Payment.BookingId set to 0 initially; after booking save, update (simplified seed)
        db.SaveChanges();

        // Update payment FKs after bookings saved (simplified)
        var bookings = db.Bookings.ToList();
        if (bookings.Count > 0)
        {
            var b0 = bookings[0];
            if (b0.Payment != null)
                b0.Payment.BookingId = b0.Id;
        }

        db.Bookings.AddRange(
            new Booking { Status = BookingStatus.Confirmed, ServiceFee = 50m, TotalPrice = 150m, Commission = 0m, PatientId = p2.Id, ProviderId = doctor.Id, ServiceId = services[1].Id, TimeSlotId = slots[1].Id, Payment = new Payment { Method = PaymentMethod.CashOnArrival, Status = PaymentStatus.Pending, Amount = 150m } },
            new Booking { Status = BookingStatus.ConfirmedCollectionPending, ServiceFee = 50m, TotalPrice = 200m, Commission = 0m, PatientId = p1.Id, ProviderId = doctor.Id, ServiceId = services[0].Id, TimeSlotId = slots[0].Id, Payment = new Payment { Method = PaymentMethod.CashOnArrival, Status = PaymentStatus.Pending, Amount = 200m } },
            new Booking { Status = BookingStatus.Started, ServiceFee = 50m, TotalPrice = 200m, Commission = 0m, OtpCode = "123456", PatientId = p1.Id, ProviderId = doctor.Id, ServiceId = services[0].Id, TimeSlotId = slots[0].Id, Payment = new Payment { Method = PaymentMethod.CashOnArrival, Status = PaymentStatus.Pending, Amount = 200m } },
            new Booking { Status = BookingStatus.Completed, ServiceFee = 50m, TotalPrice = 200m, Commission = 30m, PatientId = p2.Id, ProviderId = doctor.Id, ServiceId = services[2].Id, TimeSlotId = slots[1].Id, Payment = new Payment { Method = PaymentMethod.CashOnArrival, Status = PaymentStatus.Collected, Amount = 200m } },
            new Booking { Status = BookingStatus.Cancelled, ServiceFee = 50m, TotalPrice = 150m, Commission = 0m, PatientId = p1.Id, ProviderId = doctor.Id, ServiceId = services[1].Id, TimeSlotId = slots[1].Id, Payment = new Payment { Method = PaymentMethod.OnlinePayment, Status = PaymentStatus.Released, Amount = 150m } },
            new Booking { Status = BookingStatus.Rejected, ServiceFee = 50m, TotalPrice = 150m, Commission = 0m, PatientId = p2.Id, ProviderId = doctor.Id, ServiceId = services[2].Id, TimeSlotId = slots[0].Id, Payment = new Payment { Method = PaymentMethod.CashOnArrival, Status = PaymentStatus.Pending, Amount = 150m } },
            new Booking { Status = BookingStatus.Expired, ServiceFee = 50m, TotalPrice = 100m, Commission = 0m, PatientId = p1.Id, ProviderId = doctor.Id, ServiceId = services[2].Id, TimeSlotId = slots[2].Id, Payment = new Payment { Method = PaymentMethod.CashOnArrival, Status = PaymentStatus.Pending, Amount = 100m } }
        );
        db.SaveChanges();
    }
}
