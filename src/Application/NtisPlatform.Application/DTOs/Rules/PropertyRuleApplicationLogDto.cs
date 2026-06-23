using System;

namespace NtisPlatform.Application.DTOs.Rules
{
    public class PropertyRuleApplicationLogDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int PropertyDetailsId { get; set; }
        public int FinanceYear { get; set; }
        public string RuleCategory { get; set; } = string.Empty;
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string EffectType { get; set; } = string.Empty;
        public decimal EffectValue { get; set; }
        public decimal ApplyRate { get; set; }
        public decimal BaseValue { get; set; }
        public decimal ComputedValue { get; set; }
        public decimal CumulativeValue { get; set; }
        public int ApplyOrder { get; set; }
        public bool StopProcessing { get; set; }
        public DateTime AppliedAt { get; set; }
        public bool IsActive { get; set; }
        public bool MarkedForDeletion { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? RuleScopeId { get; set; }
        public string? RuleScopeName { get; set; }
        public string? Name { get; set; }
    }
}
