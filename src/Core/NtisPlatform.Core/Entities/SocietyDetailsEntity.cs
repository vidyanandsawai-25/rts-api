using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents society details in the PTIS system
/// </summary>
[Table("SocietyDetailsMast", Schema = "PTIS")]
public class SocietyDetailsEntity : BaseEntity
{
    public int? PropertyId { get; set; }

    public int? WingId { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    [StringLength(30)]
    public string? WingName { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? SocietyName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? SocietyAddress { get; set; }

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

    public int? ManagerMobileNoRemarkId { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? SecretaryMobileNo { get; set; }

    public int? SecretaryMobileNoRemarkId { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? BuilderMobileNo { get; set; }

    public int? BuilderMobileNoRemarkId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? SocietyEmailId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? SecretaryEmailId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? ManagerEmailId { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }


    public PropertyEntity? PropertyMast { get; set; }
}
