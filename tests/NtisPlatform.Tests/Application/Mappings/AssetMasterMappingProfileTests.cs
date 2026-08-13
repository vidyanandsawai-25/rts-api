using System;
using System.Linq;
using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Mapping tests for AssetMasterMappingProfile (roadmap item C11). Covers the 5 CreateMap calls:
/// AssetMasterEntity -&gt; AssetMasterDto, AssetFieldValueEntity -&gt; AssetFieldValueDto,
/// CreateAssetMasterDto -&gt; AssetMasterEntity, UpdateAssetMasterDto -&gt; AssetMasterEntity, and
/// CreateAssetFieldValueDto -&gt; AssetFieldValueEntity.
///
/// Note on Configuration_IsValid: AssetMasterDto carries several DTO-only fields (Photos, Documents,
/// AssetCategoryName, AssetTypeName, DepartmentName, WardName/WardNo, ZoneName/ZoneNo, MoujaName,
/// SubZoneName/SubZoneNo, AssetCondition, Address, CapitalValue, AssetLife) that are populated by
/// dedicated application services (AssetPhotoApplicationService, AssetDocumentApplicationService,
/// join/lookup logic) after mapping, not by AutoMapper itself, and are not covered by a ForMember/
/// Ignore in this profile. These are the exact same gaps already documented in the project-wide
/// AutoMapperValidationTest.AutoMapper_Configuration_ShouldBeValid_WithDocumentedUnmappedProperties
/// allowlist (see tests/NtisPlatform.Tests/AutoMapperValidationTest.cs). A bare, un-caught
/// AssertConfigurationIsValid() on a MapperConfiguration built from ONLY this profile would throw for
/// those already-known gaps, so - like AssetDesignationMappingProfileTests/InventoryBatchMappingProfileTests
/// in this same folder - we run AssertConfigurationIsValid() for real and assert that any resulting
/// errors are exactly the documented "populated after mapping" unmapped-member gaps, not new/unexpected
/// configuration breakage (missing type maps, constructor failures, etc.).
/// </summary>
public class AssetMasterMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public AssetMasterMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    #region Configuration

    [Fact]
    public void Configuration_IsValid()
    {
        try
        {
            _configuration.AssertConfigurationIsValid();
            // All members mapped or ignored - nothing further to assert.
        }
        catch (AutoMapperConfigurationException ex)
        {
            // Any error that is NOT an "unmapped property" error indicates a real configuration
            // problem (missing CreateMap<>, bad ForMember expression, etc.) and must fail the test.
            var errors = ex.Errors ?? Enumerable.Empty<AutoMapperConfigurationException.TypeMapConfigErrors>();
            var otherErrors = errors.Where(e => e.UnmappedPropertyNames?.Any() != true).ToList();
            Assert.True(otherErrors.Count == 0,
                $"AssetMasterMappingProfile has non-unmapped-member configuration error(s): " +
                string.Join("; ", otherErrors.Select(e => e.ToString())));

            var unmappedMembers = errors
                .SelectMany(e => e.UnmappedPropertyNames ?? Enumerable.Empty<string>())
                .Distinct()
                .ToList();

            // Documented gaps only: AssetMasterDto fields resolved by dedicated application services
            // or join/lookup logic AFTER mapping (not by AutoMapper), matching the allowlist already
            // maintained in AutoMapperValidationTest.cs for the whole-assembly configuration.
            var expectedUnmapped = new[]
            {
                "Photos", "Documents", "AssetCategoryName", "AssetTypeName", "DepartmentName",
                "WardName", "WardNo", "ZoneName", "ZoneNo", "MoujaName", "SubZoneName", "SubZoneNo",
                "AssetCondition", "Address", "CapitalValue", "AssetLife",
                // Fields on Create/Update AssetMasterEntity destination with no DTO-side source and
                // not explicitly Ignore()d (populated elsewhere: repository/service/EF, or legacy
                // compatibility shim columns excluded from the EF model).
                "Id", "IsActive", "CreatedBy", "UpdatedBy", "MarkedForDeletion", "MarkedForDeletionDate",
                "InventoryBatchId",
                // AssetFieldValueDto fields resolved via a nav-property join not modeled on
                // AssetFieldValueEntity itself.
                "AssetName", "FieldDefinitionName"
            };

            var unexpected = unmappedMembers
                .Where(m => !expectedUnmapped.Contains(m, StringComparer.OrdinalIgnoreCase))
                .ToList();

            Assert.True(unexpected.Count == 0,
                $"AssetMasterMappingProfile has unexpected unmapped member(s) not in the documented " +
                $"list: {string.Join(", ", unexpected)}. If intentional, document them above; otherwise " +
                "fix the profile.");
        }
    }

    #endregion

    #region Entity to Dto

    [Fact]
    public void EntityToDto_IgnoresComputedFields()
    {
        // No Details/AssetCategory/AssetType/ParentAsset/FieldValues set up - just a bare entity.
        // TotalUnits/TotalSubUnits/TotalFloors/AssetDocumentId are DTO-only additions with no
        // matching entity property at all; the profile explicitly Ignore()s them so the service
        // (not AutoMapper) is responsible for populating them.
        var entity = new AssetMasterEntity
        {
            Id = 1,
            AssetNo = "AST-0001",
            AssetName = "Test Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20
        };

        var dto = _mapper.Map<AssetMasterDto>(entity);

        Assert.Equal(0, dto.TotalUnits);
        Assert.Equal(0, dto.TotalSubUnits);
        Assert.Equal(0, dto.TotalFloors);
        Assert.Null(dto.AssetDocumentId);
    }

    [Fact]
    public void EntityToDto_MapsDetailsAndNamesViaCustomProjection()
    {
        var entity = new AssetMasterEntity
        {
            Id = 5,
            AssetNo = "AST-0005",
            AssetName = "Ward Office",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            ParentAssetId = 1,
            Details = new AssetDetailsEntity
            {
                Id = 5,
                AssetId = 5,
                OrganizationId = 3,
                PlotNo = "PLOT-77",
                PropertyNo = "PROP-99",
                PartitionNo = "PART-1",
                UpicId = "UPIC-1",
                Address = "123 Main Street",
                InChargeName = "Ramesh Kumar"
            },
            AssetCategory = new AssetCategoryEntity
            {
                Id = 10,
                CategoryCode = "BLD",
                CategoryName = "Building"
            },
            AssetType = new AssetTypeEntity
            {
                Id = 20,
                TypeCode = "OFF",
                TypeName = "Office"
            },
            ParentAsset = new AssetMasterEntity
            {
                Id = 1,
                AssetNo = "AST-0001",
                AssetName = "Head Office",
                AssetCategoryId = 1,
                AssetTypeId = 1
            }
        };

        var dto = _mapper.Map<AssetMasterDto>(entity);

        // Details is projected via a custom MapFrom lambda, not flat property mapping.
        Assert.NotNull(dto.Details);
        Assert.Equal(5, dto.Details.Id);
        Assert.Equal("PLOT-77", dto.Details.PlotNo);
        Assert.Equal("PROP-99", dto.Details.PropertyNo);
        Assert.Equal("PART-1", dto.Details.PartitionNo);
        Assert.Equal("UPIC-1", dto.Details.UpicId);
        Assert.Equal(3, dto.Details.OrganizationId);
        Assert.Equal("123 Main Street", dto.Details.Address);
        Assert.Equal("Ramesh Kumar", dto.Details.InChargeName);

        // Names is likewise a custom MapFrom lambda reading the AssetCategory/AssetType/ParentAsset
        // navigation properties.
        Assert.NotNull(dto.Names);
        Assert.Equal("Building", dto.Names.AssetCategoryName);
        Assert.Equal("Office", dto.Names.AssetTypeName);
        Assert.Equal("Head Office", dto.Names.ParentAssetName);
    }

    [Fact]
    public void EntityToDto_WithNullNavigationProperties_ProjectsDetailsAndNamesWithoutThrowing()
    {
        // Details/AssetCategory/AssetType/ParentAsset are all null - the profile's custom MapFrom
        // lambdas guard every access with a null check, so this must not throw and must produce the
        // documented defaults (Id/OrganizationId = 0, everything else null).
        var entity = new AssetMasterEntity
        {
            Id = 7,
            AssetNo = "AST-0007",
            AssetName = "No Nav Props",
            AssetCategoryId = 10,
            AssetTypeId = 20
        };

        var dto = _mapper.Map<AssetMasterDto>(entity);

        Assert.NotNull(dto.Details);
        Assert.Equal(0, dto.Details.Id);
        Assert.Null(dto.Details.PlotNo);
        Assert.Equal(0, dto.Details.OrganizationId);

        Assert.NotNull(dto.Names);
        Assert.Null(dto.Names.AssetCategoryName);
        Assert.Null(dto.Names.AssetTypeName);
        Assert.Null(dto.Names.ParentAssetName);
    }

    #endregion

    #region AssetFieldValueEntity to AssetFieldValueDto

    [Fact]
    public void Map_AssetFieldValueEntityToDto_MapsAllProperties()
    {
        var entity = new AssetFieldValueEntity
        {
            Id = 1,
            AssetId = 5,
            FieldDefinitionId = 9,
            FieldName = "Colour",
            FieldValue = "Blue",
            MarkedForDeletion = false
        };

        var dto = _mapper.Map<AssetFieldValueDto>(entity);

        Assert.Equal(1, dto.Id);
        Assert.Equal(5, dto.AssetId);
        Assert.Equal(9, dto.FieldDefinitionId);
        Assert.Equal("Colour", dto.FieldName);
        Assert.Equal("Blue", dto.FieldValue);
    }

    #endregion

    #region CreateDto to Entity

    [Fact]
    public void CreateDtoToEntity_IgnoresAssetNoAndAuditDates()
    {
        // CreateAssetMasterDto has no AssetNo property at all - AssetNo is always backend-generated
        // via GenerateAssetNoAsync, so the entity's AssetNo must stay at its CLR default after
        // mapping. CreatedDate/UpdatedDate are likewise Ignore()d per CLAUDE.md section 11 (the
        // repository sets them).
        var createDto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "New Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetMasterEntity>(createDto);

        Assert.Equal(string.Empty, entity.AssetNo);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void CreateDtoToEntity_IgnoresNavigationAndLegacyShimFields()
    {
        var createDto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "New Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetMasterEntity>(createDto);

        Assert.Null(entity.FieldValues);
        Assert.Null(entity.AssetCategory);
        Assert.Null(entity.AssetType);
        Assert.Null(entity.ParentAsset);
        Assert.Null(entity.Details);
        Assert.Null(entity.InventoryBatch);
        Assert.Null(entity.SubUnitsDetails);
        // Dropped-from-AMS.AssetMaster compatibility shims - explicitly Ignore()d.
        Assert.Null(entity.PurchaseValue);
        Assert.Null(entity.PurchaseDate);
        Assert.Null(entity.DepreciationId);
        Assert.Equal(0, entity.AssetLocationDetailsId);
    }

    [Fact]
    public void CreateDtoToEntity_MapsCoreIdentificationFields()
    {
        var createDto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "New Asset",
            AssetRegionalName = "नयी संपत्ति",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            ParentAssetId = 3,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetMasterEntity>(createDto);

        Assert.Equal("New Asset", entity.AssetName);
        Assert.Equal("नयी संपत्ति", entity.AssetRegionalName);
        Assert.Equal(10, entity.AssetCategoryId);
        Assert.Equal(20, entity.AssetTypeId);
        Assert.Equal(3, entity.ParentAssetId);
        Assert.Equal(100, entity.CreatedBy);
    }

    #endregion

    #region UpdateDto to Entity

    [Fact]
    public void UpdateDtoToEntity_IgnoresIsActive()
    {
        // Setting IsActive = false on the update DTO must have NO effect on the entity through
        // AutoMapper: the profile explicitly Ignore()s IsActive on this map, because deactivation is
        // meant to go through ValidateForDeactivationAsync (reference-checked), not a direct field
        // copy that would silently bypass that guard.
        var updateDto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Existing Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            IsActive = false,
            UpdatedBy = 200
        };

        var existingEntity = new AssetMasterEntity
        {
            Id = 1,
            AssetNo = "AST-0001",
            AssetName = "Existing Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            IsActive = true
        };

        _mapper.Map(updateDto, existingEntity);

        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public void UpdateDtoToEntity_IgnoresAuditDates()
    {
        var updateDto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Existing Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            UpdatedBy = 200
        };

        var originalCreatedDate = DateTime.UtcNow.AddDays(-30);
        var existingEntity = new AssetMasterEntity
        {
            Id = 1,
            AssetNo = "AST-0001",
            AssetName = "Existing Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            CreatedDate = originalCreatedDate,
            UpdatedDate = DateTime.UtcNow.AddDays(-1)
        };
        var originalUpdatedDate = existingEntity.UpdatedDate;

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate);
        Assert.Equal(originalUpdatedDate, existingEntity.UpdatedDate);
    }

    [Fact]
    public void UpdateDtoToEntity_MapsUpdatedBy()
    {
        var updateDto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Existing Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            UpdatedBy = 300
        };

        var existingEntity = new AssetMasterEntity
        {
            Id = 1,
            AssetNo = "AST-0001",
            AssetName = "Existing Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20
        };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(300, existingEntity.UpdatedBy);
    }

    [Fact]
    public void UpdateDtoToEntity_IgnoresNavigationAndLegacyShimFields()
    {
        var updateDto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Existing Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            UpdatedBy = 200
        };

        var existingEntity = new AssetMasterEntity
        {
            Id = 1,
            AssetNo = "AST-0001",
            AssetName = "Existing Asset",
            AssetCategoryId = 10,
            AssetTypeId = 20,
            PurchaseValue = 5000m,
            PurchaseDate = new DateTime(2020, 1, 1),
            DepreciationId = 7,
            AssetLocationDetailsId = 42
        };

        _mapper.Map(updateDto, existingEntity);

        // Ignore()d on this map - a fresh existing-entity value must survive untouched.
        Assert.Equal(5000m, existingEntity.PurchaseValue);
        Assert.Equal(new DateTime(2020, 1, 1), existingEntity.PurchaseDate);
        Assert.Equal(7, existingEntity.DepreciationId);
        Assert.Equal(42, existingEntity.AssetLocationDetailsId);
    }

    #endregion

    #region CreateAssetFieldValueDto to Entity

    [Fact]
    public void CreateFieldValueDtoToEntity_DoesNotMapFieldValueViaAutoMapper()
    {
        // The profile's own comment claims FieldValue is set from "TextValue/NumberValue/DateValue/
        // BooleanValue" fields in hand-written service code (AssetMasterService) - but
        // CreateAssetFieldValueDto (see AssetFieldValueDto.cs) has no such typed-value properties;
        // it only has a single FieldValue string, matching the collapsed [FieldValue] column on
        // AssetFieldValueEntity (see the entity's own comment: "Schema collapses the former typed
        // columns ... into a single string column"). That comment is stale from an earlier DTO shape
        // and should be cleaned up/removed - it doesn't describe how FieldValue is set today. What we
        // CAN verify from the current profile is the actual, unconditional behavior of Ignore():
        // AutoMapper never touches the destination's FieldValue, regardless of what the source DTO
        // carries.
        var createDto = new CreateAssetFieldValueDto
        {
            AssetId = 5,
            FieldName = "Colour",
            FieldValue = "some value"
        };

        var entity = new AssetFieldValueEntity();

        _mapper.Map(createDto, entity);

        Assert.Null(entity.FieldValue);
    }

    [Fact]
    public void CreateFieldValueDtoToEntity_IgnoresIdAndAuditAndAssetNavigation()
    {
        var createDto = new CreateAssetFieldValueDto
        {
            AssetId = 5,
            FieldName = "Colour",
            FieldValue = "Blue"
        };

        var entity = new AssetFieldValueEntity { Asset = new AssetMasterEntity() };

        _mapper.Map(createDto, entity);

        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.AssetId);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
        // IsActive is Ignore()d on this map too, so the entity's own pre-mapping value (BaseEntity's
        // "= true" default here, since it was never set explicitly) survives untouched - it is NOT
        // overwritten by CreateBaseDtos.IsActive (which defaults to false and was left unset above).
        Assert.True(entity.IsActive);
        Assert.NotNull(entity.Asset);
    }

    [Fact]
    public void CreateFieldValueDtoToEntity_MapsFieldName()
    {
        var createDto = new CreateAssetFieldValueDto
        {
            AssetId = 5,
            FieldDefinitionId = 9,
            FieldName = "Colour",
            FieldValue = "Blue"
        };

        var entity = new AssetFieldValueEntity();

        _mapper.Map(createDto, entity);

        Assert.Equal("Colour", entity.FieldName);
        Assert.Equal(9, entity.FieldDefinitionId);
    }

    #endregion
}
