using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using MAGNOR_POS.Models.Sales;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for printing receipts/invoices on thermal printers
/// </summary>
public class PrintService
{
    // Receipt width in characters for 80mm thermal printer
    private const int RECEIPT_WIDTH = 42;
    private const string COMPANY_NAME = "MAGNOR POS";
    private const string COMPANY_NIT = "NIT: 000.000.000-0";
    private const string COMPANY_ADDRESS = "Direccion del negocio";
    private const string COMPANY_PHONE = "Tel: (000) 000-0000";

    private Sale? _currentSale;
    private string? _printerName;

    /// <summary>
    /// Print a receipt for a sale
    /// </summary>
    public void PrintReceipt(Sale sale, string? printerName = null)
    {
        _currentSale = sale;
        _printerName = printerName;

        var printDoc = new PrintDocument();

        if (!string.IsNullOrEmpty(_printerName))
        {
            printDoc.PrinterSettings.PrinterName = _printerName;
        }

        // Configure for thermal printer (80mm = ~302 pixels at 96dpi)
        printDoc.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 302, 0);
        printDoc.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);

        printDoc.PrintPage += PrintReceiptPage;
        printDoc.Print();
    }

    /// <summary>
    /// Preview receipt as text (for display or debugging)
    /// </summary>
    public string GenerateReceiptText(Sale sale)
    {
        var lines = new List<string>();

        // Header
        lines.Add(CenterText(COMPANY_NAME));
        lines.Add(CenterText(COMPANY_NIT));
        lines.Add(CenterText(COMPANY_ADDRESS));
        lines.Add(CenterText(COMPANY_PHONE));
        lines.Add(new string('=', RECEIPT_WIDTH));

        // Invoice info
        lines.Add($"Factura: {sale.InvoiceNumber}");
        lines.Add($"Fecha:   {sale.SaleDate:dd/MM/yyyy HH:mm:ss}");
        lines.Add($"Cajero:  {sale.User?.FullName ?? "N/A"}");

        if (sale.Customer != null)
        {
            lines.Add($"Cliente: {sale.Customer.FullName}");
            if (!string.IsNullOrEmpty(sale.Customer.DocumentNumber))
                lines.Add($"Doc:     {sale.Customer.DocumentNumber}");
        }
        else
        {
            lines.Add("Cliente: Consumidor Final");
        }

        lines.Add(new string('-', RECEIPT_WIDTH));

        // Column headers
        lines.Add(FormatColumns("CANT", "PRODUCTO", "TOTAL", 5, 25, 12));
        lines.Add(new string('-', RECEIPT_WIDTH));

        // Items
        foreach (var detail in sale.Details)
        {
            var productName = detail.Product?.Name ?? "Producto";
            if (productName.Length > 25)
                productName = productName[..22] + "...";

            lines.Add(FormatColumns(
                $"{detail.Quantity:N0}",
                productName,
                $"${detail.Total:N0}",
                5, 25, 12));

            // Show unit price if quantity > 1
            if (detail.Quantity > 1)
            {
                lines.Add($"     ${detail.UnitPrice:N0} x {detail.Quantity:N0}");
            }
        }

        lines.Add(new string('-', RECEIPT_WIDTH));

        // Totals
        lines.Add(FormatRight("Subtotal:", $"${sale.Subtotal:N0}"));
        lines.Add(FormatRight("IVA:", $"${sale.TaxAmount:N0}"));

        if (sale.DiscountAmount > 0)
        {
            lines.Add(FormatRight("Descuento:", $"-${sale.DiscountAmount:N0}"));
        }

        lines.Add(new string('=', RECEIPT_WIDTH));
        lines.Add(FormatRight("TOTAL:", $"${sale.Total:N0}"));
        lines.Add(new string('=', RECEIPT_WIDTH));

        // Payment info
        var paymentTypeName = sale.PaymentType switch
        {
            Models.Enums.PaymentType.Efectivo => "Efectivo",
            Models.Enums.PaymentType.Tarjeta => "Tarjeta",
            Models.Enums.PaymentType.Transferencia => "Transferencia",
            Models.Enums.PaymentType.Mixto => "Mixto",
            _ => "Otro"
        };

        lines.Add(FormatRight("Pago:", paymentTypeName));
        lines.Add(FormatRight("Recibido:", $"${sale.AmountPaid:N0}"));

        if (sale.ChangeAmount > 0)
        {
            lines.Add(FormatRight("Cambio:", $"${sale.ChangeAmount:N0}"));
        }

        lines.Add("");
        lines.Add(new string('-', RECEIPT_WIDTH));

        // Footer
        lines.Add(CenterText("Gracias por su compra!"));
        lines.Add(CenterText("Conserve su factura"));
        lines.Add("");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Get list of available printers
    /// </summary>
    public static List<string> GetAvailablePrinters()
    {
        var printers = new List<string>();
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            printers.Add(printer);
        }
        return printers;
    }

    /// <summary>
    /// Get default printer name
    /// </summary>
    public static string? GetDefaultPrinter()
    {
        var settings = new PrinterSettings();
        return settings.IsDefaultPrinter ? settings.PrinterName : null;
    }

    // --- Print event handler ---

    private void PrintReceiptPage(object sender, PrintPageEventArgs e)
    {
        if (_currentSale == null || e.Graphics == null) return;

        var g = e.Graphics;
        var font = new Font("Consolas", 8f, FontStyle.Regular);
        var fontBold = new Font("Consolas", 8f, FontStyle.Bold);
        var fontLarge = new Font("Consolas", 10f, FontStyle.Bold);
        var brush = Brushes.Black;

        float x = 5;
        float y = 5;
        float lineHeight = font.GetHeight(g) + 2;

        void DrawLine(string text, Font? f = null)
        {
            g.DrawString(text, f ?? font, brush, x, y);
            y += lineHeight;
        }

        void DrawLineBold(string text) => DrawLine(text, fontBold);
        void DrawLineLarge(string text)
        {
            g.DrawString(text, fontLarge, brush, x, y);
            y += fontLarge.GetHeight(g) + 2;
        }

        // Header
        DrawLineLarge(CenterText(COMPANY_NAME, 35));
        DrawLine(CenterText(COMPANY_NIT, 35));
        DrawLine(CenterText(COMPANY_ADDRESS, 35));
        DrawLine(CenterText(COMPANY_PHONE, 35));
        DrawLine(new string('=', 35));

        // Invoice info
        DrawLine($"Factura: {_currentSale.InvoiceNumber}");
        DrawLine($"Fecha:   {_currentSale.SaleDate:dd/MM/yyyy HH:mm}");
        DrawLine($"Cajero:  {_currentSale.User?.FullName ?? "N/A"}");

        if (_currentSale.Customer != null)
            DrawLine($"Cliente: {_currentSale.Customer.FullName}");
        else
            DrawLine("Cliente: Consumidor Final");

        DrawLine(new string('-', 35));
        DrawLineBold(FormatColumns("CANT", "PRODUCTO", "TOTAL", 5, 20, 10));
        DrawLine(new string('-', 35));

        // Items
        foreach (var detail in _currentSale.Details)
        {
            var name = detail.Product?.Name ?? "Producto";
            if (name.Length > 20) name = name[..17] + "...";

            DrawLine(FormatColumns(
                $"{detail.Quantity:N0}", name, $"${detail.Total:N0}",
                5, 20, 10));
        }

        DrawLine(new string('-', 35));

        // Totals
        DrawLine(FormatRight("Subtotal:", $"${_currentSale.Subtotal:N0}", 35));
        DrawLine(FormatRight("IVA:", $"${_currentSale.TaxAmount:N0}", 35));

        if (_currentSale.DiscountAmount > 0)
            DrawLine(FormatRight("Desc:", $"-${_currentSale.DiscountAmount:N0}", 35));

        DrawLine(new string('=', 35));
        DrawLineBold(FormatRight("TOTAL:", $"${_currentSale.Total:N0}", 35));
        DrawLine(new string('=', 35));

        // Payment
        DrawLine(FormatRight("Recibido:", $"${_currentSale.AmountPaid:N0}", 35));
        if (_currentSale.ChangeAmount > 0)
            DrawLineBold(FormatRight("Cambio:", $"${_currentSale.ChangeAmount:N0}", 35));

        DrawLine("");
        DrawLine(CenterText("Gracias por su compra!", 35));
        DrawLine(CenterText("Conserve su factura", 35));

        e.HasMorePages = false;

        // Dispose fonts
        font.Dispose();
        fontBold.Dispose();
        fontLarge.Dispose();
    }

    // --- Text formatting helpers ---

    private static string CenterText(string text, int width = RECEIPT_WIDTH)
    {
        if (text.Length >= width) return text;
        int padding = (width - text.Length) / 2;
        return text.PadLeft(padding + text.Length).PadRight(width);
    }

    private static string FormatRight(string label, string value, int width = RECEIPT_WIDTH)
    {
        int spaces = width - label.Length - value.Length;
        if (spaces < 1) spaces = 1;
        return label + new string(' ', spaces) + value;
    }

    private static string FormatColumns(string col1, string col2, string col3, int w1, int w2, int w3)
    {
        return col1.PadRight(w1) + col2.PadRight(w2) + col3.PadLeft(w3);
    }
}
