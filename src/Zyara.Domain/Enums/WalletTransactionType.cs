namespace Zyara.Domain.Enums
{
    public enum WalletTransactionType
    {
        OnlineEarning = 0,   // + provider's share of an online payment (price - service fee - commission)
        CashCommission = 1,  // - provider collected cash, so he owes the platform fee + commission
        Withdrawal = 2,      // - the provider asked to take money out
        CancellationCompensation = 3, // + a patient cancelled a confirmed booking less than 1 hour before the visit
        CancellationPenalty = 4,      // - the provider cancelled a confirmed booking on the day of the visit
        PenaltyCollection = 5         // - the provider collected an earlier cancellation penalty from the patient (it belongs to the platform)
    }
}
