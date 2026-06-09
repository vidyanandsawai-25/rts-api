using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    /// <summary>
    /// Encapsulates all inputs required to execute tax calculation rules.
    /// </summary>
    public class RuleApplierContext
    {
        public string Category { get; set; } = string.Empty;
        public string ValueKey { get; set; } = string.Empty;
        public decimal InitialValue { get; set; }
        public PropertyCalculationContext PropertyContext { get; set; } = null!;
    }
}
