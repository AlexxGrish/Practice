using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Tests;

public sealed class PlanCalculationTests
{
    private static PlanProductRow Product(
        string name = "Тестовый напиток",
        decimal raw = 1m,
        decimal fill = 1m,
        decimal pack = 1m,
        decimal profit = 100m,
        bool included = true) =>
        new()
        {
            Product = name,
            Raw = raw,
            Fill = fill,
            Pack = pack,
            Profit = profit,
            IsIncluded = included
        };

    [Fact]
    public void CalculatePlan_WithOneProduct_ReturnsExpectedVolumeAndProfit()
    {
        var result = CalculationService.CalculatePlan(
            new[] { Product() },
            rawLimit: 10m,
            fillLimit: 10m,
            packLimit: 10m);

        PlanResultRow row = Assert.Single(result.Rows);
        Assert.Equal("Тестовый напиток", row.Product);
        Assert.Equal(10, row.Volume);
        Assert.Equal(1000m, result.TotalProfit);
        Assert.Equal(100m, result.RawUsagePercent);
        Assert.Equal(100m, result.FillUsagePercent);
        Assert.Equal(100m, result.PackUsagePercent);
    }

    [Fact]
    public void CalculatePlan_WithRawLimit_RespectsResourceConstraint()
    {
        var result = CalculationService.CalculatePlan(
            new[] { Product(raw: 2m, fill: 1m, pack: 1m, profit: 100m) },
            rawLimit: 10m,
            fillLimit: 20m,
            packLimit: 20m);

        PlanResultRow row = Assert.Single(result.Rows);
        Assert.Equal(5, row.Volume);
        Assert.Equal(500m, result.TotalProfit);
        Assert.Equal(100m, result.RawUsagePercent);
        Assert.Equal(25m, result.FillUsagePercent);
    }

    [Fact]
    public void CalculatePlan_IgnoresExcludedProducts()
    {
        var result = CalculationService.CalculatePlan(
            new[] { Product(included: false) },
            rawLimit: 10m,
            fillLimit: 10m,
            packLimit: 10m);

        Assert.Empty(result.Rows);
        Assert.Equal(0m, result.TotalProfit);
    }

    [Fact]
    public void CalculatePlan_WithZeroLimits_ReturnsEmptyResult()
    {
        var result = CalculationService.CalculatePlan(
            new[] { Product() },
            rawLimit: 0m,
            fillLimit: 10m,
            packLimit: 10m);

        Assert.Empty(result.Rows);
        Assert.Equal(0m, result.TotalProfit);
    }

    [Fact]
    public void CalculatePlan_SelectsMoreProfitableProduct()
    {
        var result = CalculationService.CalculatePlan(
            new[]
            {
                Product(name: "Дешёвый", profit: 100m),
                Product(name: "Прибыльный", profit: 250m)
            },
            rawLimit: 20m,
            fillLimit: 20m,
            packLimit: 20m);

        PlanResultRow row = Assert.Single(result.Rows);
        Assert.Equal("Прибыльный", row.Product);
        Assert.Equal(20, row.Volume);
        Assert.Equal(5000m, result.TotalProfit);
    }
}
