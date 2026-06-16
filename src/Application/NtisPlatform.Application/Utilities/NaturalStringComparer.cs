using System.Text.RegularExpressions;

namespace NtisPlatform.Application.Utilities;

/// <summary>
/// Compares strings using natural (human) order so that numeric segments are sorted
/// numerically rather than lexicographically.  "1","2"..."10" instead of "1","10","2".
/// Also handles mixed strings such as "A1","A2","A10","B1".
/// </summary>
public sealed class NaturalStringComparer : IComparer<string?>
{
    public static readonly NaturalStringComparer Instance = new();

    private static readonly Regex _segments = new(@"(\d+)", RegexOptions.Compiled);

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var xParts = _segments.Split(x);
        var yParts = _segments.Split(y);

        int len = Math.Min(xParts.Length, yParts.Length);
        for (int i = 0; i < len; i++)
        {
            var xp = xParts[i];
            var yp = yParts[i];

            bool xIsNum = xp.Length > 0 && char.IsDigit(xp[0]);
            bool yIsNum = yp.Length > 0 && char.IsDigit(yp[0]);

            int cmp;
            if (xIsNum && yIsNum)
            {
                var xTrim = xp.TrimStart('0');
                var yTrim = yp.TrimStart('0');

                if (xTrim.Length == 0) xTrim = "0";
                if (yTrim.Length == 0) yTrim = "0";

                // Compare numeric value without risking overflow.
                cmp = xTrim.Length.CompareTo(yTrim.Length);
                if (cmp == 0) cmp = string.CompareOrdinal(xTrim, yTrim);

                // If the numeric value is equal, shorter (fewer leading zeros) sorts first.
                if (cmp == 0) cmp = xp.Length.CompareTo(yp.Length);
            }
            else
            {
                cmp = string.Compare(xp, yp, StringComparison.OrdinalIgnoreCase);
                if (cmp == 0) cmp = string.CompareOrdinal(xp, yp); // deterministic tie-breaker
            }

            if (cmp != 0) return cmp;
        }

        if (xParts.Length != yParts.Length) return xParts.Length.CompareTo(yParts.Length);

        // Final tie-breaker so distinct strings never compare as equal.
        return string.CompareOrdinal(x, y);
    }
}
