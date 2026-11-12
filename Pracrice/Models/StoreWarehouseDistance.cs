namespace Pracrice.Models;

public partial class StoreWarehouseDistance
{
    public int StoreId { get; set; }

    public int WarehouseId { get; set; }

    public decimal DistanceKm { get; set; }

    public virtual Store Store { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
