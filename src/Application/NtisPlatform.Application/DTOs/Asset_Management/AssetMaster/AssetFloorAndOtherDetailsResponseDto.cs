namespace NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;


public class AssetFloorAndOtherDetailsResponseDto
{
    public AssetFloorSummaryResponseDto? FloorSummary { get; set; }
    public List<AssetChildAssetResponseDto> ChildAssets { get; set; } = new();
    public AssetInventoryDataResponseDto? InventoryData { get; set; }
}

public class AssetFloorSummaryResponseDto
{
    public List<AssetFloorDetailResponseDto> FloorDetails { get; set; } = new();
    public decimal TotalBaseValue { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public decimal TotalMarketValue { get; set; }
    public int TotalFloors { get; set; }
}

public class AssetFloorDetailResponseDto
{
    public string? FloorName { get; set; }
    public string? ConstructionTypeName { get; set; }
    public string? TypeOfUseName { get; set; }
    public string? ConstructionYear { get; set; }
    public decimal? CarpetAreaSqMeter { get; set; }
    public decimal? CarpetAreaSqFeet { get; set; }
    public decimal? BuiltUpAreaSqMeter { get; set; }
    public decimal? BuiltUpAreaSqFeet { get; set; }
    public decimal? CapitalValue { get; set; }
}

public class AssetChildAssetResponseDto
{
    public int AssetId { get; set; }
    public int SubUnitsDetailsId { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Type { get; set; }
    public string? OccupancyStatus { get; set; }
    public string? TypeOfUse { get; set; }
    public string? SubTypeOfUse { get; set; }
    public decimal? CarpetAreaSqMeter { get; set; }
    public decimal? CarpetAreaSqFeet { get; set; }
    public decimal? BuiltUpAreaSqMeter { get; set; }
    public decimal? BuiltUpAreaSqFeet { get; set; }
    public string? FloorName { get; set; }
    public List<AssetChildFloorDetailResponseDto> FloorDetails { get; set; } = new();
}

public class AssetChildFloorDetailResponseDto
{
    public string? FloorName { get; set; }
    public string? ConstructionTypeName { get; set; }
    public string? TypeOfUseName { get; set; }
    public string? SubTypeOfUseName { get; set; }
    public string? ConstructionYear { get; set; }
    public decimal? CarpetAreaSqMeter { get; set; }
    public decimal? CarpetAreaSqFeet { get; set; }
    public decimal? BuiltUpAreaSqMeter { get; set; }
    public decimal? BuiltUpAreaSqFeet { get; set; }
    public decimal? CapitalValue { get; set; }
}

public class AssetInventoryDataResponseDto
{
    public int ParentAssetId { get; set; }
    public string ParentAssetName { get; set; } = string.Empty;
    public int TotalBatches { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalPurchaseValue { get; set; }
    public decimal TotalCapitalValue { get; set; }
    public List<AssetInventoryBatchResponseDto> Batches { get; set; } = new();
}

public class AssetInventoryBatchResponseDto
{
    public int BatchId { get; set; }
    public string? InventoryType { get; set; }
    public string? ItemName { get; set; }
    public string? ModelBrand { get; set; }
    public string? Specifications { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? OwningDepartment { get; set; }
    public string? Condition { get; set; }
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalBatchValue { get; set; }
    public decimal TotalBatchCV { get; set; }
    public string? PhotoFileName { get; set; }
    public string? InvoiceFileName { get; set; }
    public List<AssetInventoryUnitResponseDto> Units { get; set; } = new();
    public List<InventoryDocumentDto> Documents { get; set; } = new();
}

public class AssetInventoryUnitResponseDto
{
    public int AssetId { get; set; }
    public int UnitNumber { get; set; }
    public string? Condition { get; set; }
    public decimal? UnitPurchaseValue { get; set; }
    public decimal? UnitCapitalValue { get; set; }
}
