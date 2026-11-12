using System;
using System.Collections.Generic;

namespace Pracrice.Modeles;

public partial class InvestmentCompany
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<InvestmentRate> InvestmentRates { get; set; } = new List<InvestmentRate>();
}
