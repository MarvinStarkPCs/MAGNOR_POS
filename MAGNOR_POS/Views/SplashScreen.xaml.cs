using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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
            3 => "Configurando servicios...",
            4 => "Preparando interfaz...",
            5 => "Casi listo...",
            _ => "Listo!"
        };

        // After all steps, show login window
        if (_progressStep >= 6)
        {
            _timer?.Stop();

            // Small delay before showing login
            var closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            closeTimer.Tick += (s, args) =>
            {
                closeTimer.Stop();
                ShowLoginWindow();
            };
            closeTimer.Start();
        }
    }

    private void ShowLoginWindow()
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        this.Close();
    }
}
