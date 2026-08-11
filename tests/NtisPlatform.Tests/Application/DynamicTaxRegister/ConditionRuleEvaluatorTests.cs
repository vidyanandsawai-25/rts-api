using System.Collections.Generic;
using System.Linq;
using NtisPlatform.Application.Services.Rules;
using Xunit;

namespace NtisPlatform.Tests.Application.DynamicTaxRegister;

/// <summary>
/// <see cref="ConditionRuleEvaluator"/> decides whether a condition-based tax applies to a
/// property, so a wrong answer here is a wrong bill. Two behaviours are load-bearing and easy to
/// regress: an EMPTY condition list is a valid "always matches" catch-all, and multi-condition rows
/// fold strictly LEFT-TO-RIGHT with no operator precedence ("A AND B OR C" means "(A AND B) OR C").
/// </summary>
public class ConditionRuleEvaluatorTests
{
    private static ConditionRuleEvaluator Evaluator() => new();

    /// <summary>Builds the ConditionsJson blob exactly as the config screen persists it.</summary>
    private static string Json(params (string Field, string Op, object? Value, string Logical)[] items) =>
        "[" + string.Join(",", items.Select(i =>
            $$"""{"FieldId":"{{i.Field}}","Operator":"{{i.Op}}","Value":{{Serialize(i.Value)}},"LogicalOperator":"{{i.Logical}}"}""")) + "]";

    private static string Serialize(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        object[] arr => "[" + string.Join(",", arr.Select(Serialize)) + "]",
        _ => value.ToString()!,
    };

    private static Dictionary<string, object> Fields(params (string Key, object Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value);

    // ── the catch-all contract ──────────────────────────────────────────────────

    [Theory]
    [InlineData("[]")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void EmptyOrAbsentConditionList_AlwaysMatches(string? conditionsJson)
    {
        // A zero-condition row is a deliberate catch-all in this feature, not a misconfiguration.
        Assert.True(Evaluator().Evaluate(conditionsJson, Fields()).IsMatch);
    }

    [Fact]
    public void MalformedJson_NeverMatches_AndNeverThrows()
    {
        // Deliberately NOT treated as an empty (catch-all) list: a corrupt blob that "always
        // matches" would silently charge every property. Instead it degrades to a sentinel
        // condition that can never match, so the rule is inert but the run survives.
        var result = Evaluator().Evaluate("{ this is not valid json", Fields(("Floor", 1)));

        Assert.False(result.IsMatch);
        var trace = Assert.Single(result.ConditionResults);
        Assert.False(trace.FieldResolved);
    }

    // ── operators ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("=", 5, "5", true)]
    [InlineData("=", 5, "6", false)]
    [InlineData("!=", 5, "6", true)]
    [InlineData(">", 6, "5", true)]
    [InlineData(">", 5, "5", false)]
    [InlineData(">=", 5, "5", true)]
    [InlineData("<", 4, "5", true)]
    [InlineData("<=", 5, "5", true)]
    [InlineData("<=", 6, "5", false)]
    public void ComparisonOperators(string op, object actual, string expected, bool shouldMatch)
    {
        var result = Evaluator().Evaluate(
            Json(("Floor", op, expected, "AND")),
            Fields(("Floor", actual)));

        Assert.Equal(shouldMatch, result.IsMatch);
    }

    [Theory]
    [InlineData("Equals")]
    [InlineData("EQUALS")]
    [InlineData("Equal To")]     // spaces are normalised to underscores
    [InlineData("equal_to")]
    [InlineData("==")]
    public void EqualityOperator_AliasesAndCasingAllResolve(string op)
    {
        // The UI and the rule builder emit different spellings for the same operator.
        Assert.True(Evaluator().Evaluate(Json(("Floor", op, "5", "AND")), Fields(("Floor", 5))).IsMatch);
    }

    [Fact]
    public void NumericComparison_IgnoresStringVsNumberTyping()
    {
        // Field values arrive boxed from a flattener; expected values arrive as JSON strings.
        Assert.True(Evaluator().Evaluate(Json(("Area", ">=", "500", "AND")), Fields(("Area", 500.0))).IsMatch);
        Assert.True(Evaluator().Evaluate(Json(("Area", ">=", "500", "AND")), Fields(("Area", "500"))).IsMatch);
    }

    [Fact]
    public void StringEquality_IsCaseInsensitive()
    {
        Assert.True(Evaluator().Evaluate(Json(("Use", "=", "SHOP", "AND")), Fields(("Use", "shop"))).IsMatch);
    }

    [Fact]
    public void NonNumericValue_OnANumericOperator_DoesNotMatch_AndDoesNotThrow()
    {
        var result = Evaluator().Evaluate(Json(("Use", ">", "5", "AND")), Fields(("Use", "shop")));

        Assert.False(result.IsMatch);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(1, true)]    // inclusive lower bound
    [InlineData(5, true)]    // inclusive upper bound
    [InlineData(6, false)]
    [InlineData(0, false)]
    public void Between_IsInclusive(object actual, bool shouldMatch)
    {
        var json = Json(("Area", "BETWEEN", new object[] { "1", "5" }, "AND"));

        Assert.Equal(shouldMatch, Evaluator().Evaluate(json, Fields(("Area", actual))).IsMatch);
    }

    [Theory]
    [InlineData("In", 2, true)]
    [InlineData("In", 9, false)]
    [InlineData("Not In", 9, true)]
    [InlineData("Not In", 2, false)]
    public void InAndNotIn(string op, object actual, bool shouldMatch)
    {
        var json = Json(("Category", op, new object[] { "1", "2", "3" }, "AND"));

        Assert.Equal(shouldMatch, Evaluator().Evaluate(json, Fields(("Category", actual))).IsMatch);
    }

    [Fact]
    public void UnknownOperator_NeverMatches_AndNeverThrows()
    {
        var result = Evaluator().Evaluate(Json(("Floor", "SOUNDS_LIKE", "5", "AND")), Fields(("Floor", 5)));

        Assert.False(result.IsMatch);
    }

    // ── field resolution ────────────────────────────────────────────────────────

    [Fact]
    public void MissingField_DoesNotMatch_AndIsReportedAsUnresolved()
    {
        // The trace drives the Test panel's "field not found on property" warning, so the flag
        // matters as much as the verdict.
        var result = Evaluator().Evaluate(Json(("NotRecorded", "=", "5", "AND")), Fields(("Floor", 5)));

        Assert.False(result.IsMatch);
        var trace = Assert.Single(result.ConditionResults);
        Assert.False(trace.FieldResolved);
    }

    [Fact]
    public void FieldIdSpacesAndPaddingAreNormalisedAway()
    {
        // "Rateable Value" in the UI is stored flattened as "RateableValue".
        var result = Evaluator().Evaluate(
            Json(("  Rateable Value  ", ">=", "100", "AND")),
            Fields(("RateableValue", 250)));

        Assert.True(result.IsMatch);
    }

    // ── left-to-right folding (no precedence) ───────────────────────────────────

    [Fact]
    public void AndChain_RequiresEveryCondition()
    {
        var json = Json(
            ("Floor", ">=", "2", "AND"),
            ("Area", ">=", "500", "AND"));

        Assert.True(Evaluator().Evaluate(json, Fields(("Floor", 3), ("Area", 600))).IsMatch);
        Assert.False(Evaluator().Evaluate(json, Fields(("Floor", 3), ("Area", 100))).IsMatch);
    }

    [Fact]
    public void OrChain_RequiresOnlyOne()
    {
        var json = Json(
            ("Floor", "=", "1", "AND"),
            ("Floor", "=", "9", "OR"));

        Assert.True(Evaluator().Evaluate(json, Fields(("Floor", 9))).IsMatch);
    }

    [Fact]
    public void MixedChain_FoldsStrictlyLeftToRight_WithNoOperatorPrecedence()
    {
        // "A AND B OR C" is ((A AND B) OR C). Under normal precedence (AND binds tighter) this
        // would evaluate the same, so the discriminating case is the reverse ordering below.
        var json = Json(
            ("A", "=", "1", "AND"),
            ("B", "=", "1", "AND"),
            ("C", "=", "1", "OR"));

        // A=0, B=0, C=1 ⇒ ((false AND false) OR true) = true
        Assert.True(Evaluator().Evaluate(json, Fields(("A", 0), ("B", 0), ("C", 1))).IsMatch);
    }

    [Fact]
    public void OrThenAnd_FoldsLeftToRight_NotByPrecedence()
    {
        // "A OR B AND C" folds as ((A OR B) AND C) = (true OR false) AND false = FALSE.
        // Under conventional precedence (AND first) it would be A OR (B AND C) = true. This test
        // is the one that actually pins the documented no-precedence behaviour.
        var json = Json(
            ("A", "=", "1", "AND"),
            ("B", "=", "1", "OR"),
            ("C", "=", "1", "AND"));

        var result = Evaluator().Evaluate(json, Fields(("A", 1), ("B", 0), ("C", 0)));

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void FirstConditionsLogicalOperator_IsIgnoredByTheFold()
    {
        // Nothing precedes the first condition, so its own AND/OR cannot join anything — an "OR"
        // there must not turn the row into a match-anything.
        var alone = Json(("Floor", "=", "9", "OR"));
        Assert.False(Evaluator().Evaluate(alone, Fields(("Floor", 1))).IsMatch);

        var leading = Json(
            ("Floor", "=", "9", "OR"),   // leading OR — ignored
            ("Area", "=", "500", "AND"));
        Assert.False(Evaluator().Evaluate(leading, Fields(("Floor", 1), ("Area", 500))).IsMatch);
    }

    [Fact]
    public void FirstConditionsLogicalOperator_IsStillEchoedVerbatimInTheTrace()
    {
        // NOTE: Evaluate's own comment claims the first item is "always reported as AND (set inside
        // EvaluateOne)" — that is stale; EvaluateOne echoes whatever was configured. Harmless,
        // because the fold ignores it (above) and the Test panel only renders the operator for
        // conditions after the first. Pinned here so the discrepancy is visible rather than a
        // surprise to the next reader.
        var json = Json(("Floor", "=", "1", "OR"));

        var result = Evaluator().Evaluate(json, Fields(("Floor", 1)));

        Assert.True(result.IsMatch);
        Assert.Equal("OR", result.ConditionResults[0].LogicalOperator);
    }

    [Fact]
    public void EveryConditionIsTraced_EvenAfterTheOutcomeIsDecided()
    {
        // The Test panel shows a per-condition pass/fail breakdown, so short-circuiting the trace
        // would blank out the explanation an admin relies on.
        var json = Json(
            ("A", "=", "1", "AND"),
            ("B", "=", "1", "AND"),
            ("C", "=", "1", "AND"));

        var result = Evaluator().Evaluate(json, Fields(("A", 0), ("B", 0), ("C", 0)));

        Assert.False(result.IsMatch);
        Assert.Equal(3, result.ConditionResults.Count);
    }

    // ── TryResolveNumericField (drives PER_UNIT) ────────────────────────────────

    [Theory]
    [InlineData(3, 3)]
    [InlineData("3", 3)]
    [InlineData(3.5, 3.5)]
    public void TryResolveNumericField_CoercesNumericValues(object stored, decimal expected)
    {
        Assert.True(Evaluator().TryResolveNumericField("Toilets", Fields(("Toilets", stored)), out var value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void TryResolveNumericField_UsesTheSameKeyNormalisationAsConditions()
    {
        // PER_UNIT must be able to multiply by any field a condition could match on.
        Assert.True(Evaluator().TryResolveNumericField(" No Of Toilets ", Fields(("NoOfToilets", 4)), out var value));
        Assert.Equal(4m, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolveNumericField_BlankFieldId_IsUnresolved(string? fieldId)
    {
        Assert.False(Evaluator().TryResolveNumericField(fieldId, Fields(("Toilets", 3)), out var value));
        Assert.Equal(0m, value);
    }

    [Fact]
    public void TryResolveNumericField_MissingField_IsUnresolved()
    {
        // Distinguishes "not recorded for this property" from "recorded as zero" — the caller must
        // not present a confident ₹0 charge for the former.
        Assert.False(Evaluator().TryResolveNumericField("Toilets", Fields(("Floor", 1)), out _));
    }

    [Fact]
    public void TryResolveNumericField_NonNumericValue_IsUnresolved()
    {
        Assert.False(Evaluator().TryResolveNumericField("Toilets", Fields(("Toilets", "many")), out _));
    }
}
