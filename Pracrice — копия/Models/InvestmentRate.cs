using Pracrice.Modeles;
using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class InvestmentRate
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public decimal MinAmount { get; set; }

    public decimal MaxAmount { get; set; }

    public virtual InvestmentCompany Company { get; set; } = null!;
}
