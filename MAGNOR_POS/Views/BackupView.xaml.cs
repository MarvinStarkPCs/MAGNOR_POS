using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.Views;

public partial class BackupView : UserControl
{
    public BackupView()
    {
        InitializeComponent();

        // Show license key
        var license = LicenseService.GetLocalLicense();
        if (license != null)
        {
            TxtLicenseKey.Text = $"Licencia: {license.LicenseKey}";
        }
        else
        {
            TxtLicenseKey.Text = "Sin licencia activa";
            BtnSync.IsEnabled = false;
        }

        // Load status on init
        Loaded += async (s, e) => await LoadStatusAsync();
    }

    private async void BtnSync_Click(object sender, RoutedEventArgs e)
    {
        BtnSync.IsEnabled = false;
        BtnSync.Content = "SINCRONIZANDO...";
        ProgressPanel.Visibility = Visibility.Visible;
        ResultPanel.Visibility = Visibility.Collapsed;
        TxtProgress.Text = "Enviando datos al servidor...";

        var (success, message, stats) = await BackupService.SyncAllAsync();

        ProgressPanel.Visibility = Visibility.Collapsed;
        ResultPanel.Visibility = Visibility.Visible;

        if (success)
        {
            ResultPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
            ResultPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#146e39"));
            ResultPanel.BorderThickness = new Thickness(1);
            TxtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#146e39"));

            string details = stats != null
                ? $"\n{stats.Customers} clientes, {stats.Products} productos, {stats.Sales} ventas, {stats.Suppliers} proveedores, {stats.Purchases} compras"
                : "";
            TxtResult.Text = $"Backup completado exitosamente.{details}";

            // Update stats
            if (stats != null)
            {
                TxtCustomers.Text = stats.Customers.ToString();
                TxtProducts.Text = stats.Products.ToString();
                TxtSales.Text = stats.Sales.ToString();
                TxtSuppliers.Text = stats.Suppliers.ToString();
                TxtPurchases.Text = stats.Purchases.ToString();
            }

            TxtLastSync.Text = $"Ultimo backup: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        }
        else
        {
            ResultPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
            ResultPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cc2128"));
            ResultPanel.BorderThickness = new Thickness(1);
            TxtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cc2128"));
            TxtResult.Text = $"Error: {message}";
        }

        BtnSync.IsEnabled = true;
        BtnSync.Content = "SINCRONIZAR AHORA";
    }

    private async void BtnCheckStatus_Click(object sender, RoutedEventArgs e)
    {
        await LoadStatusAsync();
    }

    private async Task LoadStatusAsync()
    {
        var (success, _, info) = await BackupService.GetStatusAsync();

        if (success && info != null)
        {
            TxtCustomers.Text = info.Customers.ToString();
            TxtProducts.Text = info.Products.ToString();
            TxtSales.Text = info.Sales.ToString();
            TxtSuppliers.Text = info.Suppliers.ToString();
            TxtPurchases.Text = info.Purchases.ToString();

            if (info.LastSync != null)
            {
                TxtLastSync.Text = $"Ultimo backup: {info.LastSync:dd/MM/yyyy HH:mm:ss}";
            }
        }
    }
}
