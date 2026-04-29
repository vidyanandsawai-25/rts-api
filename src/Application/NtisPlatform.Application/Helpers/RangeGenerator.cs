namespace NtisPlatform.Application.Helpers;

/// <summary>
/// Helper class for generating values from ranges (numeric or alphabetic).
/// </summary>
public static class RangeGenerator
{
    /// <summary>
    /// Generates a list of values from a range with optional prefix and suffix.
    /// Supports both numeric (1-9) and alphabetic (A-C) ranges.
    /// </summary>
    /// <param name="rangeFrom">Start of the range</param>
    /// <param name="rangeTo">End of the range</param>
    /// <param name="prefix">Optional prefix to prepend</param>
    /// <param name="suffix">Optional suffix to append</param>
    /// <returns>List of generated values</returns>
    public static List<string> GenerateRangeValues(string rangeFrom, string rangeTo, string? prefix = null, string? suffix = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rangeFrom, nameof(rangeFrom));
        ArgumentException.ThrowIfNullOrWhiteSpace(rangeTo, nameof(rangeTo));

        var values = new List<string>();
        var rangeType = DetermineRangeType(rangeFrom, rangeTo);

        var rawValues = rangeType switch
        {
            RangeType.Numeric => GenerateNumericRange(rangeFrom, rangeTo),
            RangeType.Alphabetic => GenerateAlphabeticRange(rangeFrom, rangeTo),
            _ => throw new ArgumentException($"Invalid range: '{rangeFrom}' to '{rangeTo}'. Both values must be either numeric or alphabetic.")
        };

        foreach (var value in rawValues)
        {
            var formattedValue = $"{prefix ?? string.Empty}{value}{suffix ?? string.Empty}";
            values.Add(formattedValue);
        }

        return values;
    }

    /// <summary>
    /// Determines the type of range based on the input values.
    /// </summary>
    private static RangeType DetermineRangeType(string rangeFrom, string rangeTo)
    {
        var fromIsNumeric = int.TryParse(rangeFrom, out _);
        var toIsNumeric = int.TryParse(rangeTo, out _);

        if (fromIsNumeric && toIsNumeric)
            return RangeType.Numeric;

        var fromIsAlphabetic = IsAlphabetic(rangeFrom);
        var toIsAlphabetic = IsAlphabetic(rangeTo);

        if (fromIsAlphabetic && toIsAlphabetic)
            return RangeType.Alphabetic;

        return RangeType.Invalid;
    }

    /// <summary>
    /// Checks if a string contains only alphabetic characters.
    /// </summary>
    private static bool IsAlphabetic(string value)
        => !string.IsNullOrEmpty(value) && value.All(char.IsLetter);

    /// <summary>
    /// Generates a numeric range.
    /// </summary>
    private static IEnumerable<string> GenerateNumericRange(string rangeFrom, string rangeTo)
    {
        var from = int.Parse(rangeFrom);
        var to = int.Parse(rangeTo);

        if (from > to)
            throw new ArgumentException($"Range start ({from}) cannot be greater than range end ({to}).");

        // Only pad if at least one bound is explicitly zero-padded (starts with '0' and length > 1)
        bool pad = (rangeFrom.Length > 1 && rangeFrom.StartsWith('0')) ||
                   (rangeTo.Length > 1 && rangeTo.StartsWith('0'));
        var padLength = pad ? Math.Max(rangeFrom.Length, rangeTo.Length) : 0;

        for (var i = from; i <= to; i++)
        {
            yield return pad ? i.ToString().PadLeft(padLength, '0') : i.ToString();
        }
    }

    /// <summary>
    /// Generates an alphabetic range (e.g., A-Z, AA-ZZ).
    /// </summary>
    private static IEnumerable<string> GenerateAlphabeticRange(string rangeFrom, string rangeTo)
    {
        var from = rangeFrom.ToUpperInvariant();
        var to = rangeTo.ToUpperInvariant();

        if (from.Length != to.Length)
            throw new ArgumentException($"Mixed-length alphabetic ranges are not supported: '{rangeFrom}' to '{rangeTo}'.");


        var fromIndex = AlphaToIndex(from);
        var toIndex = AlphaToIndex(to);

        if (fromIndex > toIndex)
            throw new ArgumentException($"Range start ({from}) cannot be greater than range end ({to}).");

        var maxLength = Math.Max(from.Length, to.Length);

        for (var i = fromIndex; i <= toIndex; i++)
        {
            yield return IndexToAlpha(i, maxLength);
        }
    }

    /// <summary>
    /// Converts an alphabetic string to a zero-based index.
    /// A=0, B=1, ..., Z=25, AA=26, AB=27, etc.
    /// </summary>
    private static int AlphaToIndex(string alpha)
    {
        var result = 0;
        foreach (var c in alpha)
        {
            result = result * 26 + (c - 'A' + 1);
        }
        return result - 1;
    }

    /// <summary>
    /// Converts a zero-based index to an alphabetic string.
    /// </summary>
    private static string IndexToAlpha(int index, int minLength = 1)
    {
        var result = string.Empty;
        index++; // Convert to 1-based

        while (index > 0)
        {
            index--;
            result = (char)('A' + index % 26) + result;
            index /= 26;
        }

        return result.PadLeft(minLength, 'A');
    }

    private enum RangeType
    {
        Invalid,
        Numeric,
        Alphabetic
    }
}
