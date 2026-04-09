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
    public int Id { get; set; }

    public int? PropertyId { get; set; }

    public int? WingId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? WingName { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? SocietyName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? SocietyAddress { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? BHKType { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? SecretaryName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? ManagerName { get; set; }

    [Column(TypeName = "nvarchar(200)")]

    public string? LandOwnerName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? BuilderName { get; set; }

    public string? SecretaryNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? SocietyNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]

    public string? SocietyAddressEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? ManagerNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? LandOwnerNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? BuilderNameEnglish { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? ManagerMobileNo { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? SecretaryMobileNo { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? SocietyEmailId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? SecretaryEmailId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? ManagerEmailId { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    [NotMapped]
    public DateTime? MarkedForDeletionDate { get; set; }
}
