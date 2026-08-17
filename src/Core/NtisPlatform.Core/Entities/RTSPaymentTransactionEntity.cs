using System;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RTSPaymentTransactionEntity
{
    public long Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public int DepartmentId { get; set; }

    public int GatewayConfigId { get; set; }
    public int PaymentStatusId { get; set; }
    public int? PaymentModeId { get; set; }

    public decimal BaseAmount { get; set; }
    public decimal LateFeeAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";

    public string GatewayOrderId { get; set; } = string.Empty;
    public string? GatewayPaymentId { get; set; }
    public string? GatewaySignature { get; set; }
    public decimal? GatewayFee { get; set; }
    public decimal? GatewayTax { get; set; }
    public string? BankRefNo { get; set; }
    public string? PayerVpaOrAccount { get; set; }
    public string? ReceiptNo { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? GatewayResponseJson { get; set; }

    public string? FailureReason { get; set; }
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual RTSApplicationDetailsEntity Application { get; set; } = null!;
    public virtual RTSServiceEntity Service { get; set; } = null!;
    public virtual RTSDepartmentEntity Department { get; set; } = null!;
    public virtual RTSPaymentGatewayConfigEntity GatewayConfig { get; set; } = null!;
    public virtual RTSPaymentStatusMasterEntity PaymentStatus { get; set; } = null!;
    public virtual RTSPaymentModeMasterEntity? PaymentMode { get; set; }
}
