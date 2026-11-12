using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pracrice.Models;
using Pracrice.Services;
using Pracrice.Windows;

namespace Pracrice;

public partial class MainWindow
{
    private ProductRow? _selectedProduct;

    private async Task LoadProductsAsync()
    {
        if (_isLoadingProducts)
        {
            return;
        }

        _isLoadingProducts = true;
        try
        {
            IReadOnlyList<ProductRow> products = await ProductService.GetRowsAsync();
            ProductsGrid.ItemsSource = products;
            ProductsCountText.Text = products.Count == 1
                ? "1 позиция"
                : $"{products.Count} позиций";

            if (products.Count > 0)
            {
                ProductsGrid.SelectedIndex = 0;
                UpdateProductDetails(products[0]);
            }
            else
            {
                UpdateProductDetails(null);
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось загрузить каталог товаров.\n\n" + exception.Message);
        }
        finally
        {
            _isLoadingProducts = false;
            UpdateProductButtons();
        }
    }

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateProductDetails(ProductsGrid.SelectedItem as ProductRow);
        UpdateProductButtons();
    }

    private void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProductsGrid.SelectedItem is ProductRow)
        {
            OpenProductEditor();
        }
    }

    private void UpdateProductDetails(ProductRow? product)
    {
        _selectedProduct = product;

        if (product is null)
        {
            ProductDetailImage.Source = ImageService.LoadPlaceholder();
            ProductDetailName.Text = "Товар не выбран";
            ProductDetailSku.Text = "Выберите товар в каталоге";
            ProductDetailPrice.Text = "—";
            ProductDetailImagePath.Text = "Изображение не задано";
            return;
        }

        ProductDetailImage.Source = ImageService.LoadBitmap(product.ImagePath);
        ProductDetailName.Text = product.Name;
        ProductDetailSku.Text = $"Артикул: {product.Sku}";
        ProductDetailPrice.Text = product.PriceDisplay;
        ProductDetailImagePath.Text = string.IsNullOrWhiteSpace(product.ImagePath)
            ? "Изображение не задано"
            : $"Файл: {product.ImagePath}";
    }

    private void UpdateProductButtons()
    {
        bool hasSelection = _selectedProduct is not null;
        BtnEditProduct.IsEnabled = hasSelection;
        BtnDeleteProduct.IsEnabled = hasSelection;
    }

    private async void BtnAddProduct_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentUser is null)
        {
            return;
        }

        try
        {
            var window = new ProductEditWindow(null) { Owner = this };
            if (window.ShowDialog() == true)
            {
                await LoadProductsAsync();
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть форму добавления товара.\n\n" + exception.Message);
        }
    }

    private async void BtnEditProduct_Click(object sender, RoutedEventArgs e)
    {
        await OpenProductEditorAsync();
    }

    private async Task OpenProductEditorAsync()
    {
        if (_selectedProduct is null)
        {
            DialogService.ShowWarning("Выберите товар в каталоге.");
            return;
        }

        try
        {
            Product? product = await ProductService.GetAsync(_selectedProduct.Id);
            if (product is null)
            {
                DialogService.ShowWarning("Товар не найден. Обновите список.");
                await LoadProductsAsync();
                return;
            }

            var window = new ProductEditWindow(product) { Owner = this };
            if (window.ShowDialog() == true)
            {
                await LoadProductsAsync();
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть карточку товара.\n\n" + exception.Message);
        }
    }

    private void OpenProductEditor()
    {
        _ = OpenProductEditorAsync();
    }

    private async void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct is null)
        {
            DialogService.ShowWarning("Выберите товар в каталоге.");
            return;
        }

        ProductRow product = _selectedProduct;

        if (!DialogService.Confirm(
                $"Удалить товар «{product.Name}»?\n\n" +
                "Если для товара задано изображение, файл также будет удалён. " +
                "Действие нельзя отменить.",
                "Удаление товара"))
        {
            return;
        }

        try
        {
            await ProductService.DeleteAsync(product.Id);
            ImageService.DeleteImage(product.ImagePath);
            DialogService.ShowInfo("Товар удалён.");
            await LoadProductsAsync();
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Удаление невозможно");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось удалить товар.\n\n" + exception.Message);
        }
    }

    private async void BtnRefreshProducts_Click(object sender, RoutedEventArgs e)
    {
        await LoadProductsAsync();
    }
}
