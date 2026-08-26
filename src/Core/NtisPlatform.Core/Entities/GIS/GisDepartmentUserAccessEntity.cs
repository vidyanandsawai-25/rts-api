using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities.GIS;

/// <summary>
/// Entity for Multi-Department User Security Access Matrix
/// </summary>
[Table("GisDepartmentUserAccess", Schema = "GIS")]
public class GisDepartmentUserAccessEntity : BaseEntity
{
    public int UserId { get; set; }
    public int DepartmentId { get; set; }
    public int UlbId { get; set; } = 1;
    public int ZoneId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public virtual DepartmentMasterEntity? Department { get; set; }

    public bool CanView { get; set; } = true;
    public bool CanEdit { get; set; }
    public bool CanExport { get; set; }
}
