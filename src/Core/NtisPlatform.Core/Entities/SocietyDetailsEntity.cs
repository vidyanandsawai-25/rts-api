using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents society details in the PTIS system
/// </summary>
[Table("SocietyDetailsMast", Schema = "PTIS")]
public class SocietyDetailsEntity : BaseEntity
{
    [Key]
    public int SocietyDetailId { get; set; }

    public int? WingId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? WingName { get; set; }

    // Add other society-related fields as needed
}
