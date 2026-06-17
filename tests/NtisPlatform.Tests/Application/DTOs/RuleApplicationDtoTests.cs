using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using System.Collections.Generic;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs
{
    public class RuleApplicationDtoTests
    {
        [Fact]
        public void RuleApplicationTraceEntry_Properties_RoundTrip()
        {
            var entry = new RuleApplicationTraceEntry
            {
                RuleCode = "RULE-1",
                RuleName = "Test Rule",
                EffectType = "Override",
                EffectValue = 500m,
                BaseValue = 1000m,
                ComputedValue = 500m,
                CumulativeValue = 500m,
                ApplyOrder = 1,
                StopProcessing = true
            };

            Assert.Equal("RULE-1", entry.RuleCode);
            Assert.Equal("Test Rule", entry.RuleName);
            Assert.Equal("Override", entry.EffectType);
            Assert.Equal(500m, entry.EffectValue);
            Assert.Equal(1000m, entry.BaseValue);
            Assert.Equal(500m, entry.ComputedValue);
            Assert.Equal(500m, entry.CumulativeValue);
            Assert.Equal(1, entry.ApplyOrder);
            Assert.True(entry.StopProcessing);
        }

        [Fact]
        public void RuleApplicationResult_Properties_RoundTrip()
        {
            var result = new RuleApplicationResult
            {
                FinalValue = 500m,
                AppliedRules = new List<RuleApplicationTraceEntry>
                {
                    new RuleApplicationTraceEntry { RuleCode = "RULE-1" }
                }
            };

            Assert.Equal(500m, result.FinalValue);
            Assert.NotNull(result.AppliedRules);
            Assert.Single(result.AppliedRules);
            Assert.Equal("RULE-1", result.AppliedRules[0].RuleCode);
        }
    }
}
