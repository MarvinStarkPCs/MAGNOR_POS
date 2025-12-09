using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models.Purchases;

/// <summary>
/// Purchase detail/line item
/// </summary>
public class PurchaseDetail : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    // Received quantities
    public decimal QuantityReceived { get; set; }

    public string? Notes { get; set; }

    // Foreign keys
    public int PurchaseId { get; set; }
    public int ProductId { get; set; }

    // Navigation properties
    public Purchase? Purchase { get; set; }
    public Inventory.Product? Product { get; set; }
}
