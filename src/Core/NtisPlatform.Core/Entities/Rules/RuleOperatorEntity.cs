namespace NtisPlatform.Core.Entities.Rules;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("RuleOperatorMaster", Schema = "PTIS")]
public class RuleOperatorEntity : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Operator { get; set; } = string.Empty;  // "=", ">", "LIKE"

    [Required]
    [MaxLength(100)]
    public string OperatorDescription { get; set; } = string.Empty;  // "Equals", "Greater Than"
}
