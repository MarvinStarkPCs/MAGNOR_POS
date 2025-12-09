using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Inventory;

/// <summary>
/// Product variant (size, color, etc.)
/// </summary>
public class ProductVariant : AuditableEntity
{
    public required string Name { get; set; }
    public required string SKU { get; set; }
    public string? Barcode { get; set; }

    // Variant attributes
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? Material { get; set; }

    // Pricing
    public decimal AdditionalPrice { get; set; }

    // Stock
    public decimal CurrentStock { get; set; }

    // Foreign keys
    public int ProductId { get; set; }

    // Navigation properties
    public Product? Product { get; set; }
}
