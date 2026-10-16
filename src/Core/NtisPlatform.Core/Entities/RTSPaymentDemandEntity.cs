using System;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RTSPaymentDemandEntity
{
    public long Id { get; set; }
    public string DemandNo { get; set; } = string.Empty;
    public int ApplicationId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public int DepartmentId { get; set; }

    public decimal BaseAmount { get; set; }
    public decimal LateFeeAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";

    public string DemandStatus { get; set; } = "Pending"; // Pending, Paid, Cancelled, Exempted
    public string? RuleDetailsJson { get; set; }
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual RTSApplicationDetailsEntity Application { get; set; } = null!;
    public virtual RTSServiceEntity Service { get; set; } = null!;
    public virtual RTSDepartmentEntity Department { get; set; } = null!;
}
