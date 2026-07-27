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

    /// <summary>
    /// Short code identifying the certificate type. Example: 'BP', 'CC', 'OC'.
    /// </summary>
    [Column(TypeName = "varchar(50)")]
    public string CertificateTypeCode { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    /// <summary>
    /// When true, this certificate type is a protected/system-defined type that cannot be
    /// removed or reassigned through standard maintenance. This is purely a delete-protection
    /// flag — it does NOT gate Occupation Tax recalculation; see <see cref="IsTaxable"/> for that.
    /// </summary>
    public bool IsProtected { get; set; }

    /// <summary>
    /// Descriptive/UI-facing flag indicating a document is expected for a certificate of this
    /// type. Not enforced by SaveCertificateAsync — certificate metadata can be saved without an
    /// uploaded document (the document, when present, is attached separately via the Global
    /// Document endpoint).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// When true, saving/changing a certificate of this type should trigger
    /// Occupation Tax recalculation. Distinct from IsProtected (delete-protection).
    /// The certificate type is correlated to a PolicyCodeMaster family (OC/CC/ELECTRIC_BILL)
    /// via CertificateTypeCode matching, not a dedicated FK column.
    /// </summary>
    public bool IsTaxable { get; set; }
}
