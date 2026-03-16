using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.Views;

public partial class SplashScreen : Window
{
    private DispatcherTimer? _timer;
    private int _progressStep = 0;

    public SplashScreen()
    {
        InitializeComponent();

        // Start animations
        Loaded += SplashScreen_Loaded;
    }

    private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
    {
        // Start pulse animation for logo
        var pulseAnimation = (Storyboard)FindResource("PulseAnimation");
        pulseAnimation.Begin();

        // Start loading bar animation
        var loadingAnimation = (Storyboard)FindResource("LoadingAnimation");
        loadingAnimation.Begin();

        // Start spinner animation
        var spinAnimation = (Storyboard)FindResource("SpinAnimation");
        spinAnimation.Begin();

        // Simulate loading progress
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _progressStep++;

        // Update loading text
        LoadingText.Text = _progressStep switch
        {
            1 => "Iniciando aplicación...",
            2 => "Cargando base de datos...",
            3 => "Verificando licencia...",
            4 => "Configurando servicios...",
            5 => "Preparando interfaz...",
            6 => "Casi listo...",
            _ => "Listo!"
        };

        // After all steps, validate license and proceed
        if (_progressStep >= 7)
        {
            _timer?.Stop();

            // Small delay before proceeding
            var closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            closeTimer.Tick += async (s, args) =>
            {
                closeTimer.Stop();
                await ValidateLicenseAndProceed();
            };
            closeTimer.Start();
        }
    }

    private async Task ValidateLicenseAndProceed()
    {
        // Verificar si hay licencia local
        if (!LicenseService.HasLocalLicense())
        {
            // No hay licencia -> mostrar ventana de activación
            ShowLicenseWindow();
            return;
        }

        // Hay licencia local -> validar contra el servidor
        var (isValid, message) = await LicenseService.ValidateLicenseAsync();

        if (isValid)
        {
            ShowLoginWindow();
        }
        else
        {
            // Licencia inválida o expirada -> mostrar ventana de activación
            MessageBox.Show(message, "Licencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            ShowLicenseWindow();
        }
    }

    private void ShowLicenseWindow()
    {
        var licenseWindow = new LicenseWindow();
        this.Hide();
        var result = licenseWindow.ShowDialog();

        if (result == true && licenseWindow.IsLicenseActivated)
        {
            ShowLoginWindow();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }

    private void ShowLoginWindow()
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        this.Close();
    }
}
