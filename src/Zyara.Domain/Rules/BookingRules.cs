using System;

namespace Zyara.Domain.Rules;

public static class BookingRules
{
    public const int SlotWindowMinutes = 120;
    public const int MinLeadTimeHours = 2;
    public const int BookingHorizonDays = 30;
    public const int OtpMaxFailedAttempts = 5;

    public const int StartVisitEarlyMinutes = 60;
    public const int StartVisitLateMinutes = 120;

    // Default configuration values
    public const int DefaultPendingApprovalExpiryHours = 24;
    public const int DefaultPendingPaymentExpiryMinutes = 15;
    public const bool DefaultExpiryJobEnabled = true;
    public const int DefaultExpiryJobIntervalSeconds = 120;
}

