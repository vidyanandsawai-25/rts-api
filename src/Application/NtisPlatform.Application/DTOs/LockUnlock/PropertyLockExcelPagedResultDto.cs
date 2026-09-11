namespace NtisPlatform.Application.DTOs.LockUnlock;

using NtisPlatform.Application.Models;

/// <summary>
/// Paged result DTO for property lock Excel search operations.
/// Extends platform-standard <see cref="PagedResult{T}"/> to include duplicate row statistics.
/// </summary>
public class PropertyLockExcelPagedResultDto : PagedResult<PropertyLockRowDto>
{
    public int DuplicateCount { get; set; }

    /// <summary>
    /// Backward-compatibility alias for DuplicateCount.
    /// </summary>
    public int DublicateCount
    {
        get => DuplicateCount;
        set => DuplicateCount = value;
    }

    public PropertyLockExcelPagedResultDto()
    {
    }

    public PropertyLockExcelPagedResultDto(
        IEnumerable<PropertyLockRowDto> items,
        int totalCount,
        int pageNumber,
        int pageSize,
        int duplicateCount = 0)
        : base(items, totalCount, pageNumber, pageSize)
    {
        DuplicateCount = duplicateCount;
    }
}
