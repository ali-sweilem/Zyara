using System;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;

namespace Zyara.Domain.Entities
{
    internal class Booking
    {
        public int Id { get; set; }
        public BookingStatus Status { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Commission { get; set; }
        public string OtpCode { get; set; } = string.Empty;

        // FKs
        public int PatientId { get; set; }
        public int ProviderId { get; set; }
        public int ServiceId { get; set; }
        public int TimeSlotId { get; set; }

        // Navigation properties
        public Patient? Patient { get; set; }
        // TODO(M2): change to Provider once the class exists
        public User? Provider { get; set; }
        public Service? Service { get; set; }
        public TimeSlot? TimeSlot { get; set; }
        public Payment? Payment { get; set; }

        public Rating? Rating { get; set; }
        // TODO(M4): Add navigation to ServiceNotes (1:0..*) once ServiceNote entity is created.
        // TODO(M4): Add navigation to Prescription (1:0..1) once Prescription entity is created.

        // TODO(M3-or-service-layer): When creating a booking, the creating service must lock
        //   the TimeSlot (Available -> Locked) before persisting. Booking itself does not do it.
        // TODO(M4): OtpCode generation is not specified. The property is left settable.
        //   The creating service or a separate service must generate and assign it.

        public void Accept()
        {
            if (Status != BookingStatus.PendingProviderApproval)
                throw new InvalidOperationException(
                    $"Cannot accept a booking that is in status {Status}. Expected {BookingStatus.PendingProviderApproval}.");

            if (TimeSlot == null)
                throw new InvalidOperationException("Cannot accept booking: TimeSlot navigation property is not loaded.");

            if (TimeSlot.Status != TimeSlotStatus.Locked)
                throw new InvalidOperationException(
                    $"Cannot accept booking: TimeSlot must be {TimeSlotStatus.Locked} but is {TimeSlot.Status}.");

            if (Payment == null)
                throw new InvalidOperationException("Cannot accept booking: Payment navigation property is not loaded.");

            if (Payment.Method == PaymentMethod.OnlinePayment)
            {
                if (Payment.Status != PaymentStatus.Authorized)
                    throw new InvalidOperationException(
                        $"Cannot accept online booking: Payment must be {PaymentStatus.Authorized} but is {Payment.Status}.");

                Payment.Capture();
                Status = BookingStatus.Confirmed;
            }
            else
            {
                Status = BookingStatus.ConfirmedCollectionPending;
            }

            TimeSlot.Status = TimeSlotStatus.Booked;
        }

        public void Reject()
        {
            if (Status != BookingStatus.PendingProviderApproval)
                throw new InvalidOperationException(
                    $"Cannot reject a booking that is in status {Status}. Expected {BookingStatus.PendingProviderApproval}.");

            if (TimeSlot == null)
                throw new InvalidOperationException("Cannot reject booking: TimeSlot navigation property is not loaded.");

            if (TimeSlot.Status != TimeSlotStatus.Locked)
                throw new InvalidOperationException(
                    $"Cannot reject booking: TimeSlot must be {TimeSlotStatus.Locked} but is {TimeSlot.Status}.");

            if (Payment == null)
                throw new InvalidOperationException("Cannot reject booking: Payment navigation property is not loaded.");

            if (Payment.Status != PaymentStatus.Authorized && Payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException(
                    $"Cannot reject booking: Payment is in unexpected status {Payment.Status}.");

            Status = BookingStatus.Rejected;
            TimeSlot.Status = TimeSlotStatus.Available;

            if (Payment.Status == PaymentStatus.Authorized)
                Payment.Release();
        }

        public void Cancel(DateTime nowUtc)
        {
            if (Status != BookingStatus.PendingProviderApproval &&
                Status != BookingStatus.Confirmed &&
                Status != BookingStatus.ConfirmedCollectionPending)
                throw new InvalidOperationException(
                    $"Cannot cancel a booking that is in status {Status}.");

            if (TimeSlot == null)
                throw new InvalidOperationException("Cannot cancel booking: TimeSlot navigation property is not loaded.");

            var visitStart = TimeSlot.Date.Date + TimeSlot.StartTime;
            if ((visitStart - nowUtc).TotalHours < 24)
                throw new InvalidOperationException(
                    "Cancellation is only allowed 24 or more hours before the visit start time.");

            if (TimeSlot.Status != TimeSlotStatus.Locked && TimeSlot.Status != TimeSlotStatus.Booked)
                throw new InvalidOperationException(
                    $"Cannot cancel booking: TimeSlot is in unexpected status {TimeSlot.Status}.");

            if (Payment == null)
                throw new InvalidOperationException("Cannot cancel booking: Payment navigation property is not loaded.");

            if (Payment.Status != PaymentStatus.Authorized &&
                Payment.Status != PaymentStatus.Captured &&
                Payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException(
                    $"Cannot cancel booking: Payment is in unexpected status {Payment.Status}.");

            Status = BookingStatus.Cancelled;
            TimeSlot.Status = TimeSlotStatus.Available;

            switch (Payment.Status)
            {
                case PaymentStatus.Authorized:
                    Payment.Release();
                    break;
                case PaymentStatus.Captured:
                    // TODO(M4): Cancellation fee rule is not specified. Performing full refund for now.
                    Payment.Refund(Payment.Amount);
                    break;
                case PaymentStatus.Pending:
                    // TODO(M4): Cash payment (Pending) cancellation status is not specified. Left unchanged.
                    break;
            }
        }

        public void Reschedule(TimeSlot newSlot, DateTime nowUtc)
        {
            if (newSlot == null)
                throw new ArgumentNullException(nameof(newSlot), "New time slot cannot be null.");

            if (TimeSlot == null)
                throw new InvalidOperationException("Cannot reschedule booking: TimeSlot navigation property is not loaded.");

            if (newSlot.Id == TimeSlot.Id)
                throw new InvalidOperationException("Cannot reschedule to the same time slot.");

            if (newSlot.ProviderId != 0 && newSlot.ProviderId != ProviderId)
                throw new InvalidOperationException(
                    $"Cannot reschedule: new TimeSlot belongs to a different provider.");

            var visitStart = TimeSlot.Date.Date + TimeSlot.StartTime;
            if ((visitStart - nowUtc).TotalHours < 24)
                throw new InvalidOperationException(
                    "Rescheduling is only allowed 24 or more hours before the visit start time.");

            if (TimeSlot.Status != TimeSlotStatus.Booked)
                throw new InvalidOperationException(
                    $"Cannot reschedule: current TimeSlot must be {TimeSlotStatus.Booked} but is {TimeSlot.Status}.");

            if (newSlot.Status != TimeSlotStatus.Available)
                throw new InvalidOperationException(
                    $"Cannot reschedule: new TimeSlot must be {TimeSlotStatus.Available} but is {newSlot.Status}.");

            TimeSlot.Status = TimeSlotStatus.Available;
            newSlot.Status = TimeSlotStatus.Booked;
            TimeSlotId = newSlot.Id;
            TimeSlot = newSlot;

            // TODO(M4 open-point-3): Resulting booking status after reschedule is not specified. Status left unchanged.
        }

        public void Start(string otp)
        {
            if (Status != BookingStatus.Confirmed && Status != BookingStatus.ConfirmedCollectionPending)
                throw new InvalidOperationException(
                    $"Cannot start a booking that is in status {Status}.");

            if (Service == null)
                throw new InvalidOperationException("Cannot start booking: Service navigation property is not loaded.");

            bool isHomeVisit = Service.Type == ServiceType.DoctorHomeVisit ||
                               Service.Type == ServiceType.NurseHomeVisit;

            if (isHomeVisit)
            {
                if (string.IsNullOrEmpty(otp))
                    throw new InvalidOperationException("OTP code is required for home visits.");
                if (otp != OtpCode)
                    throw new InvalidOperationException("OTP code does not match.");
            }

            Status = BookingStatus.Started;
        }

        // Complete() calculates Commission using CommissionCalculator and the rate passed in.
        public void Complete(decimal commissionRate)
        {
            if (Status != BookingStatus.Started)
                throw new InvalidOperationException(
                    $"Cannot complete a booking that is in status {Status}. Expected {BookingStatus.Started}.");

            Commission = CommissionCalculator.Calculate(TotalPrice, commissionRate);

            Status = BookingStatus.Completed;

            if (Payment?.Method == PaymentMethod.CashOnArrival &&
                Payment?.Status == PaymentStatus.Pending)
                Payment.Collect();
        }
    }
}
