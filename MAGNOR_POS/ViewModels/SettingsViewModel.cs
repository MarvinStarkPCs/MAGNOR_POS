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

    // Factus Electronic Invoicing
    private bool _factusEnabled = false;
    private bool _factusUseSandbox = true;
    private string _factusClientId = string.Empty;
    private string _factusClientSecret = string.Empty;
    private string _factusUsername = string.Empty;
    private string _factusPassword = string.Empty;
    private string _factusStatusMessage = string.Empty;

    private string _statusMessage = string.Empty;

    public SettingsViewModel()
    {
        // Initialize commands
        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
        ResetSettingsCommand = new RelayCommand(_ => ResetSettings());
        ConfigureTablesCommand = new RelayCommand(_ => ConfigureTables(), _ => EnableRestaurantMode);
        TestFactusConnectionCommand = new RelayCommand(async _ => await TestFactusConnectionAsync());

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

    // Factus Properties
    public bool FactusEnabled
    {
        get => _factusEnabled;
        set { _factusEnabled = value; OnPropertyChanged(nameof(FactusEnabled)); }
    }

    public bool FactusUseSandbox
    {
        get => _factusUseSandbox;
        set { _factusUseSandbox = value; OnPropertyChanged(nameof(FactusUseSandbox)); }
    }

    public string FactusClientId
    {
        get => _factusClientId;
        set { _factusClientId = value; OnPropertyChanged(nameof(FactusClientId)); }
    }

    public string FactusClientSecret
    {
        get => _factusClientSecret;
        set { _factusClientSecret = value; OnPropertyChanged(nameof(FactusClientSecret)); }
    }

    public string FactusUsername
    {
        get => _factusUsername;
        set { _factusUsername = value; OnPropertyChanged(nameof(FactusUsername)); }
    }

    public string FactusPassword
    {
        get => _factusPassword;
        set { _factusPassword = value; OnPropertyChanged(nameof(FactusPassword)); }
    }

    public string FactusStatusMessage
    {
        get => _factusStatusMessage;
        set { _factusStatusMessage = value; OnPropertyChanged(nameof(FactusStatusMessage)); }
    }

    #endregion

    #region Commands

    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand ConfigureTablesCommand { get; }
    public ICommand TestFactusConnectionCommand { get; }

    #endregion

    #region Methods

    private void LoadSettings()
    {
        // Load Factus settings from license data (synced from server)
        try
        {
            var license = LicenseService.GetLocalLicense();
            if (license != null)
            {
                FactusEnabled = license.FactusEnabled;
                FactusUseSandbox = license.FactusSandbox;
                FactusClientId = license.FactusClientId;
                FactusClientSecret = license.FactusClientSecret;
                FactusUsername = license.FactusUsername;
                FactusPassword = license.FactusPassword;

                if (FactusEnabled)
                {
                    FactusStatusMessage = FactusUseSandbox
                        ? "Modo Sandbox (configurado desde servidor)"
                        : "Modo Producción (configurado desde servidor)";
                }
            }
        }
        catch { /* Use defaults */ }

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

    /// <summary>
    /// Create FactusService from license data (synced from server)
    /// </summary>
    public static FactusService CreateFactusService()
    {
        var service = new FactusService();
        try
        {
            var license = LicenseService.GetLocalLicense();
            if (license != null)
            {
                service.IsEnabled = license.FactusEnabled;
                service.UseSandbox = license.FactusSandbox;
                service.ClientId = license.FactusClientId;
                service.ClientSecret = license.FactusClientSecret;
                service.Username = license.FactusUsername;
                service.Password = license.FactusPassword;
                service.BaseUrl = service.UseSandbox
                    ? "https://api-sandbox.factus.com.co"
                    : "https://api.factus.com.co";
            }
        }
        catch { /* Use defaults */ }
        return service;
    }

    private async Task TestFactusConnectionAsync()
    {
        try
        {
            FactusStatusMessage = "Probando conexión...";

            var factusService = new FactusService
            {
                ClientId = FactusClientId,
                ClientSecret = FactusClientSecret,
                Username = FactusUsername,
                Password = FactusPassword,
                UseSandbox = FactusUseSandbox
            };

            var (success, message) = await factusService.TestConnectionAsync();

            FactusStatusMessage = message;

            if (success)
            {
                MessageBox.Show("Conexión exitosa con Factus API.", "Factus", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Error: {message}", "Factus", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            FactusStatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error al conectar con Factus: {ex.Message}", "Factus", MessageBoxButton.OK, MessageBoxImage.Error);
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
