using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    public class RuleApplicationResult
    {
        public decimal FinalValue { get; set; } // the final adjusted rate
        public List<RuleApplicationTraceEntry> AppliedRules { get; set; } = new(); // full trace
    }
}
