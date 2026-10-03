using System;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class RTSAapleSarkarRequestEntity
{
    public long Id { get; set; }
    public string AapleSarkarTrackId { get; set; } = string.Empty;
    public int? ApplicationId { get; set; }
    public string? ApplicationNo { get; set; }
    public string? CitizenUserId { get; set; }
    public string? CitizenName { get; set; }
    public string? MobileNo { get; set; }
    public string? Email { get; set; }
    public int? DistrictId { get; set; }
    public int? TalukaId { get; set; }
    public int? VillageId { get; set; }
    public int? DivisionId { get; set; }
    public int? RtsServiceId { get; set; }
    public int? MahaItServiceId { get; set; }
    public int? UlbId { get; set; }
    public int? UlbDistrict { get; set; }
    public string Status { get; set; } = "Received";
    public string? RawPayload { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public virtual RTSApplicationDetailsEntity? Application { get; set; }

    [ForeignKey(nameof(RtsServiceId))]
    public virtual RTSServiceEntity? RtsService { get; set; }
}
