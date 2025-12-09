using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Restaurant;

/// <summary>
/// Restaurant order
/// </summary>
public class Order : BaseEntity
{
    public required string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public OrderStatus Status { get; set; } = OrderStatus.Pendiente;

    // Amounts
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public int NumberOfGuests { get; set; } = 1;
    public string? Notes { get; set; }

    // Foreign keys
    public int? TableId { get; set; }
    public int WaiterId { get; set; }
    public int? SaleId { get; set; }

    // Navigation properties
    public RestaurantTable? Table { get; set; }
    public User? Waiter { get; set; }
    public Sales.Sale? Sale { get; set; }
    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
}
