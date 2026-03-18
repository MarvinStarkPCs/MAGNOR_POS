using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Sales;

/// <summary>
/// Sale/Invoice entity
/// </summary>
public class Sale : BaseEntity
{
    public required string InvoiceNumber { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public SaleStatus Status { get; set; } = SaleStatus.Completada;

    // Amounts
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    // Payment
    public PaymentType PaymentType { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }

    // References
    public string? Notes { get; set; }

    // Factus Electronic Invoice (DIAN)
    public string? FactusCUFE { get; set; }
    public string? FactusQRCode { get; set; }
    public string? FactusNumber { get; set; }
    public string? FactusPrefix { get; set; }
    public string? FactusStatus { get; set; }

    // Foreign keys
    public int? CustomerId { get; set; }
    public int UserId { get; set; }
    public int? CashRegisterId { get; set; }

    // Navigation properties
    public Parties.Customer? Customer { get; set; }
    public User? User { get; set; }
    public CashRegister? CashRegister { get; set; }
    public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
    public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
}
