using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

public class RTSAapleSarkarServiceMappingEntity : BaseEntity
{
    public int RtsServiceId { get; set; }
    public int MahaITServiceId { get; set; }
    public string? GovtServiceName { get; set; }

    [NotMapped]
    public int GovtCode
    {
        get => MahaITServiceId;
        set => MahaITServiceId = value;
    }

    [NotMapped]
    public string? MahaItServiceName
    {
        get => GovtServiceName;
        set => GovtServiceName = value;
    }

    public int MaxProcessingDays { get; set; } = 7;

    [ForeignKey(nameof(RtsServiceId))]
    public virtual RTSServiceEntity? RtsService { get; set; }
}
