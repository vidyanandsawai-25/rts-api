using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseSubmissionDetails;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using Xunit;
using AssetPhotoDto = NtisPlatform.Application.DTOs.Asset_Management.AssetPhotoDto;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in AssetMasterDto.cs - the read/Create/Update DTOs for AMS.AssetMaster plus
/// the grouped parent/sub-asset response DTOs and the flat AssetMasterNamesDto lookup shape.
/// </summary>
public class AssetMasterDtoTests
{
    #region AssetMasterDto (read)

    [Fact]
    public void AssetMasterDto_PropertiesGetAndSetCorrectly()
    {
        var createdDate = DateTime.Now.AddDays(-10);
        var updatedDate = DateTime.Now;
        var details = new AssetDetailsDto { AssetId = 1 };
        var names = new AssetMasterNamesDto { OrganizationName = "Org" };
        var photos = new List<AssetPhotoDto> { new() { PhotoId = 1 } };
        var documents = new List<AssetDocumentDto> { new() { DocumentId = 1 } };
        var fieldValues = new List<AssetFieldValueDto> { new() { AssetId = 1 } };

        var dto = new AssetMasterDto
        {
            Id = 1,
            IsActive = true,
            CreatedDate = createdDate,
            UpdatedDate = updatedDate,
            AssetNo = "AST-001",
            AssetName = "Building A",
            AssetRegionalName = "इमारत",
            AssetCategoryId = 2,
            AssetTypeId = 3,
            ParentAssetId = 4,
            DepartmentId = 5,
            HierarchyLevel = 1,
            HierarchyPath = "/1/2",
            OwnershipType = "Owned",
            OccupancyStatus = "Occupied",
            AssetConditionId = 6,
            TotalUnits = 10,
            TotalSubUnits = 20,
            TotalFloors = 3,
            AssetDocumentId = 7,
            FieldValues = fieldValues,
            Photos = photos,
            Documents = documents,
            Details = details,
            Names = names,
            AssetCategoryName = "Buildings",
            AssetTypeName = "Commercial",
            DepartmentName = "Revenue",
            WardName = "Ward 1",
            WardNo = "W-1",
            ZoneName = "Zone A",
            ZoneNo = "Z-1",
            MoujaName = "Mouja X",
            SubZoneName = "SubZone Y",
            SubZoneNo = "SZ-1",
            AssetCondition = "Good",
            Address = "123 Main St",
            CapitalValue = 100000m,
            AssetLife = 30
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(createdDate, dto.CreatedDate);
        Assert.Equal(updatedDate, dto.UpdatedDate);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Building A", dto.AssetName);
        Assert.Equal("इमारत", dto.AssetRegionalName);
        Assert.Equal(2, dto.AssetCategoryId);
        Assert.Equal(3, dto.AssetTypeId);
        Assert.Equal(4, dto.ParentAssetId);
        Assert.Equal(5, dto.DepartmentId);
        Assert.Equal(1, dto.HierarchyLevel);
        Assert.Equal("/1/2", dto.HierarchyPath);
        Assert.Equal("Owned", dto.OwnershipType);
        Assert.Equal("Occupied", dto.OccupancyStatus);
        Assert.Equal(6, dto.AssetConditionId);
        Assert.Equal(10, dto.TotalUnits);
        Assert.Equal(20, dto.TotalSubUnits);
        Assert.Equal(3, dto.TotalFloors);
        Assert.Equal(7, dto.AssetDocumentId);
        Assert.Same(fieldValues, dto.FieldValues);
        Assert.Same(photos, dto.Photos);
        Assert.Same(documents, dto.Documents);
        Assert.Same(details, dto.Details);
        Assert.Same(names, dto.Names);
        Assert.Equal("Buildings", dto.AssetCategoryName);
        Assert.Equal("Commercial", dto.AssetTypeName);
        Assert.Equal("Revenue", dto.DepartmentName);
        Assert.Equal("Ward 1", dto.WardName);
        Assert.Equal("W-1", dto.WardNo);
        Assert.Equal("Zone A", dto.ZoneName);
        Assert.Equal("Z-1", dto.ZoneNo);
        Assert.Equal("Mouja X", dto.MoujaName);
        Assert.Equal("SubZone Y", dto.SubZoneName);
        Assert.Equal("SZ-1", dto.SubZoneNo);
        Assert.Equal("Good", dto.AssetCondition);
        Assert.Equal("123 Main St", dto.Address);
        Assert.Equal(100000m, dto.CapitalValue);
        Assert.Equal(30, dto.AssetLife);
    }

    [Fact]
    public void AssetMasterDto_Defaults_PhotosAndDocumentsAreEmptyList_FieldValuesIsNull_DetailsAndNamesAreInitialized()
    {
        var dto = new AssetMasterDto();

        Assert.NotNull(dto.Photos);
        Assert.Empty(dto.Photos);
        Assert.NotNull(dto.Documents);
        Assert.Empty(dto.Documents);
        Assert.Null(dto.FieldValues);
        Assert.NotNull(dto.Details);
        Assert.NotNull(dto.Names);
        Assert.Equal(0, dto.TotalUnits);
        Assert.Equal(0, dto.TotalSubUnits);
        Assert.Equal(0, dto.TotalFloors);
        Assert.Null(dto.AssetNo);
        Assert.Null(dto.AssetCategoryId);
        Assert.Null(dto.CapitalValue);
    }

    [Fact]
    public void AssetMasterDto_Names_HasJsonIgnoreAttribute()
    {
        // Names is a display-only lookup shape resolved server-side from FK joins; it's marked
        // [JsonIgnore] so it never round-trips through the wire, unlike the other flat name
        // properties (AssetCategoryName, DepartmentName, etc.) which do get serialized.
        var property = typeof(AssetMasterDto).GetProperty(nameof(AssetMasterDto.Names));

        var hasJsonIgnore = property?.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any();

        Assert.True(hasJsonIgnore);
    }

    #endregion

    #region CreateAssetMasterDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroOrganizationId_IsInvalid()
    {
        // OrganizationId is a non-nullable int, so [Required] can never fire (a struct is never
        // "missing"); omitting it just leaves the CLR default 0, which [Range(1, ...)] then rejects.
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 0,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.OrganizationId))
            && r.ErrorMessage == "AMS_AssetMaster_OrganizationId_InvalidRange");
    }

    [Fact]
    public void Create_WithEmptyAssetName_IsInvalid()
    {
        // AssetName is a non-nullable string defaulting to string.Empty - unlike a value type,
        // [Required] on a string DOES fire here because AllowEmptyStrings defaults to false.
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = string.Empty,
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.AssetName))
            && r.ErrorMessage == "AMS_AssetMaster_AssetName_Required");
    }

    [Fact]
    public void Create_WithAssetNameExceeding200Characters_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = new string('A', 201),
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.AssetName))
            && r.ErrorMessage == "AMS_AssetMaster_AssetName_MaxLengthExceeded_200");
    }

    [Fact]
    public void Create_WithZeroAssetCategoryId_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 0,
            AssetTypeId = 1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.AssetCategoryId))
            && r.ErrorMessage == "AMS_AssetMaster_AssetCategoryId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroAssetTypeId_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 0
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.AssetTypeId))
            && r.ErrorMessage == "AMS_AssetMaster_AssetTypeId_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeHierarchyLevel_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            HierarchyLevel = -1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.HierarchyLevel))
            && r.ErrorMessage == "AMS_AssetMaster_HierarchyLevel_InvalidRange");
    }

    [Fact]
    public void Create_WithHierarchyPathExceeding500Characters_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            HierarchyPath = new string('P', 501)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.HierarchyPath))
            && r.ErrorMessage == "AMS_AssetMaster_HierarchyPath_MaxLengthExceeded_500");
    }

    [Fact]
    public void Create_WithZeroWardId_IsInvalid()
    {
        // Representative of the "int?, [Range(1, int.MaxValue)]" shape shared by WardId, ZoneId,
        // SubZoneId, MoujaId, InChargeDesignationId, and AssetConditionId.
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            WardId = 0
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.WardId))
            && r.ErrorMessage == "AMS_AssetMaster_WardId_InvalidRange");
    }

    [Fact]
    public void Create_WithLatitudeOutOfRange_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            Latitude = 91m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.Latitude))
            && r.ErrorMessage == "AMS_AssetMaster_Latitude_InvalidRange");
    }

    [Fact]
    public void Create_WithLongitudeOutOfRange_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            Longitude = -181m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.Longitude))
            && r.ErrorMessage == "AMS_AssetMaster_Longitude_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeLandAreaSqMeter_IsInvalid()
    {
        // Representative of the "decimal?, [Range(0, double.MaxValue)]" shape shared by LandRate,
        // TotalLength, AverageWidth, LengthFt, WidthFt, and LandAreaSqFeet.
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            LandAreaSqMeter = -1m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.LandAreaSqMeter))
            && r.ErrorMessage == "AMS_AssetMaster_LandAreaSqMeter_InvalidRange");
    }

    [Fact]
    public void Create_WithInvalidInChargeMobile_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            InChargeMobile = "abcXYZ"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.InChargeMobile))
            && r.ErrorMessage == "AMS_AssetMaster_InChargeMobile_Invalid");
    }

    [Fact]
    public void Create_WithInvalidInChargeEmail_IsInvalid()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            InChargeEmail = "not-an-email"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.InChargeEmail))
            && r.ErrorMessage == "AMS_AssetMaster_InChargeEmail_Invalid");
    }

    [Fact]
    public void Create_Defaults_OptionalFieldsAreNull()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        Assert.Null(dto.ParentAssetId);
        Assert.Null(dto.DepartmentId);
        Assert.Null(dto.AssetRegionalName);
        Assert.Null(dto.HierarchyPath);
        Assert.Null(dto.Address);
        Assert.Null(dto.FieldValuesJson);
        Assert.Null(dto.PhotoFiles);
        Assert.Null(dto.PhotoMetadataJson);
        Assert.Equal(0, dto.HierarchyLevel);
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetMasterDto
        {
            OrganizationId = 0,
            AssetName = string.Empty,
            AssetCategoryId = 0,
            AssetTypeId = 0
        };

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 4);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.OrganizationId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.AssetName)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.AssetCategoryId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetMasterDto.AssetTypeId)));
    }

    #endregion

    #region UpdateAssetMasterDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithZeroOrganizationId_IsInvalid()
    {
        var dto = new UpdateAssetMasterDto
        {
            OrganizationId = 0,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetMasterDto.OrganizationId))
            && r.ErrorMessage == "AMS_AssetMaster_OrganizationId_InvalidRange");
    }

    [Fact]
    public void Update_WithEmptyAssetName_IsInvalid()
    {
        var dto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = string.Empty,
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetMasterDto.AssetName))
            && r.ErrorMessage == "AMS_AssetMaster_AssetName_Required");
    }

    [Fact]
    public void Update_WithAssetNoExceeding50Characters_IsInvalid()
    {
        var dto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            AssetNo = new string('N', 51)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetMasterDto.AssetNo))
            && r.ErrorMessage == "AMS_AssetMaster_AssetNo_MaxLengthExceeded_50");
    }

    [Fact]
    public void Update_WithNegativeParentAssetId_IsInvalid()
    {
        // Unlike CreateAssetMasterDto.ParentAssetId (plain int?, no attributes), the Update-side
        // ParentAssetId carries a [Range(1, int.MaxValue)] guard - an intentional asymmetry
        // (Create can't reparent on creation; Update can, and must supply a valid target id).
        var dto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            ParentAssetId = -1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetMasterDto.ParentAssetId))
            && r.ErrorMessage == "AMS_AssetMaster_ParentAssetId_InvalidRange");
    }

    [Fact]
    public void Update_Defaults_FieldValuesIsEmptyList_PhotoFilesIsNull()
    {
        var dto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Building A",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        Assert.NotNull(dto.FieldValues);
        Assert.Empty(dto.FieldValues);
        Assert.Null(dto.PhotoFiles);
        Assert.Null(dto.PhotoMetadataJson);
        Assert.Null(dto.AssetNo);
    }

    #endregion

    #region SubAssetGroupedResponseDto

    [Fact]
    public void SubAssetGroupedResponseDto_PropertiesGetAndSetCorrectly()
    {
        var parentAsset = new ParentAssetDetailDto { Id = 1, AssetNo = "AST-001" };
        var subAssets = new List<SubAssetDetailDto> { new() { Id = 2, AssetNo = "AST-002" } };

        var dto = new SubAssetGroupedResponseDto
        {
            ParentAsset = parentAsset,
            TotalSubAssets = 1,
            SubAssets = subAssets
        };

        Assert.Same(parentAsset, dto.ParentAsset);
        Assert.Equal(1, dto.TotalSubAssets);
        Assert.Same(subAssets, dto.SubAssets);
    }

    [Fact]
    public void SubAssetGroupedResponseDto_Defaults_ParentAssetIsNull_SubAssetsIsEmptyList()
    {
        var dto = new SubAssetGroupedResponseDto();

        Assert.Null(dto.ParentAsset);
        Assert.NotNull(dto.SubAssets);
        Assert.Empty(dto.SubAssets);
    }

    #endregion

    #region ParentAssetDetailDto

    [Fact]
    public void ParentAssetDetailDto_PropertiesGetAndSetCorrectly()
    {
        var createdDate = DateTime.Now.AddDays(-5);
        var details = new AssetDetailsDto { AssetId = 1 };
        var names = new AssetMasterNamesDto { OrganizationName = "Org" };
        var fieldValues = new List<AssetFieldValueDto> { new() { AssetId = 1 } };

        var dto = new ParentAssetDetailDto
        {
            Id = 1,
            IsActive = true,
            CreatedDate = createdDate,
            UpdatedDate = createdDate,
            AssetNo = "AST-001",
            AssetName = "Building A",
            AssetCategoryId = 2,
            AssetTypeId = 3,
            ParentAssetId = null,
            DepartmentId = 4,
            HierarchyLevel = 0,
            HierarchyPath = "/1",
            OwnershipType = "Owned",
            OccupancyStatus = "Vacant",
            FieldValues = fieldValues,
            Details = details,
            Names = names
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(createdDate, dto.CreatedDate);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Building A", dto.AssetName);
        Assert.Equal(2, dto.AssetCategoryId);
        Assert.Equal(3, dto.AssetTypeId);
        Assert.Null(dto.ParentAssetId);
        Assert.Equal(4, dto.DepartmentId);
        Assert.Equal("/1", dto.HierarchyPath);
        Assert.Equal("Owned", dto.OwnershipType);
        Assert.Equal("Vacant", dto.OccupancyStatus);
        Assert.Same(fieldValues, dto.FieldValues);
        Assert.Same(details, dto.Details);
        Assert.Same(names, dto.Names);
    }

    [Fact]
    public void ParentAssetDetailDto_Defaults_StringsAreEmpty_CollectionsAndNestedObjectsAreInitialized()
    {
        var dto = new ParentAssetDetailDto();

        Assert.Equal(string.Empty, dto.AssetNo);
        Assert.Equal(string.Empty, dto.AssetName);
        Assert.NotNull(dto.FieldValues);
        Assert.Empty(dto.FieldValues);
        Assert.NotNull(dto.Details);
        Assert.NotNull(dto.Names);
    }

    #endregion

    #region SubAssetDetailDto

    [Fact]
    public void SubAssetDetailDto_PropertiesGetAndSetCorrectly()
    {
        var details = new AssetDetailsDto { AssetId = 2 };
        var names = new AssetMasterNamesDto { OrganizationName = "Org" };
        var floorDetails = new List<SubUnitsDetailsDto> { new() { AssetId = 2 } };
        var roomWiseSubmissions = new List<AssetRoomWiseSubmissionDetailsDto> { new() { AssetId = 2 } };
        var renterDetails = new List<AssetLeaseRentDetailsDto> { new() { AssetId = 2 } };

        var dto = new SubAssetDetailDto
        {
            Id = 2,
            IsActive = true,
            AssetNo = "AST-002",
            AssetName = "Shop 1",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            ParentAssetId = 1,
            DepartmentId = 2,
            HierarchyLevel = 1,
            HierarchyPath = "/1/2",
            OwnershipType = "Owned",
            OccupancyStatus = "Occupied",
            Details = details,
            Names = names,
            TypeOfUseName = "Retail",
            SubTypeOfUseName = "General",
            FloorDetails = floorDetails,
            RoomWiseSubmissions = roomWiseSubmissions,
            RenterDetails = renterDetails
        };

        Assert.Equal(2, dto.Id);
        Assert.Equal("AST-002", dto.AssetNo);
        Assert.Equal("Shop 1", dto.AssetName);
        Assert.Equal(1, dto.ParentAssetId);
        Assert.Same(details, dto.Details);
        Assert.Same(names, dto.Names);
        Assert.Equal("Retail", dto.TypeOfUseName);
        Assert.Equal("General", dto.SubTypeOfUseName);
        Assert.Same(floorDetails, dto.FloorDetails);
        Assert.Same(roomWiseSubmissions, dto.RoomWiseSubmissions);
        Assert.Same(renterDetails, dto.RenterDetails);
    }

    [Fact]
    public void SubAssetDetailDto_Defaults_CollectionsAreEmpty_NamesResolvedFieldsAreNull()
    {
        var dto = new SubAssetDetailDto();

        Assert.NotNull(dto.FloorDetails);
        Assert.Empty(dto.FloorDetails);
        Assert.NotNull(dto.RoomWiseSubmissions);
        Assert.Empty(dto.RoomWiseSubmissions);
        Assert.NotNull(dto.RenterDetails);
        Assert.Empty(dto.RenterDetails);
        Assert.Null(dto.TypeOfUseName);
        Assert.Null(dto.SubTypeOfUseName);
    }

    #endregion

    #region AssetMasterNamesDto

    [Fact]
    public void AssetMasterNamesDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new AssetMasterNamesDto
        {
            OrganizationName = "Org",
            DepartmentName = "Revenue",
            AssetCategoryName = "Buildings",
            AssetTypeName = "Commercial",
            ParentAssetName = "Building A",
            ZoneName = "Zone A",
            WardName = "Ward 1",
            MoujaName = "Mouja X",
            ZoneNo = "Z-1",
            WardNo = "W-1",
            SubZoneNo = "SZ-1",
            AssetCondition = "Good"
        };

        Assert.Equal("Org", dto.OrganizationName);
        Assert.Equal("Revenue", dto.DepartmentName);
        Assert.Equal("Buildings", dto.AssetCategoryName);
        Assert.Equal("Commercial", dto.AssetTypeName);
        Assert.Equal("Building A", dto.ParentAssetName);
        Assert.Equal("Zone A", dto.ZoneName);
        Assert.Equal("Ward 1", dto.WardName);
        Assert.Equal("Mouja X", dto.MoujaName);
        Assert.Equal("Z-1", dto.ZoneNo);
        Assert.Equal("W-1", dto.WardNo);
        Assert.Equal("SZ-1", dto.SubZoneNo);
        Assert.Equal("Good", dto.AssetCondition);
    }

    [Fact]
    public void AssetMasterNamesDto_Defaults_AllFieldsAreNull()
    {
        var dto = new AssetMasterNamesDto();

        Assert.Null(dto.OrganizationName);
        Assert.Null(dto.DepartmentName);
        Assert.Null(dto.AssetCategoryName);
        Assert.Null(dto.AssetTypeName);
        Assert.Null(dto.ParentAssetName);
        Assert.Null(dto.ZoneName);
        Assert.Null(dto.WardName);
        Assert.Null(dto.MoujaName);
        Assert.Null(dto.ZoneNo);
        Assert.Null(dto.WardNo);
        Assert.Null(dto.SubZoneNo);
        Assert.Null(dto.AssetCondition);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
