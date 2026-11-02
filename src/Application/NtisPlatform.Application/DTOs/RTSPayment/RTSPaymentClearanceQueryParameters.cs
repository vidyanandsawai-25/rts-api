using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.RTSPayment;

public class RTSPaymentClearanceQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? DepartmentId { get; set; }

    [Filterable]
    [Sortable]
    public int? ServiceId { get; set; }

    [Filterable]
    [Sortable]
    public int? UserId { get; set; }

    [Filterable]
    [Sortable]
    public string? PaymentMode { get; set; }

    [Filterable]
    [Sortable]
    public string? BankName { get; set; }

    public string? SearchText { get; set; }

    [Filterable]
    [Sortable]
    public string? StatusCode { get; set; }
    [Filterable]
    [Sortable]
    public decimal? BaseAmount { get; set; }

    [Filterable]
    [Sortable]
    public DateTime? ReceiptDate { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

}
