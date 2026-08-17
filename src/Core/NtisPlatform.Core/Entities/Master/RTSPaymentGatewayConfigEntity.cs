using System;

namespace NtisPlatform.Core.Entities.Master;

public class RTSPaymentGatewayConfigEntity : BaseEntity
{
    public string GatewayCode { get; set; } = string.Empty;
    public string GatewayName { get; set; } = string.Empty;
    public string? MerchantId { get; set; }
    public string KeyId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }
    public string ServiceUrl { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public bool IsDefault { get; set; }
}
