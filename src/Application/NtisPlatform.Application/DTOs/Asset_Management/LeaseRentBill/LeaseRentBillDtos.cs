using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.LeaseRentBill;

/// <summary>One month's allocation within a payment request (used by "Select Months").</summary>
public class BillPaymentAllocationDto
{
    // No per-item attributes: allocations only apply to Monthwise, so they are validated
    // conditionally in CreateBillPaymentDto.Validate — otherwise a Swagger placeholder item
    // (e.g. { 0, 0 }) would 400 a Full/Partial request where allocations are ignored.
    public int MonthWiseDemandId { get; set; }

    /// <summary>
    /// Server-computed settled amount for this demand. Clients do not supply it — for every mode the
    /// service resolves the amount (full pending balance, or the oldest-first slice of a Custom Amount).
    /// </summary>
    public decimal PayAmount { get; set; }
}

/// <summary>
/// Payment request: one header plus the mode-specific settlement instruction.
/// PaymentType (maps to the "Pending Payment" choice on the payment screen):
///   "Full"      — "Full Payment": settles every pending demand on the lease; Allocations / CustomAmount ignored (server auto-fetches).
///   "Partial"   — "Custom Amount": a single lump-sum CustomAmount is distributed across pending demands oldest-first; Allocations ignored.
///   "Monthwise" — "Select Months": settles only the demands listed in Allocations, each paid to its full pending balance.
/// PaymentMode allowed values: Cash, Cheque, DD, UPI, Online, NEFT, RTGS. (The "QR / UPI" option submits "UPI".)
/// </summary>
public class CreateBillPaymentDto : IValidatableObject
{
    [Required(ErrorMessage = "AMS_BillPayment_PaymentType_Required")]
    [RegularExpression("^(Full|Monthwise|Partial)$", ErrorMessage = "AMS_BillPayment_PaymentType_Invalid")]
    public string PaymentType { get; set; } = "Full";

    [Required(ErrorMessage = "AMS_BillPayment_PaymentMode_Required")]
    [RegularExpression("^(Cash|Cheque|DD|UPI|Online|NEFT|RTGS)$", ErrorMessage = "AMS_BillPayment_PaymentMode_Invalid")]
    public string PaymentMode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AMS_BillPayment_PaymentDate_Required")]
    public DateTime PaymentDate { get; set; }

    [Required(ErrorMessage = "AMS_BillPayment_PayerMobile_Required")]
    [RegularExpression("^[0-9]{10}$", ErrorMessage = "AMS_BillPayment_PayerMobile_Invalid")]
    public string PayerMobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "AMS_BillPayment_PayerEmail_Required")]
    [EmailAddress(ErrorMessage = "AMS_BillPayment_PayerEmail_Invalid")]
    [StringLength(200, ErrorMessage = "AMS_BillPayment_PayerEmail_MaxLengthExceeded_200")]
    public string PayerEmail { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "AMS_BillPayment_BankName_MaxLengthExceeded_100")]
    public string? BankName { get; set; }

    [StringLength(100, ErrorMessage = "AMS_BillPayment_BranchName_MaxLengthExceeded_100")]
    public string? BranchName { get; set; }

    [StringLength(100, ErrorMessage = "AMS_BillPayment_ChequeOrTransactionNo_MaxLengthExceeded_100")]
    public string? ChequeOrTransactionNo { get; set; }

    public DateTime? ChequeDate { get; set; }

    [StringLength(100, ErrorMessage = "AMS_BillPayment_OnlineTransactionId_MaxLengthExceeded_100")]
    public string? OnlineTransactionId { get; set; }

    [StringLength(100, ErrorMessage = "AMS_BillPayment_PaymentGatewayName_MaxLengthExceeded_100")]
    public string? PaymentGatewayName { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_BillPayment_DiscountAmount_InvalidRange")]
    public decimal DiscountAmount { get; set; }

    public decimal AdjustmentAmount { get; set; }

    [StringLength(500, ErrorMessage = "AMS_BillPayment_Remark_MaxLengthExceeded_500")]
    public string? Remark { get; set; }

    /// <summary>
    /// "Custom Amount" lump-sum to settle. Required and must be &gt; 0 only when PaymentType is "Partial".
    /// The server distributes it across pending demands oldest-first; it must not exceed the total pending balance.
    /// Ignored for "Full" and "Monthwise".
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "AMS_BillPayment_CustomAmount_InvalidRange")]
    public decimal CustomAmount { get; set; }

    /// <summary>
    /// "Select Months": the demands to settle (demand ids only — PayAmount is ignored, each is paid in full).
    /// Required for Monthwise. Ignored for Full and Partial (the server resolves the demands itself).
    /// </summary>
    public List<BillPaymentAllocationDto> Allocations { get; set; } = new();

    /// <summary>
    /// Mode-aware validation: only Monthwise needs Allocations (with valid demand ids), and only
    /// Partial needs a CustomAmount. Allocations are ignored for Full/Partial, so a placeholder
    /// item there must not fail the request.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PaymentType == "Monthwise")
        {
            if (Allocations == null || Allocations.Count == 0)
            {
                yield return new ValidationResult("AMS_BillPayment_Allocations_Required", new[] { nameof(Allocations) });
                yield break;
            }

            for (var i = 0; i < Allocations.Count; i++)
            {
                if (Allocations[i].MonthWiseDemandId < 1)
                    yield return new ValidationResult(
                        "AMS_BillPayment_MonthWiseDemandId_InvalidRange",
                        new[] { $"{nameof(Allocations)}[{i}].{nameof(BillPaymentAllocationDto.MonthWiseDemandId)}" });
            }
        }
        else if (PaymentType == "Partial" && CustomAmount <= 0m)
        {
            yield return new ValidationResult("AMS_BillPayment_CustomAmount_Required", new[] { nameof(CustomAmount) });
        }
    }
}

/// <summary>A settled-month line on the receipt.</summary>
public class BillReceiptLineDto
{
    public int MonthWiseDemandId { get; set; }
    public int DemandYear { get; set; }
    public byte DemandMonth { get; set; }
    public decimal CurrentPaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

/// <summary>Receipt returned after a successful payment.</summary>
public class BillReceiptDto
{
    public int TransactionId { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string? ReceiptNo { get; set; }
    /// <summary>"Full" or "Partial" — reflects the payment intent from the request.</summary>
    public string PaymentType { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal TotalDemandAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public decimal NetPayableAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? TenantName { get; set; }
    public List<BillReceiptLineDto> Lines { get; set; } = new();
}

/// <summary>Transaction history row for a lease.</summary>
public class BillTransactionDto
{
    public int Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string? ReceiptNo { get; set; }
    public int FinanceYear { get; set; }
    public decimal TotalDemandAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public decimal NetPayableAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}
