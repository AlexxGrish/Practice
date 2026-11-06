using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Route
{
    public int RouteId { get; set; }

    public int ShopId { get; set; }

    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual Shop Shop { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
