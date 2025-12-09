using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Restaurant;

/// <summary>
/// Restaurant table
/// </summary>
public class RestaurantTable : AuditableEntity
{
    public required string TableNumber { get; set; }
    public int Capacity { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Disponible;

    // Position for visual layout
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }

    // Foreign keys
    public int ZoneId { get; set; }

    // Navigation properties
    public RestaurantZone? Zone { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
