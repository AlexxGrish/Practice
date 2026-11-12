using System.IO;
using System.Windows.Media.Imaging;
using Pracrice.Configuration;

namespace Pracrice.Services;

public static class ImageService
{
    private static readonly string[] SupportedExtensions =
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif"
    };

    public static readonly string[] OpenFileFilter =
    {
        "Изображения (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
        "Все файлы (*.*)|*.*"
    };

    public static string ImagesDirectory => AppPaths.ImagesDirectory;

    public static string PlaceholderImageUri => "pack://application:,,,/Assets/no-image.png";

    public static void EnsureDirectory() => Directory.CreateDirectory(ImagesDirectory);

    public static bool IsSupportedExtension(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public static string ImportImage(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Выбранный файл изображения не найден.", sourcePath);
        }

        if (!IsSupportedExtension(sourcePath))
        {
            throw new InvalidOperationException(
                "Поддерживаются изображения PNG, JPG, JPEG, BMP и GIF.");
        }

        EnsureDirectory();

        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        string fileName = $"{Guid.NewGuid():N}{extension}";
        string destination = Path.Combine(ImagesDirectory, fileName);

        File.Copy(sourcePath, destination, overwrite: false);

        return $"images/{fileName}";
    }

    public static string? ResolveExistingPath(string? relativePath)
    {
        string? fullPath = ResolvePath(relativePath);
        return fullPath is not null && File.Exists(fullPath) ? fullPath : null;
    }

    public static string? ResolvePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        try
        {
            string normalized = relativePath.Replace('\\', '/').TrimStart('/');
            if (!normalized.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string candidate = Path.GetFullPath(Path.Combine(AppPaths.BaseDirectory, normalized));
            string root = Path.GetFullPath(ImagesDirectory) + Path.DirectorySeparatorChar;

            return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? candidate : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void DeleteImage(string? relativePath)
    {
        string? fullPath = ResolveExistingPath(relativePath);
        if (fullPath is null)
        {
            return;
        }

        try
        {
            File.Delete(fullPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static BitmapImage LoadBitmap(string? relativePath)
    {
        string? fullPath = ResolveExistingPath(relativePath);
        if (fullPath is not null)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(fullPath, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception)
            {
            }
        }

        return LoadPlaceholder();
    }

    public static BitmapImage LoadPlaceholder()
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(PlaceholderImageUri, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static string? ReadAsDataUri(string? relativePath)
    {
        string? fullPath = ResolveExistingPath(relativePath);
        if (fullPath is null)
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            string mime = Path.GetExtension(fullPath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "image/png"
            };

            return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception)
        {
            return null;
        }
    }
}
