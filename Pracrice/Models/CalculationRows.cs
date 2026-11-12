namespace Pracrice.Models;

public sealed class PlanProductRow
{
    public bool IsIncluded { get; set; } = true;

    public required string Product { get; init; }

    public decimal Raw { get; init; }

    public decimal Fill { get; init; }

    public decimal Pack { get; init; }

    public decimal Profit { get; init; }
}

public sealed class PlanResultRow
{
    public required string Product { get; init; }

    public int Volume { get; init; }

    public decimal Profit { get; init; }

    public string Share { get; init; } = string.Empty;
}

public sealed class PlanCalculationResult
{
    public required IReadOnlyList<PlanResultRow> Rows { get; init; }

    public decimal TotalProfit { get; init; }

    public decimal RawUsagePercent { get; init; }

    public decimal FillUsagePercent { get; init; }

    public decimal PackUsagePercent { get; init; }
}

public sealed class TransportTariffRow
{
    public bool IsIncluded { get; set; } = true;

    public required string Warehouse { get; init; }

    public decimal Shop1 { get; init; }

    public decimal Shop2 { get; init; }

    public decimal Shop3 { get; init; }

    public int Supply { get; init; }
}

public sealed class TransportStoreRow
{
    public bool IsIncluded { get; set; } = true;

    public required int Id { get; init; }

    public required string Name { get; init; }

    public required int Demand { get; init; }
}

public sealed class TransportResultRow
{
    public required string Warehouse { get; init; }

    public int Shop1 { get; init; }

    public int Shop2 { get; init; }

    public int Shop3 { get; init; }

    public int Reserve { get; init; }
}

public sealed class TransportCalculationResult
{
    public required IReadOnlyList<TransportResultRow> Rows { get; init; }

    public required IReadOnlyList<Store> Stores { get; init; }

    public decimal TotalCost { get; init; }

    public int TotalSupply { get; init; }

    public int TotalDemand { get; init; }

    public bool IsOpen { get; init; }
}

public sealed class EquipmentCostRow
{
    public bool IsIncluded { get; set; } = true;

    public required string Equipment { get; init; }

    public decimal Year1 { get; init; }

    public decimal Year2 { get; init; }

    public decimal Year3 { get; init; }

    public decimal Residual { get; init; }
}

public sealed class EquipmentStrategyRow
{
    public required string Strategy { get; init; }

    public decimal TotalCosts { get; init; }

    public required string Decision { get; init; }
}

public sealed class EquipmentCalculationResult
{
    public required IReadOnlyList<EquipmentCostRow> Costs { get; init; }

    public required IReadOnlyList<EquipmentStrategyRow> Strategies { get; init; }

    public decimal Saving { get; init; }

    public required string Decision { get; init; }
}

public sealed class CashFlowRow
{
    public bool IsIncluded { get; set; } = true;

    public required string Market { get; init; }

    public int Flow { get; init; }

    public int ServiceRate { get; init; }

    public int Limit { get; init; }
}

public sealed class CashResultRow
{
    public required string Market { get; init; }

    public int Count { get; init; }

    public string LoadPercent { get; init; } = string.Empty;

    public string WaitTime { get; init; } = string.Empty;
}

public sealed class CashCalculationResult
{
    public required IReadOnlyList<CashResultRow> Rows { get; init; }

    public int TotalCashiers { get; init; }

    public int TotalAvailable { get; init; }
}

public sealed class InvestmentProjectRow
{
    public bool IsIncluded { get; set; } = true;

    public required string ProjectName { get; init; }

    public decimal Invests { get; init; }

    public decimal Income { get; init; }

    public string ProfitPercent { get; init; } = string.Empty;

    public decimal InvestmentValue { get; init; }
}

public sealed class InvestmentPortfolioRow
{
    public required string SelectedProject { get; init; }

    public string Allocated { get; init; } = string.Empty;

    public string Share { get; init; } = string.Empty;
}

public sealed class InvestmentCalculationResult
{
    public required IReadOnlyList<InvestmentPortfolioRow> Portfolio { get; init; }

    public decimal TotalIncome { get; init; }

    public decimal TotalAllocated { get; init; }
}
