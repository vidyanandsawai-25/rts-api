using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive tests for ReferenceValidationService to achieve 100% line and branch coverage
/// </summary>
public class ReferenceValidationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ReferenceValidationService _service;

    public ReferenceValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new ReferenceValidationService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed data for AssessmentYearRangeCVEntity references
        var yearRangeCV = EntityTestHelpers.CreateAssessmentYearRangeCVEntity(id: 1);
        _context.AssessmentYearRangeCVEntities.Add(yearRangeCV);

        var ageFactor = EntityTestHelpers.CreateAgeFactorCVMasterEntity(
            id: 1,
            yearRangeCVId: 1,
            constructionTypeId: 1);
        _context.AgeFactorCVMasters.Add(ageFactor);

        // Seed data for SubFloorEntity references
        var subFloor = EntityTestHelpers.CreateSubFloorEntity(id: 1);
        _context.SubFloorEntity.Add(subFloor);

        var propertyDetails = EntityTestHelpers.CreatePropertyDetailsEntity(id: 1);
        propertyDetails.SubFloorId = 1;
        _context.PropertyDetails.Add(propertyDetails);

        // Seed data for ConstructionTypeEntity references
        var constructionType = EntityTestHelpers.CreateConstructionTypeEntity(id: 1);
        _context.ConstructionTypeEntity.Add(constructionType);

        var rate = EntityTestHelpers.CreateRateEntity(id: 1);
        rate.ConstructionTypeId = 1;
        _context.RateEntity.Add(rate);

        // Seed data for TaxZoneEntity references
        var taxZone = EntityTestHelpers.CreateTaxZoneEntity(id: 1);
        _context.TaxZoneMaster.Add(taxZone);

        var property = EntityTestHelpers.CreatePropertyEntity(id: 1);
        property.TaxZoneId = 1;
        _context.PropertyMast.Add(property);

        // Seed data for FloorEntity references
        var floor = EntityTestHelpers.CreateFloorEntity(id: 1);
        _context.FloorEntity.Add(floor);

        var floorFactor = EntityTestHelpers.CreateFloorFactorCVMasterEntity(
            id: 1,
            floorId: 1,
            yearRangeCVId: 1);
        _context.FloorFactorCVMasters.Add(floorFactor);

        // Seed data for AssessmentYearRangeEntity references
        var yearRange = EntityTestHelpers.CreateAssessmentYearRangeEntity(id: 1);
        _context.AssessmentYearRangeEntities.Add(yearRange);

        var depreciation = EntityTestHelpers.CreateDepreciationEntity(id: 1);
        depreciation.YearRangeRVId = 1;
        _context.DepreciationMaster.Add(depreciation);

        // Seed data for RateSectionEntity references
        var rateSection = EntityTestHelpers.CreateRateSectionEntity(id: 1);
        _context.RateSection.Add(rateSection);

        var rateSectionDetail = EntityTestHelpers.CreateRateSectionDetailsEntity(id: 1);
        rateSectionDetail.RateSectionId = 1;
        _context.RateSectionDetails.Add(rateSectionDetail);

        // Seed data for WardEntity references
        var ward = EntityTestHelpers.CreateWardEntity(id: 1);
        _context.WardMaster.Add(ward);

        var block = EntityTestHelpers.CreateBlockMasterEntity(id: 1);
        block.WardId = 1;
        _context.BlockMasters.Add(block);

        // Seed data for ZoneEntity references
        var zone = EntityTestHelpers.CreateZoneEntity(id: 1);
        _context.ZoneMaster.Add(zone);

        var ward2 = EntityTestHelpers.CreateWardEntity(id: 2);
        ward2.ZoneId = 1;
        _context.WardMaster.Add(ward2);

        // Seed data for TypeOfUseGroupEntity references
        var typeOfUseGroup = EntityTestHelpers.CreateTypeOfUseGroupEntity(id: 1);
        _context.TypeOfUseGroup.Add(typeOfUseGroup);

        var typeOfUse = EntityTestHelpers.CreateTypeOfUseEntity(id: 1);
        typeOfUse.TypeOfUseGroupId = 1;
        _context.TypeOfUse.Add(typeOfUse);

        // Seed data for TypeOfUseEntity references
        var parkingType = EntityTestHelpers.CreateParkingTypeMasterEntity(id: 1);
        parkingType.TypeOfUseId = 1;
        _context.ParkingTypeMaster.Add(parkingType);

        // Seed data for SubTypeOfUseEntity references
        var subTypeOfUse = EntityTestHelpers.CreateSubTypeOfUseEntity(id: 1);
        _context.SubTypeOfUse.Add(subTypeOfUse);

        var propertyDetails2 = EntityTestHelpers.CreatePropertyDetailsEntity(id: 2);
        propertyDetails2.SubTypeOfUseId = 1;
        _context.PropertyDetails.Add(propertyDetails2);

        // Seed data for SocietyDetailsEntity references
        var society = EntityTestHelpers.CreateSocietyDetailsEntity(id: 1);
        _context.SocietyDetailsMast.Add(society);

        var property2 = EntityTestHelpers.CreatePropertyEntity(id: 2);
        property2.SocietyDetailId = 1;
        _context.PropertyMast.Add(property2);

        _context.SaveChanges();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidContext_CreatesInstance()
    {
        // Arrange & Act
        var service = new ReferenceValidationService(_context);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region ValidateReferencesAsync Tests - AssessmentYearRangeCVEntity

    [Fact]
    public async Task ValidateReferencesAsync_AssessmentYearRangeCVWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<AssessmentYearRangeCVEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Age Factor CV Master", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_AssessmentYearRangeCVWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<AssessmentYearRangeCVEntity>(999);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region ValidateReferencesAsync Tests - SubFloorEntity

    [Fact]
    public async Task ValidateReferencesAsync_SubFloorWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<SubFloorEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Property Details", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_SubFloorWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<SubFloorEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - ConstructionTypeEntity

    [Fact]
    public async Task ValidateReferencesAsync_ConstructionTypeWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<ConstructionTypeEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Rates", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_ConstructionTypeWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<ConstructionTypeEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - TaxZoneEntity

    [Fact]
    public async Task ValidateReferencesAsync_TaxZoneWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<TaxZoneEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Properties", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_TaxZoneWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<TaxZoneEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - FloorEntity

    [Fact]
    public async Task ValidateReferencesAsync_FloorWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<FloorEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Floor Factors", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_FloorWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<FloorEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - AssessmentYearRangeEntity

    [Fact]
    public async Task ValidateReferencesAsync_AssessmentYearRangeWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<AssessmentYearRangeEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Depreciation Master", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_AssessmentYearRangeWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<AssessmentYearRangeEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - RateSectionEntity

    [Fact]
    public async Task ValidateReferencesAsync_RateSectionWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<RateSectionEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Rate Section Details", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_RateSectionWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<RateSectionEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - WardEntity

    [Fact]
    public async Task ValidateReferencesAsync_WardWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<WardEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Block Master", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_WardWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<WardEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - ZoneEntity

    [Fact]
    public async Task ValidateReferencesAsync_ZoneWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<ZoneEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Ward Master", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_ZoneWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<ZoneEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - TypeOfUseGroupEntity

    [Fact]
    public async Task ValidateReferencesAsync_TypeOfUseGroupWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<TypeOfUseGroupEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Type Of Use Master", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_TypeOfUseGroupWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<TypeOfUseGroupEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - TypeOfUseEntity

    [Fact]
    public async Task ValidateReferencesAsync_TypeOfUseWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<TypeOfUseEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Parking Type Master", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_TypeOfUseWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<TypeOfUseEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - SubTypeOfUseEntity

    [Fact]
    public async Task ValidateReferencesAsync_SubTypeOfUseWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<SubTypeOfUseEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Property Details", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_SubTypeOfUseWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<SubTypeOfUseEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - SocietyDetailsEntity

    [Fact]
    public async Task ValidateReferencesAsync_SocietyDetailsWithReferences_ReturnsFailure()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<SocietyDetailsEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Property Master", errorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_SocietyDetailsWithoutReferences_ReturnsSuccess()
    {
        // Act
        var result = await _service.ValidateReferencesAsync<SocietyDetailsEntity>(999);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateReferencesAsync Tests - Unconfigured Entity

    [Fact]
    public async Task ValidateReferencesAsync_UnconfiguredEntity_ReturnsSuccess()
    {
        // Act - Using PropertyEntity which is not configured for validation
        var result = await _service.ValidateReferencesAsync<PropertyEntity>(1);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Multiple References Tests

    [Fact]
    public async Task ValidateReferencesAsync_MultipleReferences_ReturnsAllReferencingTables()
    {
        // Arrange - Add multiple references for TypeOfUseEntity
        var propertyDescValidation = EntityTestHelpers.CreatePropertyDescriptionAndTypeOfUseValidationEntity(id: 1);
        propertyDescValidation.TypeOfUseId = 1;
        _context.PropertyDescriptionAndTypeOfUseValidations.Add(propertyDescValidation);

        var taxPercentageCV = EntityTestHelpers.CreateTaxPercentageMasterCVEntity(id: 1);
        taxPercentageCV.TypeOfUseId = 1;
        _context.TaxPercentageMasterCVs.Add(taxPercentageCV);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateReferencesAsync<TypeOfUseEntity>(1);

        // Assert
        Assert.False(result.IsValid);
        var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains("Parking Type Master", errorMessage);
        Assert.Contains("Property Description And TypeOfUseValidation", errorMessage);
        Assert.Contains("Tax PercentageMaster CV", errorMessage);
    }

    #endregion

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}
