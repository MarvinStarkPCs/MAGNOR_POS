using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Models;
using MAGNOR_POS.Services;
using MAGNOR_POS.Views;

namespace MAGNOR_POS.ViewModels;

/// <summary>
/// ViewModel for user and role management
/// </summary>
public class UsersViewModel : ViewModelBase
{
    private readonly UserService _userService;
    private readonly int _currentUserId;

    private ObservableCollection<User> _users = new();
    private User? _selectedUser;
    private string _searchText = string.Empty;
    private bool _showInactiveUsers = false;
    private bool _isLoading = false;

    // Statistics
    private int _totalUsers;
    private int _activeUsers;
    private int _inactiveUsers;

    public UsersViewModel(UserService userService, int currentUserId)
    {
        _userService = userService;
        _currentUserId = currentUserId;

        // Initialize commands
        AddUserCommand = new RelayCommand(_ => AddUser());
        EditUserCommand = new RelayCommand(_ => EditUser(), _ => SelectedUser != null);
        DeleteUserCommand = new RelayCommand(_ => DeleteUser(), _ => SelectedUser != null);
        ResetPasswordCommand = new RelayCommand(_ => ResetPassword(), _ => SelectedUser != null);
        RefreshCommand = new RelayCommand(async _ => await LoadUsersAsync());
        SearchCommand = new RelayCommand(async _ => await LoadUsersAsync());
    }

    #region Properties

    public ObservableCollection<User> Users
    {
        get => _users;
        set
        {
            _users = value;
            OnPropertyChanged(nameof(Users));
        }
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            _selectedUser = value;
            OnPropertyChanged(nameof(SelectedUser));
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

    public bool ShowInactiveUsers
    {
        get => _showInactiveUsers;
        set
        {
            _showInactiveUsers = value;
            OnPropertyChanged(nameof(ShowInactiveUsers));
            _ = LoadUsersAsync();
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

    public int TotalUsers
    {
        get => _totalUsers;
        set
        {
            _totalUsers = value;
            OnPropertyChanged(nameof(TotalUsers));
        }
    }

    public int ActiveUsers
    {
        get => _activeUsers;
        set
        {
            _activeUsers = value;
            OnPropertyChanged(nameof(ActiveUsers));
        }
    }

    public int InactiveUsers
    {
        get => _inactiveUsers;
        set
        {
            _inactiveUsers = value;
            OnPropertyChanged(nameof(InactiveUsers));
        }
    }

    #endregion

    #region Commands

    public ICommand AddUserCommand { get; }
    public ICommand EditUserCommand { get; }
    public ICommand DeleteUserCommand { get; }
    public ICommand ResetPasswordCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }

    #endregion

    #region Methods

    public async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;

            List<User> users;
            if (ShowInactiveUsers)
            {
                users = await _userService.GetAllUsersAsync();
            }
            else
            {
                users = await _userService.GetActiveUsersAsync();
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                users = users.Where(u =>
                    u.FullName.ToLower().Contains(searchLower) ||
                    u.Username.ToLower().Contains(searchLower) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                    (u.Role != null && u.Role.Name.ToLower().Contains(searchLower))
                ).ToList();
            }

            Users = new ObservableCollection<User>(users);

            // Load statistics
            await LoadStatisticsAsync();
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al cargar usuarios: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            var stats = await _userService.GetUserStatisticsAsync();
            TotalUsers = stats.total;
            ActiveUsers = stats.active;
            InactiveUsers = stats.inactive;
        }
        catch (Exception ex)
        {
            // Silently fail for statistics
            System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");
        }
    }

    private void AddUser()
    {
        try
        {
            var window = new UserFormWindow(_userService, _currentUserId);
            if (window.ShowDialog() == true)
            {
                _ = LoadUsersAsync();
                CustomMessageBox.Show("Usuario agregado exitosamente", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al abrir el formulario: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditUser()
    {
        if (SelectedUser == null) return;

        try
        {
            var window = new UserFormWindow(_userService, _currentUserId, SelectedUser);
            if (window.ShowDialog() == true)
            {
                _ = LoadUsersAsync();
                CustomMessageBox.Show("Usuario actualizado exitosamente", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al abrir el formulario: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteUser()
    {
        if (SelectedUser == null) return;

        var result = CustomMessageBox.Show(
            $"¿Está seguro que desea desactivar al usuario '{SelectedUser.FullName}'?\n\n" +
            "El usuario no podrá iniciar sesión pero sus registros se conservarán.",
            "Confirmar Desactivación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var (success, message) = await _userService.DeleteUserAsync(SelectedUser.Id, _currentUserId);

            if (success)
            {
                await LoadUsersAsync();
                CustomMessageBox.Show(message, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ResetPassword()
    {
        if (SelectedUser == null) return;

        var result = CustomMessageBox.Show(
            $"¿Está seguro que desea restablecer la contraseña del usuario '{SelectedUser.FullName}'?\n\n" +
            "Se generará una contraseña temporal que deberá comunicarle al usuario.",
            "Confirmar Restablecimiento",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var (success, message, tempPassword) = await _userService.ResetPasswordAsync(SelectedUser.Id, _currentUserId);

            if (success && tempPassword != null)
            {
                CustomMessageBox.Show(
                    $"Contraseña restablecida exitosamente.\n\n" +
                    $"Contraseña temporal: {tempPassword}\n\n" +
                    "Asegúrese de comunicar esta contraseña al usuario de forma segura.",
                    "Contraseña Restablecida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion
}
