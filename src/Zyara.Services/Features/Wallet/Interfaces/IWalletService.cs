using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Features.Wallet.Dtos;

namespace Zyara.Services.Features.Wallet.Interfaces;

public interface IWalletService
{
    /// <summary>
    /// Adds the wallet ledger line for a COMPLETED booking whose payment was already settled
    /// (online: Captured -> credit the provider's share; cash: Collected -> debit platform fee + commission).
    /// Does not save. Returns the balance change (0 = nothing to record). The caller must then save and
    /// call <see cref="ApplyBalanceDeltaAsync"/> inside the same transaction.
    /// </summary>
    decimal RecordBookingSettlement(Booking booking);

    /// <summary>
    /// Adds ONE extra ledger line (cancellation compensation / penalty, collected patient penalty). Does not save.
    /// Returns the balance change (= amount); the caller applies it with <see cref="ApplyBalanceDeltaAsync"/> inside the same transaction.
    /// Pass bookingId = null when the booking already has (or will get) its settlement line: the ledger allows only ONE line per booking.
    /// </summary>
    decimal RecordEntry(int providerId, int? bookingId, WalletTransactionType type, decimal amount, string description);

    /// <summary>Atomic, relative balance update in SQL (no lost updates when two bookings finish at once).</summary>
    Task ApplyBalanceDeltaAsync(int providerId, decimal delta, CancellationToken cancellationToken = default);

    Task<WalletPageDto> GetWalletAsync(int providerId, int page, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>Takes the amount out of the balance (never below zero) and logs a Withdrawal line.</summary>
    Task<ServiceResult> RequestWithdrawalAsync(int providerId, decimal amount, CancellationToken cancellationToken = default);
}
