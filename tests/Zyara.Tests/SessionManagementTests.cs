using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Administration;
using Zyara.Services.Features.Identity;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Tests;

public class SessionManagementTests
{
    private (ZyaraDbContext Db, SqliteConnection Connection) CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = OFF;";
        cmd.ExecuteNonQuery();

        var options = new DbContextOptionsBuilder<ZyaraDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ZyaraDbContext(options);
        db.Database.EnsureCreated();

        return (db, connection);
    }

    [Fact]
    public void CookieRefresh_configures_security_stamp_validation_interval_to_one_minute()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddZyaraCookieRefresh();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SecurityStampValidatorOptions>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(1), options.ValidationInterval);
    }

    [Fact]
    public async Task Admin_deactivating_user_triggers_session_invalidation()
    {
        var (db, connection) = CreateInMemoryDbContext();
        using (connection)
        using (db)
        {
            var user = new Patient
            {
                Id = 101,
                UserName = "patient@zyara.com",
                Email = "patient@zyara.com",
                PhoneNumber = "01012345678",
                FullName = "Test Patient",
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var userRepo = new EfRepository<User>(db);
            var uow = new EfUnitOfWork(db);
            var fakeSessionService = new FakeSessionService();

            var adminUserService = new AdminUserService(userRepo, uow, fakeSessionService);

            // Act: Toggle active -> deactivates user
            var result = await adminUserService.ToggleUserActiveAsync(101);

            // Assert
            Assert.True(result);
            Assert.False(user.IsActive);
            Assert.Contains(101, fakeSessionService.InvalidatedUserIds);
        }
    }

    [Fact]
    public async Task Admin_activating_inactive_user_does_not_call_session_invalidation()
    {
        var (db, connection) = CreateInMemoryDbContext();
        using (connection)
        using (db)
        {
            var user = new Patient
            {
                Id = 102,
                UserName = "inactive@zyara.com",
                Email = "inactive@zyara.com",
                PhoneNumber = "01012345679",
                FullName = "Inactive Patient",
                IsActive = false
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var userRepo = new EfRepository<User>(db);
            var uow = new EfUnitOfWork(db);
            var fakeSessionService = new FakeSessionService();

            var adminUserService = new AdminUserService(userRepo, uow, fakeSessionService);

            // Act: Toggle active -> activates user
            var result = await adminUserService.ToggleUserActiveAsync(102);

            // Assert
            Assert.True(result);
            Assert.True(user.IsActive);
            Assert.Empty(fakeSessionService.InvalidatedUserIds);
        }
    }

    private sealed class FakeSessionService : ISessionService
    {
        public List<int> InvalidatedUserIds { get; } = new();
        public List<User> RefreshedUsers { get; } = new();

        public Task RefreshAsync(User user)
        {
            RefreshedUsers.Add(user);
            return Task.CompletedTask;
        }

        public Task InvalidateAsync(int userId, CancellationToken cancellationToken = default)
        {
            InvalidatedUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }
}
