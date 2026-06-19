using System.Linq.Expressions;

namespace NtisPlatform.Application.Extensions;

/// <summary>
/// Extension methods for natural (human) alphanumeric ordering in EF Core LINQ queries.
/// Splits each value into its alpha prefix and numeric suffix via PATINDEX, then sorts by
/// (alpha prefix, numeric-suffix length, full value) — correctly ordering values like:
/// A1, A2, A9, A10, AM1, AM7, D1, D12, D13.
/// </summary>
public static class AlphanumericSortExtensions
{
    private const string DigitPattern = "%[0-9]%";

    public static IOrderedQueryable<T> OrderByNatural<T>(
        this IQueryable<T> source,
        Expression<Func<T, string?>> keySelector,
        bool descending = false)
    {
        var (alphaPrefix, numSuffixLen, param) = BuildParts(keySelector);

        var alphaPrefixLambda  = Expression.Lambda<Func<T, string>>(alphaPrefix,  param);
        var numSuffixLenLambda = Expression.Lambda<Func<T, int>>(numSuffixLen, param);

        if (descending)
            return source
                .OrderByDescending(alphaPrefixLambda)
                .ThenByDescending(numSuffixLenLambda)
                .ThenByDescending(keySelector!);

        return source
            .OrderBy(alphaPrefixLambda)
            .ThenBy(numSuffixLenLambda)
            .ThenBy(keySelector!);
    }

    public static IOrderedQueryable<T> ThenByNatural<T>(
        this IOrderedQueryable<T> source,
        Expression<Func<T, string?>> keySelector,
        bool descending = false)
    {
        var (alphaPrefix, numSuffixLen, param) = BuildParts(keySelector);

        var alphaPrefixLambda  = Expression.Lambda<Func<T, string>>(alphaPrefix,  param);
        var numSuffixLenLambda = Expression.Lambda<Func<T, int>>(numSuffixLen, param);

        if (descending)
            return source
                .ThenByDescending(alphaPrefixLambda)
                .ThenByDescending(numSuffixLenLambda)
                .ThenByDescending(keySelector!);

        return source
            .ThenBy(alphaPrefixLambda)
            .ThenBy(numSuffixLenLambda)
            .ThenBy(keySelector!);
    }

    // Appending '0' before PATINDEX ensures a digit is always found, preventing a zero
    // return that would produce a negative substring length for all-alpha values.
    private static (Expression alphaPrefix, Expression numSuffixLen, ParameterExpression param)
        BuildParts<T>(Expression<Func<T, string?>> keySelector)
    {
        var param  = keySelector.Parameters[0];
        var col    = keySelector.Body;
        var isNull = Expression.Equal(col, Expression.Constant(null, typeof(string)));

        var colPlusZero = Expression.Add(
            col,
            Expression.Constant("0"),
            typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!);

        var patIndex = Expression.Call(
            typeof(SqlDbFunctions),
            nameof(SqlDbFunctions.PatIndex),
            Type.EmptyTypes,
            Expression.Constant(DigitPattern),
            colPlusZero);

        var alphaLen        = Expression.Subtract(patIndex, Expression.Constant(1));
        var substringMethod = typeof(string).GetMethod("Substring", [typeof(int), typeof(int)])!;
        var alphaPrefix     = Expression.Condition(
            isNull,
            Expression.Constant(""),
            Expression.Call(col, substringMethod, Expression.Constant(0), alphaLen));

        var numSuffixLen = Expression.Condition(
            isNull,
            Expression.Constant(0),
            Expression.Add(
                Expression.Subtract(Expression.Property(col, "Length"), patIndex),
                Expression.Constant(1)));

        return (alphaPrefix, numSuffixLen, param);
    }
}
