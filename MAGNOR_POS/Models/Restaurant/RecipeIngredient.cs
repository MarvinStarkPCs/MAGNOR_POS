using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models.Restaurant;

/// <summary>
/// Recipe ingredient
/// </summary>
public class RecipeIngredient : BaseEntity
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }

    // Foreign keys
    public int RecipeId { get; set; }
    public int IngredientProductId { get; set; }

    // Navigation properties
    public Recipe? Recipe { get; set; }
    public Inventory.Product? IngredientProduct { get; set; }
}
