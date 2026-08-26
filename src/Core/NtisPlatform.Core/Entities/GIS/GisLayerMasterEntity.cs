using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities.GIS;

/// <summary>
/// Entity for Dynamic GIS Layer Catalog per Department
/// </summary>
[Table("GisLayerMaster", Schema = "GIS")]
public class GisLayerMasterEntity : BaseEntity
{
    public int DepartmentId { get; set; }
    public int UlbId { get; set; } = 1;
    public string LayerCode { get; set; } = null!;
    public string LayerName { get; set; } = null!;
    public string GeometryType { get; set; } = null!;
    public string? StyleConfigJson { get; set; }
    public string? PopupSchemaJson { get; set; }
    public int MinZoom { get; set; } = 10;
    public int MaxZoom { get; set; } = 20;
    public bool IsDefaultVisible { get; set; } = true;

    [ForeignKey(nameof(DepartmentId))]
    public virtual DepartmentMasterEntity? Department { get; set; }
}
