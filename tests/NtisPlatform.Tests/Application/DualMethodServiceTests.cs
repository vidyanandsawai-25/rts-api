using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace NtisPlatform.Tests.Application;

public class DualMethodServiceTests
{
    private readonly Mock<IRepository<PolicyTaxDetailsCVEntity, int>> _transMastRepo = new();
    private readonly Mock<IRepository<PolicyTaxDetailsEntity, int>> _rvRepo = new();
    private readonly Mock<IRepository<TransMastOldEntity, int>> _oldTaxRepo = new();
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo = new();
    private readonly IMapper _mapper;
    private readonly ILogger<DualMethodService> _logger;

    public DualMethodServiceTests()
    {
        // Configure AutoMapper with the DualMethodMappingProfile
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DualMethodMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        // Use NullLogger for tests
        _logger = NullLogger<DualMethodService>.Instance;

        var properties = new List<PropertyEntity>
        {
            new() { Id = 1, PropertyMastOldId = 1, IsActive = true },
            new() { Id = 999, PropertyMastOldId = 999, IsActive = true }
        };
        _propertyRepo.Setup(x => x.GetQueryable()).Returns(properties.BuildMock());
    }

    private DualMethodService GetService()
    {
        return new DualMethodService(
            _transMastRepo.Object,
            _rvRepo.Object,
            _oldTaxRepo.Object,
            _propertyRepo.Object,
            _mapper,
            _logger
        );
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_WithValidData_ReturnsCorrectTaxTotals()
    {
        // Arrange
        const int propertyId = 1;

        var taxMaster1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", IsActive = true };
        var taxMaster2 = new TaxMasterEntity { Id = 2, TaxName = "Water Tax", IsActive = true };

        // CV Data - uses PolicyTaxDetailsCVEntity
        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 1000.50m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 },
            new() { Id = 2, PropertyId = propertyId, TaxId = 2, TaxAmount = 500.25m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster2 }
        };

        // RV Data - uses PolicyTaxDetailsEntity
        var rvData = new List<PolicyTaxDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 800.75m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 },
            new() { Id = 2, PropertyId = propertyId, TaxId = 2, TaxAmount = 300.50m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster2 }
        };

        var oldData = new List<TransMastOldEntity>
        {
            new() { Id = 1, PropertyMastOldId = propertyId, TaxId = 1, TaxAmount = 600.25m, IsActive = true, TaxMaster = taxMaster1 },
            new() { Id = 2, PropertyMastOldId = propertyId, TaxId = 2, TaxAmount = 200.00m, IsActive = true, TaxMaster = taxMaster2 }
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(rvData.BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(oldData.BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CVTaxes);
        Assert.NotNull(result.RVTaxes);
        Assert.NotNull(result.OldTaxes);

        // CV: 1000.50 rounds to 1001, 500.25 rounds to 500 → TaxTotal = 1501
        Assert.Equal(1001m, result.CVTaxes["Property Tax"]);
        Assert.Equal(500m, result.CVTaxes["Water Tax"]);
        Assert.Equal(1501m, result.CVTaxes["TaxTotal"]);

        // RV: 800.75 rounds to 801, 300.50 rounds to 301 (AwayFromZero) → TaxTotal = 1102
        Assert.Equal(801m, result.RVTaxes["Property Tax"]);
        Assert.Equal(301m, result.RVTaxes["Water Tax"]);
        Assert.Equal(1102m, result.RVTaxes["TaxTotal"]);

        // Old: 600.25 rounds to 600, 200.00 rounds to 200 → TaxTotal = 800
        Assert.Equal(600m, result.OldTaxes["Property Tax"]);
        Assert.Equal(200m, result.OldTaxes["Water Tax"]);
        Assert.Equal(800m, result.OldTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_RoundsAwayFromZero_Correctly()
    {
        // Arrange
        const int propertyId = 1;
        var taxMaster = new TaxMasterEntity { Id = 1, TaxName = "Test Tax", IsActive = true };

        // Test edge cases for MidpointRounding.AwayFromZero
        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 10.5m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster },
            new() { Id = 2, PropertyId = propertyId, TaxId = 1, TaxAmount = 11.5m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster },
            new() { Id = 3, PropertyId = propertyId, TaxId = 1, TaxAmount = -10.5m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster }
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        // 10.5 + 11.5 + (-10.5) = 11.5, which rounds to 12 (AwayFromZero)
        Assert.Equal(12m, result.CVTaxes["Test Tax"]);
        Assert.Equal(12m, result.CVTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_WithNullOrBlankTaxNames_UsesDefaultNaming()
    {
        // Arrange
        const int propertyId = 1;
        
        var taxMaster1 = new TaxMasterEntity { Id = 1, TaxName = null, IsActive = true };
        var taxMaster2 = new TaxMasterEntity { Id = 2, TaxName = "", IsActive = true };
        var taxMaster3 = new TaxMasterEntity { Id = 3, TaxName = "   ", IsActive = true };

        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 },
            new() { Id = 2, PropertyId = propertyId, TaxId = 2, TaxAmount = 200m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster2 },
            new() { Id = 3, PropertyId = propertyId, TaxId = 3, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster3 }
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        Assert.True(result.CVTaxes.ContainsKey("Tax_1"));
        Assert.True(result.CVTaxes.ContainsKey("Tax_2"));
        Assert.True(result.CVTaxes.ContainsKey("Tax_3"));
        Assert.Equal(100m, result.CVTaxes["Tax_1"]);
        Assert.Equal(200m, result.CVTaxes["Tax_2"]);
        Assert.Equal(300m, result.CVTaxes["Tax_3"]);
        Assert.Equal(600m, result.CVTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_WithDuplicateTaxNames_AddsIdSuffix()
    {
        // Arrange
        const int propertyId = 1;
        
        var taxMaster1 = new TaxMasterEntity { Id = 1, TaxName = "General Tax", IsActive = true };
        var taxMaster2 = new TaxMasterEntity { Id = 2, TaxName = "General Tax", IsActive = true };
        var taxMaster3 = new TaxMasterEntity { Id = 3, TaxName = "GENERAL TAX", IsActive = true }; // Case-insensitive duplicate

        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 },
            new() { Id = 2, PropertyId = propertyId, TaxId = 2, TaxAmount = 200m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster2 },
            new() { Id = 3, PropertyId = propertyId, TaxId = 3, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster3 }
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        // First occurrence gets the base key "General Tax"
        Assert.True(result.CVTaxes.ContainsKey("General Tax"));
        Assert.Equal(100m, result.CVTaxes["General Tax"]);
        
        // Subsequent duplicates get ID suffix
        Assert.True(result.CVTaxes.ContainsKey("General Tax_2"));
        Assert.Equal(200m, result.CVTaxes["General Tax_2"]);
        
        Assert.True(result.CVTaxes.ContainsKey("GENERAL TAX_3"));
        Assert.Equal(300m, result.CVTaxes["GENERAL TAX_3"]);
        
        Assert.Equal(600m, result.CVTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_WithReservedTaxTotalName_FiltersOutAsAggregate()
    {
        // Arrange
        const int propertyId = 1;
        
        var taxMaster1 = new TaxMasterEntity { Id = 1, TaxName = "TaxTotal", IsActive = true };
        var taxMaster2 = new TaxMasterEntity { Id = 2, TaxName = "Regular Tax", IsActive = true };

        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 },
            new() { Id = 2, PropertyId = propertyId, TaxId = 2, TaxAmount = 200m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster2 }
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        // Tax named "TaxTotal" should be filtered out as the database aggregate row
        Assert.False(result.CVTaxes.ContainsKey("Tax_1_Total"));
        Assert.False(result.CVTaxes.ContainsKey("TaxTotal_1"));
        
        Assert.True(result.CVTaxes.ContainsKey("Regular Tax"));
        Assert.Equal(200m, result.CVTaxes["Regular Tax"]);
        
        // The computed TaxTotal should be the sum of all individual taxes (excluding the aggregate row)
        Assert.True(result.CVTaxes.ContainsKey("TaxTotal"));
        Assert.Equal(200m, result.CVTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_WithGroupedTaxes_SumsCorrectly()
    {
        // Arrange
        const int propertyId = 1;
        
        var taxMaster1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", IsActive = true };

        // Multiple records for the same tax should be summed
        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 },
            new() { Id = 2, PropertyId = propertyId, TaxId = 1, TaxAmount = 200m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 },
            new() { Id = 3, PropertyId = propertyId, TaxId = 1, TaxAmount = 300.75m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 }
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        // 100 + 200 + 300.75 = 600.75, rounds to 601
        Assert.Equal(601m, result.CVTaxes["Property Tax"]);
        Assert.Equal(601m, result.CVTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_FiltersInactiveTaxes_Correctly()
    {
        // Arrange
        const int propertyId = 1;
        
        var activeTaxMaster = new TaxMasterEntity { Id = 1, TaxName = "Active Tax", IsActive = true };
        var inactiveTaxMaster = new TaxMasterEntity { Id = 2, TaxName = "Inactive Tax", IsActive = false };

        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, TaxMaster = activeTaxMaster },
            new() { Id = 2, PropertyId = propertyId, TaxId = 2, TaxAmount = 200m, IsActive = true, MarkedForDeletion = false, TaxMaster = inactiveTaxMaster }, // Active transaction with inactive TaxMaster
            new() { Id = 3, PropertyId = propertyId, TaxId = 1, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false, TaxMaster = activeTaxMaster }
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        // Two active records with TaxId=1: 100 + 300 = 400
        Assert.Equal(2, result.CVTaxes.Count);
        Assert.True(result.CVTaxes.ContainsKey("Active Tax"));
        Assert.Equal(400m, result.CVTaxes["Active Tax"]);
        Assert.Equal(400m, result.CVTaxes["TaxTotal"]);
        Assert.False(result.CVTaxes.ContainsKey("Inactive Tax"));
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_WithZeroTaxId_FiltersOut()
    {
        // Arrange
        const int propertyId = 1;
        
        var taxMaster = new TaxMasterEntity { Id = 1, TaxName = "Valid Tax", IsActive = true };

        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster },
            new() { Id = 2, PropertyId = propertyId, TaxId = 0, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false, TaxMaster = null! } // Invalid TaxId
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.CVTaxes.Count);
        Assert.True(result.CVTaxes.ContainsKey("Valid Tax"));
        Assert.Equal(100m, result.CVTaxes["Valid Tax"]);
        Assert.Equal(100m, result.CVTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_WithNoData_ReturnsEmptyDictionaries()
    {
        // Arrange
        const int propertyId = 1;

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsCVEntity>().BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CVTaxes);
        Assert.NotNull(result.RVTaxes);
        Assert.NotNull(result.OldTaxes);
        
        // Only TaxTotal key should exist with value 0
        Assert.Single(result.CVTaxes);
        Assert.Single(result.RVTaxes);
        Assert.Single(result.OldTaxes);
        
        Assert.Equal(0m, result.CVTaxes["TaxTotal"]);
        Assert.Equal(0m, result.RVTaxes["TaxTotal"]);
        Assert.Equal(0m, result.OldTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_WithMissingTaxInOneSource_ShowsCorrectAmounts()
    {
        // Arrange
        const int propertyId = 1;
        
        var taxMaster1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", IsActive = true };
        var taxMaster2 = new TaxMasterEntity { Id = 2, TaxName = "Water Tax", IsActive = true };

        // CV has both taxes
        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 },
            new() { Id = 2, PropertyId = propertyId, TaxId = 2, TaxAmount = 200m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster2 }
        };

        // RV only has Property Tax
        var rvData = new List<PolicyTaxDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 150m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster1 }
        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(rvData.BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        // CV should have both taxes
        Assert.Equal(100m, result.CVTaxes["Property Tax"]);
        Assert.Equal(200m, result.CVTaxes["Water Tax"]);
        Assert.Equal(300m, result.CVTaxes["TaxTotal"]);

        // RV should only have Property Tax (each source is independent now)
        Assert.Equal(2, result.RVTaxes.Count); // Property Tax + TaxTotal
        Assert.Equal(150m, result.RVTaxes["Property Tax"]);
        Assert.False(result.RVTaxes.ContainsKey("Water Tax")); // Water Tax is not in RV data
        Assert.Equal(150m, result.RVTaxes["TaxTotal"]);
    }

    [Fact]
    public async Task GetRVCVTaxesAsync_FiltersCorrectPropertyId()
    {
        // Arrange
        const int propertyId = 1;
        
        var taxMaster = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", IsActive = true };

        var cvData = new List<PolicyTaxDetailsCVEntity>
        {
            new() { Id = 1, PropertyId = propertyId, TaxId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster },
            new() { Id = 2, PropertyId = 999, TaxId = 1, TaxAmount = 500m, IsActive = true, MarkedForDeletion = false, TaxMaster = taxMaster } // Different property

        };

        _transMastRepo.Setup(x => x.GetQueryable()).Returns(cvData.BuildMock());
        _rvRepo.Setup(x => x.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _oldTaxRepo.Setup(x => x.GetQueryable()).Returns(new List<TransMastOldEntity>().BuildMock());

        var service = GetService();

        // Act
        var result = await service.GetRVCVTaxesAsync(propertyId, CancellationToken.None);

        // Assert
        // Should only include data for propertyId 1, not 999
        Assert.Equal(100m, result.CVTaxes["Property Tax"]);
        Assert.Equal(100m, result.CVTaxes["TaxTotal"]);
    }
}






