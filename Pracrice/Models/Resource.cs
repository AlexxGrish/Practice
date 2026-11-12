namespace Pracrice.Models;

public partial class Resource
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public virtual ICollection<ProductRecipe> ProductRecipes { get; set; } = new List<ProductRecipe>();
}
