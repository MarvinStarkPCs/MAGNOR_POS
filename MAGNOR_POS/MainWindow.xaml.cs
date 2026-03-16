using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MAGNOR_POS.Data;
using MAGNOR_POS.Services;
using MAGNOR_POS.ViewModels;
using MAGNOR_POS.Views;

namespace MAGNOR_POS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Load POS view by default
            LoadPOSView();
        }

        private void BtnPOS_Click(object sender, RoutedEventArgs e)
        {
            LoadPOSView();
            UpdateMenuSelection(BtnPOS);
        }

        private void BtnProducts_Click(object sender, RoutedEventArgs e)
        {
            LoadProductsView();
            UpdateMenuSelection(BtnProducts);
        }

        private void BtnCustomers_Click(object sender, RoutedEventArgs e)
        {
            LoadCustomersView();
            UpdateMenuSelection(BtnCustomers);
        }

        private void BtnPurchases_Click(object sender, RoutedEventArgs e)
        {
            LoadPurchasesView();
            UpdateMenuSelection(BtnPurchases);
        }

        private void BtnSuppliers_Click(object sender, RoutedEventArgs e)
        {
            LoadSuppliersView();
            UpdateMenuSelection(BtnSuppliers);
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            LoadReportsView();
            UpdateMenuSelection(BtnReports);
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            LoadUsersView();
            UpdateMenuSelection(BtnUsers);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            LoadSettingsView();
            UpdateMenuSelection(BtnSettings);
        }

        private void LoadPOSView()
        {
            TitleText.Text = "Punto de Venta";
            SubtitleText.Text = "Realiza ventas rápidas y gestiona el carrito de compra";
            MainContent.Content = new POSView();
        }

        private void LoadProductsView()
        {
            TitleText.Text = "Gestión de Productos";
            SubtitleText.Text = "Administra tu inventario y catálogo de productos";
            MainContent.Content = new ProductsView();
        }

        private void LoadCustomersView()
        {
            TitleText.Text = "Gestión de Clientes";
            SubtitleText.Text = "Administra tu base de clientes y consulta su historial";

            // Create database context and service
            var context = new AppDbContext();
            var customerService = new CustomerService(context);

            // Create ViewModel
            var viewModel = new CustomersViewModel(customerService);

            // Create and configure view
            var customersView = new CustomersView
            {
                DataContext = viewModel
            };

            MainContent.Content = customersView;
        }

        private void LoadPurchasesView()
        {
            TitleText.Text = "Gestión de Compras";
            SubtitleText.Text = "Administra tus órdenes de compra a proveedores";

            // Create database context and service
            var context = new AppDbContext();
            var purchaseService = new PurchaseService(context);

            // Create ViewModel - using userId 1 (admin) as placeholder
            var viewModel = new PurchasesViewModel(purchaseService, 1);

            // Create and configure view
            var purchasesView = new PurchasesView
            {
                DataContext = viewModel
            };

            MainContent.Content = purchasesView;
        }

        private void LoadSuppliersView()
        {
            TitleText.Text = "Gestión de Proveedores";
            SubtitleText.Text = "Administra tu base de proveedores y sus condiciones comerciales";

            // Create database context and service
            var context = new AppDbContext();
            var supplierService = new SupplierService(context);

            // Create ViewModel
            var viewModel = new SuppliersViewModel(supplierService);

            // Create and configure view
            var suppliersView = new SuppliersView
            {
                DataContext = viewModel
            };

            MainContent.Content = suppliersView;
        }

        private void LoadReportsView()
        {
            TitleText.Text = "Reportes y Estadísticas";
            SubtitleText.Text = "Visualiza el rendimiento y métricas de tu negocio";

            // Create database context and services
            var context = new AppDbContext();
            var customerService = new CustomerService(context);
            var purchaseService = new PurchaseService(context);
            var supplierService = new SupplierService(context);

            // Create ViewModel
            var viewModel = new ReportsViewModel(customerService, purchaseService, supplierService);

            // Create and configure view
            var reportsView = new ReportsView
            {
                DataContext = viewModel
            };

            MainContent.Content = reportsView;
        }

        private void LoadUsersView()
        {
            TitleText.Text = "Gestión de Usuarios y Roles";
            SubtitleText.Text = "Administra los usuarios del sistema y asigna roles";

            // Create database context and service
            var context = new AppDbContext();
            var userService = new UserService(context);

            // Create ViewModel - using userId 1 (admin) as placeholder
            var viewModel = new UsersViewModel(userService, 1);

            // Create and configure view
            var usersView = new UsersView
            {
                DataContext = viewModel
            };

            MainContent.Content = usersView;
        }

        private void LoadSettingsView()
        {
            TitleText.Text = "Configuración del Sistema";
            SubtitleText.Text = "Administra los ajustes y preferencias del sistema";

            // Create ViewModel
            var viewModel = new SettingsViewModel();

            // Create and configure view
            var settingsView = new SettingsView
            {
                DataContext = viewModel
            };

            MainContent.Content = settingsView;
        }

        private void UpdateMenuSelection(Button selectedButton)
        {
            // Reset all buttons
            BtnPOS.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
            BtnProducts.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
            BtnCustomers.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
            BtnPurchases.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
            BtnSuppliers.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
            BtnReports.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
            BtnUsers.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
            BtnSettings.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));

            // Highlight selected button
            selectedButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
        }
    }
}