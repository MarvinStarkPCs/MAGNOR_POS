using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models.Sales;

/// <summary>
/// Cash movement (withdrawal/deposit) during cash register session
/// </summary>
public class CashMovement : BaseEntity
{
    public DateTime MovementDate { get; set; } = DateTime.Now;
    public string MovementType { get; set; } = string.Empty; // "Retiro" or "Deposito"
    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public string? Notes { get; set; }

    // Foreign keys
    public int CashRegisterId { get; set; }
    public int UserId { get; set; }

    // Navigation properties
    public CashRegister? CashRegister { get; set; }
    public User? User { get; set; }
}
