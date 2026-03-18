using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MAGNOR_POS.Models.Enums;
using MAGNOR_POS.Models.Parties;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.Views;

/// <summary>
/// Interaction logic for CustomerFormWindow.xaml
/// </summary>
public partial class CustomerFormWindow : Window
{
    private readonly CustomerService _customerService;
    private readonly Customer? _editingCustomer;
    private readonly bool _isEditMode;

    public CustomerFormWindow(CustomerService customerService, Customer? editingCustomer = null)
    {
        InitializeComponent();

        _customerService = customerService;
        _editingCustomer = editingCustomer;
        _isEditMode = editingCustomer != null;

        if (_isEditMode && _editingCustomer != null)
        {
            HeaderText.Text = "Editar Cliente";
            LoadCustomerData(_editingCustomer);
        }

        // Allow dragging
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        };

        Loaded += (s, e) => FullNameTextBox.Focus();
    }

    private void LoadCustomerData(Customer customer)
    {
        FullNameTextBox.Text = customer.FullName;

        // Set document type
        foreach (ComboBoxItem item in DocumentTypeComboBox.Items)
        {
            if (item.Tag is DocumentType docType && docType == customer.DocumentType)
            {
                DocumentTypeComboBox.SelectedItem = item;
                break;
            }
        }

        DocumentNumberTextBox.Text = customer.DocumentNumber;
        PhoneTextBox.Text = customer.Phone;
        EmailTextBox.Text = customer.Email;
        AddressTextBox.Text = customer.Address;
        CityTextBox.Text = customer.City;
        StateTextBox.Text = customer.State;
        PostalCodeTextBox.Text = customer.PostalCode;

        // Set customer type
        foreach (ComboBoxItem item in CustomerTypeComboBox.Items)
        {
            if (item.Tag is CustomerType custType && custType == customer.CustomerType)
            {
                CustomerTypeComboBox.SelectedItem = item;
                break;
            }
        }

        DiscountTextBox.Text = customer.DiscountPercentage.ToString();
        CreditLimitTextBox.Text = customer.CreditLimit.ToString();
        NotesTextBox.Text = customer.Notes;
        IsActiveCheckBox.IsChecked = customer.IsActive;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
        {
            CustomMessageBox.Show("El nombre completo es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            FullNameTextBox.Focus();
            return;
        }

        // Validate numeric fields
        if (!decimal.TryParse(DiscountTextBox.Text, out decimal discount) || discount < 0 || discount > 100)
        {
            CustomMessageBox.Show("El descuento debe ser un número entre 0 y 100", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            DiscountTextBox.Focus();
            return;
        }

        if (!decimal.TryParse(CreditLimitTextBox.Text, out decimal creditLimit) || creditLimit < 0)
        {
            CustomMessageBox.Show("El límite de crédito debe ser un número mayor o igual a 0", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            CreditLimitTextBox.Focus();
            return;
        }

        // Validate email if provided
        if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(EmailTextBox.Text);
                if (addr.Address != EmailTextBox.Text)
                {
                    throw new FormatException();
                }
            }
            catch
            {
                CustomMessageBox.Show("El email no tiene un formato válido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return;
            }
        }

        // Create or update customer
        var customer = _isEditMode && _editingCustomer != null ? _editingCustomer : new Customer
        {
            FullName = string.Empty // Required field, will be set below
        };

        customer.FullName = FullNameTextBox.Text.Trim();
        customer.DocumentType = (DocumentType)((ComboBoxItem)DocumentTypeComboBox.SelectedItem).Tag;
        customer.DocumentNumber = string.IsNullOrWhiteSpace(DocumentNumberTextBox.Text) ? null : DocumentNumberTextBox.Text.Trim();
        customer.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
        customer.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
        customer.Address = string.IsNullOrWhiteSpace(AddressTextBox.Text) ? null : AddressTextBox.Text.Trim();
        customer.City = string.IsNullOrWhiteSpace(CityTextBox.Text) ? null : CityTextBox.Text.Trim();
        customer.State = string.IsNullOrWhiteSpace(StateTextBox.Text) ? null : StateTextBox.Text.Trim();
        customer.PostalCode = string.IsNullOrWhiteSpace(PostalCodeTextBox.Text) ? null : PostalCodeTextBox.Text.Trim();
        customer.CustomerType = (CustomerType)((ComboBoxItem)CustomerTypeComboBox.SelectedItem).Tag;
        customer.DiscountPercentage = discount;
        customer.CreditLimit = creditLimit;
        customer.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
        customer.IsActive = IsActiveCheckBox.IsChecked ?? true;

        try
        {
            SaveButton.IsEnabled = false;
            SaveButton.Content = "Guardando...";

            bool success;
            string message;

            if (_isEditMode)
            {
                (success, message) = await _customerService.UpdateCustomerAsync(customer);
            }
            else
            {
                (success, message, _) = await _customerService.AddCustomerAsync(customer);
            }

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
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SaveButton.IsEnabled = true;
            SaveButton.Content = "Guardar";
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
