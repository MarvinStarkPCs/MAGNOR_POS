using System.Windows;
using System.Windows.Controls;
using MAGNOR_POS.Data;
using MAGNOR_POS.Services;
using MAGNOR_POS.ViewModels;

namespace MAGNOR_POS.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow()
    {
        InitializeComponent();

        // Initialize database
        using (var context = new AppDbContext())
        {
            DbInitializer.Initialize(context);
        }

        // Create ViewModel
        var authService = new AuthenticationService(new AppDbContext());
        _viewModel = new LoginViewModel(authService);
        _viewModel.LoginSuccessful += OnLoginSuccessful;

        DataContext = _viewModel;

        // Focus on username textbox
        Loaded += (s, e) => UsernameTextBox.Focus();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.Password = passwordBox.Password;
        }
    }

    private void OnLoginSuccessful(object? sender, EventArgs e)
    {
        // Open main window
        var mainWindow = new MainWindow();
        mainWindow.Show();

        // Close login window
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
