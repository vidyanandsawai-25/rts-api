using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.GIS;

/// <summary>
/// Entity for Versioned GeoJSON Boundary Storage
/// </summary>
[Table("GisLayerJson", Schema = "GIS")]
public class GisLayerJsonEntity : BaseEntity
{
    public int LayerMasterId { get; set; }
    public int VersionNo { get; set; } = 1;
    public string GeoJsonPayload { get; set; } = null!;

    [ForeignKey(nameof(LayerMasterId))]
    public virtual GisLayerMasterEntity? LayerMaster { get; set; }
}
