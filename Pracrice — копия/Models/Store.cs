using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Store
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<StoreWarehouseDistance> StoreWarehouseDistances { get; set; } = new List<StoreWarehouseDistance>();
}
