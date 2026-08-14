namespace NtisPlatform.Application.DTOs.CommonDetails;

/// <summary>
/// Result of a dry-run <c>import-excel-validate</c> check: only the rows that would fail a real
/// <c>import-excel</c> call, ready to render as a grid. <see cref="Columns"/> gives the display order
/// (wardNo, propertyNo, partitionNo, the update code's value fields, then ValidationRemark); each entry
/// in <see cref="Rows"/> is keyed by those same column names.
/// </summary>
public class ExcelValidationResultDto
{
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public int TotalRows { get; set; }
    public int FlaggedRowCount { get; set; }
}
