using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

[Table("PolicyConfiguration", Schema = "PTIS")]
public class PolicyConfigurationEntity : BaseEntity
{
    public string PolicyCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string? PolicyValue { get; set; }
    public string? DefaultValue { get; set; }
    public string? Unit { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
