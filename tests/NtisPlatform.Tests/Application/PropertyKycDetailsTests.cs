using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Repositories.Property;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for Property KYC Details API
/// Coverage: Repository, Service, DTOs, Entities
/// </summary>
public class PropertyKycDetailsTests
{
    /// <summary>Composes the KYC use-case service over the in-memory context (feature repo + unit of work).</summary>
    private static PropertyKycService CreateKycService(ApplicationDbContext context)
        => new(new PropertyKycRepository(context), new UnitOfWork(context), new PropertyMutationInvariantPolicy());

    #region UpdatePropertyKycDetailsDto Tests

    public class UpdatePropertyKycDetailsDtoTests
    {
        [Fact]
        public void UpdatePropertyKycDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerTypeId = 1,
                AdharCardNo = "321131311616",
                OwnerTitle = "Mr",
                OwnerName = "LODHA AMARA BUILDING",
                OwnerTitleEnglish = "Mr",
                OwnerNameEnglish = "Dharak",
                OccupierTitle = "Mr",
                OccupierName = "Nilesh",
                OccupierTitleEnglish = "Mr",
                OccupierNameEnglish = "shubhan",
                Address = "Lodha Amara Kolshet Road Thane West",
                Location = "7, Kolshet Rd, Lodha Amara",
                AddressEnglish = "kolshet",
                LocationEnglish = "7, Kolshet Rd",
                FlatOrShopName = "PARKING 13 FLOOR",
                FlatOrShopNameEnglish = "Ameya",
                FlatOrShopNo = "1203",
                FlatOrShopNoEnglish = "1203",
                MobileNo = "9921759522",
                EmailId = "user@example.com"
            };

            Assert.Equal(1, dto.OwnerTypeId);
            Assert.Equal("321131311616", dto.AdharCardNo);
            Assert.Equal("Mr", dto.OwnerTitle);
            Assert.Equal("LODHA AMARA BUILDING", dto.OwnerName);
            Assert.Equal("Mr", dto.OwnerTitleEnglish);
            Assert.Equal("Dharak", dto.OwnerNameEnglish);
            Assert.Equal("Mr", dto.OccupierTitle);
            Assert.Equal("Nilesh", dto.OccupierName);
            Assert.Equal("Mr", dto.OccupierTitleEnglish);
            Assert.Equal("shubhan", dto.OccupierNameEnglish);
            Assert.Equal("Lodha Amara Kolshet Road Thane West", dto.Address);
            Assert.Equal("7, Kolshet Rd, Lodha Amara", dto.Location);
            Assert.Equal("kolshet", dto.AddressEnglish);
            Assert.Equal("7, Kolshet Rd", dto.LocationEnglish);
            Assert.Equal("PARKING 13 FLOOR", dto.FlatOrShopName);
            Assert.Equal("Ameya", dto.FlatOrShopNameEnglish);
            Assert.Equal("1203", dto.FlatOrShopNo);
            Assert.Equal("1203", dto.FlatOrShopNoEnglish);
            Assert.Equal("9921759522", dto.MobileNo);
            Assert.Equal("user@example.com", dto.EmailId);
        }

        [Fact]
        public void UpdatePropertyKycDetailsDto_AllProperties_CanBeNull()
        {
            var dto = new UpdatePropertyKycDetailsDto();

            Assert.Null(dto.OwnerTypeId);
            Assert.Null(dto.AdharCardNo);
            Assert.Null(dto.OwnerTitle);
            Assert.Null(dto.OwnerName);
            Assert.Null(dto.OwnerTitleEnglish);
            Assert.Null(dto.OwnerNameEnglish);
            Assert.Null(dto.OccupierTitle);
            Assert.Null(dto.OccupierName);
            Assert.Null(dto.OccupierTitleEnglish);
            Assert.Null(dto.OccupierNameEnglish);
            Assert.Null(dto.Address);
            Assert.Null(dto.Location);
            Assert.Null(dto.AddressEnglish);
            Assert.Null(dto.LocationEnglish);
            Assert.Null(dto.FlatOrShopName);
            Assert.Null(dto.FlatOrShopNameEnglish);
            Assert.Null(dto.FlatOrShopNo);
            Assert.Null(dto.FlatOrShopNoEnglish);
            Assert.Null(dto.MobileNo);
            Assert.Null(dto.EmailId);
        }

        [Fact]
        public void UpdatePropertyKycDetailsDto_PartialData_WorksCorrectly()
        {
            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerName = "John Doe",
                MobileNo = "1234567890"
            };

            Assert.Equal("John Doe", dto.OwnerName);
            Assert.Equal("1234567890", dto.MobileNo);
            Assert.Null(dto.OwnerTypeId);
            Assert.Null(dto.Address);
        }
    }

    #endregion

    #region PropertyKycDetailsDto Tests

    public class PropertyKycDetailsDtoTests
    {
        [Fact]
        public void PropertyKycDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new PropertyKycDetailsDto
            {
                PropertyId = 549357,
                OwnerTypeId = 1,
                OwnerType = "Individual",
                AdharCardNo = "321131311616",
                OwnerTitle = "Mr",
                OwnerName = "LODHA AMARA BUILDING",
                OwnerTitleEnglish = "Mr",
                OwnerNameEnglish = "Dharak",
                OccupierTitle = "Mr",
                OccupierName = "Nilesh",
                OccupierTitleEnglish = "Mr",
                OccupierNameEnglish = "shubhan",
                Address = "Lodha Amara Kolshet Road",
                Location = "7, Kolshet Rd",
                AddressEnglish = "kolshet",
                LocationEnglish = "7, Kolshet Rd",
                FlatOrShopName = "PARKING 13 FLOOR",
                FlatOrShopNameEnglish = "Ameya",
                FlatOrShopNo = "1203",
                FlatOrShopNoEnglish = "1203",
                MobileNo = "9921759522",
                EmailId = "user@example.com"
            };

            Assert.Equal(549357, dto.PropertyId);
            Assert.Equal(1, dto.OwnerTypeId);
            Assert.Equal("Individual", dto.OwnerType);
            Assert.Equal("321131311616", dto.AdharCardNo);
            Assert.Equal("Mr", dto.OwnerTitle);
            Assert.Equal("LODHA AMARA BUILDING", dto.OwnerName);
            Assert.Equal("Dharak", dto.OwnerNameEnglish);
            Assert.Equal("Nilesh", dto.OccupierName);
            Assert.Equal("shubhan", dto.OccupierNameEnglish);
            Assert.Equal("Lodha Amara Kolshet Road", dto.Address);
            Assert.Equal("kolshet", dto.AddressEnglish);
            Assert.Equal("PARKING 13 FLOOR", dto.FlatOrShopName);
            Assert.Equal("1203", dto.FlatOrShopNo);
            Assert.Equal("9921759522", dto.MobileNo);
            Assert.Equal("user@example.com", dto.EmailId);
        }

        [Fact]
        public void PropertyKycDetailsDto_OptionalProperties_CanBeNull()
        {
            var dto = new PropertyKycDetailsDto
            {
                PropertyId = 549357
            };

            Assert.Null(dto.OwnerTypeId);
            Assert.Null(dto.OwnerType);
            Assert.Null(dto.AdharCardNo);
            Assert.Null(dto.OwnerTitle);
            Assert.Null(dto.OwnerName);
            Assert.Null(dto.OccupierName);
            Assert.Null(dto.Address);
            Assert.Null(dto.MobileNo);
            Assert.Null(dto.EmailId);
        }

        [Fact]
        public void PropertyKycDetailsDto_RequiredPropertyId_HasValue()
        {
            var dto = new PropertyKycDetailsDto
            {
                PropertyId = 549357
            };

            Assert.Equal(549357, dto.PropertyId);
            Assert.NotEqual(0, dto.PropertyId);
        }
    }

    #endregion

    #region PropertyRepository KycDetails Tests

    public class PropertyRepositoryKycDetailsTests
    {
        [Fact]
        public async Task GetKycDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            var service = CreateKycService(context);

            var result = await service.GetKycDetailsAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetKycDetailsAsync_PropertyExists_ReturnsDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                OwnerName = "John Doe",
                MobileNo = "9921759522",
                EmailId = "test@example.com",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var service = CreateKycService(context);
            var result = await service.GetKycDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("John Doe", result.OwnerName);
            Assert.Equal("9921759522", result.MobileNo);
            Assert.Equal("test@example.com", result.EmailId);
        }

        [Fact]
        public async Task GetKycDetailsAsync_WithOwnerType_ReturnsOwnerTypeName()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var ownerType = new OwnerTypeMasterEntity
            {
                Id = 1,
                OwnerType = "Individual",
                IsActive = true
            };
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
                OwnerTypeId = 1,
                AdharCardNo = "123456789012",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.OwnerTypeMaster.Add(ownerType);
            context.PropertyMast.Add(property);
            context.PropertyMastDetails.Add(assessment);
            await context.SaveChangesAsync();

            var service = CreateKycService(context);
            var result = await service.GetKycDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(1, result.OwnerTypeId);
            Assert.Equal("Individual", result.OwnerType);
            Assert.Equal("123456789012", result.AdharCardNo);
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            var service = CreateKycService(context);

            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerName = "Test"
            };

            var result = await service.UpdateKycDetailsAsync(999, dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_UpdatesPropertyMastFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                OwnerName = "OLD NAME",
                MobileNo = "0000000000",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var service = CreateKycService(context);
            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerName = "NEW NAME",
                OwnerNameEnglish = "New English Name",
                MobileNo = "9921759522",
                EmailId = "new@example.com"
            };

            var result = await service.UpdateKycDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("NEW NAME", result.OwnerName);
            Assert.Equal("New English Name", result.OwnerNameEnglish);
            Assert.Equal("9921759522", result.MobileNo);
            Assert.Equal("new@example.com", result.EmailId);
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_PropertyMastDetailsNotExists_InsertsNewRecord()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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

            var service = CreateKycService(context);
            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerTypeId = 1,
                AdharCardNo = "321131311616",
                OwnerName = "John Doe"
            };

            var result = await service.UpdateKycDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(1, result.OwnerTypeId);
            Assert.Equal("321131311616", result.AdharCardNo);
            Assert.Equal("John Doe", result.OwnerName);

            // Verify INSERT happened
            var assessmentCount = await context.PropertyMastDetails.CountAsync();
            Assert.Equal(1, assessmentCount);

            var insertedRecord = await context.PropertyMastDetails.FirstAsync();
            Assert.Equal(549357, insertedRecord.PropertyId);
            Assert.Equal(1, insertedRecord.OwnerTypeId);
            Assert.Equal("321131311616", insertedRecord.AdharCardNo);
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_PropertyMastDetailsExists_UpdatesRecord()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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
            var assessment = new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 549357,
                OwnerTypeId = 2,
                AdharCardNo = "OLD_ADHAR",
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastDetails.Add(assessment);
            await context.SaveChangesAsync();

            var service = CreateKycService(context);
            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerTypeId = 1,
                AdharCardNo = "NEW_ADHAR"
            };

            var result = await service.UpdateKycDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(1, result.OwnerTypeId);
            Assert.Equal("NEW_ADHAR", result.AdharCardNo);

            // Verify UPDATE happened (still 1 record)
            var assessmentCount = await context.PropertyMastDetails.CountAsync();
            Assert.Equal(1, assessmentCount);

            var updatedRecord = await context.PropertyMastDetails.FirstAsync();
            Assert.Equal(1, updatedRecord.OwnerTypeId);
            Assert.Equal("NEW_ADHAR", updatedRecord.AdharCardNo);
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_NoAssessmentData_DoesNotInsertPropertyMastDetails()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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

            var service = CreateKycService(context);
            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerName = "John Doe",
                MobileNo = "9921759522"
                // No OwnerTypeId or AdharCardNo
            };

            var result = await service.UpdateKycDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("John Doe", result.OwnerName);
            Assert.Equal("9921759522", result.MobileNo);

            // Verify NO INSERT happened
            var assessmentCount = await context.PropertyMastDetails.CountAsync();
            Assert.Equal(0, assessmentCount);
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_UpdatesAllPropertyMastFields()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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

            var service = CreateKycService(context);
            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerTitle = "Mr",
                OwnerName = "Owner Name",
                OwnerTitleEnglish = "Mr",
                OwnerNameEnglish = "Owner English",
                OccupierTitle = "Ms",
                OccupierName = "Occupier Name",
                OccupierTitleEnglish = "Ms",
                OccupierNameEnglish = "Occupier English",
                Address = "Address Line",
                Location = "Location Line",
                AddressEnglish = "Address English",
                LocationEnglish = "Location English",
                FlatOrShopName = "Flat Name",
                FlatOrShopNameEnglish = "Flat English",
                FlatOrShopNo = "101",
                FlatOrShopNoEnglish = "101",
                MobileNo = "9876543210",
                EmailId = "test@test.com"
            };

            var result = await service.UpdateKycDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("Owner Name", result.OwnerName);
            Assert.Equal("Owner English", result.OwnerNameEnglish);
            Assert.Equal("Occupier Name", result.OccupierName);
            Assert.Equal("Occupier English", result.OccupierNameEnglish);
            Assert.Equal("Address Line", result.Address);
            Assert.Equal("Location Line", result.Location);
            Assert.Equal("Address English", result.AddressEnglish);
            Assert.Equal("Location English", result.LocationEnglish);
            Assert.Equal("Flat Name", result.FlatOrShopName);
            Assert.Equal("Flat English", result.FlatOrShopNameEnglish);
            Assert.Equal("101", result.FlatOrShopNo);
            Assert.Equal("101", result.FlatOrShopNoEnglish);
            Assert.Equal("9876543210", result.MobileNo);
            Assert.Equal("test@test.com", result.EmailId);
        }
    }

    #endregion

    #region PropertyKycService Tests

    public class PropertyKycServiceTests
    {
        private static PropertyKycService CreateService(out Mock<IPropertyKycRepository> repo, out Mock<IUnitOfWork> unitOfWork)
        {
            repo = new Mock<IPropertyKycRepository>();
            unitOfWork = new Mock<IUnitOfWork>();
            return new PropertyKycService(repo.Object, unitOfWork.Object, new PropertyMutationInvariantPolicy());
        }

        [Fact]
        public async Task GetKycDetailsAsync_DelegatesToRepository()
        {
            var service = CreateService(out var repo, out _);
            var expectedDto = new PropertyKycDetailsDto { PropertyId = 549357, OwnerName = "John Doe", MobileNo = "9921759522" };
            repo.Setup(r => r.GetKycDetailsAsync(549357, It.IsAny<CancellationToken>())).ReturnsAsync(expectedDto);

            var result = await service.GetKycDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal("John Doe", result.OwnerName);
            repo.Verify(r => r.GetKycDetailsAsync(549357, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var service = CreateService(out var repo, out _);
            repo.Setup(r => r.GetActivePropertyAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((PropertyEntity?)null);

            var result = await service.UpdateKycDetailsAsync(999, new UpdatePropertyKycDetailsDto { OwnerName = "Test" });

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_ValidData_SavesAndReturnsRefreshedDto()
        {
            var service = CreateService(out var repo, out var unitOfWork);
            repo.Setup(r => r.GetActivePropertyAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PropertyEntity { Id = 549357, IsActive = true });
            repo.Setup(r => r.GetFirstAssessmentIdAsync(549357, It.IsAny<CancellationToken>())).ReturnsAsync(0);
            var expected = new PropertyKycDetailsDto { PropertyId = 549357, OwnerName = "Updated Name" };
            repo.Setup(r => r.GetKycDetailsAsync(549357, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await service.UpdateKycDetailsAsync(549357, new UpdatePropertyKycDetailsDto { OwnerName = "Updated Name", MobileNo = "9921759522" });

            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.OwnerName);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    #endregion

    #region Related Entity Coverage Tests

    public class RelatedEntityCoverageTests
    {
        [Fact]
        public void PropertyAssessmentEntity_Properties_GetSet_WorksCorrectly()
        {
            var entity = new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 549357,
                NoOfResidentialToilets = 2,
                NoOfCommercialToilets = 1,
                OwnerTypeId = 1,
                AdharCardNo = "123456789012",
                MarkedForDeletion = false,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal(2, entity.NoOfResidentialToilets);
            Assert.Equal(1, entity.NoOfCommercialToilets);
            Assert.Equal(1, entity.Id);
            Assert.Equal("123456789012", entity.AdharCardNo);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void PlotDetailsEntity_Properties_GetSet_WorksCorrectly()
        {
            var entity = new PlotDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                PlotArea = 1500.50,
                PlotAreaFtLength = 50.0,
                PlotAreaFtWidth = 30.0,
                PlotAreaMtrLength = 15.24,
                PlotAreaMtrWidth = 9.14,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal(1500.50, entity.PlotArea);
            Assert.Equal(50.0, entity.PlotAreaFtLength);
            Assert.Equal(30.0, entity.PlotAreaFtWidth);
            Assert.Equal(15.24, entity.PlotAreaMtrLength);
            Assert.Equal(9.14, entity.PlotAreaMtrWidth);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void SocietyDetailsEntity_Properties_GetSet_WorksCorrectly()
        {
            var entity = new SocietyDetailsEntity
            {
                Id = 1,
                WingId = 5,
                WingName = "West Wing",
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(5, entity.WingId);
            Assert.Equal("West Wing", entity.WingName);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void OwnerTypeMasterEntity_Properties_GetSet_WorksCorrectly()
        {
            var entity = new OwnerTypeMasterEntity
            {
                Id = 1,
                OwnerType = "Individual",
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal("Individual", entity.OwnerType);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void PropertyTypeMasterEntity_Properties_GetSet_WorksCorrectly()
        {
            var entity = new PropertyTypeMasterEntity
            {
                Id = 1,
                PropertyDescription = "Apartment",
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal("Apartment", entity.PropertyDescription);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void PropertyDetailsEntity_Properties_GetSet_WorksCorrectly()
        {
            var entity = new PropertyDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                CarpetAreaSqMeter = 1000.0,
                BuiltupAreaSqMeter = 1200.0,
                CarpetAreaSqFeet = 10764.0,
                BuiltupAreaSqFeet = 12917.0,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal(1000.0, entity.CarpetAreaSqMeter);
            Assert.Equal(1200.0, entity.BuiltupAreaSqMeter);
            Assert.Equal(10764.0, entity.CarpetAreaSqFeet);
            Assert.Equal(12917.0, entity.BuiltupAreaSqFeet);
            Assert.True(entity.IsActive);
        }
    }

    #endregion

    #region Edge Case Tests

    public class EdgeCaseTests
    {
        // Basic Details now runs through the per-tab service; KYC edge cases still use PropertyRepository directly.
        private static PropertyBasicDetailsService CreateBasicDetailsService(ApplicationDbContext context)
            => new(new PropertyBasicDetailsRepository(context), new MasterRepository(context), new UnitOfWork(context), new PropertyMutationInvariantPolicy());

        [Fact]
        public async Task UpdateBasicDetailsAsync_MarkedForDeletionTrue_NotReturned()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = true // Marked for deletion
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var service = CreateBasicDetailsService(context);
            var dto = new UpdatePropertyBasicDetailsDto
            {
                WardId = 79,
                TaxZoneId = 10
            };

            var result = await service.UpdateBasicDetailsAsync(549357, dto);

            Assert.Null(result); // Should not find property marked for deletion
        }

        [Fact]
        public async Task UpdateKycDetailsAsync_IsActiveFalse_NotReturned()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var service = CreateKycService(context);
            var dto = new UpdatePropertyKycDetailsDto
            {
                OwnerName = "Test"
            };

            var result = await service.UpdateKycDetailsAsync(549357, dto);

            Assert.Null(result); // Should not find inactive property
        }

        [Fact]
        public async Task GetBasicDetailsAsync_PropertyMastDetailsInactive_NotIncluded()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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
            var assessment = new PropertyAssessmentEntity
            {
                Id = 1,
                PropertyId = 549357,
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.PropertyMastDetails.Add(assessment);
            await context.SaveChangesAsync();

            var service = CreateBasicDetailsService(context);
            var result = await service.GetBasicDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Null(result.WingNo); // Inactive assessment should not be included
        }

        [Fact]
        public async Task GetKycDetailsAsync_OwnerTypeMasterInactive_OwnerTypeNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            
            var ownerType = new OwnerTypeMasterEntity
            {
                Id = 1,
                OwnerType = "Individual",
                IsActive = false // Inactive
            };
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
                OwnerTypeId = 1,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.OwnerTypeMaster.Add(ownerType);
            context.PropertyMast.Add(property);
            context.PropertyMastDetails.Add(assessment);
            await context.SaveChangesAsync();

            var service = CreateKycService(context);
            var result = await service.GetKycDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(1, result.OwnerTypeId);
            Assert.Null(result.OwnerType); // Inactive owner type should not be loaded
        }
    }

    #endregion
}

