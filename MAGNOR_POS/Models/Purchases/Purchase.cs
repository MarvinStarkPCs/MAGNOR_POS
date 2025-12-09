using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Purchases;

/// <summary>
/// Purchase order entity
/// </summary>
public class Purchase : BaseEntity
{
    public required string PurchaseNumber { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
    public DateTime? DeliveryDate { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Pendiente;

    // Amounts
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    // Payment
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }

    // Invoice reference
    public string? SupplierInvoiceNumber { get; set; }
    public string? Notes { get; set; }

    // Foreign keys
    public int SupplierId { get; set; }
    public int UserId { get; set; }
    public int? WarehouseId { get; set; }

    // Navigation properties
    public Parties.Supplier? Supplier { get; set; }
    public User? User { get; set; }
    public Inventory.Warehouse? Warehouse { get; set; }
    public ICollection<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
}
