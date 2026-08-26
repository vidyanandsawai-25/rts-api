using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities.GIS;

/// <summary>
/// Entity for Global Master KPI Card Catalog
/// </summary>
[Table("GisKpiMaster", Schema = "GIS")]
public class GisKpiMasterEntity : BaseEntity
{
    public string KpiCode { get; set; } = null!;
    public string DefaultTitle { get; set; } = null!;
    public string DefaultIcon { get; set; } = "fa-building";
    public string DefaultColor { get; set; } = "#0078FF";
}

/// <summary>
/// Entity for Department to KPI Card Mapping
/// </summary>
[Table("GisDepartmentKpiMapping", Schema = "GIS")]
public class GisDepartmentKpiMappingEntity : BaseEntity
{
    public int DepartmentId { get; set; }
    public int KpiMasterId { get; set; }
    public string? CustomTitle { get; set; }
    public string? CustomIcon { get; set; }
    public string? CustomColor { get; set; }
    public int DisplayOrder { get; set; } = 1;

    [ForeignKey(nameof(DepartmentId))]
    public virtual DepartmentMasterEntity? Department { get; set; }

    [ForeignKey(nameof(KpiMasterId))]
    public virtual GisKpiMasterEntity? KpiMaster { get; set; }
}
