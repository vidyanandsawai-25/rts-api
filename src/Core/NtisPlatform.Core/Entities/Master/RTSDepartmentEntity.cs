namespace NtisPlatform.Core.Entities.Master;

public class RTSDepartmentEntity : BaseEntity
{
    public string? DepartmentCode { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? DepartmentNameLocal { get; set; }
    public string? DepartmentIcon { get; set; }
    public int DisplayOrder { get; set; }
}
