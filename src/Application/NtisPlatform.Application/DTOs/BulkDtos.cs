namespace NtisPlatform.Application.DTOs.Bulk;

#region Result Types

/// <summary>
/// Generic result for Bulk operations.
/// </summary>
public sealed record BulkResult<T>(
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<T> Results,
    IReadOnlyList<string>? Errors = null)
{
    public bool HasFailures => FailedCount > 0;
    public bool AllSucceeded => FailedCount == 0;
}

#endregion

#region Bulk Update

/// <summary>
/// Generic wrapper for Bulk update operations.
/// </summary>
public record BulkUpdateItem<TKey, TUpdateDto>(TKey Id, TUpdateDto Data);

#endregion
