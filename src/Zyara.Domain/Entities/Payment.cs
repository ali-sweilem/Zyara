using System;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    internal class Payment
    {
        public int Id { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public decimal RefundedAmount { get; set; }

        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        public void Authorize()
        {
            if (Method != PaymentMethod.OnlinePayment)
                throw new InvalidOperationException($"Authorize is only valid for {PaymentMethod.OnlinePayment} payments.");
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException($"Cannot authorize payment from status {Status}");
            Status = PaymentStatus.Authorized;
        }

        public void Capture()
        {
            if (Method != PaymentMethod.OnlinePayment)
                throw new InvalidOperationException($"Capture is only valid for {PaymentMethod.OnlinePayment} payments.");
            if (Status != PaymentStatus.Authorized)
                throw new InvalidOperationException($"Cannot capture payment from status {Status}");
            Status = PaymentStatus.Captured;
        }

        public void Release()
        {
            if (Method != PaymentMethod.OnlinePayment)
                throw new InvalidOperationException($"Release is only valid for {PaymentMethod.OnlinePayment} payments.");
            if (Status != PaymentStatus.Authorized)
                throw new InvalidOperationException($"Cannot release payment from status {Status}");
            Status = PaymentStatus.Released;
        }

        public void Collect()
        {
            if (Method != PaymentMethod.CashOnArrival)
                throw new InvalidOperationException($"Collect is only valid for {PaymentMethod.CashOnArrival} payments.");
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException($"Cannot collect payment from status {Status}");
            Status = PaymentStatus.Collected;
        }

        public void Refund(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Refund amount must be positive");
            if (amount > Amount)
                throw new ArgumentOutOfRangeException(nameof(amount), "Refund amount cannot exceed payment amount");
            if (Status != PaymentStatus.Captured)
                throw new InvalidOperationException($"Cannot refund payment from status {Status}");

            RefundedAmount += amount;
            if (RefundedAmount >= Amount)
                Status = PaymentStatus.Refunded;
            else
                Status = PaymentStatus.PartiallyRefunded;
        }
    }
}
