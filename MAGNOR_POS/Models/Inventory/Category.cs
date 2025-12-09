using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models.Inventory;

/// <summary>
/// Product category
/// </summary>
public class Category : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    // Parent category for hierarchical structure
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    // Navigation properties
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
