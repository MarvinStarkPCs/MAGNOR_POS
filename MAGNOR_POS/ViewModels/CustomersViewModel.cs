using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Models.Parties;
using MAGNOR_POS.Services;
using MAGNOR_POS.Views;

namespace MAGNOR_POS.ViewModels;

/// <summary>
/// ViewModel for managing customers
/// </summary>
public class CustomersViewModel : ViewModelBase
{
    private readonly CustomerService _customerService;

    private ObservableCollection<Customer> _customers = new();
    private Customer? _selectedCustomer;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private bool _showInactive;

    public CustomersViewModel(CustomerService customerService)
    {
        _customerService = customerService;

        // Initialize commands
        LoadCustomersCommand = new RelayCommand(async _ => await LoadCustomersAsync());
        SearchCommand = new RelayCommand(async _ => await SearchCustomersAsync());
        AddCustomerCommand = new RelayCommand(_ => AddCustomer());
        EditCustomerCommand = new RelayCommand(_ => EditCustomer(), _ => SelectedCustomer != null);
        DeleteCustomerCommand = new RelayCommand(async _ => await DeleteCustomerAsync(), _ => SelectedCustomer != null);
        ToggleActiveCommand = new RelayCommand(async _ => await ToggleCustomerActiveAsync(), _ => SelectedCustomer != null);
        RefreshCommand = new RelayCommand(async _ => await LoadCustomersAsync());
        ClearSearchCommand = new RelayCommand(_ => ClearSearch());

        // Load customers on initialization
        _ = LoadCustomersAsync();
    }

    #region Properties

    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set
        {
            _customers = value;
            OnPropertyChanged(nameof(Customers));
        }
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            _selectedCustomer = value;
            OnPropertyChanged(nameof(SelectedCustomer));
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
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

    public bool ShowInactive
    {
        get => _showInactive;
        set
        {
            _showInactive = value;
            OnPropertyChanged(nameof(ShowInactive));
            _ = LoadCustomersAsync();
        }
    }

    #endregion

    #region Commands

    public ICommand LoadCustomersCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AddCustomerCommand { get; }
    public ICommand EditCustomerCommand { get; }
    public ICommand DeleteCustomerCommand { get; }
    public ICommand ToggleActiveCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearSearchCommand { get; }

    #endregion

    #region Methods

    private async Task LoadCustomersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Cargando clientes...";

            var customers = await _customerService.GetAllCustomersAsync(ShowInactive);
            Customers = new ObservableCollection<Customer>(customers);

            StatusMessage = $"{Customers.Count} cliente(s) cargado(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar clientes: {ex.Message}";
            MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchCustomersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Buscando clientes...";

            var customers = await _customerService.SearchCustomersAsync(SearchText);
            Customers = new ObservableCollection<Customer>(customers);

            StatusMessage = $"{Customers.Count} cliente(s) encontrado(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error en búsqueda: {ex.Message}";
            MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
        _ = LoadCustomersAsync();
    }

    private void AddCustomer()
    {
        var formWindow = new CustomerFormWindow(_customerService)
        {
            Owner = Application.Current.MainWindow
        };
        if (formWindow.ShowDialog() == true)
        {
            StatusMessage = "Cliente agregado exitosamente";
            _ = LoadCustomersAsync();
        }
    }

    private void EditCustomer()
    {
        if (SelectedCustomer == null) return;

        var formWindow = new CustomerFormWindow(_customerService, SelectedCustomer)
        {
            Owner = Application.Current.MainWindow
        };
        if (formWindow.ShowDialog() == true)
        {
            StatusMessage = "Cliente actualizado exitosamente";
            _ = LoadCustomersAsync();
        }
    }

    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        var result = MessageBox.Show(
            $"¿Está seguro que desea eliminar el cliente {SelectedCustomer.FullName}?\n\nEsta acción no se puede deshacer.",
            "Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                IsLoading = true;
                var (success, message) = await _customerService.DeleteCustomerAsync(SelectedCustomer.Id);

                StatusMessage = message;

                if (success)
                {
                    MessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCustomersAsync();
                }
                else
                {
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al eliminar: {ex.Message}";
                MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task ToggleCustomerActiveAsync()
    {
        if (SelectedCustomer == null) return;

        try
        {
            IsLoading = true;

            var (success, message) = SelectedCustomer.IsActive
                ? await _customerService.DeactivateCustomerAsync(SelectedCustomer.Id)
                : await _customerService.ReactivateCustomerAsync(SelectedCustomer.Id);

            StatusMessage = message;

            if (success)
            {
                await LoadCustomersAsync();
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
