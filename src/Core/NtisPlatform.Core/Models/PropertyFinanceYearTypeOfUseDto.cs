namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Details mapped with unique Finance Years and TypeOfUse
/// </summary>
public class PropertyFinanceYearTypeOfUseDto
{
    public int PropertyId { get; set; }
    public int PropertyDetailId { get; set; }
    public int? FinanceYearId { get; set; }
    public string? FinanceYear { get; set; }
    public int? FloorId { get; set; }
    public int? SubFloorId { get; set; }
    public int TypeOfUseId { get; set; }
    public string? TypeOfUseCode { get; set; }
    public string? TypeOfUseDescription { get; set; }
}
