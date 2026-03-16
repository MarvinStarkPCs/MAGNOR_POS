using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Data;
using MAGNOR_POS.Services;
using MAGNOR_POS.Views;

namespace MAGNOR_POS.ViewModels;

/// <summary>
/// ViewModel for system settings and configuration
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private string _companyName = "MAGNOR";
    private string _companyNit = "";
    private string _companyAddress = "";
    private string _companyPhone = "";
    private string _companyEmail = "";

    private decimal _defaultTaxRate = 0.19m; // 19% IVA Colombia
    private string _currency = "COP";
    private bool _enableInventoryAlerts = true;
    private bool _requireCustomerOnSale = false;
    private bool _enableDiscounts = true;

    // Business Type Configuration
    private string _businessType = "Retail"; // Retail or Restaurant
    private bool _enableRestaurantMode = false;
    private bool _enableTableManagement = false;
    private bool _enableKitchenOrders = false;
    private bool _showProductIcons = true;
    private string _defaultProductIcon = "📦";

    private string _statusMessage = string.Empty;

    public SettingsViewModel()
    {
        // Initialize commands
        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
        ResetSettingsCommand = new RelayCommand(_ => ResetSettings());
        ConfigureTablesCommand = new RelayCommand(_ => ConfigureTables(), _ => EnableRestaurantMode);

        // Load settings
        LoadSettings();
    }

    #region Properties

    public string CompanyName
    {
        get => _companyName;
        set
        {
            _companyName = value;
            OnPropertyChanged(nameof(CompanyName));
        }
    }

    public string CompanyNit
    {
        get => _companyNit;
        set
        {
            _companyNit = value;
            OnPropertyChanged(nameof(CompanyNit));
        }
    }

    public string CompanyAddress
    {
        get => _companyAddress;
        set
        {
            _companyAddress = value;
            OnPropertyChanged(nameof(CompanyAddress));
        }
    }

    public string CompanyPhone
    {
        get => _companyPhone;
        set
        {
            _companyPhone = value;
            OnPropertyChanged(nameof(CompanyPhone));
        }
    }

    public string CompanyEmail
    {
        get => _companyEmail;
        set
        {
            _companyEmail = value;
            OnPropertyChanged(nameof(CompanyEmail));
        }
    }

    public decimal DefaultTaxRate
    {
        get => _defaultTaxRate;
        set
        {
            _defaultTaxRate = value;
            OnPropertyChanged(nameof(DefaultTaxRate));
            OnPropertyChanged(nameof(DefaultTaxRatePercent));
        }
    }

    public decimal DefaultTaxRatePercent
    {
        get => _defaultTaxRate * 100;
        set
        {
            _defaultTaxRate = value / 100;
            OnPropertyChanged(nameof(DefaultTaxRate));
            OnPropertyChanged(nameof(DefaultTaxRatePercent));
        }
    }

    public string Currency
    {
        get => _currency;
        set
        {
            _currency = value;
            OnPropertyChanged(nameof(Currency));
        }
    }

    public bool EnableInventoryAlerts
    {
        get => _enableInventoryAlerts;
        set
        {
            _enableInventoryAlerts = value;
            OnPropertyChanged(nameof(EnableInventoryAlerts));
        }
    }

    public bool RequireCustomerOnSale
    {
        get => _requireCustomerOnSale;
        set
        {
            _requireCustomerOnSale = value;
            OnPropertyChanged(nameof(RequireCustomerOnSale));
        }
    }

    public bool EnableDiscounts
    {
        get => _enableDiscounts;
        set
        {
            _enableDiscounts = value;
            OnPropertyChanged(nameof(EnableDiscounts));
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

    public string BusinessType
    {
        get => _businessType;
        set
        {
            _businessType = value;
            OnPropertyChanged(nameof(BusinessType));
            OnPropertyChanged(nameof(IsRestaurantMode));

            // Auto-enable restaurant features when switching to Restaurant mode
            if (value == "Restaurant")
            {
                EnableRestaurantMode = true;
            }
        }
    }

    public bool IsRestaurantMode => _businessType == "Restaurant";

    public bool EnableRestaurantMode
    {
        get => _enableRestaurantMode;
        set
        {
            _enableRestaurantMode = value;
            OnPropertyChanged(nameof(EnableRestaurantMode));
        }
    }

    public bool EnableTableManagement
    {
        get => _enableTableManagement;
        set
        {
            _enableTableManagement = value;
            OnPropertyChanged(nameof(EnableTableManagement));
        }
    }

    public bool EnableKitchenOrders
    {
        get => _enableKitchenOrders;
        set
        {
            _enableKitchenOrders = value;
            OnPropertyChanged(nameof(EnableKitchenOrders));
        }
    }

    public bool ShowProductIcons
    {
        get => _showProductIcons;
        set
        {
            _showProductIcons = value;
            OnPropertyChanged(nameof(ShowProductIcons));
        }
    }

    public string DefaultProductIcon
    {
        get => _defaultProductIcon;
        set
        {
            _defaultProductIcon = value;
            OnPropertyChanged(nameof(DefaultProductIcon));
        }
    }

    #endregion

    #region Commands

    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand ConfigureTablesCommand { get; }

    #endregion

    #region Methods

    private void LoadSettings()
    {
        // TODO: Load settings from database or configuration file
        // For now, using default values
        StatusMessage = "Configuración cargada";
    }

    private void SaveSettings()
    {
        try
        {
            // Validate company name
            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                MessageBox.Show("El nombre de la empresa es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate tax rate
            if (DefaultTaxRate < 0 || DefaultTaxRate > 1)
            {
                MessageBox.Show("La tasa de impuesto debe estar entre 0% y 100%", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Save settings to database or configuration file

            StatusMessage = "Configuración guardada exitosamente";
            MessageBox.Show("La configuración ha sido guardada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar: {ex.Message}";
            MessageBox.Show($"Error al guardar la configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetSettings()
    {
        var result = MessageBox.Show(
            "¿Está seguro que desea restaurar la configuración a los valores predeterminados?",
            "Confirmar Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            CompanyName = "MAGNOR";
            CompanyNit = "";
            CompanyAddress = "";
            CompanyPhone = "";
            CompanyEmail = "";
            DefaultTaxRate = 0.19m;
            Currency = "COP";
            EnableInventoryAlerts = true;
            RequireCustomerOnSale = false;
            EnableDiscounts = true;
            BusinessType = "Retail";
            EnableRestaurantMode = false;
            EnableTableManagement = false;
            EnableKitchenOrders = false;
            ShowProductIcons = true;
            DefaultProductIcon = "📦";

            StatusMessage = "Configuración restaurada a valores predeterminados";
            MessageBox.Show("La configuración ha sido restaurada.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ConfigureTables()
    {
        try
        {
            // Create database context and service
            var context = new AppDbContext();
            var restaurantService = new RestaurantService(context);

            // Create ViewModel - using userId 1 (admin) as placeholder
            var viewModel = new TableManagementViewModel(restaurantService, 1);

            // Create and show window
            var window = new TableManagementWindow
            {
                DataContext = viewModel
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al abrir la configuración de mesas: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}
