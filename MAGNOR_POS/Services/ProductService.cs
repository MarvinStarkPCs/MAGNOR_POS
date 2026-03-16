using Microsoft.EntityFrameworkCore;
using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Inventory;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for managing products
/// </summary>
public class ProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all active products
    /// </summary>
    public async Task<List<Product>> GetAllProductsAsync(bool includeInactive = false)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Get product by SKU
    /// </summary>
    public async Task<Product?> GetProductBySKUAsync(string sku)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive);
    }

    /// <summary>
    /// Get product by Barcode
    /// </summary>
    public async Task<Product?> GetProductByBarcodeAsync(string barcode)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
    }

    /// <summary>
    /// Search products by name or SKU
    /// </summary>
    public async Task<List<Product>> SearchProductsAsync(string searchTerm)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive &&
                       (p.Name.Contains(searchTerm) ||
                        p.SKU.Contains(searchTerm) ||
                        (p.Barcode != null && p.Barcode.Contains(searchTerm))))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get products with low stock
    /// </summary>
    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive &&
                       p.TrackStock &&
                       p.CurrentStock <= p.MinimumStock)
            .OrderBy(p => p.CurrentStock)
            .ToListAsync();
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    public async Task<(bool success, string message, Product? product)> AddProductAsync(Product product, int createdByUserId)
    {
        try
        {
            // Validate SKU is unique
            var existingSKU = await _context.Products
                .FirstOrDefaultAsync(p => p.SKU == product.SKU && p.IsActive);

            if (existingSKU != null)
            {
                return (false, "Ya existe un producto con este SKU", null);
            }

            // Validate barcode is unique if provided
            if (!string.IsNullOrWhiteSpace(product.Barcode))
            {
                var existingBarcode = await _context.Products
                    .FirstOrDefaultAsync(p => p.Barcode == product.Barcode && p.IsActive);

                if (existingBarcode != null)
                {
                    return (false, "Ya existe un producto con este código de barras", null);
                }
            }

            // Validate category exists
            var category = await _context.Categories.FindAsync(product.CategoryId);
            if (category == null || !category.IsActive)
            {
                return (false, "La categoría seleccionada no existe o está inactiva", null);
            }

            // Set audit fields
            product.CreatedAt = DateTime.Now;
            product.CreatedBy = createdByUserId;
            product.IsActive = true;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Reload with category
            await _context.Entry(product).Reference(p => p.Category).LoadAsync();

            return (true, "Producto creado exitosamente", product);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear el producto: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    public async Task<(bool success, string message)> UpdateProductAsync(Product product, int updatedByUserId)
    {
        try
        {
            var existingProduct = await _context.Products.FindAsync(product.Id);
            if (existingProduct == null)
            {
                return (false, "Producto no encontrado");
            }

            // Validate SKU is unique (excluding current product)
            var duplicateSKU = await _context.Products
                .FirstOrDefaultAsync(p => p.SKU == product.SKU && p.Id != product.Id && p.IsActive);

            if (duplicateSKU != null)
            {
                return (false, "Ya existe un producto con este SKU");
            }

            // Validate barcode is unique if provided (excluding current product)
            if (!string.IsNullOrWhiteSpace(product.Barcode))
            {
                var duplicateBarcode = await _context.Products
                    .FirstOrDefaultAsync(p => p.Barcode == product.Barcode && p.Id != product.Id && p.IsActive);

                if (duplicateBarcode != null)
                {
                    return (false, "Ya existe un producto con este código de barras");
                }
            }

            // Validate category exists
            var category = await _context.Categories.FindAsync(product.CategoryId);
            if (category == null || !category.IsActive)
            {
                return (false, "La categoría seleccionada no existe o está inactiva");
            }

            // Update fields
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.SKU = product.SKU;
            existingProduct.Barcode = product.Barcode;
            existingProduct.PurchasePrice = product.PurchasePrice;
            existingProduct.SalePrice = product.SalePrice;
            existingProduct.WholesalePrice = product.WholesalePrice;
            existingProduct.MinimumStock = product.MinimumStock;
            existingProduct.MaximumStock = product.MaximumStock;
            existingProduct.UnitOfMeasure = product.UnitOfMeasure;
            existingProduct.TaxRate = product.TaxRate;
            existingProduct.IsTaxable = product.IsTaxable;
            existingProduct.HasVariants = product.HasVariants;
            existingProduct.TrackStock = product.TrackStock;
            existingProduct.AllowNegativeStock = product.AllowNegativeStock;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.UpdatedAt = DateTime.Now;
            existingProduct.UpdatedBy = updatedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Producto actualizado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar el producto: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a product (soft delete)
    /// </summary>
    public async Task<(bool success, string message)> DeleteProductAsync(int productId, int deletedByUserId)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return (false, "Producto no encontrado");
            }

            // Soft delete
            product.IsActive = false;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = deletedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Producto eliminado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar el producto: {ex.Message}");
        }
    }

    /// <summary>
    /// Update product stock
    /// </summary>
    public async Task<(bool success, string message)> UpdateStockAsync(int productId, decimal newStock, int updatedByUserId)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return (false, "Producto no encontrado");
            }

            if (!product.TrackStock)
            {
                return (false, "Este producto no tiene control de inventario habilitado");
            }

            if (newStock < 0 && !product.AllowNegativeStock)
            {
                return (false, "No se permite stock negativo para este producto");
            }

            product.CurrentStock = newStock;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = updatedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Stock actualizado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar el stock: {ex.Message}");
        }
    }
}
