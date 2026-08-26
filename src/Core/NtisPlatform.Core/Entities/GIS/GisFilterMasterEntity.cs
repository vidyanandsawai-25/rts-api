using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities.GIS;

/// <summary>
/// Entity for Global Master Filter Catalog
/// </summary>
[Table("GisFilterMaster", Schema = "GIS")]
public class GisFilterMasterEntity : BaseEntity
{
    public string FilterKey { get; set; } = null!;
    public string FilterLabel { get; set; } = null!;
    public string ControlType { get; set; } = "DROPDOWN";
    public string? ApiSourceUrl { get; set; }
}

/// <summary>
/// Entity for Department to Filter Mapping
/// </summary>
[Table("GisDepartmentFilterMapping", Schema = "GIS")]
public class GisDepartmentFilterMappingEntity : BaseEntity
{
    public int DepartmentId { get; set; }
    public int FilterMasterId { get; set; }
    public string? CustomLabel { get; set; }
    public int DisplayOrder { get; set; } = 1;

    [ForeignKey(nameof(DepartmentId))]
    public virtual DepartmentMasterEntity? Department { get; set; }

    [ForeignKey(nameof(FilterMasterId))]
    public virtual GisFilterMasterEntity? FilterMaster { get; set; }
}
