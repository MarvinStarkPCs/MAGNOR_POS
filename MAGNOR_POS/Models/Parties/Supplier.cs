using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Parties;

/// <summary>
/// Supplier entity
/// </summary>
public class Supplier : AuditableEntity
{
    public required string CompanyName { get; set; }
    public required string ContactName { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.NIT;
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }

    // Payment terms
    public int PaymentTermDays { get; set; } = 30;
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<Purchases.Purchase> Purchases { get; set; } = new List<Purchases.Purchase>();
}
