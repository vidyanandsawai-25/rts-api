namespace NtisPlatform.Core.Models;

/// <summary>
/// Query parameters for floor details old pagination
/// </summary>
public class FloorDetailsOldQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
    public int? OldFloorId { get; set; }
    public int? OldSubFloorId { get; set; }
    public int? OldConstructionTypeId { get; set; }
    public int? OldTypeOfUseId { get; set; }
    public int? OldSubTypeOfUseId { get; set; }
    public string? OldConstructionYear { get; set; }
    public string? OldAssessmentYear { get; set; }
}

/// <summary>
/// Paginated result for floor details old
/// </summary>
public class FloorDetailsOldPagedResult
{
    public List<PropertyDetailsOldDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
