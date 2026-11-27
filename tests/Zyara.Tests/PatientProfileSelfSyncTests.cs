using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Patients;
using Zyara.Services.Features.Patients.Dtos;
using Xunit;

namespace Zyara.Tests;

public class PatientProfileSelfSyncTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ZyaraDbContext _db;
    private readonly PatientProfileService _profileService;

    public PatientProfileSelfSyncTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ZyaraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ZyaraDbContext(options);
        _db.Database.EnsureCreated();

        // Seed patient
        var patient = new Patient
        {
            Id = 1,
            UserName = "patient@test.com",
            Email = "patient@test.com",
            FullName = "Original Name",
            PhoneNumber = "01011112222",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1990, 1, 1),
            Role = UserRole.Patient
        };
        _db.Patients.Add(patient);

        // Seed Self CareRecipient
        var selfCr = new CareRecipient
        {
            Id = 50,
            PatientId = 1,
            Slot = 1,
            FullName = "Original Name",
            PhoneNumber = "01011112222",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1990, 1, 1),
            Relationship = Relationship.Self,
            BloodType = BloodType.OPositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        };
        _db.CareRecipients.Add(selfCr);
        _db.SaveChanges();

        var pRepo = new EfRepository<Patient>(_db);
        var aRepo = new EfRepository<Address>(_db);
        var bRepo = new EfRepository<Booking>(_db);
        var rRepo = new EfRepository<Review>(_db);
        var crRepo = new EfRepository<CareRecipient>(_db);
        var uow = new EfUnitOfWork(_db);

        UserManager<User> userMgrStub = null!;
        ISessionService sessionStub = null!;

        _profileService = new PatientProfileService(
            pRepo, aRepo, bRepo, rRepo, crRepo, uow, userMgrStub, sessionStub, TimeProvider.System);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Updating_Profile_Syncs_Self_Care_Recipient_In_Same_Transaction()
    {
        var editDto = new EditPatientProfileDto
        {
            FullName = "Updated Name",
            PhoneNumber = "01099998888",
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(1992, 2, 2)
        };

        var result = await _profileService.UpdateAsync(1, editDto);
        Assert.True(result.Succeeded);

        // Check account holder
        var updatedPatient = await _db.Patients.FindAsync(1);
        Assert.NotNull(updatedPatient);
        Assert.Equal("Updated Name", updatedPatient.FullName);
        Assert.Equal("01099998888", updatedPatient.PhoneNumber);
        Assert.Equal(Gender.Female, updatedPatient.Gender);
        Assert.Equal(new DateOnly(1992, 2, 2), updatedPatient.DateOfBirth);

        // Check Self CareRecipient is synced
        var updatedCr = await _db.CareRecipients.FindAsync(50);
        Assert.NotNull(updatedCr);
        Assert.Equal("Updated Name", updatedCr.FullName);
        Assert.Equal("01099998888", updatedCr.PhoneNumber);
        Assert.Equal(Gender.Female, updatedCr.Gender);
        Assert.Equal(new DateOnly(1992, 2, 2), updatedCr.DateOfBirth);
    }
}

