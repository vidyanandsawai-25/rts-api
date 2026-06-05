using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents property photo type master data in the PTIS system
/// </summary>
[Table("PropertyPhotoType", Schema = "PTIS")]
public class PropertyPhotoTypeEntity : BaseEntity
{
    [Column(TypeName = "varchar(50)")]
    public string PhotoTypeCode { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(200)")]
    public string PhotoTypeName { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }
}
