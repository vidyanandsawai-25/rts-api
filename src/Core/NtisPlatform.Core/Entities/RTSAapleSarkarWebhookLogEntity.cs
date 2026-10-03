using System;

namespace NtisPlatform.Core.Entities;

public class RTSAapleSarkarWebhookLogEntity
{
    public long Id { get; set; }
    public string AapleSarkarTrackId { get; set; } = string.Empty;
    public string? ApplicationNo { get; set; }
    public int? ServiceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MahaItStatusCode { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public bool IsSuccess { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
