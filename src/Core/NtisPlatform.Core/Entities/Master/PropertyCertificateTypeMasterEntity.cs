using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Master/lookup table that defines the kinds of certificates a property can have
/// (e.g. Building Permit, Commencement Certificate, Occupancy Certificate).
/// Used by [PTIS].[PropertyCertificates] to classify each certificate row.
/// </summary>
[Table("PropertyCertificateTypeMaster", Schema = "PTIS")]
public class PropertyCertificateTypeMasterEntity : BaseEntity
{
    /// <summary>
    /// Display name shown in UI. May contain non-ASCII (multilingual).
    /// Example: 'Building Permit', 'Commencement Certificate'
    /// </summary>
    [Column(TypeName = "nvarchar(100)")]
    public string CertificateTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Sort order for UI dropdowns/lists. Lower = shown first.
    /// Example: 10, 20, 30, etc.
    /// </summary>
    public int DisplayOrder { get; set; }
}
