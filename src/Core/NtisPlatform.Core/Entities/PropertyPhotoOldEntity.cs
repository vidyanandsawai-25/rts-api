using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents historical property photo data in the PTIS system (PropertyPhotoOld table).
/// Stores photo records for a property's old/historical (PropertyMastOld) record.
/// </summary>
[Table("PropertyPhotoOld", Schema = "PTIS")]
public class PropertyPhotoOldEntity : BaseEntity
{
    /// <summary>
    /// Foreign Key to PropertyMastOld.Id
    /// </summary>
    public int PropertyMastOldId { get; set; }

    /// <summary>
    /// Foreign Key to PropertyPhotoType.Id
    /// </summary>
    public int PhotoTypeId { get; set; }

    /// <summary>
    /// Foreign Key to CORE.DocumentBinding.Id
    /// </summary>
    public int? DocumentBindingId { get; set; }

    /// <summary>
    /// 1 = current photo, 0 = superseded by a newer version.
    /// </summary>
    public bool IsLatest { get; set; } = true;

    public int? DisplayOrder { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? Remarks { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}
