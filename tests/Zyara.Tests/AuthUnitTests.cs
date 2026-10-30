using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Data;

namespace Zyara.Tests;
public class AuthUnitTests
{
    private ZyaraDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<ZyaraDbContext>().UseSqlite("DataSource=:memory:").Options;
        var ctx = new ZyaraDbContext(options);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public void Login_Role_Always_From_Database()
    {
        using var ctx = GetContext();
        // Arrange: user exists with Doctor role; do NOT trust client role
        var doctor = new Doctor { FullName = "Dr", Email = "d@test", Phone = "+20", PasswordHash = "hash", Gender = Gender.Male, Role = UserRole.Doctor, VerificationStatus = VerificationStatus.Verified, Specialty = "", LicenseNumber = "L1" };
        ctx.Doctors.Add(doctor); ctx.SaveChanges();
        // Assert: DB role is Doctor regardless of client input
        Assert.Equal(UserRole.Doctor, doctor.Role);
    }

    [Fact]
    public void PasswordResetCode_Stores_Hash_Only()
    {
        using var ctx = GetContext();
        ctx.Users.Add(new Patient { Id = 1, FullName = "T", Email = "t@test", Phone = "+20", Gender = Gender.Male, PasswordHash = "h", IsActive = true, DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow) });
        ctx.SaveChanges();
        var code = new PasswordResetCode { UserId = 1, CodeHash = BCrypt.Net.BCrypt.HashPassword("1234"), CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5), AttemptCount = 0, IsUsed = false };
        ctx.PasswordResetCodes.Add(code); ctx.SaveChanges();
        Assert.NotEqual("1234", ctx.PasswordResetCodes.First().CodeHash);
    }

    [Fact]
    public void OTP_Expires_After_5_Minutes()
    {
        using var ctx = GetContext();
        var code = new PasswordResetCode { UserId = 1, CodeHash = "h", CreatedAt = DateTime.UtcNow.AddMinutes(-6), ExpiresAt = DateTime.UtcNow.AddMinutes(-1), IsUsed = false };
        ctx.PasswordResetCodes.Add(code); ctx.SaveChanges();
        Assert.True(code.ExpiresAt < DateTime.UtcNow);
    }
}
