namespace Pracrice.Models;

public partial class InvestmentProject
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CompanyId { get; set; }

    public decimal Investment { get; set; }

    public decimal AnnualIncome { get; set; }

    public virtual InvestmentCompany Company { get; set; } = null!;
}
