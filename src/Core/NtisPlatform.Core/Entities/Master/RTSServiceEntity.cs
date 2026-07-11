namespace NtisPlatform.Core.Entities.Master;

public class RTSServiceEntity:BaseEntity
{
    public int DepartmentId { get; set; }

    public int? RTSServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string? ServiceNameLocal { get; set; }

    public string? Description { get; set; }

    public string? ServiceUrl { get; set; }

    public string? ServiceIcon { get; set; }

}
