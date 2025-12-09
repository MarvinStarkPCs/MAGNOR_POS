using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models.Restaurant;

/// <summary>
/// Restaurant zone/salon
/// </summary>
public class RestaurantZone : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int Capacity { get; set; }

    // Navigation properties
    public ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
}
