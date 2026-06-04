using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents property type master data in the PTIS system
/// </summary>
[Table("PropertyTypeMaster", Schema = "PTIS")]
public class PropertyTypeMasterEntity : BaseEntity
{
    [Column(TypeName = "nvarchar(100)")]
    public string PropertyDescription { get; set; } = string.Empty;
    
    [Column(TypeName = "varchar(5)")]
    public string? Type { get; set; } = string.Empty;
    
    public int? SearchSequence { get; set; } = 0;
    
    public int? PropertyTypeCategoryId { get; set; }

    public string? PartType { get; set; } = string.Empty;
}
