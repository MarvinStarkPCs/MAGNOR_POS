using System.Windows;
using MAGNOR_POS.Models;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.Views;

public partial class UserFormWindow : Window
{
    private readonly UserService _userService;
    private readonly int _currentUserId;
    private readonly User? _editingUser;
    private readonly bool _isEditMode;

    public UserFormWindow(UserService userService, int currentUserId, User? editingUser = null)
    {
        InitializeComponent();

        _userService = userService;
        _currentUserId = currentUserId;
        _editingUser = editingUser;
        _isEditMode = editingUser != null;

        Loaded += async (s, e) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Load roles
            var roles = await _userService.GetAllRolesAsync();
            RoleComboBox.ItemsSource = roles;

            // Configure form for edit mode
            if (_isEditMode && _editingUser != null)
            {
                TitleText.Text = "Editar Usuario";
                FullNameTextBox.Text = _editingUser.FullName;
                UsernameTextBox.Text = _editingUser.Username;
                EmailTextBox.Text = _editingUser.Email ?? string.Empty;
                RoleComboBox.SelectedValue = _editingUser.RoleId;
                IsActiveCheckBox.IsChecked = _editingUser.IsActive;

                // Hide password fields in edit mode
                PasswordSectionTitle.Text = "Cambiar Contraseña (Opcional)";
                ChangePasswordCheckBox.Visibility = Visibility.Visible;
                PasswordLabel.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordLabel.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Select first role by default
                if (roles.Count > 0)
                {
                    RoleComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChangePasswordCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        PasswordLabel.Visibility = Visibility.Visible;
        PasswordBox.Visibility = Visibility.Visible;
        ConfirmPasswordLabel.Visibility = Visibility.Visible;
        ConfirmPasswordBox.Visibility = Visibility.Visible;
    }

    private void ChangePasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        PasswordLabel.Visibility = Visibility.Collapsed;
        PasswordBox.Visibility = Visibility.Collapsed;
        ConfirmPasswordLabel.Visibility = Visibility.Collapsed;
        ConfirmPasswordBox.Visibility = Visibility.Collapsed;
        PasswordBox.Clear();
        ConfirmPasswordBox.Clear();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                CustomMessageBox.Show("El nombre completo es requerido", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                CustomMessageBox.Show("El nombre de usuario es requerido", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameTextBox.Focus();
                return;
            }

            if (RoleComboBox.SelectedValue == null)
            {
                CustomMessageBox.Show("Debe seleccionar un rol", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleComboBox.Focus();
                return;
            }

            // Validate password for new users or when changing password
            string? password = null;
            if (!_isEditMode)
            {
                // New user - password is required
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    CustomMessageBox.Show("La contraseña es requerida", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (PasswordBox.Password.Length < 6)
                {
                    CustomMessageBox.Show("La contraseña debe tener al menos 6 caracteres", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    CustomMessageBox.Show("Las contraseñas no coinciden", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return;
                }

                password = PasswordBox.Password;
            }
            else if (ChangePasswordCheckBox.IsChecked == true)
            {
                // Editing user and wants to change password
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    CustomMessageBox.Show("Ingrese la nueva contraseña", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (PasswordBox.Password.Length < 6)
                {
                    CustomMessageBox.Show("La contraseña debe tener al menos 6 caracteres", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    CustomMessageBox.Show("Las contraseñas no coinciden", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return;
                }

                password = PasswordBox.Password;
            }

            // Create or update user
            if (_isEditMode && _editingUser != null)
            {
                // Update existing user
                _editingUser.FullName = FullNameTextBox.Text.Trim();
                _editingUser.Username = UsernameTextBox.Text.Trim();
                _editingUser.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
                _editingUser.RoleId = (int)RoleComboBox.SelectedValue;
                _editingUser.IsActive = IsActiveCheckBox.IsChecked ?? true;

                var (success, message) = await _userService.UpdateUserAsync(_editingUser, _currentUserId, password);

                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Create new user
                var newUser = new User
                {
                    FullName = FullNameTextBox.Text.Trim(),
                    Username = UsernameTextBox.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim(),
                    RoleId = (int)RoleComboBox.SelectedValue,
                    PasswordHash = string.Empty, // Will be set by service
                    IsActive = IsActiveCheckBox.IsChecked ?? true
                };

                var (success, message, user) = await _userService.AddUserAsync(newUser, password!, _currentUserId);

                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al guardar el usuario: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
