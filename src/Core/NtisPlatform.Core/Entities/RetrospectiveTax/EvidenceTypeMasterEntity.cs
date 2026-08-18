using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.RetrospectiveTax;

/// <summary>
/// Evidence type master (OC / CC / Electricity / Change Detection / Construction Year)
/// used as building blocks for retrospective rule conditions.
/// </summary>
[Table("EvidenceTypeMaster", Schema = "PTIS")]
public class EvidenceTypeMasterEntity : BaseEntity
{
    public string EvidenceCode { get; set; } = string.Empty;

    public string EvidenceName { get; set; } = string.Empty;

    public bool IsCertificate { get; set; }

    public int DisplayOrder { get; set; }
}
