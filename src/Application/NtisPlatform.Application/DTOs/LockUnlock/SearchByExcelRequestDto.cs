using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.LockUnlock;

public class ExcelPropertyRow
{
    public string ZoneNo { get; set; } = string.Empty;
    public string WardNo { get; set; } = string.Empty;
    public string PropertyNo { get; set; } = string.Empty;
    public string? PartitionNo { get; set; }
}

/// <summary>
/// Request DTO for property lock search by pre-parsed Excel rows.
/// Extends <see cref="BaseQueryParameters"/> for platform-standard pagination and search filter terms.
/// </summary>
public class SearchByExcelRequestDto : BaseQueryParameters
{
    public List<ExcelPropertyRow> Rows { get; set; } = [];
}
