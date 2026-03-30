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

    [Column(TypeName = "nvarchar(100)")]
    public string? WingName { get; set; }

    public int? WingId { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? BHKType { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? SecretaryName { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? SocietyName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? ManagerName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? SecretaryNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? SocietyNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? ManagerNameEnglish { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? ManagerMobileNo { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? SecretaryMobileNo { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for permanent deletion.
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Foreign key to PropertyMast
    /// </summary>
    public int PropertyId { get; set; }
}