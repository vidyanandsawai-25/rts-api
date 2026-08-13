using NtisPlatform.Application.Utilities;

namespace NtisPlatform.Application.Helpers;

/// <summary>
/// Fresh, self-contained range/gap-matching helper for the Tax Zoning Range feature,
/// delegating alphanumeric comparison to the shared <see cref="NaturalStringComparer"/>.
/// </summary>
public static class PropertyRangeMatcher
{
    /// <summary>
    /// Natural-sort comparer usable anywhere an <see cref="IComparer{T}"/> of string is needed
    /// (e.g. <c>OrderBy(x, PropertyRangeMatcher.Comparer)</c>).
    /// </summary>
    public static readonly IComparer<string?> Comparer = NaturalStringComparer.Instance;

    /// <summary>
    /// Compares two property numbers using the shared natural (human) sort order.
    /// </summary>
    public static int Compare(string? a, string? b)
    {
        return NaturalStringComparer.Instance.Compare(a, b);
    }

    /// <summary>
    /// Returns true when <paramref name="propertyNo"/> falls between <paramref name="from"/> and
    /// <paramref name="to"/> (inclusive) under natural-sort comparison. Order of from/to is
    /// self-correcting — whichever naturally sorts first is treated as the lower bound.
    /// </summary>
    public static bool IsInRange(string? propertyNo, string? from, string? to)
    {
        if (string.IsNullOrWhiteSpace(propertyNo))
            return false;

        var p = propertyNo.Trim();
        var lo = (from ?? string.Empty).Trim();
        var hi = (to ?? string.Empty).Trim();

        if (Compare(lo, hi) > 0)
            (lo, hi) = (hi, lo);

        return Compare(p, lo) >= 0 && Compare(p, hi) <= 0;
    }

    /// <summary>
    /// Given the property numbers that actually exist in a ward, the ranges already assigned in
    /// that ward, and a candidate new range, returns every property number that would be left
    /// uncovered by any range (existing or new) within the combined span of all of them. An empty
    /// list means the new range does not introduce a coverage gap; a non-empty list is the set of
    /// "orphaned" property numbers the caller should reject the save for.
    /// </summary>
    public static IReadOnlyList<string> FindGap(
        IEnumerable<string?> allPropertyNosInWard,
        IEnumerable<(string? From, string? To)> existingRanges,
        string? newFrom,
        string? newTo)
    {
        var ranges = (existingRanges ?? Enumerable.Empty<(string? From, string? To)>())
            .Append((From: newFrom, To: newTo))
            .Where(r => !string.IsNullOrWhiteSpace(r.From) && !string.IsNullOrWhiteSpace(r.To))
            .Select(r =>
            {
                var lo = r.From!.Trim();
                var hi = r.To!.Trim();
                (string From, string To) result = Compare(lo, hi) > 0 ? (hi, lo) : (lo, hi);
                return result;
            })
            .ToList();

        if (ranges.Count == 0)
            return Array.Empty<string>();

        var overallFrom = ranges.Select(r => r.From).OrderBy(x => x, Comparer).First();
        var overallTo = ranges.Select(r => r.To).OrderBy(x => x, Comparer).Last();

        return (allPropertyNosInWard ?? Enumerable.Empty<string?>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(p => Compare(p, overallFrom) >= 0 && Compare(p, overallTo) <= 0)
            .Where(p => !ranges.Any(r => IsInRange(p, r.From, r.To)))
            .OrderBy(p => p, Comparer)
            .ToList();
    }
}
