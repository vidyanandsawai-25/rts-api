using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.GIS;

/// <summary>
/// Entity for Spatial GeoJSON File Upload Audit Log
/// </summary>
[Table("GisUploadHistory", Schema = "GIS")]
public class GisUploadHistoryEntity : BaseEntity
{
    public string FileName { get; set; } = null!;
    public string FileType { get; set; } = "GeoJSON";
    public int RecordCount { get; set; }
    public string UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
