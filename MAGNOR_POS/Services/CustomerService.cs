using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Parties;
using MAGNOR_POS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for managing customer operations
/// </summary>
public class CustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all active customers
    /// </summary>
    public async Task<List<Customer>> GetAllCustomersAsync(bool includeInactive = false)
    {
        var query = _context.Customers.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.FullName).ToListAsync();
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    public async Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
    }

    /// <summary>
    /// Search customers by name, document, phone or email
    /// </summary>
    public async Task<List<Customer>> SearchCustomersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllCustomersAsync();
        }

        searchTerm = searchTerm.ToLower().Trim();

        return await _context.Customers
            .Where(c => c.IsActive && (
                c.FullName.ToLower().Contains(searchTerm) ||
                (c.DocumentNumber != null && c.DocumentNumber.Contains(searchTerm)) ||
                (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                (c.Email != null && c.Email.ToLower().Contains(searchTerm))
            ))
            .OrderBy(c => c.FullName)
            .ToListAsync();
    }

    /// <summary>
    /// Check if a customer with the same document already exists
    /// </summary>
    public async Task<bool> CustomerExistsAsync(DocumentType documentType, string documentNumber, int? excludeCustomerId = null)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            return false;

        var query = _context.Customers
            .Where(c => c.DocumentType == documentType && c.DocumentNumber == documentNumber);

        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCustomerId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Validate document number format based on document type (Colombia)
    /// </summary>
    private (bool IsValid, string Message) ValidateDocumentFormat(DocumentType documentType, string? documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
            return (true, ""); // Optional field

        documentNumber = documentNumber.Trim();

        switch (documentType)
        {
            case DocumentType.CedulaCiudadania:
                // CC: 6-10 dígitos numéricos
                if (!System.Text.RegularExpressions.Regex.IsMatch(documentNumber, @"^\d{6,10}$"))
                    return (false, "La Cédula de Ciudadanía debe tener entre 6 y 10 dígitos");
                break;

            case DocumentType.NIT:
                // NIT: 9-10 dígitos con posible dígito de verificación
                if (!System.Text.RegularExpressions.Regex.IsMatch(documentNumber, @"^\d{9,10}(-\d)?$"))
                    return (false, "El NIT debe tener formato 123456789-0 o 123456789");
                break;

            case DocumentType.CedulaExtranjeria:
                // CE: Generalmente 6-7 dígitos
                if (!System.Text.RegularExpressions.Regex.IsMatch(documentNumber, @"^\d{6,7}$"))
                    return (false, "La Cédula de Extranjería debe tener entre 6 y 7 dígitos");
                break;

            case DocumentType.TarjetaIdentidad:
                // TI: 10-11 dígitos
                if (!System.Text.RegularExpressions.Regex.IsMatch(documentNumber, @"^\d{10,11}$"))
                    return (false, "La Tarjeta de Identidad debe tener entre 10 y 11 dígitos");
                break;
        }

        return (true, "");
    }

    /// <summary>
    /// Add a new customer
    /// </summary>
    public async Task<(bool Success, string Message, Customer? Customer)> AddCustomerAsync(Customer customer)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(customer.FullName))
                return (false, "El nombre completo es requerido", null);

            // Validate document format
            var (isValid, validationMessage) = ValidateDocumentFormat(customer.DocumentType, customer.DocumentNumber);
            if (!isValid)
                return (false, validationMessage, null);

            // Check for duplicate document
            if (!string.IsNullOrWhiteSpace(customer.DocumentNumber))
            {
                if (await CustomerExistsAsync(customer.DocumentType, customer.DocumentNumber))
                {
                    return (false, $"Ya existe un cliente con {customer.DocumentType}: {customer.DocumentNumber}", null);
                }
            }

            customer.CreatedAt = DateTime.Now;
            customer.UpdatedAt = DateTime.Now;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return (true, "Cliente creado exitosamente", customer);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear cliente: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateCustomerAsync(Customer customer)
    {
        try
        {
            var existing = await _context.Customers.FindAsync(customer.Id);
            if (existing == null)
            {
                return (false, "Cliente no encontrado");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(customer.FullName))
                return (false, "El nombre completo es requerido");

            // Validate document format
            var (isValid, validationMessage) = ValidateDocumentFormat(customer.DocumentType, customer.DocumentNumber);
            if (!isValid)
                return (false, validationMessage);

            // Check for duplicate document
            if (!string.IsNullOrWhiteSpace(customer.DocumentNumber))
            {
                if (await CustomerExistsAsync(customer.DocumentType, customer.DocumentNumber, customer.Id))
                {
                    return (false, $"Ya existe otro cliente con {customer.DocumentType}: {customer.DocumentNumber}");
                }
            }

            // Update properties
            existing.FullName = customer.FullName;
            existing.DocumentType = customer.DocumentType;
            existing.DocumentNumber = customer.DocumentNumber;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.PostalCode = customer.PostalCode;
            existing.CustomerType = customer.CustomerType;
            existing.CreditLimit = customer.CreditLimit;
            existing.DiscountPercentage = customer.DiscountPercentage;
            existing.Notes = customer.Notes;
            existing.IsActive = customer.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, "Cliente actualizado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar cliente: {ex.Message}");
        }
    }

    /// <summary>
    /// Deactivate a customer (soft delete)
    /// </summary>
    public async Task<(bool Success, string Message)> DeactivateCustomerAsync(int customerId)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return (false, "Cliente no encontrado");
            }

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, "Cliente desactivado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al desactivar cliente: {ex.Message}");
        }
    }

    /// <summary>
    /// Reactivate a customer
    /// </summary>
    public async Task<(bool Success, string Message)> ReactivateCustomerAsync(int customerId)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return (false, "Cliente no encontrado");
            }

            customer.IsActive = true;
            customer.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, "Cliente reactivado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al reactivar cliente: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a customer permanently
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteCustomerAsync(int customerId)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return (false, "Cliente no encontrado");
            }

            // Check if customer has sales
            var hasSales = await _context.Sales.AnyAsync(s => s.CustomerId == customer.Id);
            if (hasSales)
            {
                return (false, "No se puede eliminar un cliente con ventas registradas. Considere desactivarlo en su lugar.");
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return (true, "Cliente eliminado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar cliente: {ex.Message}");
        }
    }

    /// <summary>
    /// Get customer statistics
    /// </summary>
    public async Task<Dictionary<string, int>> GetCustomerStatisticsAsync()
    {
        var stats = new Dictionary<string, int>
        {
            { "Total", await _context.Customers.CountAsync() },
            { "Activos", await _context.Customers.CountAsync(c => c.IsActive) },
            { "Inactivos", await _context.Customers.CountAsync(c => !c.IsActive) },
            { "Minorista", await _context.Customers.CountAsync(c => c.IsActive && c.CustomerType == CustomerType.Minorista) },
            { "Mayorista", await _context.Customers.CountAsync(c => c.IsActive && c.CustomerType == CustomerType.Mayorista) },
            { "Corporativo", await _context.Customers.CountAsync(c => c.IsActive && c.CustomerType == CustomerType.Corporativo) }
        };

        return stats;
    }
}
