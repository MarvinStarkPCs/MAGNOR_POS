using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Models.Enums;
using MAGNOR_POS.Models.Purchases;
using MAGNOR_POS.Services;
using MAGNOR_POS.Views;

namespace MAGNOR_POS.ViewModels;

/// <summary>
/// ViewModel for managing purchases
/// </summary>
public class PurchasesViewModel : ViewModelBase
{
    private readonly PurchaseService _purchaseService;
    private readonly int _currentUserId;

    private ObservableCollection<Purchase> _purchases = new();
    private Purchase? _selectedPurchase;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private PurchaseStatus? _selectedStatus;

    public PurchasesViewModel(PurchaseService purchaseService, int currentUserId)
    {
        _purchaseService = purchaseService;
        _currentUserId = currentUserId;

        // Initialize commands
        LoadPurchasesCommand = new RelayCommand(async _ => await LoadPurchasesAsync());
        SearchCommand = new RelayCommand(async _ => await SearchPurchasesAsync());
        AddPurchaseCommand = new RelayCommand(_ => AddPurchase());
        ViewPurchaseCommand = new RelayCommand(_ => ViewPurchase(), _ => SelectedPurchase != null);
        ReceivePurchaseCommand = new RelayCommand(async _ => await ReceivePurchaseAsync(), _ => SelectedPurchase != null && SelectedPurchase.Status == PurchaseStatus.Pendiente);
        CancelPurchaseCommand = new RelayCommand(async _ => await CancelPurchaseAsync(), _ => SelectedPurchase != null && SelectedPurchase.Status != PurchaseStatus.Recibida);
        DeletePurchaseCommand = new RelayCommand(async _ => await DeletePurchaseAsync(), _ => SelectedPurchase != null && SelectedPurchase.Status != PurchaseStatus.Recibida);
        RefreshCommand = new RelayCommand(async _ => await LoadPurchasesAsync());
        ClearSearchCommand = new RelayCommand(_ => ClearSearch());

        // Load purchases on initialization
        _ = LoadPurchasesAsync();
    }

    #region Properties

    public ObservableCollection<Purchase> Purchases
    {
        get => _purchases;
        set
        {
            _purchases = value;
            OnPropertyChanged(nameof(Purchases));
        }
    }

    public Purchase? SelectedPurchase
    {
        get => _selectedPurchase;
        set
        {
            _selectedPurchase = value;
            OnPropertyChanged(nameof(SelectedPurchase));
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

    public PurchaseStatus? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            _selectedStatus = value;
            OnPropertyChanged(nameof(SelectedStatus));
            _ = LoadPurchasesAsync();
        }
    }

    #endregion

    #region Commands

    public ICommand LoadPurchasesCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AddPurchaseCommand { get; }
    public ICommand ViewPurchaseCommand { get; }
    public ICommand ReceivePurchaseCommand { get; }
    public ICommand CancelPurchaseCommand { get; }
    public ICommand DeletePurchaseCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearSearchCommand { get; }

    #endregion

    #region Methods

    private async Task LoadPurchasesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Cargando compras...";

            var purchases = await _purchaseService.GetAllPurchasesAsync(SelectedStatus);
            Purchases = new ObservableCollection<Purchase>(purchases);

            StatusMessage = $"{Purchases.Count} compra(s) cargada(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar compras: {ex.Message}";
            CustomMessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchPurchasesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Buscando compras...";

            var purchases = await _purchaseService.SearchPurchasesAsync(SearchText);
            Purchases = new ObservableCollection<Purchase>(purchases);

            StatusMessage = $"{Purchases.Count} compra(s) encontrada(s)";
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
        _ = LoadPurchasesAsync();
    }

    private void AddPurchase()
    {
        try
        {
            var formWindow = new PurchaseFormWindow(_purchaseService, _currentUserId)
            {
                Owner = Application.Current.MainWindow
            };
            if (formWindow.ShowDialog() == true)
            {
                StatusMessage = "Compra registrada exitosamente";
                _ = LoadPurchasesAsync();
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al abrir formulario: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ViewPurchase()
    {
        if (SelectedPurchase == null) return;

        var formWindow = new PurchaseFormWindow(_purchaseService, _currentUserId, SelectedPurchase)
        {
            Owner = Application.Current.MainWindow
        };
        formWindow.ShowDialog();
    }

    private async Task ReceivePurchaseAsync()
    {
        if (SelectedPurchase == null) return;

        var result = CustomMessageBox.Show(
            $"¿Está seguro que desea recibir la compra {SelectedPurchase.PurchaseNumber}?\n\nEsto actualizará el inventario de productos.",
            "Confirmar Recepción",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                IsLoading = true;
                var (success, message) = await _purchaseService.ReceivePurchaseAsync(SelectedPurchase.Id, _currentUserId);

                StatusMessage = message;

                if (success)
                {
                    CustomMessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadPurchasesAsync();
                }
                else
                {
                    CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al recibir: {ex.Message}";
                CustomMessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task CancelPurchaseAsync()
    {
        if (SelectedPurchase == null) return;

        var result = CustomMessageBox.Show(
            $"¿Está seguro que desea cancelar la compra {SelectedPurchase.PurchaseNumber}?",
            "Confirmar Cancelación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                IsLoading = true;
                var (success, message) = await _purchaseService.CancelPurchaseAsync(SelectedPurchase.Id);

                StatusMessage = message;

                if (success)
                {
                    CustomMessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadPurchasesAsync();
                }
                else
                {
                    CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cancelar: {ex.Message}";
                CustomMessageBox.Show(StatusMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task DeletePurchaseAsync()
    {
        if (SelectedPurchase == null) return;

        var result = CustomMessageBox.Show(
            $"¿Está seguro que desea eliminar la compra {SelectedPurchase.PurchaseNumber}?\n\nEsta acción no se puede deshacer.",
            "Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                IsLoading = true;
                var (success, message) = await _purchaseService.DeletePurchaseAsync(SelectedPurchase.Id);

                StatusMessage = message;

                if (success)
                {
                    CustomMessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadPurchasesAsync();
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

    #endregion
}
