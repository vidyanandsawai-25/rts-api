
namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    public class RuleApplicationTraceEntry
    {
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string EffectType { get; set; } = string.Empty;
        public decimal EffectValue { get; set; }
        public decimal BaseValue { get; set; }    // initial rate before any rule
        public decimal ComputedValue { get; set; }    // output of this specific rule
        public decimal CumulativeValue { get; set; }    // running total after this rule
        public int ApplyOrder { get; set; }
        public bool StopProcessing { get; set; }
    }
}
