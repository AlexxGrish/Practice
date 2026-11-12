using Microsoft.EntityFrameworkCore;
using Pracrice.Data;
using Pracrice.Models;

namespace Pracrice.Services;

public static class ProductService
{
    public static async Task<IReadOnlyList<ProductRow>> GetRowsAsync()
    {
        await using var context = AppDatabase.CreateContext();
        var products = await context.Products.AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync();

        return products.Select(ProductRow.From).ToList();
    }

    public static async Task<Product?> GetAsync(int productId)
    {
        await using var context = AppDatabase.CreateContext();
        return await context.Products.AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == productId);
    }

    public static async Task<Product> SaveAsync(
        int? productId,
        string name,
        string sku,
        decimal price,
        string? imagePath)
    {
        name = (name ?? string.Empty).Trim();
        sku = (sku ?? string.Empty).Trim();

        if (name.Length == 0)
        {
            throw new ValidationException("Укажите наименование товара.");
        }

        if (name.Length > 150)
        {
            throw new ValidationException("Наименование не должно превышать 150 символов.");
        }

        if (sku.Length == 0)
        {
            throw new ValidationException("Укажите артикул товара.");
        }

        if (sku.Length > 50)
        {
            throw new ValidationException("Артикул не должен превышать 50 символов.");
        }

        if (price < 0)
        {
            throw new ValidationException("Цена не может быть отрицательной.");
        }

        await using var context = AppDatabase.CreateContext();

        if (await context.Products.AnyAsync(product => product.Sku == sku && product.Id != productId))
        {
            throw new ValidationException($"Товар с артикулом «{sku}» уже существует.");
        }

        Product product;
        if (productId is null)
        {
            product = new Product();
            context.Products.Add(product);
        }
        else
        {
            product = await context.Products.FirstOrDefaultAsync(item => item.Id == productId.Value)
                      ?? throw new ValidationException("Товар не найден. Возможно, запись уже удалена.");
        }

        product.Name = name;
        product.Sku = sku;
        product.Price = decimal.Round(price, 2);
        product.ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath;

        await context.SaveChangesAsync();
        return product;
    }

    public static async Task DeleteAsync(int productId)
    {
        await using var context = AppDatabase.CreateContext();
        var product = await context.Products.FirstOrDefaultAsync(item => item.Id == productId)
                      ?? throw new ValidationException("Товар не найден. Возможно, запись уже удалена.");

        bool usedInRecipes = await context.ProductRecipes.AnyAsync(recipe => recipe.ProductId == productId);
        bool usedInStocks = await context.WarehouseStocks.AnyAsync(stock => stock.ProductId == productId);

        if (usedInRecipes || usedInStocks)
        {
            throw new ValidationException(
                "Нельзя удалить товар: он используется в рецептурах или складских остатках.\n" +
                "Сначала удалите связанные записи.");
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync();
    }

    public static async Task<int> CountAsync()
    {
        await using var context = AppDatabase.CreateContext();
        return await context.Products.CountAsync();
    }
}
