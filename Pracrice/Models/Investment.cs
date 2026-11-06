using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Investment
{
    public int InvestmentId { get; set; }

    public int? TotalResources { get; set; }

    public int? StepResource { get; set; }

    public string? ProfitMatrix { get; set; }
}
