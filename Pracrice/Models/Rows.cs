using System.Windows.Media.Imaging;
using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Models;

public sealed class UserRow
{
    public required int Id { get; init; }

    public required string Login { get; init; }

    public required string FullName { get; init; }

    public required UserRole Role { get; init; }

    public string RoleDisplay => RoleInfo.GetDisplayName(Role);

    public bool CanBeDeleted { get; init; }

    public static UserRow From(User user, int? currentUserId = null) => new()
    {
        Id = user.Id,
        Login = user.Login,
        FullName = user.FullName,
        Role = user.Role,
        CanBeDeleted = user.Id != currentUserId
    };
}

public sealed class RightsRow
{
    public required string SectionName { get; init; }

    public required string AdminRight { get; init; }

    public required string DirectorRight { get; init; }

    public required string ManagerRight { get; init; }
}

public sealed class ProductRow
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string Sku { get; init; }

    public required decimal Price { get; init; }

    public string? ImagePath { get; init; }

    public required BitmapImage ImageSource { get; init; }

    public string PriceDisplay => Price.ToString("N2") + " ₽";

    public string ImageDisplay => string.IsNullOrWhiteSpace(ImagePath) ? "Без изображения" : ImagePath;

    public static ProductRow From(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Sku = product.Sku,
        Price = product.Price,
        ImagePath = product.ImagePath,
        ImageSource = ImageService.LoadBitmap(product.ImagePath)
    };
}
