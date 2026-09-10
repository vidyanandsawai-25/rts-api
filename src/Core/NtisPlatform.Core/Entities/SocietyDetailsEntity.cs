using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents society details in the PTIS system.
/// Maps to [PTIS].[SocietyDetailsMast] table.
/// </summary>
[Table("SocietyDetailsMast", Schema = "PTIS")]
public class SocietyDetailsEntity : BaseEntity
{
    public int? PropertyId { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    [StringLength(500)]
    public string? SocietyName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? SocietyAddress { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? SecretaryName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? ManagerName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? LandOwnerName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? BuilderName { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    [StringLength(500)]
    public string? SocietyNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? SocietyAddressEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? SecretaryNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? ManagerNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? LandOwnerNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? BuilderNameEnglish { get; set; }

    [Column(TypeName = "varchar(13)")]
    [StringLength(13)]
    public string? ManagerMobileNo { get; set; }

    public int? ManagerMobileNoRemarkId { get; set; }

    [Column(TypeName = "varchar(13)")]
    [StringLength(13)]
    public string? SecretaryMobileNo { get; set; }

    public int? SecretaryMobileNoRemarkId { get; set; }

    [Column(TypeName = "varchar(13)")]
    [StringLength(13)]
    public string? BuilderMobileNo { get; set; }

    public int? BuilderMobileNoRemarkId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    [StringLength(100)]
    public string? SocietyEmailId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    [StringLength(100)]
    public string? SecretaryEmailId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    [StringLength(100)]
    public string? ManagerEmailId { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PropertyId))]
    public virtual PropertyEntity? PropertyMast { get; set; }

    [ForeignKey(nameof(ManagerMobileNoRemarkId))]
    public virtual CommonRemarkTypeMasterEntity? ManagerMobileNoRemarkMaster { get; set; }

    [ForeignKey(nameof(SecretaryMobileNoRemarkId))]
    public virtual CommonRemarkTypeMasterEntity? SecretaryMobileNoRemarkMaster { get; set; }

    [ForeignKey(nameof(BuilderMobileNoRemarkId))]
    public virtual CommonRemarkTypeMasterEntity? BuilderMobileNoRemarkMaster { get; set; }
}
