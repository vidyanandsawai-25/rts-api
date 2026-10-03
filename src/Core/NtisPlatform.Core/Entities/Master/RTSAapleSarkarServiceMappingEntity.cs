using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

public class RTSAapleSarkarServiceMappingEntity : BaseEntity
{
    public int RtsServiceId { get; set; }
    public int MahaItServiceId { get; set; }
    public string? MahaItServiceName { get; set; }
    public int MaxProcessingDays { get; set; } = 7;

    [ForeignKey(nameof(RtsServiceId))]
    public virtual RTSServiceEntity? RtsService { get; set; }
}
