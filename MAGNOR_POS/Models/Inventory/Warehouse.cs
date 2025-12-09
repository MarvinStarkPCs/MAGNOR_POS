using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models.Inventory;

/// <summary>
/// Warehouse or branch
/// </summary>
public class Warehouse : AuditableEntity
{
    public required string Name { get; set; }
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsMainWarehouse { get; set; }

    // Navigation properties
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
