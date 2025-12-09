using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthenticationService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand ExitCommand { get; }

    public event EventHandler? LoginSuccessful;

    public LoginViewModel(AuthenticationService authService)
    {
        _authService = authService;
        LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
    }

    private bool CanLogin(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !IsLoading;
    }

    private async Task LoginAsync(object? parameter)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(Username, Password);

            if (result.Success)
            {
                // Notify successful login
                LoginSuccessful?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error inesperado: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
