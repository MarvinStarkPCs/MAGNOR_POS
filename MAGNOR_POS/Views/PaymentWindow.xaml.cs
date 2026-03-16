using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Views;

public partial class PaymentWindow : Window
{
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public decimal ChangeAmount { get; private set; }
    public PaymentType SelectedPaymentType { get; private set; } = PaymentType.Efectivo;
    public bool PaymentConfirmed { get; private set; }

    public PaymentWindow(decimal total)
    {
        InitializeComponent();
        TotalAmount = total;
        TxtTotal.Text = $"$ {total:N0}";

        // Focus amount field
        Loaded += (s, e) =>
        {
            TxtAmountPaid.Focus();
            TxtAmountPaid.SelectAll();
        };

        // Allow dragging
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        };
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        PaymentConfirmed = false;
        DialogResult = false;
        Close();
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidatePayment()) return;

        PaymentConfirmed = true;
        DialogResult = true;
        Close();
    }

    private void PaymentType_Changed(object sender, RoutedEventArgs e)
    {
        // Guard: controls may not be initialized yet during InitializeComponent
        if (QuickAmountPanel == null || TxtAmountPaid == null) return;

        if (RbEfectivo.IsChecked == true)
        {
            SelectedPaymentType = PaymentType.Efectivo;
            QuickAmountPanel.Visibility = Visibility.Visible;
            TxtAmountPaid.IsEnabled = true;
            TxtAmountPaid.Text = "";
        }
        else if (RbTarjeta.IsChecked == true)
        {
            SelectedPaymentType = PaymentType.Tarjeta;
            QuickAmountPanel.Visibility = Visibility.Collapsed;
            TxtAmountPaid.Text = TotalAmount.ToString("N0");
            TxtAmountPaid.IsEnabled = false;
            UpdateChange();
        }
        else if (RbTransferencia.IsChecked == true)
        {
            SelectedPaymentType = PaymentType.Transferencia;
            QuickAmountPanel.Visibility = Visibility.Collapsed;
            TxtAmountPaid.Text = TotalAmount.ToString("N0");
            TxtAmountPaid.IsEnabled = false;
            UpdateChange();
        }
    }

    private void TxtAmountPaid_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow digits and dots/commas
        e.Handled = !Regex.IsMatch(e.Text, @"[\d.,]");
    }

    private void TxtAmountPaid_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateChange();
    }

    private void TxtAmountPaid_GotFocus(object sender, RoutedEventArgs e)
    {
        TxtAmountPaid.SelectAll();
    }

    private void QuickAmount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr)
        {
            if (decimal.TryParse(tagStr, out decimal amount))
            {
                if (amount == 0)
                {
                    // "Exacto" button
                    TxtAmountPaid.Text = TotalAmount.ToString("N0");
                }
                else
                {
                    TxtAmountPaid.Text = amount.ToString("N0");
                }
            }
        }
    }

    private void UpdateChange()
    {
        if (TxtAmountPaid == null || ChangePanel == null || BtnConfirm == null) return;

        var amountText = TxtAmountPaid.Text.Replace(".", "").Replace(",", "").Replace("$", "").Trim();

        if (decimal.TryParse(amountText, out decimal paid))
        {
            AmountPaid = paid;
            ChangeAmount = paid - TotalAmount;

            if (ChangeAmount >= 0)
            {
                ChangePanel.Visibility = Visibility.Visible;
                TxtChange.Text = $"$ {ChangeAmount:N0}";
                BtnConfirm.IsEnabled = true;

                if (ChangeAmount == 0)
                {
                    ChangePanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ChangePanel.Visibility = Visibility.Visible;
                TxtChange.Text = $"Faltan $ {Math.Abs(ChangeAmount):N0}";
                BtnConfirm.IsEnabled = false;
            }
        }
        else
        {
            ChangePanel.Visibility = Visibility.Collapsed;
            BtnConfirm.IsEnabled = false;
            AmountPaid = 0;
            ChangeAmount = 0;
        }
    }

    private bool ValidatePayment()
    {
        if (AmountPaid < TotalAmount && SelectedPaymentType == PaymentType.Efectivo)
        {
            MessageBox.Show("El monto recibido es insuficiente.",
                "Pago insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }
}
