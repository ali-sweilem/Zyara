namespace Zyara.Domain.Rules;

public static class CommissionCalculator
{
    /// <summary>
    /// (Commision = price * rate)
    /// </summary>
    public static decimal Calculate(decimal price, decimal rate)
    {
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (rate < 0 || rate > 1) throw new ArgumentOutOfRangeException(nameof(rate));
        return Math.Round(price * rate, 2, MidpointRounding.AwayFromZero);
    }
}