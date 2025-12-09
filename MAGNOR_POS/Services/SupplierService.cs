using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Parties;
using MAGNOR_POS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for managing supplier operations
/// </summary>
public class SupplierService
{
    private readonly AppDbContext _context;

    public SupplierService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all active suppliers
    /// </summary>
    public async Task<List<Supplier>> GetAllSuppliersAsync(bool includeInactive = false)
    {
        var query = _context.Suppliers.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.OrderBy(s => s.CompanyName).ToListAsync();
    }

    /// <summary>
    /// Get supplier by ID
    /// </summary>
    public async Task<Supplier?> GetSupplierByIdAsync(int supplierId)
    {
        return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
    }

    /// <summary>
    /// Search suppliers by name, document, phone or email
    /// </summary>
    public async Task<List<Supplier>> SearchSuppliersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllSuppliersAsync();
        }

        searchTerm = searchTerm.ToLower().Trim();

        return await _context.Suppliers
            .Where(s => s.IsActive && (
                s.CompanyName.ToLower().Contains(searchTerm) ||
                s.ContactName.ToLower().Contains(searchTerm) ||
                (s.DocumentNumber != null && s.DocumentNumber.Contains(searchTerm)) ||
                (s.Phone != null && s.Phone.Contains(searchTerm)) ||
                (s.Email != null && s.Email.ToLower().Contains(searchTerm))
            ))
            .OrderBy(s => s.CompanyName)
            .ToListAsync();
    }

    /// <summary>
    /// Check if a supplier with the same document already exists
    /// </summary>
    public async Task<bool> SupplierExistsAsync(DocumentType documentType, string documentNumber, int? excludeSupplierId = null)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            return false;

        var query = _context.Suppliers
            .Where(s => s.DocumentType == documentType && s.DocumentNumber == documentNumber);

        if (excludeSupplierId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSupplierId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Add a new supplier
    /// </summary>
    public async Task<(bool Success, string Message, Supplier? Supplier)> AddSupplierAsync(Supplier supplier)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(supplier.CompanyName))
                return (false, "El nombre de la empresa es requerido", null);

            if (string.IsNullOrWhiteSpace(supplier.ContactName))
                return (false, "El nombre del contacto es requerido", null);

            // Check for duplicate document
            if (!string.IsNullOrWhiteSpace(supplier.DocumentNumber))
            {
                if (await SupplierExistsAsync(supplier.DocumentType, supplier.DocumentNumber))
                {
                    return (false, $"Ya existe un proveedor con {supplier.DocumentType}: {supplier.DocumentNumber}", null);
                }
            }

            supplier.CreatedAt = DateTime.Now;
            supplier.UpdatedAt = DateTime.Now;

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return (true, "Proveedor creado exitosamente", supplier);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear proveedor: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update an existing supplier
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateSupplierAsync(Supplier supplier)
    {
        try
        {
            var existing = await _context.Suppliers.FindAsync(supplier.Id);
            if (existing == null)
            {
                return (false, "Proveedor no encontrado");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(supplier.CompanyName))
                return (false, "El nombre de la empresa es requerido");

            if (string.IsNullOrWhiteSpace(supplier.ContactName))
                return (false, "El nombre del contacto es requerido");

            // Check for duplicate document
            if (!string.IsNullOrWhiteSpace(supplier.DocumentNumber))
            {
                if (await SupplierExistsAsync(supplier.DocumentType, supplier.DocumentNumber, supplier.Id))
                {
                    return (false, $"Ya existe otro proveedor con {supplier.DocumentType}: {supplier.DocumentNumber}");
                }
            }

            // Update properties
            existing.CompanyName = supplier.CompanyName;
            existing.ContactName = supplier.ContactName;
            existing.DocumentType = supplier.DocumentType;
            existing.DocumentNumber = supplier.DocumentNumber;
            existing.Phone = supplier.Phone;
            existing.Email = supplier.Email;
            existing.Address = supplier.Address;
            existing.Website = supplier.Website;
            existing.PaymentTermDays = supplier.PaymentTermDays;
            existing.Notes = supplier.Notes;
            existing.IsActive = supplier.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, "Proveedor actualizado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar proveedor: {ex.Message}");
        }
    }

    /// <summary>
    /// Deactivate a supplier (soft delete)
    /// </summary>
    public async Task<(bool Success, string Message)> DeactivateSupplierAsync(int supplierId)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null)
            {
                return (false, "Proveedor no encontrado");
            }

            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, "Proveedor desactivado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al desactivar proveedor: {ex.Message}");
        }
    }

    /// <summary>
    /// Reactivate a supplier
    /// </summary>
    public async Task<(bool Success, string Message)> ReactivateSupplierAsync(int supplierId)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null)
            {
                return (false, "Proveedor no encontrado");
            }

            supplier.IsActive = true;
            supplier.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, "Proveedor reactivado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al reactivar proveedor: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a supplier permanently
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteSupplierAsync(int supplierId)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null)
            {
                return (false, "Proveedor no encontrado");
            }

            // Check if supplier has purchases
            var hasPurchases = await _context.Purchases.AnyAsync(p => p.SupplierId == supplier.Id);
            if (hasPurchases)
            {
                return (false, "No se puede eliminar un proveedor con compras registradas. Considere desactivarlo en su lugar.");
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return (true, "Proveedor eliminado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar proveedor: {ex.Message}");
        }
    }

    /// <summary>
    /// Get supplier statistics
    /// </summary>
    public async Task<Dictionary<string, int>> GetSupplierStatisticsAsync()
    {
        var stats = new Dictionary<string, int>
        {
            { "Total", await _context.Suppliers.CountAsync() },
            { "Activos", await _context.Suppliers.CountAsync(s => s.IsActive) },
            { "Inactivos", await _context.Suppliers.CountAsync(s => !s.IsActive) }
        };

        return stats;
    }
}
