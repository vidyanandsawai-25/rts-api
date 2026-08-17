using System;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RTSPaymentWebhookLogEntity
{
    public long Id { get; set; }
    public int? GatewayConfigId { get; set; }
    public string? EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? SignatureHeader { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public bool IsSignatureValid { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessingError { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.Now;
    public DateTime? ProcessedDate { get; set; }

    public virtual RTSPaymentGatewayConfigEntity? GatewayConfig { get; set; }
}
