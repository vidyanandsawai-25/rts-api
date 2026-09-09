using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class GetApartmentDetailsWingWiseServiceTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo = new();
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _categoryRepo = new();
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _propertyTypeRepo = new();
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _assessmentRepo = new();
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _societyRepo = new();
    private readonly Mock<IRepository<WingDetailsMastEntity, int>> _wingDetailsMastRepo = new();
    private readonly Mock<IRepository<WardEntity, int>> _wardRepo = new();
    private readonly Mock<IRepository<ZoneEntity, int>> _zoneRepo = new();
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _propertyDetailsRepo = new();
    private readonly Mock<IRepository<FloorEntity, int>> _floorRepo = new();
    private readonly Mock<IRepository<ConstructionTypeEntity, int>> _constructionTypeRepo = new();
    private readonly Mock<IRepository<TypeOfUseEntity, int>> _typeOfUseRepo = new();
    private readonly Mock<IRepository<SubTypeOfUseEntity, int>> _subTypeOfUseRepo = new();
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _oldPropertyRepo = new();
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _propertyMapDetailRepo = new();
    private readonly Mock<IRepository<TransMastEntity, int>> _transMastRepo = new();
    private readonly Mock<IRepository<TaxMasterEntity, int>> _taxMasterRepo = new();
    private readonly Mock<IRepository<YearMasterEntity, int>> _yearMasterRepo = new();
    private readonly Mock<IRepository<PolicyCodeMasterEntity, int>> _policyCodeMasterRepo = new();
    private readonly Mock<IRepository<PolicyTaxDetailsEntity, int>> _policyTaxDetailsRepo = new();
    private readonly Mock<IRepository<PolicyTaxDetailsCVEntity, int>> _policyTaxDetailsCVRepo = new();
    private readonly Mock<IRepository<PolicyConfigurationEntity, int>> _policyConfigRepo = new();
    private readonly Mock<IRepository<PropertyPhotoEntity, int>> _photoRepo = new();
    private readonly Mock<IRepository<PropertyPhotoOldEntity, int>> _photoOldRepo = new();
    private readonly Mock<IRepository<PropertyPhotoTypeEntity, int>> _photoTypeRepo = new();
    private readonly Mock<IRepository<DocumentBindingEntity, int>> _documentBindingRepo = new();
    private readonly Mock<IRepository<DocumentEntity, int>> _documentRepo = new();

    private readonly Mock<IRepository<TransMastOldEntity, int>> _transMastOldRepo = new();

    public GetApartmentDetailsWingWiseServiceTests()
    {
        _categoryRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCategoryEntity>
        {
            new() { Id = 1, PropertyCategoryName = "Apartment", IsActive = true }
        }.BuildMock());
        _propertyTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _assessmentRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _societyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());
        _wingDetailsMastRepo.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());
        _wardRepo.Setup(r => r.GetQueryable()).Returns(new List<WardEntity>().BuildMock());
        _zoneRepo.Setup(r => r.GetQueryable()).Returns(new List<ZoneEntity>().BuildMock());
        _floorRepo.Setup(r => r.GetQueryable()).Returns(new List<FloorEntity>().BuildMock());
        _constructionTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<ConstructionTypeEntity>().BuildMock());
        _typeOfUseRepo.Setup(r => r.GetQueryable()).Returns(new List<TypeOfUseEntity>().BuildMock());
        _subTypeOfUseRepo.Setup(r => r.GetQueryable()).Returns(new List<SubTypeOfUseEntity>().BuildMock());
        _transMastRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        _transMastOldRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());
        _taxMasterRepo.Setup(r => r.GetQueryable()).Returns(new List<TaxMasterEntity>().BuildMock());
        _yearMasterRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity>().BuildMock());
        _policyCodeMasterRepo.Setup(r => r.GetQueryable()).Returns(new List<PolicyCodeMasterEntity>().BuildMock());
        _policyTaxDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _policyTaxDetailsCVRepo.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsCVEntity>().BuildMock());
        _policyConfigRepo.Setup(r => r.GetQueryable()).Returns(new List<PolicyConfigurationEntity>().BuildMock());
        _photoRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyPhotoEntity>().BuildMock());
        _photoOldRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyPhotoOldEntity>().BuildMock());
        _photoTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyPhotoTypeEntity>().BuildMock());
        _documentBindingRepo.Setup(r => r.GetQueryable()).Returns(new List<DocumentBindingEntity>().BuildMock());
        _documentRepo.Setup(r => r.GetQueryable()).Returns(new List<DocumentEntity>().BuildMock());
    }

    private GetApartmentDetailsWingWiseService CreateService()
    {
        var mapper = AutoMapperTestHelper.CreateMapper();

        return new GetApartmentDetailsWingWiseService(
            _propertyRepo.Object,
            _categoryRepo.Object,
            _propertyTypeRepo.Object,
            _assessmentRepo.Object,
            _societyRepo.Object,
            _wingDetailsMastRepo.Object,
            _wardRepo.Object,
            _zoneRepo.Object,
            _propertyDetailsRepo.Object,
            _floorRepo.Object,
            _constructionTypeRepo.Object,
            _typeOfUseRepo.Object,
            _subTypeOfUseRepo.Object,
            _oldPropertyRepo.Object,
            _propertyMapDetailRepo.Object,
            _transMastRepo.Object,
            _taxMasterRepo.Object,
            _yearMasterRepo.Object,
            _policyCodeMasterRepo.Object,
            _policyTaxDetailsRepo.Object,
            _policyTaxDetailsCVRepo.Object,
            _policyConfigRepo.Object,
            _photoRepo.Object,
            _photoOldRepo.Object,
            _photoTypeRepo.Object,
            _documentBindingRepo.Object,
            _documentRepo.Object,
            mapper,
            _transMastOldRepo.Object);
    }

    [Fact]
    public async Task GetApartmentDetailsWingWise_ManyNewToOneOld_ReturnsIndividualNewPropertiesWithOwnFlatNo()
    {
        // Arrange
        var newProps = new List<PropertyEntity>
        {
            new() { Id = 101, PropertyNo = "NEW-101", FlatOrShopNo = "101", WingDetailId = 1, CategoryId = 1, IsActive = true },
            new() { Id = 102, PropertyNo = "NEW-102", FlatOrShopNo = "102", WingDetailId = 1, CategoryId = 1, IsActive = true }
        }.BuildMock();

        var newDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = 101, CarpetAreaSqFeet = 400.0, IsActive = true },
            new() { Id = 2, PropertyId = 102, CarpetAreaSqFeet = 600.0, IsActive = true }
        }.BuildMock();

        // 1 Old property (OldRV=10000, OldTotalTax=20000, ConstructionArea=1000) mapped to both 101 and 102
        var oldProps = new List<PropertyMastOldEntity>
        {
            new() { Id = 501, OldPropertyNo = "OLD-501", OldRV = 10000, OldTotalTax = 20000, OldConstructionArea = 1000, IsActive = true }
        }.BuildMock();

        var oldTaxes = new List<TransMastOldEntity>
        {
            new() { Id = 1, PropertyMastOldId = 501, FinanceYearId = 10, TaxId = 1, TaxAmount = 5000m, IsActive = true }
        }.BuildMock();

        var mappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdNew = 101, PropertyIdOld = 501, IsActive = true },
            new() { Id = 2, PropertyIdNew = 102, PropertyIdOld = 501, IsActive = true }
        }.BuildMock();

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(newProps);
        _propertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(newDetails);
        _oldPropertyRepo.Setup(r => r.GetQueryable()).Returns(oldProps);
        _transMastOldRepo.Setup(r => r.GetQueryable()).Returns(oldTaxes);
        _propertyMapDetailRepo.Setup(r => r.GetQueryable()).Returns(mappings);

        var service = CreateService();
        var query = new GetApartmentDetailsWingWiseQueryParameters { WingDetailId = 1 };

        // Act
        var result = await service.GetApartmentDetailsWingWiseAsync(query, CancellationToken.None);

        // Assert: 1 main row with aggregated calculation data and no comma separated flat no
        Assert.NotNull(result);
        var row = Assert.Single(result.Items);

        Assert.Equal(1000m, row.NewSurvey.CarpetASqFt); // 400 + 600
        Assert.Null(row.NewSurvey.FlatOrShopNo); // not comma-separated
        Assert.Equal(501L, row.OldSurvey.Id);
        Assert.Single(row.OldSurvey.OldTaxDetails);
        Assert.Equal(5000m, row.OldSurvey.OldTaxDetails[0].TaxAmount);
    }

    [Fact]
    public async Task GetApartmentDetailsWingWise_MergeMapping_AggregatesAllOldProperties()
    {
        // Arrange: 1 New property mapped to 2 Old properties
        var newProps = new List<PropertyEntity>
        {
            new() { Id = 101, PropertyNo = "NEW-101", WingDetailId = 1, CategoryId = 1, IsActive = true }
        }.BuildMock();

        var newDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = 101, CarpetAreaSqFeet = 1000.0, IsActive = true }
        }.BuildMock();

        var oldProps = new List<PropertyMastOldEntity>
        {
            new() { Id = 501, OldPropertyNo = "OLD-501", OldRV = 4000, OldTotalTax = 8000, OldConstructionArea = 400, IsActive = true },
            new() { Id = 502, OldPropertyNo = "OLD-502", OldRV = 6000, OldTotalTax = 12000, OldConstructionArea = 600, IsActive = true }
        }.BuildMock();

        var oldTaxes = new List<TransMastOldEntity>
        {
            new() { Id = 1, PropertyMastOldId = 501, FinanceYearId = 10, TaxId = 1, TaxAmount = 2000m, IsActive = true },
            new() { Id = 2, PropertyMastOldId = 502, FinanceYearId = 10, TaxId = 1, TaxAmount = 3000m, IsActive = true }
        }.BuildMock();

        var mappings = new List<PropertyMapDetailEntity>
        {
            new() { Id = 1, PropertyIdNew = 101, PropertyIdOld = 501, IsActive = true },
            new() { Id = 2, PropertyIdNew = 101, PropertyIdOld = 502, IsActive = true }
        }.BuildMock();

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(newProps);
        _propertyDetailsRepo.Setup(r => r.GetQueryable()).Returns(newDetails);
        _oldPropertyRepo.Setup(r => r.GetQueryable()).Returns(oldProps);
        _transMastOldRepo.Setup(r => r.GetQueryable()).Returns(oldTaxes);
        _propertyMapDetailRepo.Setup(r => r.GetQueryable()).Returns(mappings);

        var service = CreateService();
        var query = new GetApartmentDetailsWingWiseQueryParameters { WingDetailId = 1 };

        // Act
        var result = await service.GetApartmentDetailsWingWiseAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var row = Assert.Single(result.Items);
        // Total = 4000 + 6000 = 10000 RV, 8000 + 12000 = 20000 Tax, 400 + 600 = 1000 ConstArea
        Assert.Equal(10000m, row.OldSurvey.RateableValue);
        Assert.Equal(20000m, row.OldSurvey.TotalTax);
        Assert.Equal(1000m, row.OldSurvey.ConstructionArea);
        Assert.Contains("OLD-501", row.OldSurvey.OldPropertyNo);
        Assert.Contains("OLD-502", row.OldSurvey.OldPropertyNo);
        Assert.Equal(2, row.OldSurvey.OldTaxDetails.Count);
    }
}
