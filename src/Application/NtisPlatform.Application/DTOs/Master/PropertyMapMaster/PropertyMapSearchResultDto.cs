using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

// ─────────────────────────────────────────────────────────────────────────────
// Top-level composite API response
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The response from GET /api/PropertyMapMaster/search.
/// Contains OldPropertySuggestions records matching the search.
/// </summary>
public class PropertyMapSearchResultDto
{
    public List<OldPropertySuggestionDto> OldPropertySuggestions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

// ─────────────────────────────────────────────────────────────────────────────
// Old property data block (PropertyMastOld)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Data from the PropertyMastOld table — represents historical/old property info.
/// Used as the base for OldPropertySuggestionDto.
/// </summary>
public class OldPropertyInfoDto
{
    public int      Id                      { get; set; }
    public string?  OldPropertyNo           { get; set; }
    public string?  OldOwnerName            { get; set; }
    public string?  OldOwnerNameEnglish     { get; set; }
    public string?  OldWardNo               { get; set; }
    public string?  OldEgovNo               { get; set; }
    public string?  OldMobileNo             { get; set; }
    public string?  OldPartitionNo          { get; set; }
    public string?  OldAddress              { get; set; }
    public string?  OldAddressEnglish       { get; set; }
    public string?  OldZoneNo               { get; set; }
    public string?  OldPlotNo               { get; set; }
    public string?  OldCSN                  { get; set; }
    public double?  OldALV                  { get; set; }
    public double?  OldRV                   { get; set; }
    public double?  OldGeneralTax           { get; set; }
    public double?  OldTotalTax             { get; set; }
    public double?  OldPlotArea             { get; set; }
    public double?  OldConstructionArea     { get; set; }
    public string?  OldFloor                { get; set; }
    public string?  OldUseType              { get; set; }
    public string?  OldOccupierName         { get; set; }
    public string?  OldOccupierNameEnglish  { get; set; }
    public string?  OldSocietyName          { get; set; }
    public string?  OldFlatOrShopNumber     { get; set; }
    public string?  OldWing                 { get; set; }
    public string?  OldEmailId              { get; set; }
    public double?  OldParkingAreaSqFt      { get; set; }
    public double?  OldParkingAreaSqMtr     { get; set; }
    public int?     OldPropertyTypeId       { get; set; }
    public int?     OldAssessmentYear       { get; set; }
    public string?  OldConstructionYear     { get; set; }
    public string?  OldConstructionTypeOfUseId { get; set; }
    public int?     NoOfOldToilets          { get; set; }
    public int?     OldTotalRooms           { get; set; }
    public DateTime? OldAssessmentDate      { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Suggestion DTOs (extend info DTOs with scoring fields)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A candidate old property from PropertyMastOld that matched at least one search field.
/// </summary>
public class OldPropertySuggestionDto : OldPropertyInfoDto
{
    public bool IsMapped { get; set; }
    public int? MappedNewPropertyId { get; set; }
    public string? MappedNewPropertyNo { get; set; }
    public List<PropertyDetailsOldDto> PropertyDetailsOld { get; set; } = new();

    /// <summary>TransMastOld rows for this old property, IsActive=1.</summary>
    public List<TransMastOldDto> TransMastOldRecords { get; set; } = new();
}
