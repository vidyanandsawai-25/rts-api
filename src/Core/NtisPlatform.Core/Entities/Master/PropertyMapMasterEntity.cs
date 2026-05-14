namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a property map entity that stores property mapping information.
/// </summary>
public class PropertyMapMasterEntity : BaseEntity
{
    public int? ModuleId { get; set; }
    public int? ParentPropertyMapId { get; set; }
    public int VersionNo { get; set; } = 1;
    public string MappingCategory { get; set; } = string.Empty;
    public string? ChangeReason { get; set; }
    public string? Remark { get; set; }
}