using NtisPlatform.Core.Entities;
using System;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities
{
    public class PropertyRuleApplicationLogEntityTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var appliedAt = DateTime.Now;
            var createdDate = DateTime.Now.AddMinutes(-10);
            var updatedDate = DateTime.Now;
            var markedForDeletionDate = DateTime.Now.AddMinutes(5);

            var entity = new PropertyRuleApplicationLogEntity
            {
                Id = 1,
                PropertyId = 10,
                PropertyDetailsId = 100,
                FinanceYear = 2026,
                RuleCategory = "RV",
                RuleCode = "RULE-101",
                RuleName = "Test Rule",
                EffectType = "Increase %",
                EffectValue = 5.5m,
                BaseValue = 1000m,
                ComputedValue = 1055m,
                CumulativeValue = 1055m,
                ApplyOrder = 1,
                StopProcessing = true,
                AppliedAt = appliedAt,
                CreatedDate = createdDate,
                UpdatedDate = updatedDate,
                CreatedBy = 99,
                UpdatedBy = 99,
                IsActive = true,
                MarkedForDeletion = true,
                MarkedForDeletionDate = markedForDeletionDate
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(10, entity.PropertyId);
            Assert.Equal(100, entity.PropertyDetailsId);
            Assert.Equal(2026, entity.FinanceYear);
            Assert.Equal("RV", entity.RuleCategory);
            Assert.Equal("RULE-101", entity.RuleCode);
            Assert.Equal("Test Rule", entity.RuleName);
            Assert.Equal("Increase %", entity.EffectType);
            Assert.Equal(5.5m, entity.EffectValue);
            Assert.Equal(1000m, entity.BaseValue);
            Assert.Equal(1055m, entity.ComputedValue);
            Assert.Equal(1055m, entity.CumulativeValue);
            Assert.Equal(1, entity.ApplyOrder);
            Assert.True(entity.StopProcessing);
            Assert.Equal(appliedAt, entity.AppliedAt);
            Assert.Equal(createdDate, entity.CreatedDate);
            Assert.Equal(updatedDate, entity.UpdatedDate);
            Assert.Equal(99, entity.CreatedBy);
            Assert.Equal(99, entity.UpdatedBy);
            Assert.True(entity.IsActive);
            Assert.True(entity.MarkedForDeletion);
            Assert.Equal(markedForDeletionDate, entity.MarkedForDeletionDate);
            Assert.Null(entity.PropertyDetails);
            Assert.Null(entity.PropertyMast);
        }
    }
}
