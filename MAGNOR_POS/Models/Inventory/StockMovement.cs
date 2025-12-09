using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Inventory;

/// <summary>
/// Stock movement history (Kardex)
/// </summary>
public class StockMovement : BaseEntity
{
    public DateTime MovementDate { get; set; } = DateTime.Now;
    public InventoryMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal BalanceAfter { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    // Foreign keys
    public int ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public int? UserId { get; set; }

    // Navigation properties
    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
    public User? User { get; set; }
}
