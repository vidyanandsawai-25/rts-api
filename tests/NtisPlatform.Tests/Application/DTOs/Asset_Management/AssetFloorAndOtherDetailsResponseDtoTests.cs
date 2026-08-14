using System;
using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in AssetFloorAndOtherDetailsResponseDto.cs - plain response DTOs
/// (asset "floor + other details" aggregate view) with no DataAnnotations. Only property
/// round-trip and collection-default coverage is applicable here.
/// </summary>
public class AssetFloorAndOtherDetailsResponseDtoTests
{
    #region AssetFloorAndOtherDetailsResponseDto

    [Fact]
    public void AssetFloorAndOtherDetailsResponseDto_PropertiesGetAndSetCorrectly()
    {
        var floorSummary = new AssetFloorSummaryResponseDto { TotalFloors = 2 };
        var childAssets = new List<AssetChildAssetResponseDto> { new() { AssetId = 1 } };
        var inventoryData = new AssetInventoryDataResponseDto { ParentAssetId = 5 };

        var dto = new AssetFloorAndOtherDetailsResponseDto
        {
            FloorSummary = floorSummary,
            ChildAssets = childAssets,
            InventoryData = inventoryData
        };

        Assert.Same(floorSummary, dto.FloorSummary);
        Assert.Same(childAssets, dto.ChildAssets);
        Assert.Same(inventoryData, dto.InventoryData);
    }

    [Fact]
    public void AssetFloorAndOtherDetailsResponseDto_Defaults_ChildAssetsIsEmptyList_OthersAreNull()
    {
        var dto = new AssetFloorAndOtherDetailsResponseDto();

        Assert.Null(dto.FloorSummary);
        Assert.NotNull(dto.ChildAssets);
        Assert.Empty(dto.ChildAssets);
        Assert.Null(dto.InventoryData);
    }

    #endregion

    #region AssetFloorSummaryResponseDto

    [Fact]
    public void AssetFloorSummaryResponseDto_PropertiesGetAndSetCorrectly()
    {
        var floorDetails = new List<AssetFloorDetailResponseDto> { new() { FloorName = "Ground" } };
        var dto = new AssetFloorSummaryResponseDto
        {
            FloorDetails = floorDetails,
            TotalBaseValue = 100m,
            TotalCapitalValue = 200m,
            TotalMarketValue = 300m,
            TotalFloors = 3
        };

        Assert.Same(floorDetails, dto.FloorDetails);
        Assert.Equal(100m, dto.TotalBaseValue);
        Assert.Equal(200m, dto.TotalCapitalValue);
        Assert.Equal(300m, dto.TotalMarketValue);
        Assert.Equal(3, dto.TotalFloors);
    }

    [Fact]
    public void AssetFloorSummaryResponseDto_Defaults_FloorDetailsIsEmptyList()
    {
        var dto = new AssetFloorSummaryResponseDto();

        Assert.NotNull(dto.FloorDetails);
        Assert.Empty(dto.FloorDetails);
        Assert.Equal(0m, dto.TotalBaseValue);
        Assert.Equal(0, dto.TotalFloors);
    }

    #endregion

    #region AssetFloorDetailResponseDto

    [Fact]
    public void AssetFloorDetailResponseDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new AssetFloorDetailResponseDto
        {
            FloorName = "First Floor",
            ConstructionTypeName = "RCC",
            TypeOfUseName = "Residential",
            ConstructionYear = "2020",
            CarpetAreaSqMeter = 50m,
            CarpetAreaSqFeet = 538m,
            BuiltUpAreaSqMeter = 60m,
            BuiltUpAreaSqFeet = 645m,
            CapitalValue = 1000m
        };

        Assert.Equal("First Floor", dto.FloorName);
        Assert.Equal("RCC", dto.ConstructionTypeName);
        Assert.Equal("Residential", dto.TypeOfUseName);
        Assert.Equal("2020", dto.ConstructionYear);
        Assert.Equal(50m, dto.CarpetAreaSqMeter);
        Assert.Equal(538m, dto.CarpetAreaSqFeet);
        Assert.Equal(60m, dto.BuiltUpAreaSqMeter);
        Assert.Equal(645m, dto.BuiltUpAreaSqFeet);
        Assert.Equal(1000m, dto.CapitalValue);
    }

    [Fact]
    public void AssetFloorDetailResponseDto_Defaults_AllNullableFieldsAreNull()
    {
        var dto = new AssetFloorDetailResponseDto();

        Assert.Null(dto.FloorName);
        Assert.Null(dto.ConstructionTypeName);
        Assert.Null(dto.TypeOfUseName);
        Assert.Null(dto.ConstructionYear);
        Assert.Null(dto.CarpetAreaSqMeter);
        Assert.Null(dto.CarpetAreaSqFeet);
        Assert.Null(dto.BuiltUpAreaSqMeter);
        Assert.Null(dto.BuiltUpAreaSqFeet);
        Assert.Null(dto.CapitalValue);
    }

    #endregion

    #region AssetChildAssetResponseDto

    [Fact]
    public void AssetChildAssetResponseDto_PropertiesGetAndSetCorrectly()
    {
        var floorDetails = new List<AssetChildFloorDetailResponseDto> { new() { FloorName = "Second Floor" } };
        var dto = new AssetChildAssetResponseDto
        {
            AssetId = 1,
            SubUnitsDetailsId = 2,
            AssetNo = "AST-001",
            AssetName = "Shop 1",
            Category = "Commercial",
            Type = "Shop",
            OccupancyStatus = "Occupied",
            TypeOfUse = "Retail",
            SubTypeOfUse = "General",
            CarpetAreaSqMeter = 10m,
            CarpetAreaSqFeet = 107m,
            BuiltUpAreaSqMeter = 12m,
            BuiltUpAreaSqFeet = 129m,
            FloorName = "Second Floor",
            FloorDetails = floorDetails
        };

        Assert.Equal(1, dto.AssetId);
        Assert.Equal(2, dto.SubUnitsDetailsId);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Shop 1", dto.AssetName);
        Assert.Equal("Commercial", dto.Category);
        Assert.Equal("Shop", dto.Type);
        Assert.Equal("Occupied", dto.OccupancyStatus);
        Assert.Equal("Retail", dto.TypeOfUse);
        Assert.Equal("General", dto.SubTypeOfUse);
        Assert.Equal(10m, dto.CarpetAreaSqMeter);
        Assert.Equal(107m, dto.CarpetAreaSqFeet);
        Assert.Equal(12m, dto.BuiltUpAreaSqMeter);
        Assert.Equal(129m, dto.BuiltUpAreaSqFeet);
        Assert.Equal("Second Floor", dto.FloorName);
        Assert.Same(floorDetails, dto.FloorDetails);
    }

    [Fact]
    public void AssetChildAssetResponseDto_Defaults_StringsAreEmpty_FloorDetailsIsEmptyList()
    {
        var dto = new AssetChildAssetResponseDto();

        Assert.Equal(string.Empty, dto.AssetNo);
        Assert.Equal(string.Empty, dto.AssetName);
        Assert.Null(dto.Category);
        Assert.Null(dto.FloorName);
        Assert.NotNull(dto.FloorDetails);
        Assert.Empty(dto.FloorDetails);
    }

    #endregion

    #region AssetChildFloorDetailResponseDto

    [Fact]
    public void AssetChildFloorDetailResponseDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new AssetChildFloorDetailResponseDto
        {
            FloorName = "Third Floor",
            ConstructionTypeName = "RCC",
            TypeOfUseName = "Commercial",
            SubTypeOfUseName = "Retail",
            ConstructionYear = "2021",
            CarpetAreaSqMeter = 20m,
            CarpetAreaSqFeet = 215m,
            BuiltUpAreaSqMeter = 24m,
            BuiltUpAreaSqFeet = 258m,
            CapitalValue = 2000m
        };

        Assert.Equal("Third Floor", dto.FloorName);
        Assert.Equal("RCC", dto.ConstructionTypeName);
        Assert.Equal("Commercial", dto.TypeOfUseName);
        Assert.Equal("Retail", dto.SubTypeOfUseName);
        Assert.Equal("2021", dto.ConstructionYear);
        Assert.Equal(20m, dto.CarpetAreaSqMeter);
        Assert.Equal(215m, dto.CarpetAreaSqFeet);
        Assert.Equal(24m, dto.BuiltUpAreaSqMeter);
        Assert.Equal(258m, dto.BuiltUpAreaSqFeet);
        Assert.Equal(2000m, dto.CapitalValue);
    }

    #endregion

    #region AssetInventoryDataResponseDto

    [Fact]
    public void AssetInventoryDataResponseDto_PropertiesGetAndSetCorrectly()
    {
        var batches = new List<AssetInventoryBatchResponseDto> { new() { BatchId = 1 } };
        var dto = new AssetInventoryDataResponseDto
        {
            ParentAssetId = 1,
            ParentAssetName = "Building A",
            TotalBatches = 2,
            TotalUnits = 10,
            TotalPurchaseValue = 500m,
            TotalCapitalValue = 400m,
            Batches = batches
        };

        Assert.Equal(1, dto.ParentAssetId);
        Assert.Equal("Building A", dto.ParentAssetName);
        Assert.Equal(2, dto.TotalBatches);
        Assert.Equal(10, dto.TotalUnits);
        Assert.Equal(500m, dto.TotalPurchaseValue);
        Assert.Equal(400m, dto.TotalCapitalValue);
        Assert.Same(batches, dto.Batches);
    }

    [Fact]
    public void AssetInventoryDataResponseDto_Defaults_BatchesIsEmptyList_ParentAssetNameIsEmpty()
    {
        var dto = new AssetInventoryDataResponseDto();

        Assert.Equal(string.Empty, dto.ParentAssetName);
        Assert.NotNull(dto.Batches);
        Assert.Empty(dto.Batches);
    }

    #endregion

    #region AssetInventoryBatchResponseDto

    [Fact]
    public void AssetInventoryBatchResponseDto_PropertiesGetAndSetCorrectly()
    {
        var purchaseDate = DateTime.Now;
        var units = new List<AssetInventoryUnitResponseDto> { new() { AssetId = 1 } };
        var documents = new List<InventoryDocumentDto> { new() { InventoryDocumentId = 1 } };
        var dto = new AssetInventoryBatchResponseDto
        {
            BatchId = 1,
            InventoryType = "Furniture",
            ItemName = "Chair",
            ModelBrand = "Godrej",
            Specifications = "16GB RAM",
            PurchaseDate = purchaseDate,
            OwningDepartment = "IT",
            Condition = "Good",
            Quantity = 5,
            UnitValue = 100m,
            TotalBatchValue = 500m,
            TotalBatchCV = 400m,
            PhotoFileName = "photo.jpg",
            InvoiceFileName = "invoice.pdf",
            Units = units,
            Documents = documents
        };

        Assert.Equal(1, dto.BatchId);
        Assert.Equal("Furniture", dto.InventoryType);
        Assert.Equal("Chair", dto.ItemName);
        Assert.Equal("Godrej", dto.ModelBrand);
        Assert.Equal("16GB RAM", dto.Specifications);
        Assert.Equal(purchaseDate, dto.PurchaseDate);
        Assert.Equal("IT", dto.OwningDepartment);
        Assert.Equal("Good", dto.Condition);
        Assert.Equal(5, dto.Quantity);
        Assert.Equal(100m, dto.UnitValue);
        Assert.Equal(500m, dto.TotalBatchValue);
        Assert.Equal(400m, dto.TotalBatchCV);
        Assert.Equal("photo.jpg", dto.PhotoFileName);
        Assert.Equal("invoice.pdf", dto.InvoiceFileName);
        Assert.Same(units, dto.Units);
        Assert.Same(documents, dto.Documents);
    }

    [Fact]
    public void AssetInventoryBatchResponseDto_Defaults_UnitsAndDocumentsAreEmptyLists()
    {
        var dto = new AssetInventoryBatchResponseDto();

        Assert.NotNull(dto.Units);
        Assert.Empty(dto.Units);
        Assert.NotNull(dto.Documents);
        Assert.Empty(dto.Documents);
        Assert.Null(dto.InventoryType);
        Assert.Null(dto.PurchaseDate);
    }

    #endregion

    #region AssetInventoryUnitResponseDto

    [Fact]
    public void AssetInventoryUnitResponseDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new AssetInventoryUnitResponseDto
        {
            AssetId = 1,
            UnitNumber = 2,
            Condition = "Good",
            UnitPurchaseValue = 500m,
            UnitCapitalValue = 400m
        };

        Assert.Equal(1, dto.AssetId);
        Assert.Equal(2, dto.UnitNumber);
        Assert.Equal("Good", dto.Condition);
        Assert.Equal(500m, dto.UnitPurchaseValue);
        Assert.Equal(400m, dto.UnitCapitalValue);
    }

    [Fact]
    public void AssetInventoryUnitResponseDto_Defaults_NullableFieldsAreNull()
    {
        var dto = new AssetInventoryUnitResponseDto();

        Assert.Null(dto.Condition);
        Assert.Null(dto.UnitPurchaseValue);
        Assert.Null(dto.UnitCapitalValue);
    }

    #endregion
}
