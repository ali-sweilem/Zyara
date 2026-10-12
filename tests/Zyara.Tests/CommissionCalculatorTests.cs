using Xunit;
using Zyara.Domain.Rules;

namespace Zyara.Tests;

public class CommissionCalculatorTests
{
    [Fact]
    public void Commission_is_15_percent_of_price()
    {
        Assert.Equal(45.00m, CommissionCalculator.Calculate(300m, 0.15m));
    }

    [Fact]
    public void Halves_round_away_from_zero()
    {
        //
        Assert.Equal(1.55m, CommissionCalculator.Calculate(10.30m, 0.15m));
    }

    [Fact]
    public void Negative_price_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommissionCalculator.Calculate(-1m, 0.15m));
    }
}