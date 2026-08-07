namespace NtisPlatform.Application.Models;

/// <summary>
/// Shared clamp for this codebase's "PageSize == -1 means unlimited" pagination convention.
/// Several Dynamic Tax Register endpoints computed Skip/Take inline with no floor or ceiling —
/// PageSize 0 or a negative value other than -1 reached SQL Server's FETCH NEXT/OFFSET directly
/// and threw, and an absurd PageNumber × PageSize product could overflow int. The -1 "everything"
/// case also had no upper bound at all: any caller could pull an entire table in one response.
/// This does not change the -1 convention itself (several call sites depend on it, e.g. populating
/// reference dropdowns from small master tables) — it only puts a floor and a ceiling under it.
/// </summary>
public static class PagingGuard
{
    /// <summary>Fallback when PageSize is 0/negative (and not the -1 sentinel) — matches this
    /// codebase's existing PaginateInMemory/BaseQueryParameters default.</summary>
    private const int DefaultPageSize = 25;

    /// <summary>Ceiling for an explicit, positive PageSize — matches BaseQueryParameters.MaxPageSize,
    /// the cap already used by every other paged master-data endpoint in this codebase.</summary>
    private const int MaxPageSize = 100;

    /// <summary>Ceiling for the PageSize == -1 "everything" case. Generous relative to the
    /// reference/config tables this feature pages (taxes, rules, master keys, condition rows) —
    /// none are expected to approach this — but finite, unlike the previous unbounded behaviour.</summary>
    private const int MaxUnboundedPageSize = 5000;

    /// <summary>
    /// Normalizes (pageNumber, pageSize) against a known totalCount. Returns the page number to
    /// report back (floored at 1), the effective page size to Take, and the row count to Skip.
    /// </summary>
    public static (int PageNumber, int EffectivePageSize, int Skip) Normalize(int pageNumber, int pageSize, int totalCount)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;

        if (pageSize == -1)
        {
            var everything = Math.Min(totalCount == 0 ? 1 : totalCount, MaxUnboundedPageSize);
            return (normalizedPageNumber, everything, 0);
        }

        var effectivePageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        // long arithmetic so an absurd PageNumber (e.g. 300000000) can't overflow int here —
        // clamped down to totalCount, which Skip/Take would land on anyway (an empty page).
        var skip = (long)(normalizedPageNumber - 1) * effectivePageSize;
        if (skip > totalCount) skip = totalCount;

        return (normalizedPageNumber, effectivePageSize, (int)skip);
    }
}
