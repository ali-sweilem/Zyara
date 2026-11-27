using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Patients;
using Zyara.Services.Features.Patients.Dtos;
using Xunit;

namespace Zyara.Tests;

public class CareRecipientServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ZyaraDbContext _db;
    private readonly CareRecipientService _service;

    public CareRecipientServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ZyaraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ZyaraDbContext(options);
        _db.Database.EnsureCreated();

        // Seed a sample patient (account holder)
        var patient = new Patient
        {
            Id = 1,
            UserName = "patient@test.com",
            Email = "patient@test.com",
            FullName = "Ahmed Account Owner",
            PhoneNumber = "01011112222",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1990, 5, 15),
            Role = UserRole.Patient
        };
        _db.Patients.Add(patient);
        _db.SaveChanges();

        var crRepo = new EfRepository<CareRecipient>(_db);
        var pRepo = new EfRepository<Patient>(_db);
        var uow = new EfUnitOfWork(_db);

        _service = new CareRecipientService(crRepo, pRepo, uow, TimeProvider.System);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Add_Self_Copies_Details_From_Account_Holder()
    {
        var dto = new AddCareRecipientDto
        {
            IsAccountOwner = true,
            BloodType = BloodType.APositive,
            ChronicConditions = "None",
            Allergies = "Penicillin",
            CurrentMedications = "None",
            Notes = "Prefers morning visits"
        };

        var result = await _service.AddAsync(1, dto);

        Assert.True(result.Succeeded);
        Assert.True(result.Value > 0);

        var recipient = await _service.GetAsync(1, result.Value);
        Assert.NotNull(recipient);
        Assert.Equal("Ahmed Account Owner", recipient.FullName);
        Assert.Equal("01011112222", recipient.PhoneNumber);
        Assert.Equal(Gender.Male, recipient.Gender);
        Assert.Equal(Relationship.Self, recipient.Relationship);
        Assert.Equal(BloodType.APositive, recipient.BloodType);
        Assert.Equal("Penicillin", recipient.Allergies);
        Assert.Equal(1, recipient.Slot);
        Assert.True(recipient.IsSelf);
    }

    [Fact]
    public async Task Add_Self_Twice_Is_Rejected()
    {
        var dto = new AddCareRecipientDto
        {
            IsAccountOwner = true,
            BloodType = BloodType.OPositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        };

        var first = await _service.AddAsync(1, dto);
        Assert.True(first.Succeeded);

        var second = await _service.AddAsync(1, dto);
        Assert.False(second.Succeeded);
        Assert.Contains("owner already exists", second.Error);
    }

    [Fact]
    public async Task Add_Up_To_3_Patients_Succeeds_And_Assigns_Lowest_Free_Slot()
    {
        // 1. Add Self (slot 1)
        var res1 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = true,
            BloodType = BloodType.BPositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        });
        Assert.True(res1.Succeeded);

        // 2. Add Child (slot 2)
        var res2 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = false,
            Relationship = Relationship.Child,
            FullName = "Layla Ahmed",
            PhoneNumber = "01011112222",
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(2018, 3, 10),
            BloodType = BloodType.BPositive,
            ChronicConditions = "Asthma",
            Allergies = "None",
            CurrentMedications = "Inhaler"
        });
        Assert.True(res2.Succeeded);

        // 3. Add Spouse (slot 3)
        var res3 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = false,
            Relationship = Relationship.Spouse,
            FullName = "Mona Mohamed",
            PhoneNumber = "01099998888",
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(1992, 8, 20),
            BloodType = BloodType.ONegative,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        });
        Assert.True(res3.Succeeded);

        var list = await _service.ListAsync(1);
        Assert.Equal(3, list.Count);
        Assert.Equal(new[] { 1, 2, 3 }, list.Select(r => r.Slot).ToArray());

        // 4. Adding 4th must fail
        var res4 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = false,
            Relationship = Relationship.Parent,
            FullName = "Hassan Senior",
            PhoneNumber = "01012345678",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1960, 1, 1),
            BloodType = BloodType.ABPositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        });
        Assert.False(res4.Succeeded);
        Assert.Equal("You can add up to 3 patients. Remove one first.", res4.Error);
    }

    [Fact]
    public async Task Delete_Patient_Frees_Up_Slot_For_New_Patient()
    {
        // Add 3
        var r1 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = true,
            BloodType = BloodType.APositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        });
        var r2 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = false,
            Relationship = Relationship.Child,
            FullName = "Kid One",
            PhoneNumber = "01000000000",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(2020, 1, 1),
            BloodType = BloodType.APositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        });
        var r3 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = false,
            Relationship = Relationship.Spouse,
            FullName = "Spouse",
            PhoneNumber = "01000000001",
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(1993, 1, 1),
            BloodType = BloodType.APositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        });

        // Delete Kid One (slot 2)
        var delRes = await _service.DeleteAsync(1, r2.Value);
        Assert.True(delRes.Succeeded);

        // List should show 2 active
        var active = await _service.ListAsync(1);
        Assert.Equal(2, active.Count);

        // Now adding another should reuse lowest free slot (slot 2)
        var r4 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = false,
            Relationship = Relationship.Sibling,
            FullName = "Brother",
            PhoneNumber = "01000000002",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1995, 1, 1),
            BloodType = BloodType.BPositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        });
        Assert.True(r4.Succeeded);

        var newRecipient = await _service.GetAsync(1, r4.Value);
        Assert.NotNull(newRecipient);
        Assert.Equal(2, newRecipient.Slot);
    }

    [Fact]
    public async Task IDOR_Isolation_Patient_Cannot_Access_Others_Care_Recipients()
    {
        // Patient 1 adds a recipient
        var r1 = await _service.AddAsync(1, new AddCareRecipientDto
        {
            IsAccountOwner = true,
            BloodType = BloodType.APositive,
            ChronicConditions = "None",
            Allergies = "None",
            CurrentMedications = "None"
        });

        // Patient 2 tries to get or delete it
        var otherGet = await _service.GetAsync(2, r1.Value);
        Assert.Null(otherGet);

        var otherDel = await _service.DeleteAsync(2, r1.Value);
        Assert.False(otherDel.Succeeded);
        Assert.Equal("Patient not found.", otherDel.Error);
    }
}
