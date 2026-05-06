using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master
{
    [Table("RuleScopeMaster", Schema = "CORE")]
    public class RuleScopeEntity : BaseEntity
    {
        public string RuleScope { get; set; } = string.Empty;
    }
}