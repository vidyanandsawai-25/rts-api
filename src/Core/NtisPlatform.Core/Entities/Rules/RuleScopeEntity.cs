using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Rules
{
    [Table("RuleScopeMaster", Schema = "PTIS")]
    public class RuleScopeEntity : BaseEntity
    {
        public string RuleScope { get; set; } = string.Empty;
    }
}
