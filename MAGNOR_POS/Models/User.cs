using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models;

public class User : AuditableEntity
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public DateTime? LastLogin { get; set; }

    // Foreign key
    public int RoleId { get; set; }

    // Navigation property
    public Role? Role { get; set; }

    // Collections
    public ICollection<Sales.Sale> Sales { get; set; } = new List<Sales.Sale>();
    public ICollection<Sales.CashRegister> CashRegisters { get; set; } = new List<Sales.CashRegister>();
    public ICollection<Sales.CashMovement> CashMovements { get; set; } = new List<Sales.CashMovement>();
    public ICollection<Purchases.Purchase> Purchases { get; set; } = new List<Purchases.Purchase>();
    public ICollection<Inventory.StockMovement> StockMovements { get; set; } = new List<Inventory.StockMovement>();
    public ICollection<Restaurant.Order> Orders { get; set; } = new List<Restaurant.Order>();
}
