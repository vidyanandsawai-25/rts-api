using System;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RTSPaymentGatewayOrderEntity
{
    public long Id { get; set; }
    public long DemandId { get; set; }
    public int ApplicationId { get; set; }
    public int GatewayConfigId { get; set; }
    public string GatewayCode { get; set; } = "RAZORPAY";
    public string GatewayOrderId { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string OrderStatus { get; set; } = "Initiated"; // Initiated, Success, Failed, Expired, Cancelled

    public string? GatewayPaymentId { get; set; }
    public string? GatewaySignature { get; set; }
    public decimal? GatewayFee { get; set; }
    public decimal? GatewayTax { get; set; }
    public string? GatewayResponseJson { get; set; }
    public string? FailureReason { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerMobile { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; }

    public virtual RTSPaymentDemandEntity Demand { get; set; } = null!;
    public virtual RTSApplicationDetailsEntity Application { get; set; } = null!;
    public virtual RTSPaymentGatewayConfigEntity GatewayConfig { get; set; } = null!;
}
