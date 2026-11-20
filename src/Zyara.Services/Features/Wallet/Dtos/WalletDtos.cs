using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;
using Zyara.Services.Common;

namespace Zyara.Services.Features.Wallet.Dtos;

public sealed class WalletTransactionDto
{
    public int Id { get; set; }
    public int? BookingId { get; set; }
    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }          // signed
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }      // UTC
}

public sealed class WalletPageDto
{
    public decimal Balance { get; set; }
    public decimal TotalEarned { get; set; }     // sum of online earnings
    public decimal TotalWithdrawn { get; set; }
    public decimal MinimumWithdrawal { get; set; }
    public PagedResult<WalletTransactionDto> Transactions { get; set; } = null!;
}

public sealed class WithdrawRequestDto
{
    [Required(ErrorMessage = "Enter the amount.")]
    [Range(0.01, 1_000_000, ErrorMessage = "Enter a valid amount.")]
    public decimal Amount { get; set; }
}
