using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Shop
{
    public int ShopId { get; set; }

    public string Address { get; set; } = null!;

    public int? NumberOfCounters { get; set; }

    public decimal? AvgAttendance { get; set; }

    public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
}
