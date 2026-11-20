using Zyara.Domain.Entities;
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

    /// <summary>Atomic, relative balance update in SQL (no lost updates when two bookings finish at once).</summary>
    Task ApplyBalanceDeltaAsync(int providerId, decimal delta, CancellationToken cancellationToken = default);

    Task<WalletPageDto> GetWalletAsync(int providerId, int page, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>Takes the amount out of the balance (never below zero) and logs a Withdrawal line.</summary>
    Task<ServiceResult> RequestWithdrawalAsync(int providerId, decimal amount, CancellationToken cancellationToken = default);
}
