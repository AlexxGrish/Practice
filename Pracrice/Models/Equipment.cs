using System;
using System.Collections.Generic;

namespace Pracrice.Models;

public partial class Equipment
{
    public int EquipmentId { get; set; }

    public string Name { get; set; } = null!;

    public decimal? NewPrice { get; set; }

    public decimal? OldPrice { get; set; }

    public decimal? Profit { get; set; }

    public int? Age { get; set; }
}
