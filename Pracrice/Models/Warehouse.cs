using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public int? Resources { get; set; }

    public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
}
