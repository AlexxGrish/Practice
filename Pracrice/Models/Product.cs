using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public decimal? Demand { get; set; }

    public decimal? MaxInventory { get; set; }

    public decimal? MaxProduction { get; set; }

    public string? DpTable { get; set; }

    public string? ChoiceTable { get; set; }

    public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
}
