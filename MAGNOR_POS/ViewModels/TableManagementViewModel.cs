using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Models.Restaurant;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.ViewModels;

/// <summary>
/// ViewModel for restaurant table and zone management
/// </summary>
public class TableManagementViewModel : ViewModelBase
{
    private readonly RestaurantService _restaurantService;
    private readonly int _currentUserId;

    private ObservableCollection<RestaurantZone> _zones = new();
    private ObservableCollection<RestaurantTable> _tables = new();
    private RestaurantZone? _selectedZone;
    private RestaurantTable? _selectedTable;
    private bool _isLoading = false;

    // Statistics
    private int _totalTables;
    private int _availableTables;
    private int _occupiedTables;
    private int _reservedTables;

    public TableManagementViewModel(RestaurantService restaurantService, int currentUserId)
    {
        _restaurantService = restaurantService;
        _currentUserId = currentUserId;

        // Initialize commands
        AddZoneCommand = new RelayCommand(_ => AddZone());
        EditZoneCommand = new RelayCommand(_ => EditZone(), _ => SelectedZone != null);
        DeleteZoneCommand = new RelayCommand(_ => DeleteZone(), _ => SelectedZone != null);

        AddTableCommand = new RelayCommand(_ => AddTable());
        EditTableCommand = new RelayCommand(_ => EditTable(), _ => SelectedTable != null);
        DeleteTableCommand = new RelayCommand(_ => DeleteTable(), _ => SelectedTable != null);

        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
    }

    #region Properties

    public ObservableCollection<RestaurantZone> Zones
    {
        get => _zones;
        set
        {
            _zones = value;
            OnPropertyChanged(nameof(Zones));
        }
    }

    public ObservableCollection<RestaurantTable> Tables
    {
        get => _tables;
        set
        {
            _tables = value;
            OnPropertyChanged(nameof(Tables));
        }
    }

    public RestaurantZone? SelectedZone
    {
        get => _selectedZone;
        set
        {
            _selectedZone = value;
            OnPropertyChanged(nameof(SelectedZone));
            _ = LoadTablesForSelectedZone();
        }
    }

    public RestaurantTable? SelectedTable
    {
        get => _selectedTable;
        set
        {
            _selectedTable = value;
            OnPropertyChanged(nameof(SelectedTable));
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

    public int TotalTables
    {
        get => _totalTables;
        set
        {
            _totalTables = value;
            OnPropertyChanged(nameof(TotalTables));
        }
    }

    public int AvailableTables
    {
        get => _availableTables;
        set
        {
            _availableTables = value;
            OnPropertyChanged(nameof(AvailableTables));
        }
    }

    public int OccupiedTables
    {
        get => _occupiedTables;
        set
        {
            _occupiedTables = value;
            OnPropertyChanged(nameof(OccupiedTables));
        }
    }

    public int ReservedTables
    {
        get => _reservedTables;
        set
        {
            _reservedTables = value;
            OnPropertyChanged(nameof(ReservedTables));
        }
    }

    #endregion

    #region Commands

    public ICommand AddZoneCommand { get; }
    public ICommand EditZoneCommand { get; }
    public ICommand DeleteZoneCommand { get; }
    public ICommand AddTableCommand { get; }
    public ICommand EditTableCommand { get; }
    public ICommand DeleteTableCommand { get; }
    public ICommand RefreshCommand { get; }

    #endregion

    #region Methods

    public async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            var zones = await _restaurantService.GetAllZonesAsync();
            Zones = new ObservableCollection<RestaurantZone>(zones);

            await LoadStatisticsAsync();

            // If there's a selected zone, reload its tables
            if (SelectedZone != null)
            {
                await LoadTablesForSelectedZone();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTablesForSelectedZone()
    {
        if (SelectedZone == null)
        {
            Tables = new ObservableCollection<RestaurantTable>();
            return;
        }

        try
        {
            var tables = await _restaurantService.GetTablesByZoneAsync(SelectedZone.Id);
            Tables = new ObservableCollection<RestaurantTable>(tables);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar mesas: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            var stats = await _restaurantService.GetTableStatisticsAsync();
            TotalTables = stats.total;
            AvailableTables = stats.available;
            OccupiedTables = stats.occupied;
            ReservedTables = stats.reserved;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");
        }
    }

    private void AddZone()
    {
        var zoneName = PromptForInput("Nueva Zona", "Nombre de la zona (ej: Terraza, Salón Principal):");
        if (string.IsNullOrWhiteSpace(zoneName)) return;

        var zoneDescription = PromptForInput("Descripción", "Descripción (opcional):", allowEmpty: true);

        AddZoneAsync(zoneName, zoneDescription);
    }

    private async void AddZoneAsync(string name, string? description)
    {
        var zone = new RestaurantZone
        {
            Name = name,
            Description = description
        };

        var (success, message, createdZone) = await _restaurantService.AddZoneAsync(zone, _currentUserId);

        if (success && createdZone != null)
        {
            await LoadDataAsync();
            MessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditZone()
    {
        if (SelectedZone == null) return;

        var zoneName = PromptForInput("Editar Zona", "Nombre de la zona:", SelectedZone.Name);
        if (string.IsNullOrWhiteSpace(zoneName)) return;

        var zoneDescription = PromptForInput("Descripción", "Descripción (opcional):", SelectedZone.Description ?? "", allowEmpty: true);

        EditZoneAsync(zoneName, zoneDescription);
    }

    private async void EditZoneAsync(string name, string? description)
    {
        if (SelectedZone == null) return;

        SelectedZone.Name = name;
        SelectedZone.Description = description;

        var (success, message) = await _restaurantService.UpdateZoneAsync(SelectedZone, _currentUserId);

        if (success)
        {
            await LoadDataAsync();
            MessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteZone()
    {
        if (SelectedZone == null) return;

        var result = MessageBox.Show(
            $"¿Está seguro que desea eliminar la zona '{SelectedZone.Name}'?\n\n" +
            "Esta acción no se puede deshacer.",
            "Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var (success, message) = await _restaurantService.DeleteZoneAsync(SelectedZone.Id, _currentUserId);

            if (success)
            {
                await LoadDataAsync();
                MessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void AddTable()
    {
        if (Zones.Count == 0)
        {
            MessageBox.Show("Primero debe crear al menos una zona", "Información",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // TODO: Open TableFormWindow
        MessageBox.Show("Ventana de agregar mesa próximamente...", "Información",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void EditTable()
    {
        if (SelectedTable == null) return;

        // TODO: Open TableFormWindow for editing
        MessageBox.Show("Ventana de editar mesa próximamente...", "Información",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void DeleteTable()
    {
        if (SelectedTable == null) return;

        var result = MessageBox.Show(
            $"¿Está seguro que desea eliminar la mesa '{SelectedTable.TableNumber}'?\n\n" +
            "Esta acción no se puede deshacer.",
            "Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var (success, message) = await _restaurantService.DeleteTableAsync(SelectedTable.Id, _currentUserId);

            if (success)
            {
                await LoadDataAsync();
                MessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private string PromptForInput(string title, string message, string defaultValue = "", bool allowEmpty = false)
    {
        // Simple input dialog using InputBox pattern
        var input = Microsoft.VisualBasic.Interaction.InputBox(message, title, defaultValue);

        if (!allowEmpty && string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return input;
    }

    #endregion
}
