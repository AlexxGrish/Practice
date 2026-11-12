using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Warehouse
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public virtual ICollection<StoreWarehouseDistance> StoreWarehouseDistances { get; set; } = new List<StoreWarehouseDistance>();

    public virtual ICollection<WarehouseStock> WarehouseStocks { get; set; } = new List<WarehouseStock>();
}
