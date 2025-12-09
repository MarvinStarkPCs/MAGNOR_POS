using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models.Restaurant;

/// <summary>
/// Recipe for dish/product
/// </summary>
public class Recipe : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int PreparationTime { get; set; } // minutes
    public decimal Yield { get; set; } = 1; // number of servings

    // Foreign keys
    public int ProductId { get; set; }

    // Navigation properties
    public Inventory.Product? Product { get; set; }
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}
