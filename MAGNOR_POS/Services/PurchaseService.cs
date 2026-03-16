using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Purchases;
using MAGNOR_POS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for managing purchase operations
/// </summary>
public class PurchaseService
{
    private readonly AppDbContext _context;

    public PurchaseService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all purchases with filtering options
    /// </summary>
    public async Task<List<Purchase>> GetAllPurchasesAsync(PurchaseStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.User)
            .Include(p => p.Warehouse)
            .Include(p => p.Details)
            .ThenInclude(d => d.Product)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.PurchaseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.PurchaseDate <= toDate.Value);
        }

        return await query.OrderByDescending(p => p.PurchaseDate).ToListAsync();
    }

    /// <summary>
    /// Get purchase by ID
    /// </summary>
    public async Task<Purchase?> GetPurchaseByIdAsync(int purchaseId)
    {
        return await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.User)
            .Include(p => p.Warehouse)
            .Include(p => p.Details)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);
    }

    /// <summary>
    /// Search purchases by number or supplier
    /// </summary>
    public async Task<List<Purchase>> SearchPurchasesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllPurchasesAsync();
        }

        searchTerm = searchTerm.ToLower().Trim();

        return await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.User)
            .Include(p => p.Warehouse)
            .Where(p =>
                p.PurchaseNumber.ToLower().Contains(searchTerm) ||
                (p.SupplierInvoiceNumber != null && p.SupplierInvoiceNumber.ToLower().Contains(searchTerm)) ||
                (p.Supplier != null && p.Supplier.CompanyName.ToLower().Contains(searchTerm))
            )
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();
    }

    /// <summary>
    /// Generate next purchase number
    /// </summary>
    public async Task<string> GenerateNextPurchaseNumberAsync()
    {
        var lastPurchase = await _context.Purchases
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastPurchase != null && lastPurchase.PurchaseNumber.StartsWith("COM-"))
        {
            var numberPart = lastPurchase.PurchaseNumber.Substring(4);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"COM-{nextNumber:D6}";
    }

    /// <summary>
    /// Add a new purchase
    /// </summary>
    public async Task<(bool Success, string Message, Purchase? Purchase)> AddPurchaseAsync(Purchase purchase, int userId)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(purchase.PurchaseNumber))
            {
                purchase.PurchaseNumber = await GenerateNextPurchaseNumberAsync();
            }

            if (purchase.SupplierId == 0)
                return (false, "Debe seleccionar un proveedor", null);

            if (purchase.Details == null || !purchase.Details.Any())
                return (false, "Debe agregar al menos un producto", null);

            // Calculate totals
            purchase.Subtotal = purchase.Details.Sum(d => d.Subtotal);
            purchase.TaxAmount = purchase.Details.Sum(d => d.TaxAmount);
            purchase.DiscountAmount = purchase.Details.Sum(d => d.DiscountAmount);
            purchase.Total = purchase.Details.Sum(d => d.Total);
            purchase.Balance = purchase.Total - purchase.AmountPaid;

            purchase.UserId = userId;
            purchase.PurchaseDate = DateTime.Now;

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            return (true, "Compra registrada exitosamente", purchase);
        }
        catch (Exception ex)
        {
            return (false, $"Error al registrar compra: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update an existing purchase
    /// </summary>
    public async Task<(bool Success, string Message)> UpdatePurchaseAsync(Purchase purchase)
    {
        try
        {
            var existing = await _context.Purchases
                .Include(p => p.Details)
                .FirstOrDefaultAsync(p => p.Id == purchase.Id);

            if (existing == null)
            {
                return (false, "Compra no encontrada");
            }

            // Only allow updates if not received
            if (existing.Status == PurchaseStatus.Recibida)
            {
                return (false, "No se puede modificar una compra que ya fue recibida");
            }

            // Update properties
            existing.SupplierInvoiceNumber = purchase.SupplierInvoiceNumber;
            existing.DeliveryDate = purchase.DeliveryDate;
            existing.Notes = purchase.Notes;
            existing.AmountPaid = purchase.AmountPaid;
            existing.Balance = existing.Total - existing.AmountPaid;

            await _context.SaveChangesAsync();

            return (true, "Compra actualizada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar compra: {ex.Message}");
        }
    }

    /// <summary>
    /// Receive purchase and update inventory
    /// </summary>
    public async Task<(bool Success, string Message)> ReceivePurchaseAsync(int purchaseId, int userId)
    {
        try
        {
            var purchase = await _context.Purchases
                .Include(p => p.Details)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                return (false, "Compra no encontrada");
            }

            if (purchase.Status == PurchaseStatus.Recibida)
            {
                return (false, "Esta compra ya fue recibida");
            }

            // Update product inventory
            foreach (var detail in purchase.Details)
            {
                if (detail.Product != null)
                {
                    detail.Product.CurrentStock += detail.Quantity;
                    detail.Product.PurchasePrice = detail.UnitCost;
                    detail.QuantityReceived = detail.Quantity;

                    // Create stock movement
                    var movement = new Models.Inventory.StockMovement
                    {
                        ProductId = detail.ProductId,
                        WarehouseId = purchase.WarehouseId,
                        MovementType = InventoryMovementType.Compra,
                        Quantity = detail.Quantity,
                        UnitCost = detail.UnitCost,
                        TotalCost = detail.Total,
                        BalanceAfter = detail.Product.CurrentStock,
                        MovementDate = DateTime.Now,
                        UserId = userId,
                        Reference = purchase.PurchaseNumber,
                        Notes = $"Compra recibida - {purchase.Supplier?.CompanyName}"
                    };

                    _context.StockMovements.Add(movement);
                }
            }

            purchase.Status = PurchaseStatus.Recibida;
            purchase.DeliveryDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, "Compra recibida e inventario actualizado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al recibir compra: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancel a purchase
    /// </summary>
    public async Task<(bool Success, string Message)> CancelPurchaseAsync(int purchaseId)
    {
        try
        {
            var purchase = await _context.Purchases.FindAsync(purchaseId);
            if (purchase == null)
            {
                return (false, "Compra no encontrada");
            }

            if (purchase.Status == PurchaseStatus.Recibida)
            {
                return (false, "No se puede cancelar una compra que ya fue recibida");
            }

            purchase.Status = PurchaseStatus.Cancelada;
            await _context.SaveChangesAsync();

            return (true, "Compra cancelada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al cancelar compra: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a purchase permanently
    /// </summary>
    public async Task<(bool Success, string Message)> DeletePurchaseAsync(int purchaseId)
    {
        try
        {
            var purchase = await _context.Purchases
                .Include(p => p.Details)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                return (false, "Compra no encontrada");
            }

            if (purchase.Status == PurchaseStatus.Recibida)
            {
                return (false, "No se puede eliminar una compra que ya fue recibida");
            }

            _context.Purchases.Remove(purchase);
            await _context.SaveChangesAsync();

            return (true, "Compra eliminada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar compra: {ex.Message}");
        }
    }

    /// <summary>
    /// Get purchase statistics
    /// </summary>
    public async Task<Dictionary<string, object>> GetPurchaseStatisticsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            { "Total", await _context.Purchases.CountAsync() },
            { "Pendientes", await _context.Purchases.CountAsync(p => p.Status == PurchaseStatus.Pendiente) },
            { "Recibidas", await _context.Purchases.CountAsync(p => p.Status == PurchaseStatus.Recibida) },
            { "Canceladas", await _context.Purchases.CountAsync(p => p.Status == PurchaseStatus.Cancelada) },
            { "TotalMes", await _context.Purchases
                .Where(p => p.PurchaseDate.Month == DateTime.Now.Month && p.PurchaseDate.Year == DateTime.Now.Year)
                .Select(p => p.Total)
                .ToListAsync()
                .ContinueWith(t => t.Result.Sum()) }
        };

        return stats;
    }
}
