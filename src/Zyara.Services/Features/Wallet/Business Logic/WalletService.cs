using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Wallet.Dtos;
using Zyara.Services.Features.Wallet.Interfaces;

namespace Zyara.Services.Features.Wallet;

public sealed class WalletService : IWalletService
{
    public const decimal MinimumWithdrawal = 1m;

    private readonly IRepository<WalletTransaction> _transactions;
    private readonly IRepository<Provider> _providers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public WalletService(
        IRepository<WalletTransaction> transactions,
        IRepository<Provider> providers,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _transactions = transactions;
        _providers = providers;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    // ───────────────────────── booking settlement ─────────────────────────

    public decimal RecordBookingSettlement(Booking booking)
    {
        var payment = booking.Payment;
        if (payment is null) return 0m;

        WalletTransactionType type;
        decimal amount;
        string description;

        if (payment is { Method: PaymentMethod.OnlinePayment, Status: PaymentStatus.Captured })
        {
            // the patient paid the platform: the provider gets price - platform fee - commission
            type = WalletTransactionType.OnlineEarning;
            amount = booking.TotalPrice - booking.ServiceFee - booking.Commission;
            description = $"Booking #{booking.Id}: online payment (your share after commission)";
        }
        else if (payment is { Method: PaymentMethod.CashOnArrival, Status: PaymentStatus.Collected })
        {
            // the provider kept the cash: he owes the platform its fee + commission
            type = WalletTransactionType.CashCommission;
            amount = -(booking.ServiceFee + booking.Commission);
            description = $"Booking #{booking.Id}: cash collected (platform fee + commission)";
        }
        else
        {
            return 0m;
        }

        if (amount == 0m) return 0m;

        _transactions.Add(new WalletTransaction
        {
            ProviderId = booking.ProviderId,
            BookingId = booking.Id,
            Type = type,
            Amount = amount,
            Description = description,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        });

        return amount;
    }

    public Task ApplyBalanceDeltaAsync(int providerId, decimal delta, CancellationToken cancellationToken = default) =>
        _providers.Query()
            .Where(p => p.Id == providerId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.WalletBalance, p => p.WalletBalance + delta), cancellationToken);

    // ───────────────────────── wallet page ─────────────────────────

    public async Task<WalletPageDto> GetWalletAsync(int providerId, int page, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var balance = await _providers.Query()
            .AsNoTracking()
            .Where(p => p.Id == providerId)
            .Select(p => p.WalletBalance)
            .FirstOrDefaultAsync(cancellationToken);

        var mine = _transactions.Query().AsNoTracking().Where(t => t.ProviderId == providerId);

        var earned = await mine.Where(t => t.Type == WalletTransactionType.OnlineEarning).SumAsync(t => t.Amount, cancellationToken);
        var withdrawn = await mine.Where(t => t.Type == WalletTransactionType.Withdrawal).SumAsync(t => -t.Amount, cancellationToken);

        var transactions = await mine
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToPagedAsync(
                t => new WalletTransactionDto
                {
                    Id = t.Id,
                    BookingId = t.BookingId,
                    Type = t.Type,
                    Amount = t.Amount,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                },
                page, pageSize, maxPageSize: 50, cancellationToken: cancellationToken);

        return new WalletPageDto
        {
            Balance = balance,
            TotalEarned = earned,
            TotalWithdrawn = withdrawn,
            MinimumWithdrawal = MinimumWithdrawal,
            Transactions = transactions
        };
    }

    // ───────────────────────── withdrawal ─────────────────────────

    public async Task<ServiceResult> RequestWithdrawalAsync(int providerId, decimal amount, CancellationToken cancellationToken = default)
    {
        amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (amount < MinimumWithdrawal)
            return ServiceResult.Fail($"The minimum withdrawal is {MinimumWithdrawal:0.##} EGP.");

        var done = false;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // One atomic statement: it only takes the money if the balance still covers it,
            // so two quick clicks can never overdraw the wallet.
            var rows = await _providers.Query()
                .Where(p => p.Id == providerId && p.WalletBalance >= amount)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.WalletBalance, p => p.WalletBalance - amount), ct);

            if (rows == 0) return;

            _transactions.Add(new WalletTransaction
            {
                ProviderId = providerId,
                Type = WalletTransactionType.Withdrawal,
                Amount = -amount,
                Description = "Withdrawal request",
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            });

            await _unitOfWork.SaveChangesAsync(ct);
            done = true;
        }, cancellationToken);

        return done
            ? ServiceResult.Ok()
            : ServiceResult.Fail("Your balance is not enough for this withdrawal.");
    }
}
