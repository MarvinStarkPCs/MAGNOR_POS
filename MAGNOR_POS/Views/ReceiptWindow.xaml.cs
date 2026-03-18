using System.Windows;
using System.Windows.Input;
using MAGNOR_POS.Models.Sales;
using MAGNOR_POS.Services;

namespace MAGNOR_POS.Views;

public partial class ReceiptWindow : Window
{
    private readonly Sale _sale;
    private readonly PrintService _printService;

    public ReceiptWindow(Sale sale)
    {
        InitializeComponent();
        _sale = sale;
        _printService = new PrintService();

        // Show receipt
        TxtInvoiceNumber.Text = sale.InvoiceNumber;
        TxtReceipt.Text = _printService.GenerateReceiptText(sale);

        // Allow dragging
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        };
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _printService.PrintReceipt(_sale);
            CustomMessageBox.Show("Factura enviada a la impresora.",
                "Impresion", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error al imprimir: {ex.Message}",
                "Error de impresion", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
