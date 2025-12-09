using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Sales;

/// <summary>
/// Cash register session
/// </summary>
public class CashRegister : BaseEntity
{
    public DateTime OpeningDate { get; set; } = DateTime.Now;
    public DateTime? ClosingDate { get; set; }
    public CashRegisterStatus Status { get; set; } = CashRegisterStatus.Abierta;

    // Opening amounts
    public decimal OpeningAmount { get; set; }

    // Expected amounts
    public decimal ExpectedCash { get; set; }
    public decimal ExpectedCard { get; set; }
    public decimal ExpectedTransfer { get; set; }
    public decimal ExpectedTotal { get; set; }

    // Actual amounts (on closing)
    public decimal ActualCash { get; set; }
    public decimal ActualCard { get; set; }
    public decimal ActualTransfer { get; set; }
    public decimal ActualTotal { get; set; }

    // Differences
    public decimal CashDifference { get; set; }

    // Withdrawals and deposits
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalDeposits { get; set; }

    public string? Notes { get; set; }

    // Foreign keys
    public int UserId { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
}
