using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Parties;

/// <summary>
/// Customer entity
/// </summary>
public class Customer : AuditableEntity
{
    public required string FullName { get; set; }
    public DocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public CustomerType CustomerType { get; set; } = CustomerType.Minorista;

    // Credit management
    public decimal CreditLimit { get; set; } = 0;
    public decimal CreditBalance { get; set; } = 0;

    // Loyalty
    public decimal TotalPurchases { get; set; }
    public int TotalOrders { get; set; }
    public DateTime? LastPurchaseDate { get; set; }

    // Discounts
    public decimal DiscountPercentage { get; set; } = 0;

    // Notes
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<Sales.Sale> Sales { get; set; } = new List<Sales.Sale>();
}
