using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Inventory;

/// <summary>
/// Product entity
/// </summary>
public class Product : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string SKU { get; set; }
    public string? Barcode { get; set; }

    // Pricing
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal WholesalePrice { get; set; }

    // Stock control
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }

    // Unit of measure
    public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Unidad;

    // Tax
    public decimal TaxRate { get; set; } = 0.18m; // 18% default
    public bool IsTaxable { get; set; } = true;

    // Product attributes
    public bool HasVariants { get; set; }
    public bool TrackStock { get; set; } = true;
    public bool AllowNegativeStock { get; set; }
    public string? ImageUrl { get; set; }

    // Foreign keys
    public int CategoryId { get; set; }

    // Navigation properties
    public Category? Category { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
