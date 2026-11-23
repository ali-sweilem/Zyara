namespace Zyara.Domain.Enums
{
    public enum WalletTransactionType
    {
        OnlineEarning = 0,   // + provider's share of an online payment (price - service fee - commission)
        CashCommission = 1,  // - provider collected cash, so he owes the platform fee + commission
        Withdrawal = 2       // - the provider asked to take money out
    }
}
