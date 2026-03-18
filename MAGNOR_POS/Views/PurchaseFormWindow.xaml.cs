using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MAGNOR_POS.Models.Inventory;
using MAGNOR_POS.Models.Parties;
using MAGNOR_POS.Models.Purchases;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.Views;

/// <summary>
/// Interaction logic for PurchaseFormWindow.xaml
/// </summary>
public partial class PurchaseFormWindow : Window
{
    private readonly PurchaseService _purchaseService;
    private readonly SupplierService _supplierService;
    private readonly ProductService _productService;
    private readonly int _currentUserId;
    private readonly Purchase? _editingPurchase;
    private readonly bool _isEditMode;

    private ObservableCollection<PurchaseDetailViewModel> _purchaseDetails = new();
    private List<Product> _availableProducts = new();
    private List<Supplier> _availableSuppliers = new();

    public PurchaseFormWindow(PurchaseService purchaseService, int currentUserId, Purchase? editingPurchase = null)
    {
        InitializeComponent();

        _purchaseService = purchaseService;
        _currentUserId = currentUserId;
        _editingPurchase = editingPurchase;
        _isEditMode = editingPurchase != null;

        // Create services with same context
        var context = new Data.AppDbContext();
        _supplierService = new SupplierService(context);
        _productService = new ProductService(context);

        // Initialize DataGrid
        DetailsDataGrid.ItemsSource = _purchaseDetails;

        if (_isEditMode && _editingPurchase != null)
        {
            HeaderText.Text = "Editar Orden de Compra";
        }

        // Allow dragging
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        };

        // Load data after window is loaded
        Loaded += async (s, e) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Load suppliers
            _availableSuppliers = (await _supplierService.GetAllSuppliersAsync(false)).ToList();
            SupplierComboBox.ItemsSource = _availableSuppliers;

            // Load products
            _availableProducts = await _productService.GetAllProductsAsync(false);
            ProductComboBox.ItemsSource = _availableProducts;

            if (_isEditMode && _editingPurchase != null)
            {
                LoadPurchaseData(_editingPurchase);
            }
            else if (_availableSuppliers.Any())
            {
                SupplierComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadPurchaseData(Purchase purchase)
    {
        SupplierComboBox.SelectedValue = purchase.SupplierId;
        PurchaseDatePicker.SelectedDate = purchase.PurchaseDate;
        DeliveryDatePicker.SelectedDate = purchase.DeliveryDate;
        SupplierInvoiceTextBox.Text = purchase.SupplierInvoiceNumber;
        NotesTextBox.Text = purchase.Notes;

        // Load details
        foreach (var detail in purchase.Details)
        {
            _purchaseDetails.Add(new PurchaseDetailViewModel
            {
                ProductId = detail.ProductId,
                ProductName = detail.Product?.Name ?? "Producto",
                Quantity = detail.Quantity,
                UnitCost = detail.UnitCost,
                DiscountPercentage = detail.DiscountPercentage,
                DiscountAmount = detail.DiscountAmount,
                Subtotal = detail.Subtotal,
                TaxRate = detail.TaxRate,
                TaxAmount = detail.TaxAmount,
                Total = detail.Total
            });
        }

        RecalculateTotals();
    }

    private void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        // Toggle product entry panel visibility
        ProductEntryPanel.Visibility = ProductEntryPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        // Reset form
        if (ProductEntryPanel.Visibility == Visibility.Visible)
        {
            ProductComboBox.SelectedIndex = -1;
            QuantityTextBox.Text = "1";
            UnitCostTextBox.Text = "0";
            DiscountTextBox.Text = "0";
            LineTotalText.Text = "Total línea: $0.00";
        }
    }

    private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductComboBox.SelectedItem is Product product)
        {
            // Auto-fill with product's purchase price
            UnitCostTextBox.Text = product.PurchasePrice.ToString("F2");
        }
    }

    private void CalculateLineTotal(object sender, TextChangedEventArgs e)
    {
        // Prevent errors during initialization
        if (LineTotalText == null || QuantityTextBox == null || UnitCostTextBox == null || DiscountTextBox == null)
            return;

        if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantity) || quantity <= 0)
        {
            LineTotalText.Text = "Total línea: $0.00";
            return;
        }

        if (!decimal.TryParse(UnitCostTextBox.Text, out decimal unitCost) || unitCost < 0)
        {
            LineTotalText.Text = "Total línea: $0.00";
            return;
        }

        if (!decimal.TryParse(DiscountTextBox.Text, out decimal discountPercent) || discountPercent < 0)
        {
            discountPercent = 0;
        }

        // Calculate line total
        decimal subtotal = quantity * unitCost;
        decimal discountAmount = subtotal * (discountPercent / 100);
        decimal subtotalAfterDiscount = subtotal - discountAmount;
        decimal taxAmount = subtotalAfterDiscount * 0.19m; // 19% IVA Colombia
        decimal total = subtotalAfterDiscount + taxAmount;

        LineTotalText.Text = $"Total línea: {total:C2}";
    }

    private void AddToDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate product selection
        if (ProductComboBox.SelectedItem is not Product product)
        {
            CustomMessageBox.Show("Debe seleccionar un producto", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate quantity
        if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantity) || quantity <= 0)
        {
            CustomMessageBox.Show("La cantidad debe ser mayor a 0", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            QuantityTextBox.Focus();
            return;
        }

        // Validate unit cost
        if (!decimal.TryParse(UnitCostTextBox.Text, out decimal unitCost) || unitCost < 0)
        {
            CustomMessageBox.Show("El precio unitario debe ser mayor o igual a 0", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            UnitCostTextBox.Focus();
            return;
        }

        // Get discount
        if (!decimal.TryParse(DiscountTextBox.Text, out decimal discountPercent) || discountPercent < 0)
        {
            discountPercent = 0;
        }

        // Check if product already exists in details
        var existingDetail = _purchaseDetails.FirstOrDefault(d => d.ProductId == product.Id);
        if (existingDetail != null)
        {
            CustomMessageBox.Show("Este producto ya está en la lista. Elimínelo primero si desea modificarlo.",
                "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Calculate amounts
        decimal subtotal = quantity * unitCost;
        decimal discountAmount = subtotal * (discountPercent / 100);
        decimal subtotalAfterDiscount = subtotal - discountAmount;
        decimal taxRate = 0.19m; // 19% IVA Colombia
        decimal taxAmount = subtotalAfterDiscount * taxRate;
        decimal total = subtotalAfterDiscount + taxAmount;

        // Add to details
        _purchaseDetails.Add(new PurchaseDetailViewModel
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            UnitCost = unitCost,
            DiscountPercentage = discountPercent,
            DiscountAmount = discountAmount,
            Subtotal = subtotalAfterDiscount,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = total
        });

        // Recalculate totals
        RecalculateTotals();

        // Hide entry panel and reset
        ProductEntryPanel.Visibility = Visibility.Collapsed;
    }

    private void RemoveDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PurchaseDetailViewModel detail)
        {
            _purchaseDetails.Remove(detail);
            RecalculateTotals();
        }
    }

    private void RecalculateTotals()
    {
        decimal subtotal = 0;
        decimal taxTotal = 0;
        decimal discountTotal = 0;
        decimal total = 0;

        foreach (var detail in _purchaseDetails)
        {
            // Subtotal before discount
            decimal lineSubtotal = detail.Quantity * detail.UnitCost;
            subtotal += lineSubtotal;
            discountTotal += detail.DiscountAmount;
            taxTotal += detail.TaxAmount;
            total += detail.Total;
        }

        SubtotalText.Text = subtotal.ToString("C2");
        TaxText.Text = taxTotal.ToString("C2");
        DiscountTotalText.Text = discountTotal.ToString("C2");
        TotalText.Text = total.ToString("C2");
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate supplier
        if (SupplierComboBox.SelectedValue == null)
        {
            CustomMessageBox.Show("Debe seleccionar un proveedor", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            SupplierComboBox.Focus();
            return;
        }

        // Validate purchase date
        if (!PurchaseDatePicker.SelectedDate.HasValue)
        {
            CustomMessageBox.Show("Debe seleccionar una fecha de compra", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            PurchaseDatePicker.Focus();
            return;
        }

        // Validate details
        if (_purchaseDetails.Count == 0)
        {
            CustomMessageBox.Show("Debe agregar al menos un producto a la compra", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            SaveButton.IsEnabled = false;
            SaveButton.Content = "Guardando...";

            // Calculate totals
            decimal subtotal = 0;
            decimal taxAmount = 0;
            decimal discountAmount = 0;
            decimal total = 0;

            foreach (var detail in _purchaseDetails)
            {
                subtotal += (detail.Quantity * detail.UnitCost);
                discountAmount += detail.DiscountAmount;
                taxAmount += detail.TaxAmount;
                total += detail.Total;
            }

            // Create or update purchase
            var purchase = _isEditMode && _editingPurchase != null ? _editingPurchase : new Purchase
            {
                PurchaseNumber = string.Empty // Will be generated by service
            };

            purchase.SupplierId = (int)SupplierComboBox.SelectedValue;
            purchase.PurchaseDate = PurchaseDatePicker.SelectedDate.Value;
            purchase.DeliveryDate = DeliveryDatePicker.SelectedDate;
            purchase.SupplierInvoiceNumber = string.IsNullOrWhiteSpace(SupplierInvoiceTextBox.Text)
                ? null
                : SupplierInvoiceTextBox.Text.Trim();
            purchase.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text)
                ? null
                : NotesTextBox.Text.Trim();
            purchase.Subtotal = subtotal;
            purchase.TaxAmount = taxAmount;
            purchase.DiscountAmount = discountAmount;
            purchase.Total = total;
            purchase.Balance = total; // Initially unpaid
            purchase.UserId = _currentUserId;

            // Clear and add details
            purchase.Details.Clear();
            foreach (var detailVM in _purchaseDetails)
            {
                purchase.Details.Add(new PurchaseDetail
                {
                    ProductId = detailVM.ProductId,
                    Quantity = detailVM.Quantity,
                    UnitCost = detailVM.UnitCost,
                    DiscountPercentage = detailVM.DiscountPercentage,
                    DiscountAmount = detailVM.DiscountAmount,
                    Subtotal = detailVM.Subtotal,
                    TaxRate = detailVM.TaxRate,
                    TaxAmount = detailVM.TaxAmount,
                    Total = detailVM.Total
                });
            }

            bool success;
            string message;

            if (_isEditMode)
            {
                (success, message) = await _purchaseService.UpdatePurchaseAsync(purchase);
            }
            else
            {
                (success, message, Purchase? _) = await _purchaseService.AddPurchaseAsync(purchase, _currentUserId);
            }

            if (success)
            {
                CustomMessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SaveButton.IsEnabled = true;
            SaveButton.Content = "Guardar Compra";
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ViewModel for DataGrid binding
    public class PurchaseDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }
}
