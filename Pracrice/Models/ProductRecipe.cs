namespace Pracrice.Models;

public partial class ProductRecipe
{
    public int ProductId { get; set; }

    public int ResourceId { get; set; }

    public decimal Consumption { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Resource Resource { get; set; } = null!;
}
