using System.Windows;
using MAGNOR_POS.ViewModels;

namespace MAGNOR_POS.Views
{
    public partial class LicenseWindow : Window
    {
        private readonly LicenseViewModel _viewModel;

        public bool IsLicenseActivated { get; private set; }

        public LicenseWindow()
        {
            InitializeComponent();
            _viewModel = new LicenseViewModel();
            DataContext = _viewModel;

            _viewModel.LicenseActivated += OnLicenseActivated;
        }

        private async void OnLicenseActivated(object? sender, EventArgs e)
        {
            IsLicenseActivated = true;
            // Pequeña pausa para que el usuario vea el mensaje de éxito
            await Task.Delay(1500);
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
