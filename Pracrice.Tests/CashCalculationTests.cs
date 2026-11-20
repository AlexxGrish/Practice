using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Tests;

public sealed class CashCalculationTests
{
    private static CashFlowRow Flow(
        string market = "Магазин",
        int flow = 100,
        int serviceRate = 25,
        int limit = 6,
        bool included = true) =>
        new()
        {
            Market = market,
            Flow = flow,
            ServiceRate = serviceRate,
            Limit = limit,
            IsIncluded = included
        };

    [Fact]
    public void CalculateCash_ReturnsRequiredCashiers()
    {
        CashCalculationResult result = CalculationService.CalculateCash(
            new[] { Flow() },
            targetWaitMinutes: 4m,
            targetLoadPercent: 80m);

        CashResultRow row = Assert.Single(result.Rows);
        Assert.Equal(5, row.Count);
        Assert.Equal(5, result.TotalCashiers);
        Assert.Equal(6, result.TotalAvailable);
        Assert.Contains("80", row.LoadPercent);
    }

    [Fact]
    public void CalculateCash_RespectsRegisterLimit()
    {
        CashCalculationResult result = CalculationService.CalculateCash(
            new[] { Flow(flow: 500, serviceRate: 25, limit: 3) },
            targetWaitMinutes: 4m,
            targetLoadPercent: 80m);

        CashResultRow row = Assert.Single(result.Rows);
        Assert.Equal(3, row.Count);
        Assert.Equal(3, result.TotalCashiers);
    }

    [Fact]
    public void CalculateCash_IgnoresExcludedStores()
    {
        CashCalculationResult result = CalculationService.CalculateCash(
            new[] { Flow(included: false) },
            targetWaitMinutes: 4m,
            targetLoadPercent: 80m);

        Assert.Empty(result.Rows);
        Assert.Equal(0, result.TotalCashiers);
        Assert.Equal(0, result.TotalAvailable);
    }

    [Fact]
    public void CalculateCash_CalculatesSeveralStores()
    {
        CashCalculationResult result = CalculationService.CalculateCash(
            new[]
            {
                Flow("Первый", 100, 25, 6),
                Flow("Второй", 120, 25, 6)
            },
            targetWaitMinutes: 4m,
            targetLoadPercent: 80m);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(11, result.TotalCashiers);
        Assert.Equal(12, result.TotalAvailable);
    }
}
