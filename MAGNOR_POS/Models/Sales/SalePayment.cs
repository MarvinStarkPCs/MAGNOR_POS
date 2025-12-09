using MAGNOR_POS.Models.Base;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Models.Sales;

/// <summary>
/// Sale payment for mixed payment types
/// </summary>
public class SalePayment : BaseEntity
{
    public PaymentType PaymentType { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;

    // Foreign keys
    public int SaleId { get; set; }

    // Navigation properties
    public Sale? Sale { get; set; }
}
