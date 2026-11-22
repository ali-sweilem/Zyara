using System;
using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;

namespace Zyara.Domain.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public BookingStatus Status { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Commission { get; set; }
        public string OtpCode { get; set; } = string.Empty;

        public string? Notes { get; set; }
        public string? VisitAddress { get; set; }
        public double? VisitLatitude { get; set; }
        public double? VisitLongitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string? StatusReason { get; set; }
        public int OtpFailedAttempts { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        // FKs
        public int PatientId { get; set; }
        public int ProviderId { get; set; }
        public int ServiceId { get; set; }
        public int TimeSlotId { get; set; }

        // Navigation properties
        public Patient? Patient { get; set; }
        public Provider? Provider { get; set; }
        public Service? Service { get; set; }
        public TimeSlot? TimeSlot { get; set; }
        public Payment? Payment { get; set; }
        public Review? Review { get; set; }

        public bool IsActive =>
            Status != BookingStatus.Cancelled &&
            Status != BookingStatus.Rejected &&
            Status != BookingStatus.Expired;

        public void MarkPaymentAuthorized()
        {
            if (Status != BookingStatus.PendingPayment)
                throw new InvalidOperationException($"Cannot mark payment authorized from status {Status}.");
            Status = BookingStatus.PendingProviderApproval;
        }

        public void Accept(DateTime utcNow)
        {
            if (Status != BookingStatus.PendingProviderApproval)
                throw new InvalidOperationException($"Cannot accept booking from status {Status}.");
            Status = BookingStatus.Confirmed;
            RespondedAt = utcNow;
        }

        public void Reject(string reason, DateTime utcNow)
        {
            if (Status != BookingStatus.PendingProviderApproval)
                throw new InvalidOperationException($"Cannot reject booking from status {Status}.");
            Status = BookingStatus.Rejected;
            StatusReason = reason;
            RespondedAt = utcNow;
        }

        public void Expire(DateTime utcNow)
        {
            if (Status != BookingStatus.PendingPayment && Status != BookingStatus.PendingProviderApproval)
                throw new InvalidOperationException($"Cannot expire booking from status {Status}.");
            Status = BookingStatus.Expired;
            RespondedAt = utcNow;
        }

        public void CancelByProvider(string reason, DateTime utcNow)
        {
            if (Status != BookingStatus.Confirmed)
                throw new InvalidOperationException($"Cannot cancel booking from status {Status}.");
            Status = BookingStatus.Cancelled;
            StatusReason = reason;
        }

        public void Start(DateTime utcNow)
        {
            if (Status != BookingStatus.Confirmed)
                throw new InvalidOperationException($"Cannot start booking from status {Status}.");
            Status = BookingStatus.Started;
            StartedAt = utcNow;
        }

        public void Complete(DateTime utcNow)
        {
            if (Status != BookingStatus.Started)
                throw new InvalidOperationException($"Cannot complete booking from status {Status}.");
            Status = BookingStatus.Completed;
            CompletedAt = utcNow;
        }
    }
}
