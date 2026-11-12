using System.ComponentModel;
using System.Globalization;
using System.Windows;
using Microsoft.Win32;
using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Windows;

public partial class ProductEditWindow : Window
{
    private readonly Product? _product;
    private readonly string? _originalImagePath;
    private string? _pendingImagePath;
    private string? _pendingCopiedFile;
    private bool _saved;

    public ProductEditWindow(Product? product)
    {
        InitializeComponent();

        _product = product;
        _originalImagePath = product?.ImagePath;
        _pendingImagePath = product?.ImagePath;

        if (product is null)
        {
            Title = "Добавление товара";
            HeaderTitle.Text = "Новый товар";
            HeaderSubtitle.Text = "Заполните сведения и при необходимости добавьте изображение";
        }
        else
        {
            Title = "Редактирование товара";
            HeaderTitle.Text = "Редактирование товара";
            HeaderSubtitle.Text = product.Name;
            NameBox.Text = product.Name;
            SkuBox.Text = product.Sku;
            PriceBox.Text = product.Price.ToString("0.00", CultureInfo.CurrentCulture);
        }

        UpdateImagePreview();
        Loaded += (_, _) => NameBox.Focus();
    }

    private void UpdateImagePreview()
    {
        PreviewImage.Source = ImageService.LoadBitmap(_pendingImagePath);
        ImagePathText.Text = string.IsNullOrWhiteSpace(_pendingImagePath)
            ? "Изображение не задано"
            : $"Файл: {_pendingImagePath}";
    }

    private void BtnChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выбор изображения товара",
            Filter = string.Join("|", ImageService.OpenFileFilter),
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            string relativePath = ImageService.ImportImage(dialog.FileName);

            if (_pendingCopiedFile is not null)
            {
                ImageService.DeleteImage(ToRelative(_pendingCopiedFile));
            }

            _pendingImagePath = relativePath;
            _pendingCopiedFile = ImageService.ResolvePath(relativePath);
            UpdateImagePreview();
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError(
                "Не удалось добавить изображение.\n\n" + exception.Message,
                "Изображение");
        }
    }

    private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_pendingImagePath))
        {
            return;
        }

        if (!DialogService.Confirm("Удалить изображение из карточки товара?", "Удаление изображения"))
        {
            return;
        }

        if (_pendingCopiedFile is not null)
        {
            ImageService.DeleteImage(ToRelative(_pendingCopiedFile));
            _pendingCopiedFile = null;
        }

        _pendingImagePath = null;
        UpdateImagePreview();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string name = NameBox.Text.Trim();
        string sku = SkuBox.Text.Trim();
        string priceText = PriceBox.Text.Trim().Replace(" ", string.Empty);

        if (!TryParsePrice(priceText, out decimal price))
        {
            DialogService.ShowWarning("Укажите корректную цену товара, например 1250,00.");
            PriceBox.Focus();
            return;
        }

        BtnSave.IsEnabled = false;
        try
        {
            await ProductService.SaveAsync(_product?.Id, name, sku, price, _pendingImagePath);

            if (!string.Equals(_originalImagePath, _pendingImagePath, StringComparison.OrdinalIgnoreCase))
            {
                ImageService.DeleteImage(_originalImagePath);
            }

            _pendingCopiedFile = null;
            _saved = true;
            DialogResult = true;
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Проверьте данные");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось сохранить товар.\n\n" + exception.Message);
        }
        finally
        {
            BtnSave.IsEnabled = true;
        }
    }

    private static bool TryParsePrice(string text, out decimal price)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out price))
        {
            return true;
        }

        string normalized = text.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }

    private static string? ToRelative(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return null;
        }

        string root = ImageService.ImagesDirectory;
        string relative = System.IO.Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        return $"images/{relative}";
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_saved && _pendingCopiedFile is not null)
        {
            ImageService.DeleteImage(ToRelative(_pendingCopiedFile));
        }

        base.OnClosing(e);
    }
}
