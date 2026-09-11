
using NtisPlatform.Application.DTOs.Queries;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NtisPlatform.Application.DTOs.PropertySurveySearch;


/// <summary>
/// Paginated response for Property Survey Search
/// </summary>
public class PropertySurveySearchPaginatedResponseDto
{
    public List<PropertySurveySearchResponseDto> Data { get; set; } = new();
    public int Count { get; set; }
    public bool HasNext { get; set; }
}

public class PropertySurveySearchResponseDto
{
    public int Id { get; set; }

    public string? OldWardNo { get; set; }
    public int? PropertyId { get; set; }
    public string? PropertyDescription { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerNameEnglish { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierNameEnglish { get; set; }
    public string? Address { get; set; }
    public string? AddressEnglish { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? SocietyName { get; set; }
    public string? Wing { get; set; }
    public string? FlatOrShopNo { get; set; }
    public int TotalWingCount { get; set; }
    public int TotalFlatShopCount { get; set; }
    public int TotalRowHouseCount { get; set; }
    public string? BuilderName { get; set; }
    public double? OldRV { get; set; }
    public double? OldTotalTax { get; set; }

    public int? OldAssessmentYear { get; set; }
    public int? OldConstructionYear { get; set; }

    public string? OldFloor { get; set; }
    public double? TotalArea { get; set; }
    public string? UPICId { get; set; }
    public int? CategoryId { get; set; }
    public string? PartType { get; set; }

    // From PropertyMapDetail - Identifies if property is in DRAFT/ACTIVE status
    public bool Active { get; set; }

    // From PropertyMapDetail - Status field (DRAFT, ACTIVE, CANCELLED, MODIFIED, or null if no mapping exists)
    public string? Status { get; set; }

    // From PropertyMast (NEW side) via PropertyMapDetail
    public string? NewPropertyNo { get; set; }
    public string? NewWardNo { get; set; }
    public string? NewPartitionNo { get; set; }

    public string DisplayProperty =>
        string.IsNullOrWhiteSpace(PropertyNo)
            ? string.IsNullOrWhiteSpace(PartitionNo)
                ? string.Empty
                : $"-{PartitionNo}"
            : string.IsNullOrWhiteSpace(PartitionNo)
                ? PropertyNo!
                : $"{PropertyNo}-{PartitionNo}";
}

/// <summary>
/// Society-level grouped response for Property Survey Search
/// One record per society/building with aggregated data
/// </summary>
public class PropertySocietyGroupedResponseDto
{
    public string? SocietyName { get; set; }
    public string? WardNo { get; set; }
    public string? OldWardNo { get; set; }
    public string Source { get; set; } = "OLD";

    // Aggregated counts
    public int TotalWingCount { get; set; }
    public int TotalFlatShopCount { get; set; }
    public int TotalRowHouseCount { get; set; }
    public int TotalProperties { get; set; }

    // Aggregated financial data
    public double TotalRV { get; set; }
    public double TotalTax { get; set; }
    public double TotalArea { get; set; }

    // Full sample property data (first property in the society)
    public int? Id { get; set; }
    public int? PropertyId { get; set; }
    public string? PropertyDescription { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerNameEnglish { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierNameEnglish { get; set; }
    public string? Address { get; set; }
    public string? AddressEnglish { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? Wing { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? BuilderName { get; set; }
    public double? OldRV { get; set; }
    public double? OldTotalTax { get; set; }
    public double? SampleTotalArea { get; set; }
    public string? UPICId { get; set; }
    public int? CategoryId { get; set; }
    public string? PartType { get; set; }

   

    public string DisplayProperty =>
        string.IsNullOrWhiteSpace(PropertyNo)
            ? string.IsNullOrWhiteSpace(PartitionNo)
                ? string.Empty
                : $"-{PartitionNo}"
            : string.IsNullOrWhiteSpace(PartitionNo)
                ? PropertyNo!
                : $"{PropertyNo}-{PartitionNo}";
}


/// <summary>
/// Paginated response for Society-level grouped results
/// </summary>
public class PropertySocietyGroupedPaginatedResponseDto
{
    public List<PropertySocietyGroupedResponseDto> Data { get; set; } = new();
    public int Count { get; set; }
    public bool HasNext { get; set; }
}

public class CreatedByUserPropertySearchRequestDto : BaseQueryParameters
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "UserId is required and must be greater than zero.")]
    public int UserId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "ModuleId is required and must be greater than zero.")]
    public int ModuleId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "WardId is required and must be greater than zero.")]
    public int WardId { get; set; }

    public string? PropertyNo { get; set; }

    public string? PartitionNo { get; set; }

    public int? PropertyTypeId { get; set; }

    public List<int>? PropertyTypeIds { get; set; }

    [StringLength(
        200,
        ErrorMessage = "SearchText cannot exceed 200 characters.")]
    public string? SearchText { get; set; }
}

public class NewlyCreatedPropertyWingDetailDto
{
    public int Id { get; set; }
    public int? SocietyDetailId { get; set; }
    public int? PropertyId { get; set; }
    public int? WingId { get; set; }
    public string? WingName { get; set; }
    public string? FromFloor { get; set; }
    public string? ToFloor { get; set; }
    public int NoOfFlat { get; set; }
    public int NoOfShop { get; set; }
    public int NoOfRowHouse { get; set; }
}

public class CreatedByUserPropertyResponseDto
{
    public int Id { get; set; }

    public int WardId { get; set; }

    public string? WardNo { get; set; }

    public string? PropertyNo { get; set; }

    public string? PartitionNo { get; set; }

    public int? PropertyTypeId { get; set; }

    public string? FlatOrShopNo { get; set; }

    public string? BlockNo { get; set; }

    public string? Type { get; set; }

    public string? BHK { get; set; }

    public int? CategoryId { get; set; }

    public string? PropertyDescription { get; set; }

    public string? OwnerName { get; set; }

    public string? OccupierName { get; set; }

    public string? Address { get; set; }

    public string? UpicId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public bool CanDelete { get; set; }

    public int MapCount { get; set; }

    // Society details
    public string? SocietyName { get; set; }

    public string? BuilderName { get; set; }

    public string? SocietyAddress { get; set; }

    public int NoOfFlat { get; set; }

    public int NoOfShop { get; set; }

    public int NoOfRowHouse { get; set; }

    public int TotalAmenityCount { get; set; }

    public string? CategoryName { get; set; }

    [JsonPropertyName("oldALV")]
    public double? OldALV { get; set; }

    [JsonPropertyName("oldRV")]
    public double? OldRV { get; set; }

    public double? OldGeneralTax { get; set; }

    public double? OldTotalTax { get; set; }

    public int TotalWingCount { get; set; }

    public double TotalArea { get; set; }

    public double? OldConstructionArea { get; set; }

    public List<PropertySearchDocumentDto> Documents { get; set; } = new();

    public List<NewlyCreatedPropertyWingDetailDto> Wings { get; set; } = new();

    public string DisplayProperty =>
        string.IsNullOrWhiteSpace(PropertyNo)
            ? string.IsNullOrWhiteSpace(PartitionNo)
                ? string.Empty
                : $"-{PartitionNo}"
            : string.IsNullOrWhiteSpace(PartitionNo)
                ? PropertyNo!
                : $"{PropertyNo}-{PartitionNo}";
}

public class PropertySearchDocumentDto
{
    public int PropertyPhotoId { get; set; }

    public int PropertyId { get; set; }

    public int PhotoTypeId { get; set; }

    public int DocumentId { get; set; }

    public Guid DocumentGuid { get; set; }

    public string? OriginalFileName { get; set; }

    public string? FileName { get; set; }

    public string? MimeType { get; set; }

    public string? FileExtension { get; set; }

    public long? FileSizeBytes { get; set; }

    public int? DocumentBindingId { get; set; }

    public bool IsLatest { get; set; }

    public int? DisplayOrder { get; set; }

    public string? Remarks { get; set; }

    public string? ViewUrl { get; set; }

    public string? DownloadUrl { get; set; }
}

public class UserPropertyPageDto
{
    public bool Status { get; set; } = true;

    public string Message { get; set; } = "Properties fetched successfully.";

    public int Count { get; set; }

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public bool HasNext { get; set; }

    public List<CreatedByUserPropertyResponseDto> Data { get; set; } = new();
}