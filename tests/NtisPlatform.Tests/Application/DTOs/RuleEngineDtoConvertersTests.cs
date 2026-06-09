using System;
using System.Text.Json;
using Xunit;
using NtisPlatform.Application.DTOs.Rules.RuleEngine;

namespace NtisPlatform.Tests.Application.DTOs
{
    public class RuleEngineDtoConvertersTests
    {
        [Fact]
        public void CreateRuleEngineDto_DeserializesStandardFormatCorrectly()
        {
            // Arrange
            var json = @"{
                ""RuleCode"": ""R001"",
                ""RuleName"": ""Test Rule"",
                ""Description"": ""A test description"",
                ""RuleCategory"": ""ARV"",
                ""ConditionsJson"": ""{\""logicalOperator\"":\""AND\""}"",
                ""EffectJson"": ""{\""effectType\"":\""Decrease %\""}"",
                ""Priority"": 150,
                ""IsEnabled"": true,
                ""StopProcessing"": true,
                ""CreatedBy"": 42
            }";

            // Act
            var dto = JsonSerializer.Deserialize<CreateRuleEngineDto>(json);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("R001", dto.RuleCode);
            Assert.Equal("Test Rule", dto.RuleName);
            Assert.Equal("A test description", dto.Description);
            Assert.Equal("ARV", dto.RuleCategory);
            Assert.Equal("{\"logicalOperator\":\"AND\"}", dto.ConditionsJson);
            Assert.Equal("{\"effectType\":\"Decrease %\"}", dto.EffectJson);
            Assert.Equal(150, dto.Priority);
            Assert.True(dto.IsEnabled);
            Assert.True(dto.StopProcessing);
            Assert.Equal(42, dto.CreatedBy);
        }

        [Fact]
        public void CreateRuleEngineDto_DeserializesVisualFormatCorrectly()
        {
            // Arrange
            var json = @"{
                ""id"": ""f5697d61-7d1e-42f7-b00d-87f08a41c025"",
                ""description"": ""Rule 1 Description"",
                ""conditions"": {
                    ""logicalOperator"": ""AND"",
                    ""conditions"": [
                        { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": ""2"" }
                    ]
                },
                ""effect"": {
                    ""effectType"": ""Decrease %"",
                    ""value"": 51
                },
                ""Priority"": 200,
                ""IsEnabled"": false
            }";

            // Act
            var dto = JsonSerializer.Deserialize<CreateRuleEngineDto>(json);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("f5697d61-7d1e-42f7-b00d-87f08a41c025", dto.RuleCode);
            Assert.Equal("Rule 1 Description", dto.RuleName);
            Assert.Equal("Rule 1 Description", dto.Description);
            Assert.Equal(200, dto.Priority);
            Assert.False(dto.IsEnabled);

            // Conditions object should be serialized to raw string
            Assert.NotNull(dto.ConditionsJson);
            using var condDoc = JsonDocument.Parse(dto.ConditionsJson);
            Assert.Equal("AND", condDoc.RootElement.GetProperty("logicalOperator").GetString());

            // Effect object should be serialized to raw string
            Assert.NotNull(dto.EffectJson);
            using var effDoc = JsonDocument.Parse(dto.EffectJson);
            Assert.Equal("Decrease %", effDoc.RootElement.GetProperty("effectType").GetString());
            Assert.Equal(51, effDoc.RootElement.GetProperty("value").GetInt32());
        }

        [Fact]
        public void UpdateRuleEngineDto_DeserializesStandardFormatCorrectly()
        {
            // Arrange
            var json = @"{
                ""RuleName"": ""Updated Rule"",
                ""Description"": ""Updated desc"",
                ""ConditionsJson"": ""{\""logicalOperator\"":\""OR\""}"",
                ""EffectJson"": ""{\""effectType\"":\""Multiply\""}"",
                ""Priority"": 250,
                ""IsEnabled"": true,
                ""UpdatedBy"": 99
            }";

            // Act
            var dto = JsonSerializer.Deserialize<UpdateRuleEngineDto>(json);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("Updated Rule", dto.RuleName);
            Assert.Equal("Updated desc", dto.Description);
            Assert.Equal("{\"logicalOperator\":\"OR\"}", dto.ConditionsJson);
            Assert.Equal("{\"effectType\":\"Multiply\"}", dto.EffectJson);
            Assert.Equal(250, dto.Priority);
            Assert.True(dto.IsEnabled);
            Assert.Equal(99, dto.UpdatedBy);
        }

        [Fact]
        public void UpdateRuleEngineDto_DeserializesVisualFormatCorrectly()
        {
            // Arrange
            var json = @"{
                ""description"": ""Updated Rule Name"",
                ""conditions"": {
                    ""logicalOperator"": ""OR"",
                    ""conditions"": [
                        { ""fieldId"": ""Carpet Area"", ""operator"": ""EQUALS"", ""value"": ""100"" }
                    ]
                },
                ""effect"": {
                    ""effectType"": ""Multiply"",
                    ""value"": 20
                },
                ""Priority"": 90,
                ""IsEnabled"": true
            }";

            // Act
            var dto = JsonSerializer.Deserialize<UpdateRuleEngineDto>(json);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal("Updated Rule Name", dto.RuleName);
            Assert.Equal("Updated Rule Name", dto.Description);
            Assert.Equal(90, dto.Priority);
            Assert.True(dto.IsEnabled);

            // Conditions
            Assert.NotNull(dto.ConditionsJson);
            using var condDoc = JsonDocument.Parse(dto.ConditionsJson);
            Assert.Equal("OR", condDoc.RootElement.GetProperty("logicalOperator").GetString());

            // Effect
            Assert.NotNull(dto.EffectJson);
            using var effDoc = JsonDocument.Parse(dto.EffectJson);
            Assert.Equal("Multiply", effDoc.RootElement.GetProperty("effectType").GetString());
            Assert.Equal(20, effDoc.RootElement.GetProperty("value").GetInt32());
        }
    }
}
