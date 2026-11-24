namespace Zyara.Domain.Enums
{
    public enum NotificationType
    {
        General = 0,
        BookingRequested = 1,   // -> provider: a patient booked
        BookingAccepted = 2,    // -> patient
        BookingRejected = 3,    // -> patient
        BookingCancelled = 4,   // -> provider: the patient cancelled
        BookingExpired = 5,     // -> patient
        VisitStarted = 6,       // -> patient
        VisitCompleted = 7,     // -> patient
        BookingNoShow = 8       // -> patient and provider: the visit never started
    }
}
