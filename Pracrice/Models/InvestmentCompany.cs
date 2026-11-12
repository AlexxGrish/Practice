namespace Pracrice.Models;

public partial class InvestmentCompany
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<InvestmentRate> InvestmentRates { get; set; } = new List<InvestmentRate>();
}
