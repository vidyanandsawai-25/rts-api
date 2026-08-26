using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.GIS;

/// <summary>
/// Entity for GIS Corporation Map Viewport and Basemaps Configuration
/// </summary>
[Table("GisCorporationConfig", Schema = "GIS")]
public class GisCorporationConfigEntity : BaseEntity
{
    public int UlbId { get; set; }
    public decimal? DefaultCenterLat { get; set; }
    public decimal? DefaultCenterLng { get; set; }
    public int DefaultZoom { get; set; } = 14;
    public int MinZoom { get; set; } = 10;
    public int MaxZoom { get; set; } = 20;
    public string? BoundingBoxJson { get; set; }
    public string? BasemapsJson { get; set; }
    public string PropertyBoundaryColor { get; set; } = "#0078FF";
}
