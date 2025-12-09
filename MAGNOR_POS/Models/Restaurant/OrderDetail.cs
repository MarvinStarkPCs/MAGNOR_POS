using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Restaurant;

/// <summary>
/// Order detail/line item
/// </summary>
public class OrderDetail : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pendiente;
    public string? Modifiers { get; set; } // e.g., "Sin sal, extra queso"
    public string? Notes { get; set; }

    public DateTime? SentToKitchenAt { get; set; }
    public DateTime? PreparedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Foreign keys
    public int OrderId { get; set; }
    public int ProductId { get; set; }

    // Navigation properties
    public Order? Order { get; set; }
    public Inventory.Product? Product { get; set; }
}
