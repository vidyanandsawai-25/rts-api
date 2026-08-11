using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Lookup table for signing authorities in the property sign-off workflow.
/// Contains 4 fixed rows: Clerk → Tax Inspector → Assistant Commissioner → Additional Commissioner.
/// SequenceOrder enforces the mandatory signing sequence.
/// </summary>
[Table("SignAuthorityMaster", Schema = "PTIS")]
public class SignAuthorityMasterEntity : BaseEntity
{
    /// <summary>Display name, e.g. "Clerk", "Tax Inspector"</summary>
    public string AuthorityName { get; set; } = string.Empty;

    /// <summary>Short code, e.g. "CLERK", "TI", "AC", "ADC"</summary>
    public string AuthorityCode { get; set; } = string.Empty;

    /// <summary>Officer name responsible for this authority, e.g. "John Doe"</summary>
    public string OfficerName { get; set; } = string.Empty;

    /// <summary>
    /// Signing order: 1=Clerk, 2=TaxInspector, 3=AssistantCommissioner, 4=AdditionalCommissioner.
    /// A property must be approved by order N before it can be approved by order N+1.
    /// </summary>
    public int SequenceOrder { get; set; }

    // Navigation
    public ICollection<PropertySignatureDetailsEntity> SignatureDetails { get; set; }
        = new List<PropertySignatureDetailsEntity>();
}
