using System.Collections.ObjectModel;
using System.Windows.Input;
using MAGNOR_POS.Models.Inventory;

namespace MAGNOR_POS.ViewModels;

public class POSViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private string _selectedCategory = "Todos";
    private ObservableCollection<Product> _allProducts = new();
    private ObservableCollection<Product> _filteredProducts = new();
    private ObservableCollection<CartItem> _cartItems = new();

    public POSViewModel()
    {
        // Initialize commands
        AddProductCommand = new RelayCommand(AddProduct, CanAddProduct);
        RemoveProductCommand = new RelayCommand(RemoveProduct);
        IncreaseQuantityCommand = new RelayCommand(IncreaseQuantity);
        DecreaseQuantityCommand = new RelayCommand(DecreaseQuantity);
        ProcessPaymentCommand = new RelayCommand(ProcessPayment, CanProcessPayment);
        ClearCartCommand = new RelayCommand(ClearCart, CanClearCart);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        SelectCategoryCommand = new RelayCommand(SelectCategory);

        // Load products (will be replaced with service call)
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

    // Command implementations
    private bool CanAddProduct(object? parameter)
    {
        return parameter is Product product && product.CurrentStock > 0;
    }

    private void AddProduct(object? parameter)
    {
        if (parameter is not Product product) return;

        var existingItem = CartItems.FirstOrDefault(item => item.Product.Id == product.Id);
        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
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

    private void ProcessPayment(object? parameter)
    {
        // TODO: Implement payment dialog
        System.Windows.MessageBox.Show(
            $"Total a pagar: $ {Total:N0}\n\nImplementar diálogo de pago próximamente.",
            "Procesar Pago",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private bool CanClearCart(object? parameter)
    {
        return CartItems.Count > 0;
    }

    private void ClearCart(object? parameter)
    {
        var result = System.Windows.MessageBox.Show(
            "¿Está seguro que desea limpiar el carrito?",
            "Confirmar",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
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

    private void LoadDemoProducts()
    {
        // Demo products (will be replaced with database service)
        var categoryBebidas = new Category { Id = 1, Name = "Bebidas" };
        var categoryAlimentos = new Category { Id = 2, Name = "Alimentos" };
        var categorySnacks = new Category { Id = 3, Name = "Snacks" };

        _allProducts = new ObservableCollection<Product>
        {
            new Product
            {
                Id = 1,
                Name = "Coca Cola 500ml",
                SKU = "BEB001",
                Barcode = "7501234567890",
                SalePrice = 3500m,
                CurrentStock = 50,
                TaxRate = 0.19m,
                CategoryId = 1,
                Category = categoryBebidas,
                ImageUrl = "🥤"
            },
            new Product
            {
                Id = 2,
                Name = "Postobon Manzana 400ml",
                SKU = "BEB002",
                Barcode = "7501234567891",
                SalePrice = 2800m,
                CurrentStock = 45,
                TaxRate = 0.19m,
                CategoryId = 1,
                Category = categoryBebidas,
                ImageUrl = "🥤"
            },
            new Product
            {
                Id = 3,
                Name = "Agua Cristal 600ml",
                SKU = "BEB003",
                Barcode = "7501234567892",
                SalePrice = 2000m,
                CurrentStock = 100,
                TaxRate = 0.19m,
                CategoryId = 1,
                Category = categoryBebidas,
                ImageUrl = "💧"
            },
            new Product
            {
                Id = 4,
                Name = "Sandwich de Pollo",
                SKU = "ALI001",
                Barcode = "7501234567893",
                SalePrice = 8500m,
                CurrentStock = 20,
                TaxRate = 0.19m,
                CategoryId = 2,
                Category = categoryAlimentos,
                ImageUrl = "🥪"
            },
            new Product
            {
                Id = 5,
                Name = "Hamburguesa Clásica",
                SKU = "ALI002",
                Barcode = "7501234567894",
                SalePrice = 12000m,
                CurrentStock = 15,
                TaxRate = 0.19m,
                CategoryId = 2,
                Category = categoryAlimentos,
                ImageUrl = "🍔"
            },
            new Product
            {
                Id = 6,
                Name = "Pizza Personal",
                SKU = "ALI003",
                Barcode = "7501234567895",
                SalePrice = 15000m,
                CurrentStock = 10,
                TaxRate = 0.19m,
                CategoryId = 2,
                Category = categoryAlimentos,
                ImageUrl = "🍕"
            },
            new Product
            {
                Id = 7,
                Name = "Papas Margarita",
                SKU = "SNK001",
                Barcode = "7501234567896",
                SalePrice = 4500m,
                CurrentStock = 60,
                TaxRate = 0.19m,
                CategoryId = 3,
                Category = categorySnacks,
                ImageUrl = "🍟"
            },
            new Product
            {
                Id = 8,
                Name = "Galletas Festival",
                SKU = "SNK002",
                Barcode = "7501234567897",
                SalePrice = 5000m,
                CurrentStock = 40,
                TaxRate = 0.19m,
                CategoryId = 3,
                Category = categorySnacks,
                ImageUrl = "🍪"
            },
            new Product
            {
                Id = 9,
                Name = "Chocolate Jet",
                SKU = "SNK003",
                Barcode = "7501234567898",
                SalePrice = 2500m,
                CurrentStock = 80,
                TaxRate = 0.19m,
                CategoryId = 3,
                Category = categorySnacks,
                ImageUrl = "🍫"
            }
        };
    }
}
