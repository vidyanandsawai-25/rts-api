using System.Globalization;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Services.Rules;

public static class RTSRuleConditionMatcher
{
    public static bool Matches(RTSRuleMasterEntity rule, IReadOnlyDictionary<int, string> values)
    {
        var conditions = rule.Conditions.Where(x => x.IsActive).OrderBy(x => x.ConditionOrder).ToList();
        if (conditions.Count == 0) return false;
        var results = conditions.Select(condition =>
            values.TryGetValue(condition.FieldDefinitionId, out var actual) &&
            !string.IsNullOrWhiteSpace(actual) &&
            Compare(actual, condition.ExpectedValue, condition.ComparisonOperator));
        return string.Equals(rule.ConditionMatchType, "ANY", StringComparison.OrdinalIgnoreCase)
            ? results.Any(x => x)
            : results.All(x => x);
    }

    public static bool IsValidOperator(string fieldType, string comparisonOperator)
    {
        var type = fieldType.Trim().ToUpperInvariant();
        var op = comparisonOperator.Trim().ToUpperInvariant();
        if (op is "=" or "!=") return true;
        if (op == "CONTAINS") return type is not ("NUMBER" or "DECIMAL" or "INTEGER" or "INT" or "CURRENCY" or "AMOUNT" or "YEAR" or "DATE" or "DATETIME" or "TIME" or "MONTH");
        return op is ">" or ">=" or "<" or "<=" &&
            type is "NUMBER" or "DECIMAL" or "INTEGER" or "INT" or "CURRENCY" or "AMOUNT" or "YEAR" or "DATE" or "DATETIME" or "TIME" or "MONTH";
    }

    public static bool Compare(string actual, string expected, string comparisonOperator)
    {
        var op = comparisonOperator.Trim().ToUpperInvariant();
        if (op == "CONTAINS") return actual.Contains(expected, StringComparison.OrdinalIgnoreCase);
        if (op is "=" or "!=")
        {
            var equal = string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
            return op == "=" ? equal : !equal;
        }
        if (decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) &&
            decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var expectedNumber))
            return CompareOrdered(number, expectedNumber, op);
        if (DateTime.TryParse(actual, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
            DateTime.TryParse(expected, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expectedDate))
            return CompareOrdered(date, expectedDate, op);
        return false;
    }

    private static bool CompareOrdered<T>(T actual, T expected, string op) where T : IComparable<T> => op switch
    {
        ">" => actual.CompareTo(expected) > 0,
        ">=" => actual.CompareTo(expected) >= 0,
        "<" => actual.CompareTo(expected) < 0,
        "<=" => actual.CompareTo(expected) <= 0,
        _ => false
    };
}
