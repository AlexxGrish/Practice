using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Tests;

public sealed class EquipmentCalculationTests
{
    [Fact]
    public void CalculateEquipment_ReturnsScheduleBellmanTableAndProfit()
    {
        EquipmentCalculationResult result = CalculationService.CalculateEquipment(
            age: 6,
            horizon: 3,
            replacementPrice: 2.8m,
            annualRevenue: 2.0m,
            currentOperatingCost: 1.2m,
            newOperatingCost: 0.7m,
            costGrowthPercent: 10m,
            serviceLife: 8);

        Assert.Equal(3, result.Schedule.Count);
        Assert.Equal(3, result.Schedule[^1].Year);
        Assert.NotEmpty(result.BellmanRows);
        Assert.True(result.TotalProfit > 0m);
        Assert.True(result.FinalResidual >= 0m);
        Assert.Contains("Замена", result.ReplacementPlan);
    }

    [Fact]
    public void CalculateEquipment_WithMinimalHorizon_ProducesOneYear()
    {
        EquipmentCalculationResult result = CalculationService.CalculateEquipment(
            age: 6,
            horizon: 0,
            replacementPrice: 2.8m,
            annualRevenue: 2.0m,
            currentOperatingCost: 1.2m,
            newOperatingCost: 0.7m,
            costGrowthPercent: 10m,
            serviceLife: 8);

        Assert.Single(result.Schedule);
        Assert.Equal(1, result.Schedule[0].Year);
    }

    [Fact]
    public void CalculateEquipment_ScheduleContainsReplaceOrKeepDecisions()
    {
        EquipmentCalculationResult result = CalculationService.CalculateEquipment(
            age: 6,
            horizon: 3,
            replacementPrice: 2.8m,
            annualRevenue: 2.0m,
            currentOperatingCost: 1.2m,
            newOperatingCost: 0.7m,
            costGrowthPercent: 10m,
            serviceLife: 8);

        Assert.All(result.Schedule, row =>
        {
            Assert.Contains(row.Decision, new[] { "Заменить", "Эксплуатировать" });
            Assert.True(row.Residual >= 0m);
        });
    }

    [Fact]
    public void CalculateEquipment_AgeCostsAreNonNegative()
    {
        EquipmentCalculationResult result = CalculationService.CalculateEquipment(
            age: 6,
            horizon: 3,
            replacementPrice: 2.8m,
            annualRevenue: 2.0m,
            currentOperatingCost: 1.2m,
            newOperatingCost: 0.7m,
            costGrowthPercent: 10m,
            serviceLife: 8);

        Assert.NotEmpty(result.AgeCosts);
        Assert.All(result.AgeCosts, row => Assert.True(row.OperatingCost >= 0m));
    }
}
