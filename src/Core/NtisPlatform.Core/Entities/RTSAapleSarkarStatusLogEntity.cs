using System;

namespace NtisPlatform.Core.Entities;

public class RTSAapleSarkarStatusLogEntity
{
    public long Id { get; set; }
    public string AapleSarkarTrackId { get; set; } = string.Empty;
    public string? ApplicationNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
}
