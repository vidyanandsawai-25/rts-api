using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Range;

#region Result Types

/// <summary>
/// Generic result for Range operations.
/// </summary>
public sealed record RangeResult<T>(int SuccessCount, int FailedCount, IReadOnlyList<T> Results, IReadOnlyList<string>? Errors = null)
{
    public bool HasFailures => FailedCount > 0;
    public bool AllSucceeded => FailedCount == 0;
}
#endregion

#region Range Create Request

/// <summary>
/// Generic request for creating records from a range.
/// Supports both numeric (1-9) and alphabetic (A-C) ranges.
/// </summary>
public record RangeCreateRequest<TCreateDto> where TCreateDto : class
{
    /// <summary>
    /// Start of the range (numeric: "1", "10" or alphabetic: "A", "AA")
    /// </summary>
    [Required(ErrorMessage = "RangeFrom is required")]
    public string RangeFrom { get; init; } = string.Empty;

    /// <summary>
    /// End of the range (numeric: "9", "20" or alphabetic: "C", "ZZ")
    /// </summary>
    [Required(ErrorMessage = "RangeTo is required")]
    public string RangeTo { get; init; } = string.Empty;

    /// <summary>
    /// Optional prefix to prepend to each generated value
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>
    /// Optional suffix to append to each generated value
    /// </summary>
    public string? Suffix { get; init; }

    /// <summary>
    /// Template DTO containing common properties for all generated records.
    /// Properties like ZoneId, Description template, etc.
    /// </summary>
    [Required(ErrorMessage = "Template data is required")]
    public TCreateDto Template { get; init; } = default!;

    /// <summary>
    /// Starting sequence number (optional, defaults to 1)
    /// </summary>
    public int StartSequenceNo { get; init; } = 1;
}

#endregion

#region Range Create Item
/// <summary>
/// Generic wrapper for Range Create operations.
/// </summary>
public record RangeCreateItem<TKey, TCreateDto>(TKey Id, TCreateDto Data);
#endregion