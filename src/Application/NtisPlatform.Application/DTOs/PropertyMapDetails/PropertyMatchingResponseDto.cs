namespace NtisPlatform.Application.DTOs.PropertyMapDetails;

public class PropertyMatchingResponseDto
{
    // New Property Details
    public string? RowSource { get; set; }
    public int? PropertyId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? WingName { get; set; }
    public string? FlatShopNo { get; set; }
    public string? OwnerName { get; set; }
    public string? OccupierName { get; set; }
    public string? BHK { get; set; }
    public string? Floor { get; set; }
    public int? FloorId { get; set; }
    public bool? IsPhoto { get; set; }
    public string? MobileNo { get; set; }
    public string? ShopName { get; set; }
    public string? TypeOfUse { get; set; }
    public int? TypeOfUseId { get; set; }
    public string? AssessmentYear { get; set; }
    public string? ConstructionYear { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? PropertyTypeDescription { get; set; }
    public int? SubTypeOfUseId { get; set; }
    public string? SubTypeOfUse { get; set; }
    public string? Type { get; set; }
    public int? ConstructionTypeId { get; set; }
    public string? ConstructionType { get; set; }
    public string? OldSocietyName { get; set; }

    // Old Property Details
    public int? OldPropertyId { get; set; }
    public string? OldWardNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? OldOwnerName { get; set; }
    public string? OldOccupierName { get; set; }
    public string? OldRv { get; set; }
    public decimal? OldTotalTax { get; set; }
    public decimal? OldPropertyTax { get; set; }
    public string? OldAddress { get; set; }
    public string? OldWingName { get; set; }
    public string? OldFlatShopNo { get; set; }

    // Mapping / Identification Details
    public bool IsMatchProperty { get; set; }
    public bool IsMerge { get; set; }
    public bool Identify { get; set; }
    public string? IdentifyName { get; set; }
    public DateTime? IdentifyDate { get; set; }
}

public sealed class OldPropertyBase
{
    public int Id { get; set; }
    public string? OldSocietyName { get; set; }
    public string? OldWardNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? OldOwnerName { get; set; }
    public string? OldOccupierName { get; set; }
    public double? OldRV { get; set; }
    public double? OldTotalTax { get; set; }
    public double? OldGeneralTax { get; set; }
    public string? OldAddress { get; set; }
    public string? OldWing { get; set; }
    public string? OldFlatOrShopNumber { get; set; }
    public string WingKey { get; set; } = string.Empty;
    public string FlatKey { get; set; } = string.Empty;
}

public sealed class LatestMapping
{
    public int PropertyIdOld { get; set; }
    public int Id { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
