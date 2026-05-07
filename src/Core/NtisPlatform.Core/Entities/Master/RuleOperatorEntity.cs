namespace NtisPlatform.Core.Entities.Master;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("RuleOperatorMaster", Schema = "CORE")]
public class RuleOperatorEntity : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Operator { get; set; } = string.Empty;  // "=", ">", "LIKE"

    [Required]
    [MaxLength(100)]
    public string OperatorDescription { get; set; } = string.Empty;  // "Equals", "Greater Than"
}