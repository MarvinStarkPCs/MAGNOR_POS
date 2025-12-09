using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models.Sales;

/// <summary>
/// Sale detail/line item
/// </summary>
public class SaleDetail : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public string? Notes { get; set; }

    // Foreign keys
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }

    // Navigation properties
    public Sale? Sale { get; set; }
    public Inventory.Product? Product { get; set; }
    public Inventory.ProductVariant? ProductVariant { get; set; }
}
