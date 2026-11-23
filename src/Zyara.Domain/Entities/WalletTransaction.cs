using System;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    /// <summary>One line of a provider's wallet ledger. Amount is signed: + credit, - debit.</summary>
    public class WalletTransaction
    {
        public int Id { get; set; }

        public int ProviderId { get; set; }
        public Provider? Provider { get; set; }

        /// <summary>Set for booking settlements (at most one per booking); null for withdrawals.</summary>
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public WalletTransactionType Type { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
