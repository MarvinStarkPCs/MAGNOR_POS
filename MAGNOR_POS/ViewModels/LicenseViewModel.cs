using System.Windows.Input;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.ViewModels;

public class LicenseViewModel : ViewModelBase
{
    private string _licenseKey = string.Empty;
    private string _hardwareId = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isLoading;

    public string LicenseKey
    {
        get => _licenseKey;
        set => SetProperty(ref _licenseKey, value);
    }

    public string HardwareId
    {
        get => _hardwareId;
        set => SetProperty(ref _hardwareId, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            if (!string.IsNullOrEmpty(value))
                SuccessMessage = string.Empty;
        }
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            SetProperty(ref _successMessage, value);
            if (!string.IsNullOrEmpty(value))
                ErrorMessage = string.Empty;
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand ActivateCommand { get; }

    public event EventHandler? LicenseActivated;

    public LicenseViewModel()
    {
        HardwareId = LicenseService.GetHardwareId();
        ActivateCommand = new AsyncRelayCommand(ActivateAsync, _ => !IsLoading);
    }

    private async Task ActivateAsync(object? parameter)
    {
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            ErrorMessage = "Ingrese una clave de licencia";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var (success, message) = await LicenseService.ActivateLicenseAsync(LicenseKey);

            if (success)
            {
                SuccessMessage = message;
                LicenseActivated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = message;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
