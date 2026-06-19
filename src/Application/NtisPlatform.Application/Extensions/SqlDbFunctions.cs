namespace NtisPlatform.Application.Extensions;

/// <summary>
/// Stub methods mapped to SQL Server built-in functions via EF Core's HasDbFunction.
/// These throw at runtime — they are only valid inside LINQ-to-SQL queries.
/// </summary>
public static class SqlDbFunctions
{
    /// <summary>Maps to SQL Server PATINDEX(pattern, expression).</summary>
    public static int PatIndex(string pattern, string expression) =>
        throw new InvalidOperationException("This method is for EF Core LINQ translation only.");
}
