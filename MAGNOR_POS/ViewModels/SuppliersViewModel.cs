using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Models.Parties;
using MAGNOR_POS.Services;
using MAGNOR_POS.Views;

namespace MAGNOR_POS.ViewModels;

/// <summary>
/// ViewModel for managing suppliers
/// </summary>
public class SuppliersViewModel : ViewModelBase
{
    private readonly SupplierService _supplierService;

    private ObservableCollection<Supplier> _suppliers = new();
    private Supplier? _selectedSupplier;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private bool _showInactive;

    public SuppliersViewModel(SupplierService supplierService)
    {
        _supplierService = supplierService;

        // Initialize commands
        LoadSuppliersCommand = new RelayCommand(async _ => await LoadSuppliersAsync());
        SearchCommand = new RelayCommand(async _ => await SearchSuppliersAsync());
        AddSupplierCommand = new RelayCommand(_ => AddSupplier());
        EditSupplierCommand = new RelayCommand(_ => EditSupplier(), _ => SelectedSupplier != null);
        DeleteSupplierCommand = new RelayCommand(async _ => await DeleteSupplierAsync(), _ => SelectedSupplier != null);
        ToggleActiveCommand = new RelayCommand(async _ => await ToggleSupplierActiveAsync(), _ => SelectedSupplier != null);
        RefreshCommand = new RelayCommand(async _ => await LoadSuppliersAsync());
        ClearSearchCommand = new RelayCommand(_ => ClearSearch());

        // Load suppliers on initialization
        _ = LoadSuppliersAsync();
    }

    #region Properties

    public ObservableCollection<Supplier> Suppliers
    {
        get => _suppliers;
        set
        {
            _suppliers = value;
            OnPropertyChanged(nameof(Suppliers));
        }
    }

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            _selectedSupplier = value;
            OnPropertyChanged(nameof(SelectedSupplier));
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
            _ = LoadSuppliersAsync();
        }
    }

    #endregion

    #region Commands

    public ICommand LoadSuppliersCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AddSupplierCommand { get; }
    public ICommand EditSupplierCommand { get; }
    public ICommand DeleteSupplierCommand { get; }
    public ICommand ToggleActiveCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearSearchCommand { get; }

    #endregion

    #region Methods

    private async Task LoadSuppliersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Cargando proveedores...";

            var suppliers = await _supplierService.GetAllSuppliersAsync(ShowInactive);
            Suppliers = new ObservableCollection<Supplier>(suppliers);

            StatusMessage = $"{Suppliers.Count} proveedor(es) cargado(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar proveedores: {ex.Message}";
            CustomMessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchSuppliersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Buscando proveedores...";

            var suppliers = await _supplierService.SearchSuppliersAsync(SearchText);
            Suppliers = new ObservableCollection<Supplier>(suppliers);

            StatusMessage = $"{Suppliers.Count} proveedor(es) encontrado(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error en búsqueda: {ex.Message}";
            CustomMessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
        _ = LoadSuppliersAsync();
    }

    private void AddSupplier()
    {
        var formWindow = new SupplierFormWindow(_supplierService)
        {
            Owner = Application.Current.MainWindow
        };
        if (formWindow.ShowDialog() == true)
        {
            StatusMessage = "Proveedor agregado exitosamente";
            _ = LoadSuppliersAsync();
        }
    }

    private void EditSupplier()
    {
        if (SelectedSupplier == null) return;

        var formWindow = new SupplierFormWindow(_supplierService, SelectedSupplier)
        {
            Owner = Application.Current.MainWindow
        };
        if (formWindow.ShowDialog() == true)
        {
            StatusMessage = "Proveedor actualizado exitosamente";
            _ = LoadSuppliersAsync();
        }
    }

    private async Task DeleteSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        var result = CustomMessageBox.Show(
            $"¿Está seguro que desea eliminar el proveedor {SelectedSupplier.CompanyName}?\n\nEsta acción no se puede deshacer.",
            "Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                IsLoading = true;
                var (success, message) = await _supplierService.DeleteSupplierAsync(SelectedSupplier.Id);

                StatusMessage = message;

                if (success)
                {
                    CustomMessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSuppliersAsync();
                }
                else
                {
                    CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al eliminar: {ex.Message}";
                CustomMessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task ToggleSupplierActiveAsync()
    {
        if (SelectedSupplier == null) return;

        try
        {
            IsLoading = true;

            var (success, message) = SelectedSupplier.IsActive
                ? await _supplierService.DeactivateSupplierAsync(SelectedSupplier.Id)
                : await _supplierService.ReactivateSupplierAsync(SelectedSupplier.Id);

            StatusMessage = message;

            if (success)
            {
                await LoadSuppliersAsync();
            }
            else
            {
                CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            CustomMessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
