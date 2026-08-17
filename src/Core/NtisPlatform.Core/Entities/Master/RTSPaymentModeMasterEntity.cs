using System;

namespace NtisPlatform.Core.Entities.Master;

public class RTSPaymentModeMasterEntity : BaseEntity
{
    public string ModeCode { get; set; } = string.Empty;
    public string ModeNameEn { get; set; } = string.Empty;
    public string ModeNameMr { get; set; } = string.Empty;
    public string IconName { get; set; } = "CreditCard";
}
