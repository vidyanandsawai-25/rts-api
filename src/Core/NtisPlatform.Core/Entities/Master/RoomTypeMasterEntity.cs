using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents room type master data in the PTIS system
/// </summary>
[Table("RoomTypeMaster", Schema = "PTIS")]
public class RoomTypeMasterEntity : BaseEntity
{
    [Column(TypeName = "nvarchar(100)")]
    public string RoomTypeName { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(50)")]
    public string RoomTypeCode { get; set; } = string.Empty;

}
