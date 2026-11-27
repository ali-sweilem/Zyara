using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Bookings;
using Zyara.Services.Features.Notifications.Interfaces;
using Zyara.Services.Features.Wallet.Interfaces;
using Xunit;

namespace Zyara.Tests;

public class ProviderBookingPrivacyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ZyaraDbContext _db;
    private readonly ProviderBookingService _service;

    public ProviderBookingPrivacyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ZyaraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ZyaraDbContext(options);
        _db.Database.EnsureCreated();

        // Seed provider, service, patient, care recipient, timeslots, and bookings
        var provider = new Doctor
        {
            Id = 10,
            UserName = "doctor@test.com",
            Email = "doctor@test.com",
            FullName = "Dr. Tamer",
            PhoneNumber = "01055556666",
            Role = UserRole.Doctor
        };
        _db.Doctors.Add(provider);

        var serviceEntity = new Service
        {
            Id = 20,
            Name = "General Consultation",
            Type = ServiceType.DoctorHomeVisit,
            IsActive = true
        };
        _db.Services.Add(serviceEntity);

        var patient = new Patient
        {
            Id = 1,
            UserName = "patient@test.com",
            Email = "patient@test.com",
            FullName = "Hassan Account Holder",
            PhoneNumber = "01011112222",
            Role = UserRole.Patient
        };
        _db.Patients.Add(patient);

        var careRecipient = new CareRecipient
        {
            Id = 100,
            PatientId = 1,
            Slot = 1,
            FullName = "Ali Hassan (Son)",
            PhoneNumber = "01033334444",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(2015, 6, 1),
            Relationship = Relationship.Child,
            BloodType = BloodType.BPositive,
            ChronicConditions = "Type 1 Diabetes",
            Allergies = "Peanuts",
            CurrentMedications = "Insulin",
            Notes = "Needs gentle approach",
            CreatedAt = DateTime.UtcNow
        };
        _db.CareRecipients.Add(careRecipient);

        var slot1 = new TimeSlot
        {
            Id = 301,
            ProviderId = 10,
            Date = DateTime.Today.AddDays(2),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            Status = TimeSlotStatus.Booked
        };
        var slot2 = new TimeSlot
        {
            Id = 302,
            ProviderId = 10,
            Date = DateTime.Today.AddDays(3),
            StartTime = new TimeSpan(14, 0, 0),
            EndTime = new TimeSpan(15, 0, 0),
            Status = TimeSlotStatus.Booked
        };
        _db.TimeSlots.AddRange(slot1, slot2);

        // Booking 1: PendingProviderApproval
        var pendingBooking = new Booking
        {
            Id = 501,
            ProviderId = 10,
            PatientId = 1,
            CareRecipientId = 100,
            ServiceId = 20,
            TimeSlotId = 301,
            Status = BookingStatus.PendingProviderApproval,
            TotalPrice = 500m
        };

        // Booking 2: Confirmed
        var confirmedBooking = new Booking
        {
            Id = 502,
            ProviderId = 10,
            PatientId = 1,
            CareRecipientId = 100,
            ServiceId = 20,
            TimeSlotId = 302,
            Status = BookingStatus.Confirmed,
            TotalPrice = 500m
        };

        _db.Bookings.AddRange(pendingBooking, confirmedBooking);
        _db.SaveChanges();

        var bRepo = new EfRepository<Booking>(_db);
        var uow = new EfUnitOfWork(_db);

        // Stubs for notifications and wallet (not called by GetRequestsAsync)
        INotificationService notifStub = null!;
        IWalletService walletStub = null!;

        _service = new ProviderBookingService(bRepo, uow, TimeProvider.System, notifStub, walletStub);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Pending_Booking_Hides_Patient_Name_And_Phone_But_Reveals_Medical_Details()
    {
        var requests = await _service.GetRequestsAsync(10);

        Assert.Single(requests.Pending);
        var pending = requests.Pending[0];

        // Privacy assertion: name is generic/masked, phone is null
        Assert.Equal("Patient (pending)", pending.PatientName);
        Assert.Null(pending.PatientPhone);

        // Medical details are accessible to provider
        Assert.Equal(BloodType.BPositive, pending.CareRecipientBloodType);
        Assert.Equal("B+", pending.BloodTypeDisplay);
        Assert.Equal("Type 1 Diabetes", pending.CareRecipientChronicConditions);
        Assert.Equal("Peanuts", pending.CareRecipientAllergies);
        Assert.Equal("Insulin", pending.CareRecipientCurrentMedications);
        Assert.Equal(Gender.Male, pending.CareRecipientGender);
        Assert.NotNull(pending.PatientAge);
        Assert.True(pending.PatientAge > 5);
    }

    [Fact]
    public async Task Confirmed_Booking_Reveals_Care_Recipient_Name_And_Phone()
    {
        var requests = await _service.GetRequestsAsync(10);

        Assert.Single(requests.Active);
        var active = requests.Active[0];

        // Confirmed booking reveals care recipient's actual name and phone
        Assert.Equal("Ali Hassan (Son)", active.PatientName);
        Assert.Equal("01033334444", active.PatientPhone);

        // Medical details also visible
        Assert.Equal(BloodType.BPositive, active.CareRecipientBloodType);
        Assert.Equal("Peanuts", active.CareRecipientAllergies);
    }
}
