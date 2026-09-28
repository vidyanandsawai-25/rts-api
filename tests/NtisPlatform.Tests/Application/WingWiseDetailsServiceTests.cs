using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class WingWiseDetailsServiceTests
{
    [Fact]
    public async Task GetWingWiseDetailsAsync_AggregatesAreaAndDemandByWingAndPropertyType()
    {
        var propertyRepository = Repository([
            new PropertyEntity { Id = 1, WardId = 7, PropertyNo = "100", PropertyTypeId = 1, WingDetailId = 10, IsActive = true },
            new PropertyEntity { Id = 2, WardId = 7, PropertyNo = "100", PropertyTypeId = 1, WingDetailId = 10, IsActive = true },
            new PropertyEntity { Id = 3, WardId = 7, PropertyNo = "100", PropertyTypeId = 2, WingDetailId = 10, IsActive = true },
            new PropertyEntity { Id = 4, WardId = 7, PropertyNo = "100", PropertyTypeId = 1, WingDetailId = 20, IsActive = true }
        ]);
        var propertyTypeRepository = Repository([
            new PropertyTypeMasterEntity { Id = 1, PropertyDescription = "Residential", PartType = "R", IsActive = true },
            new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Commercial", PartType = "C", IsActive = true },
            new PropertyTypeMasterEntity { Id = 3, PropertyDescription = "Amenity", PartType = "Amenity", IsActive = true }
        ]);
        var wardRepository = Repository([
            new WardEntity { Id = 7, WardNo = "W007", IsActive = true }
        ]);
        var propertyDetailsRepository = Repository([
            new PropertyDetailsEntity { Id = 1, PropertyId = 1, FloorId = 1, CarpetAreaSqFeet = 500, IsActive = true },
            new PropertyDetailsEntity { Id = 2, PropertyId = 2, FloorId = 2, CarpetAreaSqFeet = 600, IsActive = true },
            new PropertyDetailsEntity { Id = 3, PropertyId = 3, FloorId = 4, CarpetAreaSqFeet = 400, IsActive = true },
            new PropertyDetailsEntity { Id = 4, PropertyId = 4, FloorId = 3, CarpetAreaSqFeet = 1000, IsActive = true }
        ]);
        var floorRepository = Repository([
            new FloorEntity { Id = 1, FloorCode = "G", IsActive = true },
            new FloorEntity { Id = 2, FloorCode = "1", IsActive = true },
            new FloorEntity { Id = 3, FloorCode = "2", IsActive = true },
            new FloorEntity { Id = 4, FloorCode = "3", IsActive = true }
        ]);
        var wingDetailsRepository = Repository([
            new WingDetailsMastEntity { Id = 10, WingMasterId = 100, SocietyDetailsMastId = 1, WingName = "Krishna Block", IsActive = true },
            new WingDetailsMastEntity { Id = 20, WingMasterId = 200, SocietyDetailsMastId = 1, WingName = "Sai Block", IsActive = true }
        ]);
        var wingRepository = Repository([
            new WingEntity { Id = 100, WingNo = "A", IsActive = true },
            new WingEntity { Id = 200, WingNo = "B", IsActive = true }
        ]);
        var taxRepository = Repository([
            new TaxMasterEntity { Id = 99, TaxCode = "TaxTotal", TaxName = "Tax Total", IsActive = true }
        ]);
        var yearMasterRepository = Repository([
            new YearMasterEntity { Id = 1, Year = 2023 },
            new YearMasterEntity { Id = 2, Year = 2024 }
        ]);
        var retroPolicy = new PolicyCodeMasterEntity { Id = 5, PolicyCode = "RETRO", PolicyName = "Retro", IsRetroDemand = true };
        var transRepository = Repository([
            // Current demand (current finance year = 2024)
            new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 99, FinanceYearId = 2024, TaxAmount = 500m, IsActive = true },
            new TransMastEntity { Id = 2, PropertyId = 2, TaxId = 99, FinanceYearId = 2024, TaxAmount = 500m, IsActive = true },
            new TransMastEntity { Id = 3, PropertyId = 3, TaxId = 99, FinanceYearId = 2024, TaxAmount = 250000m, IsActive = true },
            new TransMastEntity { Id = 4, PropertyId = 4, TaxId = 99, FinanceYearId = 2024, TaxAmount = 10000000m, IsActive = true },
            // Retro demand (prior finance year with IsRetroDemand policy)
            new TransMastEntity { Id = 5, PropertyId = 1, TaxId = 99, FinanceYearId = 2023, PolicyCodeId = 5, PolicyCodeMaster = retroPolicy, TaxAmount = 250m, IsActive = true },
            new TransMastEntity { Id = 6, PropertyId = 2, TaxId = 99, FinanceYearId = 2023, PolicyCodeId = 5, PolicyCodeMaster = retroPolicy, TaxAmount = 250m, IsActive = true }
        ]);
        var propertyMastOldRepository = Repository([
            new PropertyMastOldEntity { Id = 501, OldTotalTax = 100d },
            new PropertyMastOldEntity { Id = 502, OldTotalTax = 200d }
        ]);
        var propertyMapDetailRepository = Repository([
            new PropertyMapDetailEntity { Id = 1, PropertyIdNew = 1, PropertyIdOld = 501 },
            new PropertyMapDetailEntity { Id = 2, PropertyIdNew = 2, PropertyIdOld = 502 }
        ]);

        var policyTaxDetailsRepository = Repository([
            new PolicyTaxDetailsEntity { Id = 1, PropertyId = 1, CalculationValue = 150m, IsCurrent = true },
            new PolicyTaxDetailsEntity { Id = 2, PropertyId = 2, CalculationValue = 200m, IsCurrent = true }
        ]);

        var service = new WingWiseDetailsService(
            propertyRepository.Object,
            propertyTypeRepository.Object,
            wardRepository.Object,
            propertyDetailsRepository.Object,
            floorRepository.Object,
            wingDetailsRepository.Object,
            wingRepository.Object,
            taxRepository.Object,
            transRepository.Object,
            yearMasterRepository.Object,
            propertyMastOldRepository.Object,
            propertyMapDetailRepository.Object,
            policyTaxDetailsRepository.Object);

        var result = await service.GetWingWiseDetailsAsync(new WingWiseDetailsQueryParameters
        {
            WardId = 7,
            PropertyNo = "100"
        });

        Assert.Equal(1, result.PropertyId);
        Assert.Equal("100", result.PropertyNo);
        Assert.Equal("W007", result.WardNo);
        Assert.Equal(2, result.Wings.Count);

        var wingA = Assert.Single(result.Wings.Where(x => x.WingNo == "A"));
        Assert.Equal(3, wingA.PropertyCount);
        Assert.Equal("G-3", wingA.FloorRange);
        Assert.Equal(1500m, wingA.TotalArea);
        Assert.Equal("2.51 Lakh", wingA.CurrentDemand);
        Assert.Equal("300", wingA.OldDemand);
        Assert.Equal("500", wingA.RetroDemand);
        Assert.Equal("2.52 Lakh", wingA.TotalDemand);
        Assert.Equal("2.51 Lakh", wingA.RevenueImpact);

        var residential = Assert.Single(wingA.PropertyTypes.Where(x => x.PropertyTypeName == "Residential"));
        Assert.Equal(2, residential.PropertyCount);
        Assert.Equal(1100m, residential.TotalArea);
        Assert.Equal("1K", residential.CurrentDemand);
        Assert.Equal("300", residential.OldDemand);
        Assert.Equal("500", residential.RetroDemand);
        Assert.Equal("1.5K", residential.TotalDemand);
        Assert.Equal("1.2K", residential.TotalRevenue);

        var commercial = Assert.Single(wingA.PropertyTypes.Where(x => x.PropertyTypeName == "Commercial"));
        Assert.Equal(1, commercial.PropertyCount);
        Assert.Equal(400m, commercial.TotalArea);
        Assert.Equal("2.5 Lakh", commercial.CurrentDemand);
        Assert.Equal("2.5 Lakh", commercial.TotalDemand);
        Assert.Equal("2.5 Lakh", commercial.TotalRevenue);

        var amenity = Assert.Single(wingA.PropertyTypes.Where(x => x.PropertyTypeName == "Amenity"));
        Assert.Equal(0, amenity.PropertyCount);
        Assert.Equal(0m, amenity.TotalArea);
        Assert.Equal("0", amenity.TotalDemand);

        var wingB = Assert.Single(result.Wings.Where(x => x.WingNo == "B"));
        Assert.Equal(1, wingB.PropertyCount);
        Assert.Equal("2-2", wingB.FloorRange);
        Assert.Equal(1000m, wingB.TotalArea);
        Assert.Equal("1 Crore", wingB.CurrentDemand);
        Assert.Equal("1 Crore", wingB.TotalDemand);
    }

    private static Mock<IRepository<TEntity, int>> Repository<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        var entityList = entities.ToList();
        var repository = new Mock<IRepository<TEntity, int>>();
        repository.Setup(x => x.GetQueryable()).Returns(entityList.BuildMock());
        return repository;
    }
}
