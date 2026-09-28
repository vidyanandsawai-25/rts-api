namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Intermediate projection DTO for property unit details in GetApartmentDetailsWingWise service.
/// </summary>
public class FetchDetailRowDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int? NoOfRooms { get; set; }
    public double? CarpetAreaSqMeter { get; set; }
    public double? CarpetAreaSqFeet { get; set; }
    public double? BuiltupAreaSqMeter { get; set; }
    public double? BuiltupAreaSqFeet { get; set; }
    public string? Floor { get; set; }
    public string? SubFloor { get; set; }
    public string? ConstructionType { get; set; }
    public string? TypeOfUse { get; set; }
    public string? Type { get; set; }
    public string? SubTypeOfUse { get; set; }
    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
}
