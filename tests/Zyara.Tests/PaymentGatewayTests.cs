using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Services.Features.Payments;

namespace Zyara.Tests
{
    public class PaymentGatewayTests
    {
        private (ZyaraDbContext Db, SqliteConnection Connection) CreateInMemoryDbContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            connection.CreateFunction("SYSUTCDATETIME", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

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
        public async Task AuthorizePayment_ShouldBeIdempotent_WhenCalledMultipleTimes()
        {
            // Arrange
            var (db, connection) = CreateInMemoryDbContext();
            using (connection)
            using (db)
            {
                var service = new PaymentService(db);

                var booking = new Booking
                {
                    Id = 101,
                    Status = BookingStatus.PendingProviderApproval,
                    TotalPrice = 500m
                };
                db.Bookings.Add(booking);

                var payment = new Payment
                {
                    Id = 1,
                    BookingId = 101,
                    Method = PaymentMethod.OnlinePayment,
                    Status = PaymentStatus.Pending,
                    Amount = 500m,
                    GatewayReference = "TEST-REF-12345"
                };
                db.Payments.Add(payment);
                await db.SaveChangesAsync();

                // Act: أول استدعاء (وصول الـ Webhook لأول مرة)
                var firstCallResult = await service.AuthorizePaymentAsync("TEST-REF-12345", "Visa", "4242");

                // Assert
                Assert.True(firstCallResult);
                var updatedPayment = await db.Payments.FindAsync(1);
                Assert.NotNull(updatedPayment);
                Assert.Equal(PaymentStatus.Authorized, updatedPayment.Status);
                Assert.Equal("Visa", updatedPayment.CardBrand);
                Assert.Equal("4242", updatedPayment.CardLast4);

                // Act: استدعاء ثاني لنفس الـ Webhook مكرراً (Idempotency)
                var secondCallResult = await service.AuthorizePaymentAsync("TEST-REF-12345", "Visa", "4242");

                // Assert
                Assert.True(secondCallResult);
                Assert.Equal(PaymentStatus.Authorized, updatedPayment.Status);
            }
        }

        [Fact]
        public void PaymentTransitions_ShouldFollowStrictLifecycle()
        {
            // Arrange
            var payment = new Payment
            {
                Method = PaymentMethod.OnlinePayment,
                Status = PaymentStatus.Pending,
                Amount = 1000m
            };

            // 1. Pending -> Authorized
            payment.Authorize();
            Assert.Equal(PaymentStatus.Authorized, payment.Status);

            // 2. Authorized -> Captured
            payment.Capture();
            Assert.Equal(PaymentStatus.Captured, payment.Status);

            // 3. Captured -> Refunded
            payment.Refund(1000m);
            Assert.Equal(PaymentStatus.Refunded, payment.Status);
            Assert.Equal(1000m, payment.RefundedAmount);
        }

        [Fact]
        public void Authorize_OnCashPayment_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var cashPayment = new Payment
            {
                Method = PaymentMethod.CashOnArrival,
                Status = PaymentStatus.Pending,
                Amount = 300m
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => cashPayment.Authorize());
        }

        [Fact]
        public void Capture_OnPendingPayment_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var payment = new Payment
            {
                Method = PaymentMethod.OnlinePayment,
                Status = PaymentStatus.Pending,
                Amount = 500m
            };

            // Act & Assert: لا يمكن عمل Capture مباشرة قبل التفويض Authorize
            Assert.Throws<InvalidOperationException>(() => payment.Capture());
        }
    }
}
