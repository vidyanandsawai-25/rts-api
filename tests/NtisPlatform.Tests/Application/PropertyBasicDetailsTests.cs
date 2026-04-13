using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for Property Basic Details API
/// Coverage: Repository, Service, DTOs, Entities
/// </summary>
public class PropertyBasicDetailsTests
{
    #region UpdatePropertyBasicDetailsDto Tests

    public class UpdatePropertyBasicDetailsDtoTests
    {
        [Fact]
        public void UpdatePropertyBasicDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                CategoryId = 1,
                PropertyTypeId = 2,
                PartitionNo = "A1",
                FlatOrShopNo = "101",
                PlotNo = "P123",
                SurveyNo = "S456",
                UPICId = "UPIC123",
                SubZoneNo = "SZ01",
                WingNo = "B",
                NoOfResidentialToilets = 2,
                NoOfCommercialToilets = 1,
                PlotArea = 1500.50,
                PlotAreaFtLength = 50.0,
                PlotAreaFtWidth = 30.0,
                PlotAreaMtrLength = 15.24,
                PlotAreaMtrWidth = 9.14,
                WingId = 5,
                WingName = "West Wing"
            };

            Assert.Equal(79, dto.WardId);
            Assert.Equal(10, dto.TaxZoneId);
            Assert.Equal(1, dto.CategoryId);
            Assert.Equal(2, dto.PropertyTypeId);
            Assert.Equal("A1", dto.PartitionNo);
            Assert.Equal("101", dto.FlatOrShopNo);
            Assert.Equal("P123", dto.PlotNo);
            Assert.Equal("S456", dto.SurveyNo);
            Assert.Equal("UPIC123", dto.UPICId);
            Assert.Equal("SZ01", dto.SubZoneNo);
            Assert.Equal("B", dto.WingNo);
            Assert.Equal(2, dto.NoOfResidentialToilets);
            Assert.Equal(1, dto.NoOfCommercialToilets);
            Assert.Equal(1500.50, dto.PlotArea);
            Assert.Equal(50.0, dto.PlotAreaFtLength);
            Assert.Equal(30.0, dto.PlotAreaFtWidth);
            Assert.Equal(15.24, dto.PlotAreaMtrLength);
            Assert.Equal(9.14, dto.PlotAreaMtrWidth);
            Assert.Equal(5, dto.WingId);
            Assert.Equal("West Wing", dto.WingName);
        }

        [Fact]
        public void UpdatePropertyBasicDetailsDto_OptionalProperties_CanBeNull()
        {
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10
            };

            Assert.Null(dto.CategoryId);
            Assert.Null(dto.PropertyTypeId);
            Assert.Null(dto.PartitionNo);
            Assert.Null(dto.FlatOrShopNo);
            Assert.Null(dto.PlotNo);
            Assert.Null(dto.SurveyNo);
            Assert.Null(dto.UPICId);
            Assert.Null(dto.SubZoneNo);
            Assert.Null(dto.WingNo);
            Assert.Null(dto.NoOfResidentialToilets);
            Assert.Null(dto.NoOfCommercialToilets);
            Assert.Null(dto.PlotArea);
            Assert.Null(dto.WingId);
            Assert.Null(dto.WingName);
        }

        [Fact]
        public void UpdatePropertyBasicDetailsDto_RequiredFields_MustHaveValues()
        {
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10
            };

            Assert.NotEqual(0, dto.WardId);
            Assert.NotEqual(0, dto.TaxZoneId);
        }
    }

    #endregion

    #region PropertyBasicDetailsDto Entity Tests

    public class PropertyBasicDetailsDtoEntityTests
    {
        [Fact]
        public void PropertyBasicDetailsDto_AllRequiredProperties_HaveValues()
        {
            var dto = new PropertyBasicDetailsDto
            {
                PropertyId = 549357,
                WardId = 79,
                TaxZoneId = 10,
                TotalCarpetAreaSqMeter = 1000.0,
                TotalBuiltupAreaSqMeter = 1200.0
            };

            Assert.Equal(549357, dto.PropertyId);
            Assert.Equal(79, dto.WardId);
            Assert.Equal(10, dto.TaxZoneId);
            Assert.Equal(1000.0, dto.TotalCarpetAreaSqMeter);
            Assert.Equal(1200.0, dto.TotalBuiltupAreaSqMeter);
        }

        [Fact]
        public void PropertyBasicDetailsDto_OptionalDoubleProperties_CanBeNull()
        {
            var dto = new PropertyBasicDetailsDto
            {
                PropertyId = 1,
                WardId = 79,
                TaxZoneId = 10,
                TotalCarpetAreaSqMeter = 0,
                TotalBuiltupAreaSqMeter = 0
            };

            Assert.Null(dto.TotalCarpetAreaSqFeet);
            Assert.Null(dto.TotalBuiltupAreaSqFeet);
            Assert.Null(dto.PlotArea);
            Assert.Null(dto.PlotAreaFtLength);
            Assert.Null(dto.PlotAreaFtWidth);
            Assert.Null(dto.PlotAreaMtrLength);
            Assert.Null(dto.PlotAreaMtrWidth);
        }

        [Fact]
        public void PropertyBasicDetailsDto_MasterDataProperties_CanBeNull()
        {
            var dto = new PropertyBasicDetailsDto
            {
                PropertyId = 1,
                WardId = 79,
                TaxZoneId = 10,
                TotalCarpetAreaSqMeter = 0,
                TotalBuiltupAreaSqMeter = 0
            };

            Assert.Null(dto.WardNo);
            Assert.Null(dto.ZoneId);
            Assert.Null(dto.Division);
            Assert.Null(dto.TaxZoneNo);
            Assert.Null(dto.CategoryId);
            Assert.Null(dto.CategoryName);
            Assert.Null(dto.PropertyTypeId);
            Assert.Null(dto.PropertyDescription);
        }
    }

    #endregion

    #region PropertyRepository BasicDetails Tests

    public class PropertyRepositoryBasicDetailsTests
    {
        [Fact]
        public async Task GetBasicDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new PropertyRepository(context);

            var result = await repository.GetBasicDetailsAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetBasicDetailsAsync_PropertyExists_ReturnsDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            // Add test data
            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "Tax Zone 10", IsActive = true };
            var category = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential", IsActive = true };
            var propertyType = new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Apartment", IsActive = true };
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                CategoryId = 1,
                PropertyTypeId = 2,
                PropertyNo = "22",
                PartitionNo = "1",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyCategoryMaster.Add(category);
            context.PropertyTypeMasters.Add(propertyType);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal(79, result.WardId);
            Assert.Equal("W79", result.WardNo);
            Assert.Equal(10, result.TaxZoneId);
            Assert.Equal("TZ10", result.TaxZoneNo);
            Assert.Equal("22", result.PropertyNo);
            Assert.Equal("1", result.PartitionNo);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new PropertyRepository(context);

            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10
            };

            var result = await repository.UpdateBasicDetailsAsync(999, dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_InvalidTaxZoneId_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 999 // Invalid TaxZoneId
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateBasicDetailsAsync(549357, dto));

            Assert.Contains("TaxZone with ID 999 does not exist or is inactive", exception.Message);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_InvalidWardId_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 999, // Invalid WardId
                TaxZoneId = 10
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateBasicDetailsAsync(549357, dto));

            Assert.Contains("Ward with ID 999 does not exist or is inactive", exception.Message);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_PropertyMastDetailsNotExists_InsertsNewRecord()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            // Setup required master data
            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var wing = new WingEntity { Id = 1, WingNo = "A", IsActive = true }; // Add WingEntity for WingNo lookup
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.Set<WingEntity>().Add(wing);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                WingNo = "A",
                NoOfResidentialToilets = 2,
                NoOfCommercialToilets = 1
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("A", result.WingNo);
            Assert.Equal(2, result.NoOfResidentialToilets);
            Assert.Equal(1, result.NoOfCommercialToilets);

            // Verify INSERT happened
            var assessmentCount = await context.PropertyMastDetails.CountAsync();
            Assert.Equal(1, assessmentCount);
            
            // Verify Society was created and linked to WingEntity
            var society = await context.SocietyDetailsMast.FirstOrDefaultAsync(s => s.PropertyId == 549357);
            Assert.NotNull(society);
            Assert.Equal(1, society.WingId);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_PropertyMastDetailsExists_UpdatesRecord()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var wing = new WingEntity { Id = 1, WingNo = "NEW", IsActive = true }; // Add WingEntity for WingNo lookup
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var assessment = new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 549357,
                NoOfResidentialToilets = 1,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.Set<WingEntity>().Add(wing);
            context.PropertyMast.Add(property);
            context.PropertyMastDetails.Add(assessment);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                WingNo = "NEW",
                NoOfResidentialToilets = 3
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("NEW", result.WingNo);
            Assert.Equal(3, result.NoOfResidentialToilets);

            // Verify UPDATE happened (still 1 record)
            var assessmentCount = await context.PropertyMastDetails.CountAsync();
            Assert.Equal(1, assessmentCount);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_PlotDetailsNotExists_InsertsNewPlotRecord()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                PlotArea = 2000.75,
                PlotAreaMtrWidth = 25.5
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(2000.75, result.PlotArea);
            Assert.Equal(25.5, result.PlotAreaMtrWidth);

            // Verify INSERT happened
            var plotCount = await context.PlotDetails.CountAsync();
            Assert.Equal(1, plotCount);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_PlotDetailsExists_UpdatesPlotRecord()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };
            var plot = new PlotDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                PlotArea = 1000.0,
                IsActive = true
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            context.PlotDetails.Add(plot);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                PlotArea = 2500.0,
                PlotAreaFtLength = 60.0
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(2500.0, result.PlotArea);
            Assert.Equal(60.0, result.PlotAreaFtLength);

            // Verify UPDATE happened (still 1 record)
            var plotCount = await context.PlotDetails.CountAsync();
            Assert.Equal(1, plotCount);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_NoPlotData_DoesNotInsertPlot()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                PartitionNo = "A1"
                // No plot data provided
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("A1", result.PartitionNo);

            // Verify NO INSERT happened
            var plotCount = await context.PlotDetails.CountAsync();
            Assert.Equal(0, plotCount);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_UpdatesPropertyMastFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 50,
                TaxZoneId = 5,
                PartitionNo = "OLD",
                UPICId = "OLD_UPIC",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                PartitionNo = "NEW",
                UPICId = "NEW_UPIC",
                SubZoneNo = "SZ01"
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(79, result.WardId);
            Assert.Equal(10, result.TaxZoneId);
            Assert.Equal("NEW", result.PartitionNo);
            Assert.Equal("NEW_UPIC", result.UPICId);
            Assert.Equal("SZ01", result.SubZoneNo);
        }
    }

    #endregion

    #region PropertyService BasicDetails Tests

    public class PropertyServiceBasicDetailsTests
    {
        [Fact]
        public async Task GetBasicDetailsAsync_CallsRepository()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var expectedDto = new PropertyBasicDetailsDto
            {
                PropertyId = 549357,
                WardId = 79,
                PropertyNo = "22"
            };

            mockPropertyRepo
                .Setup(r => r.GetBasicDetailsAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal(79, result.WardId);
            mockPropertyRepo.Verify(r => r.GetBasicDetailsAsync(549357, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_CallsRepositoryAndReturnsResult()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                WingNo = "A"
            };

            var expectedResult = new PropertyBasicDetailsDto
            {
                PropertyId = 549357,
                WardId = 79,
                TaxZoneId = 10,
                WingNo = "A"
            };

            mockPropertyRepo
                .Setup(r => r.UpdateBasicDetailsAsync(549357, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("A", result.WingNo);
            mockPropertyRepo.Verify(r => r.UpdateBasicDetailsAsync(549357, dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10
            };

            mockPropertyRepo
                .Setup(r => r.UpdateBasicDetailsAsync(999, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PropertyBasicDetailsDto?)null);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.UpdateBasicDetailsAsync(999, dto);

            Assert.Null(result);
        }
    }

    #endregion

    #region Mouja Integration Tests

    public class MoujaIntegrationTests
    {
        [Fact]
        public async Task GetBasicDetailsAsync_WithMoujaId_ReturnsMoujaName()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var mouja = new MoujaEntity { Id = 1, Year = 2023, MoujaName = "Test Mouja", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = 1,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.MoujaEntity.Add(mouja);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal(1, result.MoujaId);
            Assert.Equal("Test Mouja", result.MoujaName);
        }

        [Fact]
        public async Task GetBasicDetailsAsync_WithoutMoujaId_ReturnsNullMouja()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = null,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Null(result.MoujaId);
            Assert.Null(result.MoujaName);
        }

        [Fact]
        public async Task GetBasicDetailsAsync_WithInactiveMouja_ReturnsNullMoujaName()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var mouja = new MoujaEntity { Id = 1, Year = 2023, MoujaName = "Inactive Mouja", IsActive = false };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = 1,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.MoujaEntity.Add(mouja);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(1, result.MoujaId);
            Assert.Null(result.MoujaName);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_WithValidMoujaId_UpdatesPropertyMast()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var mouja = new MoujaEntity { Id = 2, Year = 2023, MoujaName = "New Mouja", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = null,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.MoujaEntity.Add(mouja);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = 2
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(2, result.MoujaId);
            Assert.Equal("New Mouja", result.MoujaName);

            var updatedProperty = await context.PropertyMast.FindAsync(549357);
            Assert.Equal(2, updatedProperty!.MoujaId);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_WithInvalidMoujaId_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = 999
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateBasicDetailsAsync(549357, dto));

            Assert.Contains("Mouja with ID 999 does not exist or is inactive", exception.Message);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_ChangeMoujaId_UpdatesSuccessfully()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var mouja1 = new MoujaEntity { Id = 1, Year = 2022, MoujaName = "Old Mouja", IsActive = true };
            var mouja2 = new MoujaEntity { Id = 2, Year = 2023, MoujaName = "New Mouja", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = 1,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.MoujaEntity.AddRange(mouja1, mouja2);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = 2
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(2, result.MoujaId);
            Assert.Equal("New Mouja", result.MoujaName);

            var updatedProperty = await context.PropertyMast.FindAsync(549357);
            Assert.Equal(2, updatedProperty!.MoujaId);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_WithInactiveMoujaId_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var mouja = new MoujaEntity { Id = 1, Year = 2023, MoujaName = "Inactive Mouja", IsActive = false };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.TaxZoneMaster.Add(taxZone);
            context.MoujaEntity.Add(mouja);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                MoujaId = 1
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateBasicDetailsAsync(549357, dto));

            Assert.Contains("Mouja with ID 1 does not exist or is inactive", exception.Message);
        }

        [Fact]
        public async Task UpdateBasicDetailsAsync_WithAllFieldsIncludingMoujaId_UpdatesAllFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var ward = new WardEntity { Id = 79, WardNo = "W79", ZoneId = 5, IsActive = true };
            var zone = new ZoneEntity { Id = 5, ZoneNo = "Z5", Description = "Zone 5", IsActive = true };
            var taxZone = new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "TZ", IsActive = true };
            var category = new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential", IsActive = true };
            var propertyType = new PropertyTypeMasterEntity { Id = 2, PropertyDescription = "Apartment", IsActive = true };
            var mouja = new MoujaEntity { Id = 3, Year = 2023, MoujaName = "Complete Test Mouja", IsActive = true };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WardMaster.Add(ward);
            context.ZoneMaster.Add(zone);
            context.TaxZoneMaster.Add(taxZone);
            context.PropertyCategoryMaster.Add(category);
            context.PropertyTypeMasters.Add(propertyType);
            context.MoujaEntity.Add(mouja);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10,
                CategoryId = 1,
                PropertyTypeId = 2,
                MoujaId = 3,
                PartitionNo = "P1",
                UPICId = "UPIC001"
            };

            var result = await repository.UpdateBasicDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal(3, result.MoujaId);
            Assert.Equal("Complete Test Mouja", result.MoujaName);
            Assert.Equal(1, result.CategoryId);
            Assert.Equal(2, result.PropertyTypeId);
            Assert.Equal("P1", result.PartitionNo);
            Assert.Equal("UPIC001", result.UPICId);
        }
    }

    #endregion
}
