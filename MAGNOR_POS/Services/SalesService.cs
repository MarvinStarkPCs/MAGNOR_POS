using Microsoft.EntityFrameworkCore;
using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Enums;
using MAGNOR_POS.Models.Sales;
using MAGNOR_POS.Models.Inventory;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for managing sales and invoices
/// </summary>
public class SalesService
{
    private readonly AppDbContext _context;

    public SalesService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generate a unique invoice number (FAC-YYYYMMDD-XXXX)
    /// </summary>
    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"FAC-{today}-";

        var lastInvoice = await _context.Sales
            .Where(s => s.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(s => s.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastInvoice != null)
        {
            var lastNumberStr = lastInvoice.InvoiceNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    /// <summary>
    /// Process a sale: save to DB, update stock, return the sale
    /// </summary>
    public async Task<(bool Success, string Message, Sale? Sale)> ProcessSaleAsync(
        List<SaleItem> items,
        PaymentType paymentType,
        decimal amountPaid,
        int userId,
        int? customerId = null,
        decimal discountAmount = 0,
        string? notes = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate items
            if (items == null || items.Count == 0)
                return (false, "No hay productos en la venta", null);

            // Generate invoice number
            var invoiceNumber = await GenerateInvoiceNumberAsync();

            // Calculate totals
            decimal subtotal = 0;
            decimal taxAmount = 0;
            var saleDetails = new List<SaleDetail>();

            foreach (var item in items)
            {
                // Reload product from DB to get current stock
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    return (false, $"Producto no encontrado: {item.ProductName}", null);

                if (!product.IsActive)
                    return (false, $"Producto inactivo: {product.Name}", null);

                // Check stock
                if (product.TrackStock && !product.AllowNegativeStock && product.CurrentStock < item.Quantity)
                    return (false, $"Stock insuficiente para {product.Name}. Disponible: {product.CurrentStock:N0}", null);

                var itemSubtotal = item.Quantity * item.UnitPrice;
                var itemTax = itemSubtotal * item.TaxRate;
                var itemTotal = itemSubtotal + itemTax;

                subtotal += itemSubtotal;
                taxAmount += itemTax;

                saleDetails.Add(new SaleDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Subtotal = itemSubtotal,
                    TaxRate = item.TaxRate,
                    TaxAmount = itemTax,
                    Total = itemTotal
                });

                // Update stock
                if (product.TrackStock)
                {
                    product.CurrentStock -= item.Quantity;
                    product.UpdatedAt = DateTime.Now;
                    product.UpdatedBy = userId;
                }
            }

            var total = subtotal + taxAmount - discountAmount;
            var changeAmount = amountPaid - total;

            if (amountPaid < total && paymentType == PaymentType.Efectivo)
                return (false, $"Monto insuficiente. Total: ${total:N0}, Pagado: ${amountPaid:N0}", null);

            // Create sale
            var sale = new Sale
            {
                InvoiceNumber = invoiceNumber,
                SaleDate = DateTime.Now,
                Status = SaleStatus.Completada,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                DiscountAmount = discountAmount,
                Total = total,
                PaymentType = paymentType,
                AmountPaid = amountPaid,
                ChangeAmount = Math.Max(0, changeAmount),
                CustomerId = customerId,
                UserId = userId,
                Notes = notes,
                Details = saleDetails
            };

            // Add payment record
            sale.Payments.Add(new SalePayment
            {
                Amount = amountPaid,
                PaymentType = paymentType
            });

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Reload with navigation properties
            await _context.Entry(sale).Collection(s => s.Details).LoadAsync();
            foreach (var detail in sale.Details)
            {
                await _context.Entry(detail).Reference(d => d.Product).LoadAsync();
            }

            return (true, $"Venta {invoiceNumber} procesada exitosamente", sale);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error al procesar la venta: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Get sale by invoice number
    /// </summary>
    public async Task<Sale?> GetSaleByInvoiceAsync(string invoiceNumber)
    {
        return await _context.Sales
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .Include(s => s.Customer)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.InvoiceNumber == invoiceNumber);
    }

    /// <summary>
    /// Get today's sales
    /// </summary>
    public async Task<List<Sale>> GetTodaySalesAsync()
    {
        var today = DateTime.Today;
        return await _context.Sales
            .Include(s => s.Details)
            .Where(s => s.SaleDate.Date == today && s.Status == SaleStatus.Completada)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get all active products with categories
    /// </summary>
    public async Task<List<Product>> GetActiveProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Cancel a sale (anular)
    /// </summary>
    public async Task<(bool Success, string Message)> CancelSaleAsync(int saleId, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var sale = await _context.Sales
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                return (false, "Venta no encontrada");

            if (sale.Status == SaleStatus.Anulada)
                return (false, "La venta ya fue anulada");

            // Restore stock
            foreach (var detail in sale.Details)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product != null && product.TrackStock)
                {
                    product.CurrentStock += detail.Quantity;
                    product.UpdatedAt = DateTime.Now;
                    product.UpdatedBy = userId;
                }
            }

            sale.Status = SaleStatus.Anulada;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Venta anulada exitosamente");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error al anular la venta: {ex.Message}");
        }
    }
}

/// <summary>
/// Helper class for sale items before processing
/// </summary>
public class SaleItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
}
