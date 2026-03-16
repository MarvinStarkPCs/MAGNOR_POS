using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Inventory;
using MAGNOR_POS.Models.Enums;
using MAGNOR_POS.Services;
using MAGNOR_POS.Views;

namespace MAGNOR_POS.ViewModels;

public class POSViewModel : ViewModelBase
{
    private readonly SalesService _salesService;
    private readonly int _userId;

    private string _searchText = string.Empty;
    private string _selectedCategory = "Todos";
    private ObservableCollection<Product> _allProducts = new();
    private ObservableCollection<Product> _filteredProducts = new();
    private ObservableCollection<CartItem> _cartItems = new();
    private ObservableCollection<string> _categories = new() { "Todos" };
    private bool _isLoading;

    public POSViewModel(SalesService salesService, int userId)
    {
        _salesService = salesService;
        _userId = userId;

        // Initialize commands
        AddProductCommand = new RelayCommand(AddProduct, CanAddProduct);
        RemoveProductCommand = new RelayCommand(RemoveProduct);
        IncreaseQuantityCommand = new RelayCommand(IncreaseQuantity);
        DecreaseQuantityCommand = new RelayCommand(DecreaseQuantity);
        ProcessPaymentCommand = new RelayCommand(ProcessPayment, CanProcessPayment);
        ClearCartCommand = new RelayCommand(ClearCart, CanClearCart);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        SelectCategoryCommand = new RelayCommand(SelectCategory);

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

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
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

    public ObservableCollection<CartItem> CartItems
    {
        get => _cartItems;
        set => SetProperty(ref _cartItems, value);
    }

    public ObservableCollection<string> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public decimal Subtotal => CartItems.Sum(item => item.Subtotal);
    public decimal Tax => CartItems.Sum(item => item.Tax);
    public decimal Total => CartItems.Sum(item => item.Total);

    // Commands
    public ICommand AddProductCommand { get; }
    public ICommand RemoveProductCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }
    public ICommand ProcessPaymentCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand SelectCategoryCommand { get; }

    // --- Load from database ---

    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        try
        {
            var products = await _salesService.GetActiveProductsAsync();
            _allProducts = new ObservableCollection<Product>(products);

            // Load categories
            var cats = products
                .Where(p => p.Category != null)
                .Select(p => p.Category!.Name)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var categoryList = new ObservableCollection<string> { "Todos" };
            foreach (var cat in cats)
                categoryList.Add(cat);

            Categories = categoryList;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar productos: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Reload products (call after a sale to refresh stock)
    /// </summary>
    public async Task RefreshProductsAsync()
    {
        await LoadProductsAsync();
    }

    // --- Command implementations ---

    private bool CanAddProduct(object? parameter)
    {
        return parameter is Product;
    }

    private void AddProduct(object? parameter)
    {
        if (parameter is not Product product) return;

        var existingItem = CartItems.FirstOrDefault(item => item.Product.Id == product.Id);
        if (existingItem != null)
        {
            // Check stock before increasing
            if (product.TrackStock && !product.AllowNegativeStock &&
                existingItem.Quantity + 1 > product.CurrentStock)
            {
                MessageBox.Show($"Stock insuficiente. Disponible: {product.CurrentStock:N0}",
                    "Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            existingItem.Quantity++;
        }
        else
        {
            if (product.TrackStock && !product.AllowNegativeStock && product.CurrentStock <= 0)
            {
                MessageBox.Show($"Producto sin stock disponible.",
                    "Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CartItems.Add(new CartItem
            {
                Product = product,
                Quantity = 1
            });
        }

        UpdateTotals();
    }

    private void RemoveProduct(object? parameter)
    {
        if (parameter is CartItem cartItem)
        {
            CartItems.Remove(cartItem);
            UpdateTotals();
        }
    }

    private void IncreaseQuantity(object? parameter)
    {
        if (parameter is CartItem cartItem)
        {
            // Check stock
            if (cartItem.Product.TrackStock && !cartItem.Product.AllowNegativeStock &&
                cartItem.Quantity + 1 > cartItem.Product.CurrentStock)
            {
                MessageBox.Show($"Stock insuficiente. Disponible: {cartItem.Product.CurrentStock:N0}",
                    "Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            cartItem.Quantity++;
            UpdateTotals();
        }
    }

    private void DecreaseQuantity(object? parameter)
    {
        if (parameter is CartItem cartItem)
        {
            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                UpdateTotals();
            }
            else
            {
                CartItems.Remove(cartItem);
                UpdateTotals();
            }
        }
    }

    private bool CanProcessPayment(object? parameter)
    {
        return CartItems.Count > 0;
    }

    private async void ProcessPayment(object? parameter)
    {
        if (CartItems.Count == 0) return;

        // Show payment window
        var paymentWindow = new PaymentWindow(Total)
        {
            Owner = Application.Current.MainWindow
        };

        var result = paymentWindow.ShowDialog();

        if (result != true || !paymentWindow.PaymentConfirmed) return;

        // Build sale items
        var saleItems = CartItems.Select(ci => new SaleItem
        {
            ProductId = ci.Product.Id,
            ProductName = ci.Product.Name,
            Quantity = ci.Quantity,
            UnitPrice = ci.Product.SalePrice,
            TaxRate = ci.Product.TaxRate
        }).ToList();

        // Process sale
        var (success, message, sale) = await _salesService.ProcessSaleAsync(
            saleItems,
            paymentWindow.SelectedPaymentType,
            paymentWindow.AmountPaid,
            _userId
        );

        if (success && sale != null)
        {
            // Show receipt window
            var receiptWindow = new ReceiptWindow(sale)
            {
                Owner = Application.Current.MainWindow
            };
            receiptWindow.ShowDialog();

            // Clear cart and refresh products
            CartItems.Clear();
            UpdateTotals();
            await RefreshProductsAsync();
        }
        else
        {
            MessageBox.Show(message, "Error en la venta",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanClearCart(object? parameter)
    {
        return CartItems.Count > 0;
    }

    private void ClearCart(object? parameter)
    {
        var result = MessageBox.Show(
            "Esta seguro que desea limpiar el carrito?",
            "Confirmar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            CartItems.Clear();
            UpdateTotals();
        }
    }

    private void ClearSearch(object? parameter)
    {
        SearchText = string.Empty;
    }

    private void SelectCategory(object? parameter)
    {
        if (parameter is string category)
        {
            SelectedCategory = category;
        }
    }

    // Helper methods
    private void ApplyFilters()
    {
        var filtered = _allProducts.AsEnumerable();

        // Filter by category
        if (SelectedCategory != "Todos")
        {
            filtered = filtered.Where(p => p.Category?.Name == SelectedCategory);
        }

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.SKU?.ToLower().Contains(search) ?? false) ||
                (p.Barcode?.ToLower().Contains(search) ?? false));
        }

        FilteredProducts = new ObservableCollection<Product>(filtered);
    }

    private void UpdateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(Total));
    }
}
