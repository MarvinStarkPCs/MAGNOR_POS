using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Services;
using MAGNOR_POS.Views;

namespace MAGNOR_POS.ViewModels;

/// <summary>
/// ViewModel for reports and analytics
/// </summary>
public class ReportsViewModel : ViewModelBase
{
    private readonly CustomerService _customerService;
    private readonly PurchaseService _purchaseService;
    private readonly SupplierService _supplierService;

    private bool _isLoading;
    private string _statusMessage = string.Empty;

    // Sales statistics
    private decimal _totalSales;
    private int _totalSalesCount;
    private decimal _averageSaleAmount;

    // Purchase statistics
    private decimal _totalPurchases;
    private int _totalPurchasesCount;
    private decimal _pendingPurchases;

    // Customer statistics
    private int _totalCustomers;
    private int _activeCustomers;
    private decimal _totalCreditBalance;

    // Supplier statistics
    private int _totalSuppliers;
    private int _activeSuppliers;

    public ReportsViewModel(CustomerService customerService, PurchaseService purchaseService, SupplierService supplierService)
    {
        _customerService = customerService;
        _purchaseService = purchaseService;
        _supplierService = supplierService;

        // Initialize commands
        LoadStatisticsCommand = new RelayCommand(async _ => await LoadStatisticsAsync());
        RefreshCommand = new RelayCommand(async _ => await LoadStatisticsAsync());

        // Load statistics on initialization
        _ = LoadStatisticsAsync();
    }

    #region Properties

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    // Sales Properties
    public decimal TotalSales
    {
        get => _totalSales;
        set
        {
            _totalSales = value;
            OnPropertyChanged(nameof(TotalSales));
        }
    }

    public int TotalSalesCount
    {
        get => _totalSalesCount;
        set
        {
            _totalSalesCount = value;
            OnPropertyChanged(nameof(TotalSalesCount));
        }
    }

    public decimal AverageSaleAmount
    {
        get => _averageSaleAmount;
        set
        {
            _averageSaleAmount = value;
            OnPropertyChanged(nameof(AverageSaleAmount));
        }
    }

    // Purchase Properties
    public decimal TotalPurchases
    {
        get => _totalPurchases;
        set
        {
            _totalPurchases = value;
            OnPropertyChanged(nameof(TotalPurchases));
        }
    }

    public int TotalPurchasesCount
    {
        get => _totalPurchasesCount;
        set
        {
            _totalPurchasesCount = value;
            OnPropertyChanged(nameof(TotalPurchasesCount));
        }
    }

    public decimal PendingPurchases
    {
        get => _pendingPurchases;
        set
        {
            _pendingPurchases = value;
            OnPropertyChanged(nameof(PendingPurchases));
        }
    }

    // Customer Properties
    public int TotalCustomers
    {
        get => _totalCustomers;
        set
        {
            _totalCustomers = value;
            OnPropertyChanged(nameof(TotalCustomers));
        }
    }

    public int ActiveCustomers
    {
        get => _activeCustomers;
        set
        {
            _activeCustomers = value;
            OnPropertyChanged(nameof(ActiveCustomers));
        }
    }

    public decimal TotalCreditBalance
    {
        get => _totalCreditBalance;
        set
        {
            _totalCreditBalance = value;
            OnPropertyChanged(nameof(TotalCreditBalance));
        }
    }

    // Supplier Properties
    public int TotalSuppliers
    {
        get => _totalSuppliers;
        set
        {
            _totalSuppliers = value;
            OnPropertyChanged(nameof(TotalSuppliers));
        }
    }

    public int ActiveSuppliers
    {
        get => _activeSuppliers;
        set
        {
            _activeSuppliers = value;
            OnPropertyChanged(nameof(ActiveSuppliers));
        }
    }

    #endregion

    #region Commands

    public ICommand LoadStatisticsCommand { get; }
    public ICommand RefreshCommand { get; }

    #endregion

    #region Methods

    private async Task LoadStatisticsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Cargando estadísticas...";

            // Load purchase statistics
            var purchaseStats = await _purchaseService.GetPurchaseStatisticsAsync();
            if (purchaseStats.ContainsKey("TotalPurchases"))
                TotalPurchases = Convert.ToDecimal(purchaseStats["TotalPurchases"]);
            if (purchaseStats.ContainsKey("TotalPurchaseCount"))
                TotalPurchasesCount = Convert.ToInt32(purchaseStats["TotalPurchaseCount"]);
            if (purchaseStats.ContainsKey("PendingAmount"))
                PendingPurchases = Convert.ToDecimal(purchaseStats["PendingAmount"]);

            // Load customer statistics
            var customerStats = await _customerService.GetCustomerStatisticsAsync();
            if (customerStats.ContainsKey("TotalCustomers"))
                TotalCustomers = Convert.ToInt32(customerStats["TotalCustomers"]);
            if (customerStats.ContainsKey("ActiveCustomers"))
                ActiveCustomers = Convert.ToInt32(customerStats["ActiveCustomers"]);
            if (customerStats.ContainsKey("TotalCreditBalance"))
                TotalCreditBalance = Convert.ToDecimal(customerStats["TotalCreditBalance"]);

            // Load supplier statistics
            var supplierStats = await _supplierService.GetSupplierStatisticsAsync();
            if (supplierStats.ContainsKey("TotalSuppliers"))
                TotalSuppliers = Convert.ToInt32(supplierStats["TotalSuppliers"]);
            if (supplierStats.ContainsKey("ActiveSuppliers"))
                ActiveSuppliers = Convert.ToInt32(supplierStats["ActiveSuppliers"]);

            // Sales statistics (placeholder - will be implemented with sales module)
            TotalSales = 0;
            TotalSalesCount = 0;
            AverageSaleAmount = 0;

            StatusMessage = "Estadísticas cargadas exitosamente";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar estadísticas: {ex.Message}";
            CustomMessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
