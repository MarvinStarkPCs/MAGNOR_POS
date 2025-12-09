using System.Collections.ObjectModel;
using System.Windows.Input;
using MAGNOR_POS.Models.Inventory;

namespace MAGNOR_POS.ViewModels;

public class ProductsViewModel : ViewModelBase
{
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
        // Initialize commands
        SearchCommand = new RelayCommand(_ => ApplyFilters());
        ClearSearchCommand = new RelayCommand(_ => { SearchText = string.Empty; });
        AddNewCommand = new RelayCommand(_ => ShowAddForm());
        EditCommand = new RelayCommand(ShowEditForm, CanEditOrDelete);
        DeleteCommand = new RelayCommand(DeleteProduct, CanEditOrDelete);
        SaveCommand = new RelayCommand(_ => SaveProduct());
        CancelCommand = new RelayCommand(_ => HideAddEditForm());
        AdjustStockCommand = new RelayCommand(ShowStockAdjustment, CanEditOrDelete);
        SaveStockAdjustmentCommand = new RelayCommand(_ => SaveStockAdjustment());
        CancelStockAdjustmentCommand = new RelayCommand(_ => HideStockAdjustment());
        FilterCommand = new RelayCommand(ApplyFilterType);

        // Load products
        LoadDemoProducts();
        ApplyFilters();
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

    private void ShowAddForm()
    {
        EditMode = "Agregar Producto";
        ClearForm();
        IsAddEditVisible = true;
    }

    private void ShowEditForm(object? parameter)
    {
        if (SelectedProduct == null) return;

        EditMode = "Editar Producto";
        FormName = SelectedProduct.Name;
        FormDescription = SelectedProduct.Description ?? string.Empty;
        FormSKU = SelectedProduct.SKU;
        FormBarcode = SelectedProduct.Barcode ?? string.Empty;
        FormSalePrice = SelectedProduct.SalePrice.ToString("N0");
        FormPurchasePrice = SelectedProduct.PurchasePrice.ToString("N0");
        FormCurrentStock = SelectedProduct.CurrentStock.ToString();
        FormMinStock = SelectedProduct.MinimumStock.ToString();
        FormImageUrl = SelectedProduct.ImageUrl ?? "📦";

        IsAddEditVisible = true;
    }

    private void DeleteProduct(object? parameter)
    {
        if (SelectedProduct == null) return;

        var result = System.Windows.MessageBox.Show(
            $"¿Está seguro que desea eliminar el producto '{SelectedProduct.Name}'?",
            "Confirmar Eliminación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _allProducts.Remove(SelectedProduct);
            ApplyFilters();
            System.Windows.MessageBox.Show(
                "Producto eliminado correctamente.",
                "Éxito",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    private void SaveProduct()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(FormName) || string.IsNullOrWhiteSpace(FormSKU))
        {
            System.Windows.MessageBox.Show(
                "El nombre y SKU son obligatorios.",
                "Error de Validación",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return;
        }

        // Parse numbers
        if (!decimal.TryParse(FormSalePrice.Replace(",", ""), out decimal salePrice))
        {
            System.Windows.MessageBox.Show("Precio de venta inválido.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                Id = _allProducts.Count > 0 ? _allProducts.Max(p => p.Id) + 1 : 1,
                Name = FormName,
                Description = FormDescription,
                SKU = FormSKU,
                Barcode = FormBarcode,
                SalePrice = salePrice,
                PurchasePrice = purchasePrice,
                CurrentStock = currentStock,
                MinimumStock = minStock,
                TaxRate = 0.19m,
                ImageUrl = FormImageUrl,
                CategoryId = 1,
                Category = new Category { Id = 1, Name = "General" }
            };

            _allProducts.Add(newProduct);
            System.Windows.MessageBox.Show("Producto agregado correctamente.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        else
        {
            // Edit existing product
            if (SelectedProduct != null)
            {
                SelectedProduct.Name = FormName;
                SelectedProduct.Description = FormDescription;
                SelectedProduct.SKU = FormSKU;
                SelectedProduct.Barcode = FormBarcode;
                SelectedProduct.SalePrice = salePrice;
                SelectedProduct.PurchasePrice = purchasePrice;
                SelectedProduct.CurrentStock = currentStock;
                SelectedProduct.MinimumStock = minStock;
                SelectedProduct.ImageUrl = FormImageUrl;

                System.Windows.MessageBox.Show("Producto actualizado correctamente.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        ApplyFilters();
        HideAddEditForm();
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

    private void SaveStockAdjustment()
    {
        if (SelectedProduct == null) return;

        if (!decimal.TryParse(StockQuantity, out decimal quantity) || quantity <= 0)
        {
            System.Windows.MessageBox.Show("Cantidad inválida.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        if (StockAdjustmentType == "Entrada")
        {
            SelectedProduct.CurrentStock += quantity;
        }
        else // Salida
        {
            if (SelectedProduct.CurrentStock < quantity)
            {
                System.Windows.MessageBox.Show("No hay suficiente stock disponible.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            SelectedProduct.CurrentStock -= quantity;
        }

        System.Windows.MessageBox.Show(
            $"{StockAdjustmentType} de {quantity} unidades registrada correctamente.\nStock actual: {SelectedProduct.CurrentStock}",
            "Éxito",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        ApplyFilters();
        HideStockAdjustment();
    }

    private void UpdateStats()
    {
        OnPropertyChanged(nameof(TotalProducts));
        OnPropertyChanged(nameof(LowStockCount));
        OnPropertyChanged(nameof(TotalInventoryValue));
    }

    private void LoadDemoProducts()
    {
        var categoryBebidas = new Category { Id = 1, Name = "Bebidas" };
        var categoryAlimentos = new Category { Id = 2, Name = "Alimentos" };
        var categorySnacks = new Category { Id = 3, Name = "Snacks" };

        _allProducts = new ObservableCollection<Product>
        {
            new Product { Id = 1, Name = "Coca Cola 500ml", SKU = "BEB001", Barcode = "7501234567890", SalePrice = 3500m, PurchasePrice = 2000m, CurrentStock = 50, MinimumStock = 10, TaxRate = 0.19m, CategoryId = 1, Category = categoryBebidas, ImageUrl = "🥤" },
            new Product { Id = 2, Name = "Postobon Manzana 400ml", SKU = "BEB002", Barcode = "7501234567891", SalePrice = 2800m, PurchasePrice = 1500m, CurrentStock = 45, MinimumStock = 10, TaxRate = 0.19m, CategoryId = 1, Category = categoryBebidas, ImageUrl = "🥤" },
            new Product { Id = 3, Name = "Agua Cristal 600ml", SKU = "BEB003", Barcode = "7501234567892", SalePrice = 2000m, PurchasePrice = 1000m, CurrentStock = 100, MinimumStock = 20, TaxRate = 0.19m, CategoryId = 1, Category = categoryBebidas, ImageUrl = "💧" },
            new Product { Id = 4, Name = "Sandwich de Pollo", SKU = "ALI001", Barcode = "7501234567893", SalePrice = 8500m, PurchasePrice = 4000m, CurrentStock = 20, MinimumStock = 5, TaxRate = 0.19m, CategoryId = 2, Category = categoryAlimentos, ImageUrl = "🥪" },
            new Product { Id = 5, Name = "Hamburguesa Clásica", SKU = "ALI002", Barcode = "7501234567894", SalePrice = 12000m, PurchasePrice = 6000m, CurrentStock = 15, MinimumStock = 5, TaxRate = 0.19m, CategoryId = 2, Category = categoryAlimentos, ImageUrl = "🍔" },
            new Product { Id = 6, Name = "Pizza Personal", SKU = "ALI003", Barcode = "7501234567895", SalePrice = 15000m, PurchasePrice = 7500m, CurrentStock = 10, MinimumStock = 3, TaxRate = 0.19m, CategoryId = 2, Category = categoryAlimentos, ImageUrl = "🍕" },
            new Product { Id = 7, Name = "Papas Margarita", SKU = "SNK001", Barcode = "7501234567896", SalePrice = 4500m, PurchasePrice = 2500m, CurrentStock = 60, MinimumStock = 15, TaxRate = 0.19m, CategoryId = 3, Category = categorySnacks, ImageUrl = "🍟" },
            new Product { Id = 8, Name = "Galletas Festival", SKU = "SNK002", Barcode = "7501234567897", SalePrice = 5000m, PurchasePrice = 2800m, CurrentStock = 40, MinimumStock = 10, TaxRate = 0.19m, CategoryId = 3, Category = categorySnacks, ImageUrl = "🍪" },
            new Product { Id = 9, Name = "Chocolate Jet", SKU = "SNK003", Barcode = "7501234567898", SalePrice = 2500m, PurchasePrice = 1500m, CurrentStock = 80, MinimumStock = 20, TaxRate = 0.19m, CategoryId = 3, Category = categorySnacks, ImageUrl = "🍫" }
        };
    }
}
