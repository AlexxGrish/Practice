using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Tests;

public sealed class InvestmentCalculationTests
{
    private static InvestmentProjectRow Project(
        string name = "Проект",
        decimal investmentValue = 1000m,
        decimal incomeMillions = 1m,
        bool included = true,
        int maxCopies = 1) =>
        new()
        {
            ProjectId = 1,
            ProjectName = name,
            Company = "Компания",
            Invests = investmentValue / 1_000_000m,
            Income = incomeMillions,
            InvestmentValue = investmentValue,
            IsIncluded = included,
            MaxCopies = maxCopies
        };

    [Fact]
    public void CalculateInvestments_SelectsOneProjectWithinBudget()
    {
        InvestmentCalculationResult result = CalculationService.CalculateInvestments(
            new[] { Project() },
            budget: 1000m);

        InvestmentPortfolioRow row = Assert.Single(result.Portfolio);
        Assert.Contains("Проект", row.SelectedProject);
        Assert.Equal(1000m, result.TotalAllocated);
        Assert.Equal(1_000_000m, result.TotalIncome);
    }

    [Fact]
    public void CalculateInvestments_ExcludedProjectIsNotSelected()
    {
        InvestmentCalculationResult result = CalculationService.CalculateInvestments(
            new[] { Project(included: false) },
            budget: 1000m);

        Assert.Empty(result.Portfolio);
        Assert.Equal(0m, result.TotalAllocated);
    }

    [Fact]
    public void CalculateInvestments_WithInsufficientBudget_DoesNotExceedBudget()
    {
        InvestmentCalculationResult result = CalculationService.CalculateInvestments(
            new[] { Project(investmentValue: 5000m) },
            budget: 1000m);

        Assert.True(result.TotalAllocated <= 1000m);
        Assert.True(result.TotalIncome >= 0m);
    }

    [Fact]
    public void CalculateInvestments_PrefersHigherIncomeProject()
    {
        InvestmentCalculationResult result = CalculationService.CalculateInvestments(
            new[]
            {
                Project("Малый", 1000m, 1m, maxCopies: 1),
                Project("Большой", 1000m, 3m, maxCopies: 1)
            },
            budget: 1000m);

        InvestmentPortfolioRow row = Assert.Single(result.Portfolio);
        Assert.Contains("Большой", row.SelectedProject);
        Assert.Equal(3_000_000m, result.TotalIncome);
    }
}
