using System.Windows;
using System.Windows.Controls;
using MAGNOR_POS.Models.Enums;
using MAGNOR_POS.Models.Parties;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.Views;

/// <summary>
/// Interaction logic for SupplierFormWindow.xaml
/// </summary>
public partial class SupplierFormWindow : Window
{
    private readonly SupplierService _supplierService;
    private readonly Supplier? _editingSupplier;
    private readonly bool _isEditMode;

    public SupplierFormWindow(SupplierService supplierService, Supplier? editingSupplier = null)
    {
        InitializeComponent();

        _supplierService = supplierService;
        _editingSupplier = editingSupplier;
        _isEditMode = editingSupplier != null;

        if (_isEditMode && _editingSupplier != null)
        {
            HeaderText.Text = "Editar Proveedor";
            LoadSupplierData(_editingSupplier);
        }
    }

    private void LoadSupplierData(Supplier supplier)
    {
        CompanyNameTextBox.Text = supplier.CompanyName;
        ContactNameTextBox.Text = supplier.ContactName;

        // Set document type
        foreach (ComboBoxItem item in DocumentTypeComboBox.Items)
        {
            if (item.Tag is DocumentType docType && docType == supplier.DocumentType)
            {
                DocumentTypeComboBox.SelectedItem = item;
                break;
            }
        }

        DocumentNumberTextBox.Text = supplier.DocumentNumber;
        PhoneTextBox.Text = supplier.Phone;
        EmailTextBox.Text = supplier.Email;
        AddressTextBox.Text = supplier.Address;
        WebsiteTextBox.Text = supplier.Website;
        PaymentTermDaysTextBox.Text = supplier.PaymentTermDays.ToString();
        NotesTextBox.Text = supplier.Notes;
        IsActiveCheckBox.IsChecked = supplier.IsActive;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
        {
            MessageBox.Show("El nombre de la empresa es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            CompanyNameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(ContactNameTextBox.Text))
        {
            MessageBox.Show("El nombre del contacto es requerido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            ContactNameTextBox.Focus();
            return;
        }

        // Validate payment term days
        if (!int.TryParse(PaymentTermDaysTextBox.Text, out int paymentTermDays) || paymentTermDays < 0)
        {
            MessageBox.Show("El plazo de pago debe ser un número mayor o igual a 0", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            PaymentTermDaysTextBox.Focus();
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
                MessageBox.Show("El email no tiene un formato válido", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return;
            }
        }

        // Create or update supplier
        var supplier = _isEditMode && _editingSupplier != null ? _editingSupplier : new Supplier
        {
            CompanyName = string.Empty, // Required field, will be set below
            ContactName = string.Empty  // Required field, will be set below
        };

        supplier.CompanyName = CompanyNameTextBox.Text.Trim();
        supplier.ContactName = ContactNameTextBox.Text.Trim();
        supplier.DocumentType = (DocumentType)((ComboBoxItem)DocumentTypeComboBox.SelectedItem).Tag;
        supplier.DocumentNumber = string.IsNullOrWhiteSpace(DocumentNumberTextBox.Text) ? null : DocumentNumberTextBox.Text.Trim();
        supplier.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
        supplier.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
        supplier.Address = string.IsNullOrWhiteSpace(AddressTextBox.Text) ? null : AddressTextBox.Text.Trim();
        supplier.Website = string.IsNullOrWhiteSpace(WebsiteTextBox.Text) ? null : WebsiteTextBox.Text.Trim();
        supplier.PaymentTermDays = paymentTermDays;
        supplier.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
        supplier.IsActive = IsActiveCheckBox.IsChecked ?? true;

        try
        {
            SaveButton.IsEnabled = false;
            SaveButton.Content = "Guardando...";

            bool success;
            string message;

            if (_isEditMode)
            {
                (success, message) = await _supplierService.UpdateSupplierAsync(supplier);
            }
            else
            {
                (success, message, _) = await _supplierService.AddSupplierAsync(supplier);
            }

            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
