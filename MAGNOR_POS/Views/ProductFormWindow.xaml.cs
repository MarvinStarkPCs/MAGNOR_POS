using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Inventory;
using MAGNOR_POS.Services;
using Microsoft.EntityFrameworkCore;

namespace MAGNOR_POS.Views;

public partial class ProductFormWindow : Window
{
    private readonly ProductService _productService;
    private readonly int _userId;
    private Product? _editingProduct;

    public bool ProductSaved { get; private set; }

    public ProductFormWindow(ProductService productService, int userId, Product? editProduct = null)
    {
        InitializeComponent();
        _productService = productService;
        _userId = userId;
        _editingProduct = editProduct;

        // Load categories
        LoadCategories();

        // If editing, fill form
        if (_editingProduct != null)
        {
            TxtTitle.Text = "Editar Producto";
            FillForm(_editingProduct);
        }

        // Allow dragging
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        };

        Loaded += (s, e) => TxtName.Focus();
    }

    private async void LoadCategories()
    {
        try
        {
            using var context = new AppDbContext();
            var categories = await context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            CmbCategory.ItemsSource = categories;

            if (_editingProduct != null)
            {
                CmbCategory.SelectedItem = categories.FirstOrDefault(c => c.Id == _editingProduct.CategoryId);
            }
            else if (categories.Count > 0)
            {
                CmbCategory.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar categorias: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FillForm(Product product)
    {
        TxtIcon.Text = product.ImageUrl ?? "📦";
        TxtName.Text = product.Name;
        TxtSKU.Text = product.SKU;
        TxtBarcode.Text = product.Barcode ?? "";
        TxtSalePrice.Text = product.SalePrice.ToString("N0");
        TxtPurchasePrice.Text = product.PurchasePrice.ToString("N0");
        TxtCurrentStock.Text = product.CurrentStock.ToString("N0");
        TxtMinStock.Text = product.MinimumStock.ToString("N0");
        TxtTaxRate.Text = (product.TaxRate * 100).ToString("N0");
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("El nombre es obligatorio.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtSKU.Text))
        {
            MessageBox.Show("El SKU es obligatorio.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSKU.Focus();
            return;
        }

        if (CmbCategory.SelectedItem is not Category selectedCategory)
        {
            MessageBox.Show("Seleccione una categoria.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtSalePrice.Text.Replace(",", "").Replace(".", ""), out decimal salePrice) || salePrice <= 0)
        {
            MessageBox.Show("Precio de venta invalido.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSalePrice.Focus();
            return;
        }

        decimal.TryParse(TxtPurchasePrice.Text.Replace(",", "").Replace(".", ""), out decimal purchasePrice);
        decimal.TryParse(TxtCurrentStock.Text.Replace(",", "").Replace(".", ""), out decimal currentStock);
        decimal.TryParse(TxtMinStock.Text.Replace(",", "").Replace(".", ""), out decimal minStock);
        decimal.TryParse(TxtTaxRate.Text, out decimal taxRatePercent);
        decimal taxRate = taxRatePercent / 100m;

        if (_editingProduct != null)
        {
            // Update existing
            _editingProduct.Name = TxtName.Text.Trim();
            _editingProduct.SKU = TxtSKU.Text.Trim();
            _editingProduct.Barcode = string.IsNullOrWhiteSpace(TxtBarcode.Text) ? null : TxtBarcode.Text.Trim();
            _editingProduct.SalePrice = salePrice;
            _editingProduct.PurchasePrice = purchasePrice;
            _editingProduct.CurrentStock = currentStock;
            _editingProduct.MinimumStock = minStock;
            _editingProduct.TaxRate = taxRate;
            _editingProduct.ImageUrl = TxtIcon.Text;
            _editingProduct.CategoryId = selectedCategory.Id;

            var (success, message) = await _productService.UpdateProductAsync(_editingProduct, _userId);
            if (success)
            {
                ProductSaved = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            // Create new
            var newProduct = new Product
            {
                Name = TxtName.Text.Trim(),
                SKU = TxtSKU.Text.Trim(),
                Barcode = string.IsNullOrWhiteSpace(TxtBarcode.Text) ? null : TxtBarcode.Text.Trim(),
                SalePrice = salePrice,
                PurchasePrice = purchasePrice,
                CurrentStock = currentStock,
                MinimumStock = minStock,
                TaxRate = taxRate,
                IsTaxable = true,
                TrackStock = true,
                ImageUrl = TxtIcon.Text,
                CategoryId = selectedCategory.Id
            };

            var (success, message, _) = await _productService.AddProductAsync(newProduct, _userId);
            if (success)
            {
                ProductSaved = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
