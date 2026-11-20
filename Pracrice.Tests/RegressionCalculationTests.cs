using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Tests;

public sealed class RegressionCalculationTests
{
    [Fact]
    public void BuildTransportStores_MapsStoreDemand()
    {
        var data = new TransportData(
            new[] { new Warehouse { Id = 1, Name = "Склад" } },
            new[]
            {
                new Store { Id = 7, Name = "Магазин 7", Demand = 12 }
            },
            new[] { 12 },
            new[,] { { 10 } });

        TransportStoreRow row = Assert.Single(CalculationService.BuildTransportStores(data));

        Assert.Equal(7, row.Id);
        Assert.Equal("Магазин 7", row.Name);
        Assert.Equal(12, row.Demand);
    }

    [Fact]
    public void CalculateTransport_OpenTaskWithDeficit_AddsDeficitRow()
    {
        var data = new TransportData(
            new[] { new Warehouse { Id = 1, Name = "Склад" } },
            new[] { new Store { Id = 1, Name = "Магазин", Demand = 10 } },
            new[] { 5 },
            new[,] { { 10 } });

        TransportCalculationResult result = CalculationService.CalculateTransport(data, isOpen: true);

        Assert.Contains(result.Rows, row => row.Warehouse == "Дефицит");
        Assert.Equal(10, result.TotalDemand);
        Assert.Equal(5, result.TotalSupply);
    }

    [Fact]
    public void CalculatePlan_WithFillLimit_UsesFillConstraint()
    {
        var product = new PlanProductRow
        {
            Product = "Напиток",
            Raw = 1m,
            Fill = 1m,
            Pack = 1m,
            Profit = 100m
        };

        PlanCalculationResult result = CalculationService.CalculatePlan(
            new[] { product },
            rawLimit: 100m,
            fillLimit: 2m,
            packLimit: 100m);

        PlanResultRow row = Assert.Single(result.Rows);
        Assert.Equal(2, row.Volume);
        Assert.Equal(200m, result.TotalProfit);
        Assert.Equal(100m, result.FillUsagePercent);
    }

    [Fact]
    public void CalculatePlan_ProductWithZeroProfit_IsIgnored()
    {
        var product = new PlanProductRow
        {
            Product = "Без прибыли",
            Raw = 1m,
            Fill = 1m,
            Pack = 1m,
            Profit = 0m
        };

        PlanCalculationResult result = CalculationService.CalculatePlan(
            new[] { product },
            rawLimit: 10m,
            fillLimit: 10m,
            packLimit: 10m);

        Assert.Empty(result.Rows);
    }

    [Fact]
    public void CalculateCash_StrictTargets_IncreaseCashiers()
    {
        CashCalculationResult normal = CalculationService.CalculateCash(
            new[] { new CashFlowRow { Market = "Магазин", Flow = 100, ServiceRate = 25, Limit = 10 } },
            targetWaitMinutes: 5m,
            targetLoadPercent: 80m);

        CashCalculationResult strict = CalculationService.CalculateCash(
            new[] { new CashFlowRow { Market = "Магазин", Flow = 100, ServiceRate = 25, Limit = 10 } },
            targetWaitMinutes: 0.1m,
            targetLoadPercent: 50m);

        Assert.True(strict.Rows[0].Count > normal.Rows[0].Count);
        Assert.True(strict.Rows[0].Count <= 10);
    }

    [Fact]
    public void CalculateCash_ZeroFlow_ReturnsOneCashier()
    {
        CashCalculationResult result = CalculationService.CalculateCash(
            new[] { new CashFlowRow { Market = "Пустой", Flow = 0, ServiceRate = 25, Limit = 6 } },
            targetWaitMinutes: 5m,
            targetLoadPercent: 80m);

        CashResultRow row = Assert.Single(result.Rows);
        Assert.Equal(1, row.Count);
        Assert.Equal(1, result.TotalCashiers);
    }

    [Fact]
    public void CalculateInvestments_WithMultipleCopies_DoesNotExceedBudget()
    {
        var project = new InvestmentProjectRow
        {
            ProjectId = 1,
            ProjectName = "Проект",
            Company = "Компания",
            Invests = 0.001m,
            Income = 1m,
            InvestmentValue = 1000m,
            MaxCopies = 2
        };

        InvestmentCalculationResult result = CalculationService.CalculateInvestments(
            new[] { project },
            budget: 2000m);

        Assert.True(result.TotalAllocated <= 2000m);
        Assert.True(result.TotalIncome <= 2_000_000m);
        Assert.Single(result.Portfolio);
    }

    [Fact]
    public void CalculateInvestments_ZeroBudget_ReturnsEmptyPortfolio()
    {
        var project = new InvestmentProjectRow
        {
            ProjectId = 1,
            ProjectName = "Проект",
            Company = "Компания",
            InvestmentValue = 1000m,
            Income = 1m,
            MaxCopies = 1
        };

        InvestmentCalculationResult result = CalculationService.CalculateInvestments(
            new[] { project },
            budget: 0m);

        Assert.Empty(result.Portfolio);
        Assert.Equal(0m, result.TotalAllocated);
    }

    [Fact]
    public void CalculateEquipment_NegativeHorizon_IsClampedToOne()
    {
        EquipmentCalculationResult result = CalculationService.CalculateEquipment(
            age: -5,
            horizon: -2,
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
    public void Verify_EmptyPassword_ReturnsFalse()
    {
        Assert.False(PasswordService.Verify(string.Empty, PasswordService.Hash("secret")));
        Assert.False(PasswordService.Verify("secret", string.Empty));
    }
}
