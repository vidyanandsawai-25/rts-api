using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities
{
    public class PropertyRuleApplicationLogEntity : BaseEntity, IHardDeletable
    {
        public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
        public virtual PropertyEntity? PropertyMast { get; set; }

        public int PropertyId { get; set; }
        public int PropertyDetailsId { get; set; }
        public int FinanceYear { get; set; }
        public string RuleCategory { get; set; } = string.Empty; // "RV", "CV", etc.
        public string RuleCode { get; set; } = string.Empty; // unique rule identifier
        public string RuleName { get; set; } = string.Empty; // human-readable name
        public string EffectType { get; set; } = string.Empty; // "Increase %", "Decrease %", "Override", etc.
        public decimal EffectValue { get; set; } // e.g. 5.0 for 5%
        public decimal ApplyRate { get; set; } // actual applied percentage/value, e.g. 40.0 when 60% is decreased
        public decimal BaseValue { get; set; } // initial rate BEFORE any rule in the chain
        public decimal ComputedValue { get; set; } // result of THIS rule applied to previous cumulative
        public decimal CumulativeValue { get; set; } // running total AFTER this rule
        public int ApplyOrder { get; set; } // 1-based position in the applied chain
        public bool StopProcessing { get; set; } // did this rule halt further processing
        public DateTime AppliedAt { get; set; } // timestamp of calculation run

        public bool MarkedForDeletion { get; set; } = false;
        public DateTime? MarkedForDeletionDate { get; set; }
    }
}
