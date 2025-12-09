using System.Windows;
using System.Windows.Input;

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

    private string _statusMessage = string.Empty;

    public SettingsViewModel()
    {
        // Initialize commands
        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
        ResetSettingsCommand = new RelayCommand(_ => ResetSettings());

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

    #endregion

    #region Commands

    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetSettingsCommand { get; }

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

            StatusMessage = "Configuración restaurada a valores predeterminados";
            MessageBox.Show("La configuración ha sido restaurada.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion
}
