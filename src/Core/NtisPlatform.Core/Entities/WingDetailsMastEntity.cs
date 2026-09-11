using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents wing details in the PTIS system.
/// Maps to [PTIS].[WingDetailsMast] table.
/// </summary>
[Table("WingDetailsMast", Schema = "PTIS")]
public class WingDetailsMastEntity : BaseEntity
{
    public int SocietyDetailsMastId { get; set; }

    public int WingMasterId { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    [StringLength(30)]
    public string? WingName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? SecretaryName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? ManagerName { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? SecretaryNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    [StringLength(200)]
    public string? ManagerNameEnglish { get; set; }

    [Column(TypeName = "varchar(13)")]
    [StringLength(13)]
    public string? ManagerMobileNo { get; set; }

    public int? ManagerMobileNoRemarkId { get; set; }

    [Column(TypeName = "varchar(13)")]
    [StringLength(13)]
    public string? SecretaryMobileNo { get; set; }

    public int? SecretaryMobileNoRemarkId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    [StringLength(100)]
    public string? SecretaryEmailId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    [StringLength(100)]
    public string? ManagerEmailId { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SocietyDetailsMastId))]
    public virtual SocietyDetailsEntity? SocietyDetailsMast { get; set; }

    [ForeignKey(nameof(WingMasterId))]
    public virtual WingEntity? WingMaster { get; set; }

    [ForeignKey(nameof(ManagerMobileNoRemarkId))]
    public virtual CommonRemarkTypeMasterEntity? ManagerMobileNoRemarkMaster { get; set; }

    [ForeignKey(nameof(SecretaryMobileNoRemarkId))]
    public virtual CommonRemarkTypeMasterEntity? SecretaryMobileNoRemarkMaster { get; set; }

    [InverseProperty(nameof(PropertyEntity.WingDetailsMast))]
    public virtual ICollection<PropertyEntity> Properties { get; set; } = new List<PropertyEntity>();

}
