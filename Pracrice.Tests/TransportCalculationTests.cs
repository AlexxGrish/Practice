using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Tests;

public sealed class TransportCalculationTests
{
    private static Store Store(int id, string name, int demand) =>
        new() { Id = id, Name = name, Demand = demand };

    private static Warehouse Warehouse(int id, string name) =>
        new() { Id = id, Name = name };

    [Fact]
    public void CalculateTransport_ClosedBalancedTask_ReturnsMinimumCostPlan()
    {
        var data = new TransportData(
            new[] { Warehouse(1, "Склад 1"), Warehouse(2, "Склад 2") },
            new[] { Store(1, "Магазин 1", 6), Store(2, "Магазин 2", 4) },
            new[] { 10, 0 },
            new[,] { { 1, 2 }, { 3, 4 } });

        TransportCalculationResult result = CalculationService.CalculateTransport(data, isOpen: false);

        Assert.Equal(10, result.TotalSupply);
        Assert.Equal(10, result.TotalDemand);
        Assert.Equal(14m, result.TotalCost);
        Assert.Equal(new[] { 6, 4 }, result.Rows[0].Shops);
        Assert.Equal(new[] { 0, 0 }, result.Rows[1].Shops);
    }

    [Fact]
    public void CalculateTransport_OpenTaskWithExcessSupply_AddsReserve()
    {
        var data = new TransportData(
            new[] { Warehouse(1, "Склад 1"), Warehouse(2, "Склад 2") },
            new[] { Store(1, "Магазин 1", 6), Store(2, "Магазин 2", 4) },
            new[] { 10, 10 },
            new[,] { { 1, 2 }, { 3, 4 } });

        TransportCalculationResult result = CalculationService.CalculateTransport(data, isOpen: true);

        Assert.True(result.IsOpen);
        Assert.Equal(20, result.TotalSupply);
        Assert.Equal(10, result.TotalDemand);
        Assert.Equal(10, result.Rows.Sum(row => row.Reserve));
    }

    [Fact]
    public void CalculateTransport_ClosedTaskWithUnbalancedSupply_ThrowsValidationException()
    {
        var data = new TransportData(
            new[] { Warehouse(1, "Склад 1") },
            new[] { Store(1, "Магазин 1", 5) },
            new[] { 10 },
            new[,] { { 1 } });

        Assert.Throws<ValidationException>(() =>
            CalculationService.CalculateTransport(data, isOpen: false));
    }

    [Fact]
    public void CalculateTransport_EmptyWarehouseList_ThrowsValidationException()
    {
        var data = new TransportData(
            Array.Empty<Warehouse>(),
            new[] { Store(1, "Магазин 1", 5) },
            Array.Empty<int>(),
            new int[0, 1]);

        Assert.Throws<ValidationException>(() =>
            CalculationService.CalculateTransport(data, isOpen: true));
    }

    [Fact]
    public void BuildTransportTariffs_ReturnsWarehouseRows()
    {
        var data = new TransportData(
            new[] { Warehouse(1, "Склад 1") },
            new[] { Store(1, "Магазин 1", 5) },
            new[] { 10 },
            new[,] { { 120 } });

        TransportTariffRow row = Assert.Single(CalculationService.BuildTransportTariffs(data));

        Assert.Equal("Склад 1", row.Warehouse);
        Assert.Equal(120m, row.Shop1);
        Assert.Equal(10, row.Supply);
    }
}
