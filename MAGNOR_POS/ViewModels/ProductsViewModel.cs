using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Inventory;
using MAGNOR_POS.Services;
using MAGNOR_POS.Views;

namespace MAGNOR_POS.ViewModels;

public class ProductsViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly int _currentUserId = 1; // TODO: Get from session

    private string _searchText = string.Empty;
    private string _selectedFilter = "Todos";
    private ObservableCollection<Product> _allProducts = new();
    private ObservableCollection<Product> _filteredProducts = new();
    private Product? _selectedProduct;
    private bool _isAddEditVisible;
    private bool _isStockAdjustVisible;
    private string _editMode = "Agregar";
    private string _stockAdjustmentType = "Entrada";
    private string _stockQuantity = string.Empty;
    private string _stockReason = string.Empty;

    // Form fields
    private string _formName = string.Empty;
    private string _formDescription = string.Empty;
    private string _formSKU = string.Empty;
    private string _formBarcode = string.Empty;
    private string _formSalePrice = string.Empty;
    private string _formPurchasePrice = string.Empty;
    private string _formCurrentStock = string.Empty;
    private string _formMinStock = string.Empty;
    private string _formImageUrl = "📦";

    public ProductsViewModel()
    {
        // Initialize service
        var context = new AppDbContext();
        _productService = new ProductService(context);

        // Initialize commands
        SearchCommand = new RelayCommand(_ => ApplyFilters());
        ClearSearchCommand = new RelayCommand(_ => { SearchText = string.Empty; });
        AddNewCommand = new RelayCommand(_ => ShowAddFormModal());
        EditCommand = new RelayCommand(_ => ShowEditFormModal(), CanEditOrDelete);
        DeleteCommand = new RelayCommand(async _ => await DeleteProductAsync(), CanEditOrDelete);
        SaveCommand = new RelayCommand(async _ => await SaveProductAsync());
        CancelCommand = new RelayCommand(_ => HideAddEditForm());
        AdjustStockCommand = new RelayCommand(ShowStockAdjustment, CanEditOrDelete);
        SaveStockAdjustmentCommand = new RelayCommand(async _ => await SaveStockAdjustmentAsync());
        CancelStockAdjustmentCommand = new RelayCommand(_ => HideStockAdjustment());
        FilterCommand = new RelayCommand(ApplyFilterType);

        // Load products from database
        _ = LoadProductsAsync();
    }

    // Properties
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public ObservableCollection<Product> FilteredProducts
    {
        get => _filteredProducts;
        set => SetProperty(ref _filteredProducts, value);
    }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public bool IsAddEditVisible
    {
        get => _isAddEditVisible;
        set => SetProperty(ref _isAddEditVisible, value);
    }

    public bool IsStockAdjustVisible
    {
        get => _isStockAdjustVisible;
        set => SetProperty(ref _isStockAdjustVisible, value);
    }

    public string EditMode
    {
        get => _editMode;
        set => SetProperty(ref _editMode, value);
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string StockAdjustmentType { get => _stockAdjustmentType; set => SetProperty(ref _stockAdjustmentType, value); }
    public string StockQuantity { get => _stockQuantity; set => SetProperty(ref _stockQuantity, value); }
    public string StockReason { get => _stockReason; set => SetProperty(ref _stockReason, value); }

    // Computed properties
    public int TotalProducts => _allProducts.Count;
    public int LowStockCount => _allProducts.Count(p => p.CurrentStock <= p.MinimumStock);
    public decimal TotalInventoryValue => _allProducts.Sum(p => p.CurrentStock * p.PurchasePrice);

    // Form Properties
    public string FormName { get => _formName; set => SetProperty(ref _formName, value); }
    public string FormDescription { get => _formDescription; set => SetProperty(ref _formDescription, value); }
    public string FormSKU { get => _formSKU; set => SetProperty(ref _formSKU, value); }
    public string FormBarcode { get => _formBarcode; set => SetProperty(ref _formBarcode, value); }
    public string FormSalePrice { get => _formSalePrice; set => SetProperty(ref _formSalePrice, value); }
    public string FormPurchasePrice { get => _formPurchasePrice; set => SetProperty(ref _formPurchasePrice, value); }
    public string FormCurrentStock { get => _formCurrentStock; set => SetProperty(ref _formCurrentStock, value); }
    public string FormMinStock { get => _formMinStock; set => SetProperty(ref _formMinStock, value); }
    public string FormImageUrl { get => _formImageUrl; set => SetProperty(ref _formImageUrl, value); }

    // Commands
    public ICommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand AddNewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AdjustStockCommand { get; }
    public ICommand SaveStockAdjustmentCommand { get; }
    public ICommand CancelStockAdjustmentCommand { get; }
    public ICommand FilterCommand { get; }

    // Command implementations
    private bool CanEditOrDelete(object? parameter)
    {
        return SelectedProduct != null;
    }

    private void ShowAddFormModal()
    {
        var formWindow = new ProductFormWindow(_productService, _currentUserId)
        {
            Owner = Application.Current.MainWindow
        };

        if (formWindow.ShowDialog() == true && formWindow.ProductSaved)
        {
            _ = LoadProductsAsync();
        }
    }

    private void ShowEditFormModal()
    {
        if (SelectedProduct == null) return;

        var formWindow = new ProductFormWindow(_productService, _currentUserId, SelectedProduct)
        {
            Owner = Application.Current.MainWindow
        };

        if (formWindow.ShowDialog() == true && formWindow.ProductSaved)
        {
            _ = LoadProductsAsync();
        }
    }

    private void ShowAddForm()
    {
        EditMode = "Agregar Producto";
        ClearForm();
        IsAddEditVisible = true;
    }

    private void ShowEditForm(object? parameter)
    {
        if (SelectedProduct == null) return;
        ShowEditFormModal();
    }

    private async Task DeleteProductAsync()
    {
        if (SelectedProduct == null) return;

        var result = CustomMessageBox.Show(
            $"¿Está seguro que desea eliminar el producto '{SelectedProduct.Name}'?",
            "Confirmar Eliminación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var (success, message) = await _productService.DeleteProductAsync(SelectedProduct.Id, _currentUserId);

            if (success)
            {
                await LoadProductsAsync();
                CustomMessageBox.Show(message, "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                CustomMessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private async Task SaveProductAsync()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(FormName) || string.IsNullOrWhiteSpace(FormSKU))
        {
            CustomMessageBox.Show(
                "El nombre y SKU son obligatorios.",
                "Error de Validación",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return;
        }

        // Parse numbers
        if (!decimal.TryParse(FormSalePrice.Replace(",", ""), out decimal salePrice))
        {
            CustomMessageBox.Show("Precio de venta inválido.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        if (!decimal.TryParse(FormPurchasePrice.Replace(",", ""), out decimal purchasePrice))
        {
            purchasePrice = 0;
        }

        if (!decimal.TryParse(FormCurrentStock, out decimal currentStock))
        {
            currentStock = 0;
        }

        if (!decimal.TryParse(FormMinStock, out decimal minStock))
        {
            minStock = 0;
        }

        if (EditMode == "Agregar Producto")
        {
            // Add new product
            var newProduct = new Product
            {
                Name = FormName,
                Description = string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription,
                SKU = FormSKU,
                Barcode = string.IsNullOrWhiteSpace(FormBarcode) ? null : FormBarcode,
                SalePrice = salePrice,
                PurchasePrice = purchasePrice,
                CurrentStock = currentStock,
                MinimumStock = minStock,
                TaxRate = 0.19m,
                ImageUrl = FormImageUrl,
                CategoryId = 1 // Default category - TODO: Get from category selection
            };

            var (success, message, _) = await _productService.AddProductAsync(newProduct, _currentUserId);

            if (success)
            {
                await LoadProductsAsync();
                CustomMessageBox.Show(message, "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                HideAddEditForm();
            }
            else
            {
                CustomMessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        else
        {
            // Edit existing product
            if (SelectedProduct != null)
            {
                SelectedProduct.Name = FormName;
                SelectedProduct.Description = string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription;
                SelectedProduct.SKU = FormSKU;
                SelectedProduct.Barcode = string.IsNullOrWhiteSpace(FormBarcode) ? null : FormBarcode;
                SelectedProduct.SalePrice = salePrice;
                SelectedProduct.PurchasePrice = purchasePrice;
                SelectedProduct.CurrentStock = currentStock;
                SelectedProduct.MinimumStock = minStock;
                SelectedProduct.ImageUrl = FormImageUrl;

                var (success, message) = await _productService.UpdateProductAsync(SelectedProduct, _currentUserId);

                if (success)
                {
                    await LoadProductsAsync();
                    CustomMessageBox.Show(message, "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    HideAddEditForm();
                }
                else
                {
                    CustomMessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }

    private void HideAddEditForm()
    {
        IsAddEditVisible = false;
        ClearForm();
    }

    private void ClearForm()
    {
        FormName = string.Empty;
        FormDescription = string.Empty;
        FormSKU = string.Empty;
        FormBarcode = string.Empty;
        FormSalePrice = string.Empty;
        FormPurchasePrice = string.Empty;
        FormCurrentStock = string.Empty;
        FormMinStock = string.Empty;
        FormImageUrl = "📦";
    }

    private void ApplyFilters()
    {
        var filtered = _allProducts.AsEnumerable();

        // Apply filter type
        if (SelectedFilter == "Stock Bajo")
        {
            filtered = filtered.Where(p => p.CurrentStock <= p.MinimumStock);
        }
        else if (SelectedFilter == "Sin Stock")
        {
            filtered = filtered.Where(p => p.CurrentStock == 0);
        }

        // Apply search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.SKU?.ToLower().Contains(search) ?? false) ||
                (p.Barcode?.ToLower().Contains(search) ?? false));
        }

        FilteredProducts = new ObservableCollection<Product>(filtered);
        UpdateStats();
    }

    private void ApplyFilterType(object? parameter)
    {
        if (parameter is string filter)
        {
            SelectedFilter = filter;
        }
    }

    private void ShowStockAdjustment(object? parameter)
    {
        if (SelectedProduct == null) return;

        StockAdjustmentType = "Entrada";
        StockQuantity = string.Empty;
        StockReason = string.Empty;
        IsStockAdjustVisible = true;
    }

    private void HideStockAdjustment()
    {
        IsStockAdjustVisible = false;
        StockQuantity = string.Empty;
        StockReason = string.Empty;
    }

    private async Task SaveStockAdjustmentAsync()
    {
        if (SelectedProduct == null) return;

        if (!decimal.TryParse(StockQuantity, out decimal quantity) || quantity <= 0)
        {
            CustomMessageBox.Show("Cantidad inválida.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        decimal newStock;
        if (StockAdjustmentType == "Entrada")
        {
            newStock = SelectedProduct.CurrentStock + quantity;
        }
        else // Salida
        {
            if (SelectedProduct.CurrentStock < quantity)
            {
                CustomMessageBox.Show("No hay suficiente stock disponible.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            newStock = SelectedProduct.CurrentStock - quantity;
        }

        var (success, message) = await _productService.UpdateStockAsync(SelectedProduct.Id, newStock, _currentUserId);

        if (success)
        {
            await LoadProductsAsync();
            CustomMessageBox.Show(
                $"{StockAdjustmentType} de {quantity} unidades registrada correctamente.\nStock actual: {newStock}",
                "Éxito",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            HideStockAdjustment();
        }
        else
        {
            CustomMessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void UpdateStats()
    {
        OnPropertyChanged(nameof(TotalProducts));
        OnPropertyChanged(nameof(LowStockCount));
        OnPropertyChanged(nameof(TotalInventoryValue));
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var products = await _productService.GetAllProductsAsync(false);
            _allProducts = new ObservableCollection<Product>(products);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show(
                $"Error al cargar productos: {ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
