using System.Text.Json;
using Xunit;
using NtisPlatform.Application.Services.Rules;

namespace NtisPlatform.Tests.Application
{
    public class RuleJsonBuilderTests
    {
        [Fact]
        public void Build_WithSingleRule_GeneratesCorrectPolicyJson()
        {
            // Arrange
            var ruleName = "Single Rule Test";
            var ruleCode = "SR001";
            var conditions = @"{
                ""logicalOperator"": ""AND"",
                ""conditions"": [
                    { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": ""2"" }
                ]
            }";
            var effect = @"{
                ""effectType"": ""Decrease %"",
                ""value"": 10,
                ""overrideRate"": ""Rate""
            }";

            // Act
            var result = RuleJsonBuilder.Build(ruleName, ruleCode, true, "ARV", conditions, effect, "Single test description");

            // Assert
            Assert.NotNull(result);
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            Assert.Equal(ruleName, root.GetProperty("RuleName").GetString());
            Assert.True(root.GetProperty("isActive").GetBoolean());
            Assert.Equal("ARV", root.GetProperty("RuleCategory").GetString());

            var rules = root.GetProperty("rules");
            Assert.Equal(JsonValueKind.Array, rules.ValueKind);
            Assert.Equal(1, rules.GetArrayLength());

            var rule = rules[0];
            Assert.Equal(ruleCode, rule.GetProperty("RuleCode").GetString());
            Assert.Equal("Single test description", rule.GetProperty("errorMessage").GetString());
            Assert.True(rule.GetProperty("enabled").GetBoolean());
            Assert.Equal("LambdaExpression", rule.GetProperty("ruleExpressionType").GetString());
            Assert.Equal("input.Floor == 2", rule.GetProperty("expression").GetString());

            var actions = rule.GetProperty("Actions");
            var onSuccess = actions.GetProperty("OnSuccess");
            Assert.Equal("Decrease", onSuccess.GetProperty("Name").GetString());

            var context = onSuccess.GetProperty("Context");
            Assert.Equal("input.Rate * (1 - 10 / 100)", context.GetProperty("Expression").GetString());
            Assert.Equal("Decrease %", context.GetProperty("effectType").GetString());
            Assert.Equal("10", context.GetProperty("value").GetString());
            Assert.Equal("input.Rate", context.GetProperty("ParameterCode").GetString());
        }

        [Fact]
        public void Build_WithMultiRuleArray_GeneratesCorrectPolicyJson()
        {
            // Arrange
            var ruleName = "Combined Rules Test";
            var ruleCode = "CR001";
            var multiConditions = @"[
                {
                    ""id"": ""f5697d61-7d1e-42f7-b00d-87f08a41c025"",
                    ""description"": ""rule1"",
                    ""conditions"": {
                        ""logicalOperator"": ""AND"",
                        ""conditions"": [
                            { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": ""2"" }
                        ]
                    },
                    ""effect"": {
                        ""effectType"": ""Decrease %"",
                        ""value"": 51
                    }
                },
                {
                    ""id"": ""734c446f-4141-4794-851d-f86d382b63a9"",
                    ""description"": ""rule3"",
                    ""conditions"": {
                        ""logicalOperator"": ""AND"",
                        ""conditions"": [
                            { ""fieldId"": ""Carpet Area SqFeet"", ""operator"": ""EQUALS"", ""value"": ""100"" }
                        ]
                    },
                    ""effect"": {
                        ""effectType"": ""Multiply"",
                        ""value"": 20
                    }
                }
            ]";

            // Act
            var result = RuleJsonBuilder.Build(ruleName, ruleCode, true, "ARV", multiConditions, null, "Combined test description");

            // Assert
            Assert.NotNull(result);
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            Assert.Equal(ruleName, root.GetProperty("RuleName").GetString());
            Assert.True(root.GetProperty("isActive").GetBoolean());
            Assert.Equal("ARV", root.GetProperty("RuleCategory").GetString());

            var rules = root.GetProperty("rules");
            Assert.Equal(JsonValueKind.Array, rules.ValueKind);
            Assert.Equal(2, rules.GetArrayLength());

            // Check first rule
            var rule1 = rules[0];
            Assert.Equal("f5697d61-7d1e-42f7-b00d-87f08a41c025", rule1.GetProperty("RuleCode").GetString());
            Assert.Equal("rule1", rule1.GetProperty("errorMessage").GetString());
            Assert.True(rule1.GetProperty("enabled").GetBoolean());
            Assert.Equal("input.Floor == 2", rule1.GetProperty("expression").GetString());
            var onSuccess1 = rule1.GetProperty("Actions").GetProperty("OnSuccess");
            Assert.Equal("Decrease", onSuccess1.GetProperty("Name").GetString());
            Assert.Equal("input.Rate * (1 - 51 / 100)", onSuccess1.GetProperty("Context").GetProperty("Expression").GetString());

            // Check second rule
            var rule2 = rules[1];
            Assert.Equal("734c446f-4141-4794-851d-f86d382b63a9", rule2.GetProperty("RuleCode").GetString());
            Assert.Equal("rule3", rule2.GetProperty("errorMessage").GetString());
            Assert.True(rule2.GetProperty("enabled").GetBoolean());
            Assert.Equal("input.CarpetAreaSqFeet == 100", rule2.GetProperty("expression").GetString());
            var onSuccess2 = rule2.GetProperty("Actions").GetProperty("OnSuccess");
            Assert.Equal("Multiply", onSuccess2.GetProperty("Name").GetString());
            Assert.Equal("input.Rate * 20", onSuccess2.GetProperty("Context").GetProperty("Expression").GetString());
        }

        [Fact]
        public void Build_WithMultiRuleArrayIncludingStopProcessing_GeneratesCorrectStopProcessingValues()
        {
            // Arrange
            var ruleName = "Combined Rules Stop Processing Test";
            var ruleCode = "CR002";
            var multiConditions = @"[
                {
                    ""id"": ""r1"",
                    ""description"": ""rule1"",
                    ""stopProcessing"": true,
                    ""conditions"": {
                        ""logicalOperator"": ""AND"",
                        ""conditions"": [
                            { ""fieldId"": ""Floor"", ""operator"": ""EQUALS"", ""value"": ""2"" }
                        ]
                    },
                    ""effect"": {
                        ""effectType"": ""Decrease %"",
                        ""value"": 51
                    }
                },
                {
                    ""id"": ""r2"",
                    ""description"": ""rule2"",
                    ""stopProcessing"": false,
                    ""conditions"": {
                        ""logicalOperator"": ""AND"",
                        ""conditions"": [
                            { ""fieldId"": ""Carpet Area SqFeet"", ""operator"": ""EQUALS"", ""value"": ""100"" }
                        ]
                    },
                    ""effect"": {
                        ""effectType"": ""Multiply"",
                        ""value"": 20
                    }
                }
            ]";

            // Act
            var result = RuleJsonBuilder.Build(ruleName, ruleCode, true, "ARV", multiConditions, null, "Combined test description");

            // Assert
            Assert.NotNull(result);
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;
            var rules = root.GetProperty("rules");
            Assert.Equal(2, rules.GetArrayLength());

            var rule1 = rules[0];
            Assert.True(rule1.GetProperty("stopProcessing").GetBoolean());

            var rule2 = rules[1];
            Assert.False(rule2.GetProperty("stopProcessing").GetBoolean());
        }
        [Fact]
        public void Build_WithUserConditionsJson_GeneratesCorrectExpression()
        {
            // Arrange
            var ruleName = "demo";
            var ruleCode = "SR002";
            var conditions = @"[
                {
                    ""id"": ""5c0afcce-2c8e-4af3-957c-2c373f8a78d1"",
                    ""description"": ""dgfdg"",
                    ""conditions"": {
                        ""id"": ""a37a635d-96ff-4817-8c27-ec77079dc97b"",
                        ""logicalOperator"": ""AND"",
                        ""conditions"": [
                            {
                                ""id"": ""798e2133-117d-4c70-b279-63a909c9eb70"",
                                ""fieldId"": ""FloorId"",
                                ""operator"": ""In"",
                                ""value"": [""1"", ""2"", ""3""]
                            },
                            {
                                ""id"": ""80d182de-f79c-434d-b1fa-3228498c67bc"",
                                ""fieldId"": ""FloorId"",
                                ""operator"": ""Not In"",
                                ""value"": [""2"", ""1"", ""3"", ""4""]
                            },
                            {
                                ""id"": ""5455cd60-e4df-45f2-bb53-26125047fc2d"",
                                ""fieldId"": ""SocialAttributeId"",
                                ""operator"": ""contains any"",
                                ""value"": [""38"", ""39""]
                            },
                            {
                                ""id"": ""4ca47f9a-f474-444b-9e19-40d2d71b0fc4"",
                                ""fieldId"": ""SocialAttributeId"",
                                ""operator"": ""contains all"",
                                ""value"": [""1"", ""2""]
                            }
                        ],
                        ""groups"": []
                    },
                    ""effect"": {
                        ""effectType"": ""Increase %"",
                        ""value"": 20,
                        ""isPercentage"": true,
                        ""overrideRate"": 1
                    },
                    ""stopProcessing"": false,
                    ""ruleScopeName"": ""Property Level""
                }
            ]";

            // Act
            var result = RuleJsonBuilder.Build(ruleName, ruleCode, true, "RV", conditions, null, "demo description");

            // Assert
            Assert.NotNull(result);
            using var doc = JsonDocument.Parse(result);
            var rules = doc.RootElement.GetProperty("rules");
            Assert.Equal(1, rules.GetArrayLength());
            
            var expr = rules[0].GetProperty("expression").GetString();
            Assert.Contains("(input.FloorId == 1 || input.FloorId == 2 || input.FloorId == 3)", expr);
            Assert.Contains("(input.FloorId != 2 && input.FloorId != 1 && input.FloorId != 3 && input.FloorId != 4)", expr);
            Assert.Contains("(input.SocialAttributeId.Contains(38) || input.SocialAttributeId.Contains(39))", expr);
            Assert.Contains("(input.SocialAttributeId.Contains(1) && input.SocialAttributeId.Contains(2))", expr);
        }

        [Fact]
        public void Build_WithMasterTableOperators_GeneratesCorrectExpressions()
        {
            // Arrange
            var ruleName = "demo-master-operators";
            var ruleCode = "SR003";
            var conditions = @"{
                ""logicalOperator"": ""AND"",
                ""conditions"": [
                    { ""fieldId"": ""Field1"", ""operator"": ""Equal To"", ""value"": ""A"" },
                    { ""fieldId"": ""Field2"", ""operator"": ""Not Equal To"", ""value"": ""B"" },
                    { ""fieldId"": ""Field3"", ""operator"": ""Greater Than Or Equal To"", ""value"": 5 },
                    { ""fieldId"": ""Field4"", ""operator"": ""Less Than Or Equal To"", ""value"": 10 },
                    { ""fieldId"": ""Field5"", ""operator"": ""Value exists in list"", ""value"": [""X"", ""Y""] },
                    { ""fieldId"": ""Field6"", ""operator"": ""Value does not exist in list"", ""value"": [""W"", ""Z""] },
                    { ""fieldId"": ""Field7"", ""operator"": ""Contains any matching value"", ""value"": [""M"", ""N""] },
                    { ""fieldId"": ""Field8"", ""operator"": ""Contains all matching values"", ""value"": [""O"", ""P""] },
                    { ""fieldId"": ""Field9"", ""operator"": ""Value Between Range"", ""value"": [20, 50] }
                ]
            }";

            // Act
            var result = RuleJsonBuilder.Build(ruleName, ruleCode, true, "RV", conditions, null, "test description");

            // Assert
            Assert.NotNull(result);
            using var doc = JsonDocument.Parse(result);
            var expr = doc.RootElement.GetProperty("rules")[0].GetProperty("expression").GetString();

            Assert.Contains("input.Field1 == \"A\"", expr);
            Assert.Contains("input.Field2 != \"B\"", expr);
            Assert.Contains("input.Field3 >= 5", expr);
            Assert.Contains("input.Field4 <= 10", expr);
            Assert.Contains("(input.Field5 == \"X\" || input.Field5 == \"Y\")", expr);
            Assert.Contains("(input.Field6 != \"W\" && input.Field6 != \"Z\")", expr);
            Assert.Contains("input.Field7 contains (\"M\", \"N\")", expr);
            Assert.Contains("input.Field8 contains (\"O\", \"P\")", expr);
            Assert.Contains("input.Field9 >= 20 && input.Field9 <= 50", expr);
        }

        [Fact]
        public void Build_WithLiteralOperators_GeneratesCorrectExpressions()
        {
            // Arrange
            var ruleName = "demo-literal-operators";
            var ruleCode = "SR004";
            var conditions = @"{
                ""logicalOperator"": ""AND"",
                ""conditions"": [
                    { ""fieldId"": ""FloorId"", ""operator"": "">"", ""value"": ""10"" },
                    { ""fieldId"": ""FloorId2"", ""operator"": ""<="", ""value"": ""10"" },
                    { ""fieldId"": ""TypeOfUseGroupId"", ""operator"": ""="", ""value"": ""3"" },
                    { ""fieldId"": ""TypeOfUseGroupId2"", ""operator"": ""=="", ""value"": ""1"" },
                    { ""fieldId"": ""SocialAttributeId"", ""operator"": ""="", ""value"": ""28"" }
                ]
            }";

            // Act
            var result = RuleJsonBuilder.Build(ruleName, ruleCode, true, "RV", conditions, null, "test description");

            // Assert
            Assert.NotNull(result);
            using var doc = JsonDocument.Parse(result);
            var expr = doc.RootElement.GetProperty("rules")[0].GetProperty("expression").GetString();

            Assert.Contains("input.FloorId > 10", expr);
            Assert.Contains("input.FloorId2 <= 10", expr);
            Assert.Contains("input.TypeOfUseGroupId == 3", expr);
            Assert.Contains("input.TypeOfUseGroupId2 == 1", expr);
            Assert.Contains("input.SocialAttributeId.Contains(28)", expr);
        }
    }
}
