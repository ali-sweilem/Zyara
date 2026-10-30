using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using BCrypt.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Zyara.Services.Seeders;

public static class AdminSeeder
{
    public static void EnsureAdmin(ZyaraDbContext db, IConfiguration config, IHostEnvironment env)
    {
        if (!env.IsDevelopment()) return;
        var adminEmail = config["Seed:AdminEmail"] ?? "admin@zyara.health";
        var adminPassword = config["Seed:AdminPassword"] ?? "ZyaraDev2026!";
        if (!db.Admins.Any(a => a.Email == adminEmail))
        {
            db.Admins.Add(new Admin
            {
                FullName = "Admin",
                Email = adminEmail,
                Phone = "+20 100 000 0000",
                Gender = Gender.Male,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = UserRole.Admin,
                IsActive = true
            });
            db.SaveChanges();
        }
    }
}
