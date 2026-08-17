using System;

namespace NtisPlatform.Core.Entities.Master;

public class RTSPaymentStatusMasterEntity : BaseEntity
{
    public string StatusCode { get; set; } = string.Empty;
    public string StatusNameEn { get; set; } = string.Empty;
    public string StatusNameMr { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "bg-blue-50 text-blue-700";
    public int DisplayOrder { get; set; } = 1;
}
