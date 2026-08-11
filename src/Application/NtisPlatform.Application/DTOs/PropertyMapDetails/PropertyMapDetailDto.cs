namespace NtisPlatform.Application.DTOs.PropertyMapDetails;

public class PropertyMapDetailDto : BaseDtos
{
    public int PropertyMapId { get; set; }
    public int? PropertyIdNew { get; set; }
    public int? PropertyIdOld { get; set; }
    public string PropertyNoOld { get; set; } = string.Empty;
    public string PropertyNoNew { get; set; } = string.Empty;
    public decimal? TaxSharePercent { get; set; }
    public decimal? AreaSharePercent { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ChangeReason { get; set; }
    public string? Remark { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Location { get; set; }
}
