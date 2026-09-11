using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class ApartmentQcTopSectionUpdateTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Repository_UpdateTopSection_Success_WhenPropertyExistsAndUnlocked()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var property = new PropertyEntity
        {
            Id = 1,
            OwnerName = "Old Owner",
            OwnerNameEnglish = "Old Owner Eng",
            MobileNo = "9876543210",
            IsActive = true,
            MarkedForDeletion = false
        };
        var assessment = new PropertyAssessmentEntity
        {
            Id = 10,
            PropertyId = 1,
            AdharCardNo = "111122223333",
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        };
        var society = new SocietyDetailsEntity
        {
            Id = 20,
            PropertyId = 1,
            SocietyName = "Old Society",
            SecretaryName = "Old Secretary",
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        };

        context.PropertyMast.Add(property);
        context.PropertyMastDetails.Add(assessment);
        context.SocietyDetailsMast.Add(society);
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var updateDto = new UpdateApartmentQcTopSectionDto
        {
            OwnerName = "New Owner Regional",
            OwnerNameEnglish = "New Owner English",
            MobileNo = "9999999999",
            AadharNo = "999988887777",
            SocietyName = "New Green Park",
            SecretaryName = "John Doe"
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(1, updateDto, updatedBy: 42);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);

        var updatedProp = await context.PropertyMast.FindAsync(1);
        Assert.NotNull(updatedProp);
        Assert.Equal("New Owner Regional", updatedProp.OwnerName);
        Assert.Equal("New Owner English", updatedProp.OwnerNameEnglish);
        Assert.Equal("9999999999", updatedProp.MobileNo);
        Assert.Equal(42, updatedProp.UpdatedBy);

        var updatedAssessment = await context.PropertyMastDetails.FindAsync(10);
        Assert.NotNull(updatedAssessment);
        Assert.Equal("999988887777", updatedAssessment.AdharCardNo);
        Assert.Equal(42, updatedAssessment.UpdatedBy);

        var updatedSociety = await context.SocietyDetailsMast.FindAsync(20);
        Assert.NotNull(updatedSociety);
        Assert.Equal("New Green Park", updatedSociety.SocietyName);
        Assert.Equal("John Doe", updatedSociety.SecretaryName);
        Assert.Equal(42, updatedSociety.UpdatedBy);
    }

    [Fact]
    public async Task Repository_UpdateTopSection_ReturnsNotFound_WhenPropertyDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repo = new ApartmentQcTopSectionRepository(context);
        var updateDto = new UpdateApartmentQcTopSectionDto
        {
            OwnerName = "Test"
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(999, updateDto, updatedBy: 1);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.PropertyNotFound, outcome);
    }

    [Fact]
    public async Task Repository_UpdateTopSection_ReturnsPropertyLocked_WhenScreenLockExists()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var property = new PropertyEntity
        {
            Id = 5,
            OwnerName = "Locked Property",
            IsActive = true,
            MarkedForDeletion = false
        };
        var lockEntity = new PropertyScreenLockEntity
        {
            Id = 1,
            PropertyId = 5,
            IsLocked = true,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        };

        context.PropertyMast.Add(property);
        context.PropertyScreenLocks.Add(lockEntity);
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var updateDto = new UpdateApartmentQcTopSectionDto
        {
            MobileNo = "8888888888"
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(5, updateDto, updatedBy: 1);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.PropertyLocked, outcome);
    }

    [Fact]
    public async Task Service_UpdateTopSection_ReturnsNoFieldsProvided_WhenDtoIsEmpty()
    {
        // Arrange
        var repoMock = new Mock<IApartmentQcTopSectionRepository>();
        var calc = new ApartmentQcTopSectionPerformanceCalculator();
        var service = new ApartmentQcTopSectionService(repoMock.Object, calc);

        // Act
        var outcome = await service.UpdateTopSectionAsync(1, new UpdateApartmentQcTopSectionDto(), updatedBy: 1);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.NoFieldsProvided, outcome);
        repoMock.Verify(r => r.UpdateTopSectionAsync(It.IsAny<int>(), It.IsAny<UpdateApartmentQcTopSectionDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ApartmentQCController CreateController(Mock<IApartmentQcTopSectionService> topSectionService)
    {
        return new ApartmentQCController(
            new Mock<IApartmentQCService>().Object,
            new Mock<IWingWiseDetailsService>().Object,
            new Mock<IRateableValueService>().Object,
            new Mock<ICapitalValueService>().Object,
            new Mock<IApartmentQcCertificateGridService>().Object,
            new Mock<IApartmentQcSearchService>().Object,
            topSectionService.Object,
            new Mock<IApartmentQcTopSectionBelowFlexService>().Object,
            new Mock<IApartmentTaxDetailsService>().Object,
            new Mock<IPropertyCertificateApplicationService>().Object,
            new Mock<NtisPlatform.Application.Interfaces.Master.ISocialAttributeService>().Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ApartmentQCController>.Instance,
            new Mock<IGetApartmentDetailsWingWiseService>().Object,
            new Mock<IApartmentDashboardService>().Object);
    }

    [Fact]
    public async Task Controller_Update_ReturnsOk_WhenServiceSucceeds()
    {
        // Arrange
        var serviceMock = new Mock<IApartmentQcTopSectionService>();
        serviceMock.Setup(s => s.UpdateTopSectionAsync(1, It.IsAny<UpdateApartmentQcTopSectionDto>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TopSectionUpdateOutcome.Success);

        var controller = CreateController(serviceMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var dto = new UpdateApartmentQcTopSectionDto { OwnerName = "New Owner" };

        // Act
        var result = await controller.Update(1, dto, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Controller_Update_ReturnsLocked_WhenPropertyIsLocked()
    {
        // Arrange
        var serviceMock = new Mock<IApartmentQcTopSectionService>();
        serviceMock.Setup(s => s.UpdateTopSectionAsync(1, It.IsAny<UpdateApartmentQcTopSectionDto>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TopSectionUpdateOutcome.PropertyLocked);

        var controller = CreateController(serviceMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var dto = new UpdateApartmentQcTopSectionDto { OwnerName = "New Owner" };

        // Act
        var result = await controller.Update(1, dto, default);

        // Assert
        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status423Locked, objResult.StatusCode);
    }

    [Fact]
    public async Task Repository_UpdateTopSection_NoTargetWings_LeavesWingsUntouched()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 34, PropertyId = 10, SecretaryName = "Old Sec", IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 31, SocietyDetailsMastId = 34, WingName = "A Wing", SecretaryName = "Wing A Sec", IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 131, SocietyDetailsMastId = 34, WingName = "B Wing", SecretaryName = "Wing B Sec", IsActive = true, MarkedForDeletion = false });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcTopSectionDto
        {
            SecretaryName = "New Society Sec",
            SecretaryTargetWingDetailIds = null,
            ManagerTargetWingDetailIds = null // No wings targeted
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(10, dto, updatedBy: 99);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);
        var society = await context.SocietyDetailsMast.FindAsync(34);
        Assert.Equal("New Society Sec", society!.SecretaryName);

        var wingA = await context.WingDetailsMast.FindAsync(31);
        Assert.Equal("Wing A Sec", wingA!.SecretaryName); // untouched

        var wingB = await context.WingDetailsMast.FindAsync(131);
        Assert.Equal("Wing B Sec", wingB!.SecretaryName); // untouched
    }

    [Fact]
    public async Task Repository_UpdateTopSection_AllTargetWings_UpdatesAllWings()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 34, PropertyId = 10, SecretaryName = "Old Sec", IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 31, SocietyDetailsMastId = 34, WingName = "A Wing", SecretaryName = "Wing A Sec", IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 131, SocietyDetailsMastId = 34, WingName = "B Wing", SecretaryName = "Wing B Sec", IsActive = true, MarkedForDeletion = false });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcTopSectionDto
        {
            SecretaryName = "Central Secretary",
            SecretaryTargetWingDetailIds = new List<int> { 31, 131 }
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(10, dto, updatedBy: 99);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);
        var society = await context.SocietyDetailsMast.FindAsync(34);
        Assert.Equal("Central Secretary", society!.SecretaryName);

        var wingA = await context.WingDetailsMast.FindAsync(31);
        Assert.Equal("Central Secretary", wingA!.SecretaryName); // updated!
        Assert.Equal(99, wingA.UpdatedBy);

        var wingB = await context.WingDetailsMast.FindAsync(131);
        Assert.Equal("Central Secretary", wingB!.SecretaryName); // updated!
        Assert.Equal(99, wingB.UpdatedBy);
    }

    [Fact]
    public async Task Repository_UpdateTopSection_SpecificTargetWings_UpdatesOnlySpecifiedWings()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 34, PropertyId = 10, SecretaryName = "Old Sec", IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 31, SocietyDetailsMastId = 34, WingName = "A Wing", SecretaryName = "Wing A Sec", IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 131, SocietyDetailsMastId = 34, WingName = "B Wing", SecretaryName = "Wing B Sec", IsActive = true, MarkedForDeletion = false });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcTopSectionDto
        {
            SecretaryName = "Selective Secretary",
            SecretaryTargetWingDetailIds = new List<int> { 31 }
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(10, dto, updatedBy: 99);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);
        var society = await context.SocietyDetailsMast.FindAsync(34);
        Assert.Equal("Selective Secretary", society!.SecretaryName);

        var wingA = await context.WingDetailsMast.FindAsync(31);
        Assert.Equal("Selective Secretary", wingA!.SecretaryName); // updated!

        var wingB = await context.WingDetailsMast.FindAsync(131);
        Assert.Equal("Wing B Sec", wingB!.SecretaryName); // NOT updated!
    }

    [Fact]
    public async Task Repository_GetSocietyWings_ReturnsWingsListCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 34, PropertyId = 10, SecretaryName = "Sec", IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity
        {
            Id = 31,
            SocietyDetailsMastId = 34,
            WingName = "A Wing",
            SecretaryName = "Wing A Sec",
            SecretaryMobileNo = "9820011221",
            ManagerName = "Wing A Mgr",
            IsActive = true,
            MarkedForDeletion = false
        });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);

        // Act
        var wings = await repo.GetSocietyWingsAsync(10);

        // Assert
        Assert.NotNull(wings);
        Assert.Single(wings);
        Assert.Equal(31, wings[0].WingDetailId);
        Assert.Equal("A Wing", wings[0].WingName);
        Assert.Equal("Wing A Sec", wings[0].SecretaryName);
        Assert.Equal("9820011221", wings[0].SecretaryMobileNo);
        Assert.Equal("Wing A Mgr", wings[0].ManagerName);
    }

    [Fact]
    public async Task Repository_UpdateTopSection_MultiFieldAdministrativeUpdates_ReplicateCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 34, PropertyId = 10, IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 31, SocietyDetailsMastId = 34, WingName = "A Wing", IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 131, SocietyDetailsMastId = 34, WingName = "B Wing", IsActive = true, MarkedForDeletion = false });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcTopSectionDto
        {
            SecretaryName = "New Sec",
            SecretaryMobileNo = "9876543210",
            ManagerName = "New Mgr",
            ManagerMobileNo = "9123456780",
            SecretaryTargetWingDetailIds = new List<int> { 31 },
            ManagerTargetWingDetailIds = new List<int> { 31 }
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(10, dto, updatedBy: 7);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);

        var society = await context.SocietyDetailsMast.FindAsync(34);
        Assert.Equal("New Sec", society!.SecretaryName);
        Assert.Equal("9876543210", society.SecretaryMobileNo);
        Assert.Equal("New Mgr", society.ManagerName);
        Assert.Equal("9123456780", society.ManagerMobileNo);

        var wingA = await context.WingDetailsMast.FindAsync(31);
        Assert.Equal("New Sec", wingA!.SecretaryName);
        Assert.Equal("9876543210", wingA.SecretaryMobileNo);
        Assert.Equal("New Mgr", wingA.ManagerName);
        Assert.Equal("9123456780", wingA.ManagerMobileNo);

        var wingB = await context.WingDetailsMast.FindAsync(131);
        Assert.Null(wingB!.SecretaryName); // untouched
        Assert.Null(wingB.ManagerName);
    }

    [Fact]
    public async Task Repository_UpdateTopSection_IndependentSecretaryAndManagerWings_UpdatesCorrespondingWingsCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 34, PropertyId = 10, IsActive = true, MarkedForDeletion = false });

        // Wing 31: Both
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 31, SocietyDetailsMastId = 34, WingName = "A Wing", IsActive = true, MarkedForDeletion = false });
        // Wing 131: Secretary only
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 131, SocietyDetailsMastId = 34, WingName = "B Wing", IsActive = true, MarkedForDeletion = false });
        // Wing 231: Manager only
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 231, SocietyDetailsMastId = 34, WingName = "C Wing", IsActive = true, MarkedForDeletion = false });
        // Wing 331: Neither
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 331, SocietyDetailsMastId = 34, WingName = "D Wing", IsActive = true, MarkedForDeletion = false });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcTopSectionDto
        {
            SecretaryName = "Shared Secretary",
            SecretaryMobileNo = "9820011221",
            SecretaryTargetWingDetailIds = new List<int> { 31, 131 },

            ManagerName = "Shared Manager",
            ManagerMobileNo = "9820011222",
            ManagerTargetWingDetailIds = new List<int> { 31, 231 }
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(10, dto, updatedBy: 42);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);

        // Society has both
        var society = await context.SocietyDetailsMast.FindAsync(34);
        Assert.Equal("Shared Secretary", society!.SecretaryName);
        Assert.Equal("Shared Manager", society.ManagerName);

        // Wing 31 has both
        var wingA = await context.WingDetailsMast.FindAsync(31);
        Assert.Equal("Shared Secretary", wingA!.SecretaryName);
        Assert.Equal("Shared Manager", wingA.ManagerName);
        Assert.Equal(42, wingA.UpdatedBy);

        // Wing 131 has Secretary only
        var wingB = await context.WingDetailsMast.FindAsync(131);
        Assert.Equal("Shared Secretary", wingB!.SecretaryName);
        Assert.Null(wingB.ManagerName);
        Assert.Equal(42, wingB.UpdatedBy);

        // Wing 231 has Manager only
        var wingC = await context.WingDetailsMast.FindAsync(231);
        Assert.Null(wingC!.SecretaryName);
        Assert.Equal("Shared Manager", wingC.ManagerName);
        Assert.Equal(42, wingC.UpdatedBy);

        // Wing 331 has neither
        var wingD = await context.WingDetailsMast.FindAsync(331);
        Assert.Null(wingD!.SecretaryName);
        Assert.Null(wingD.ManagerName);
    }

    [Fact]
    public async Task Repository_UpdateTopSection_SocietyDetails_UpdatesAllSocietyFieldsCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity
        {
            Id = 34,
            PropertyId = 10,
            SocietyName = "Old Society",
            SocietyNameEnglish = "Old Society English",
            SocietyAddress = "Old Address",
            SocietyAddressEnglish = "Old Address English",
            SocietyEmailId = "old@society.com",
            LandOwnerName = "Old LandOwner",
            LandOwnerNameEnglish = "Old LandOwner English",
            BuilderName = "Old Builder",
            BuilderNameEnglish = "Old Builder English",
            BuilderMobileNo = "9000000001",
            IsActive = true,
            MarkedForDeletion = false
        });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcTopSectionDto
        {
            SocietyName = "New Society",
            SocietyNameEnglish = "New Society English",
            SocietyAddress = "New Address",
            SocietyAddressEnglish = "New Address English",
            SocietyEmailId = "new@society.com",
            LandOwnerName = "New LandOwner",
            LandOwnerNameEnglish = "New LandOwner English",
            BuilderName = "New Builder",
            BuilderNameEnglish = "New Builder English",
            BuilderMobileNo = "9876543210"
        };

        // Act
        var outcome = await repo.UpdateTopSectionAsync(10, dto, updatedBy: 77);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);
        var society = await context.SocietyDetailsMast.FindAsync(34);
        Assert.NotNull(society);
        Assert.Equal("New Society", society.SocietyName);
        Assert.Equal("New Society English", society.SocietyNameEnglish);
        Assert.Equal("New Address", society.SocietyAddress);
        Assert.Equal("New Address English", society.SocietyAddressEnglish);
        Assert.Equal("new@society.com", society.SocietyEmailId);
        Assert.Equal("New LandOwner", society.LandOwnerName);
        Assert.Equal("New LandOwner English", society.LandOwnerNameEnglish);
        Assert.Equal("New Builder", society.BuilderName);
        Assert.Equal("New Builder English", society.BuilderNameEnglish);
        Assert.Equal("9876543210", society.BuilderMobileNo);
        Assert.Equal(77, society.UpdatedBy);
    }

    [Fact]
    public async Task Repository_UpdateWingDetails_Success_WhenWingExistsAndUnlocked()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 34, PropertyId = 10, IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity
        {
            Id = 100,
            SocietyDetailsMastId = 34,
            WingMasterId = 1,
            WingName = "A",
            SecretaryName = "Old Sec",
            SecretaryNameEnglish = "Old Sec Eng",
            SecretaryMobileNo = "1111111111",
            SecretaryEmailId = "oldsec@mail.com",
            ManagerName = "Old Mgr",
            ManagerNameEnglish = "Old Mgr Eng",
            ManagerMobileNo = "2222222222",
            ManagerEmailId = "oldmgr@mail.com",
            IsActive = true,
            MarkedForDeletion = false
        });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcWingDetailsDto
        {
            WingName = "A1",
            SecretaryName = "New Sec",
            SecretaryNameEnglish = "New Sec Eng",
            SecretaryMobileNo = "9999999999",
            SecretaryEmailId = "newsec@mail.com",
            ManagerName = "New Mgr",
            ManagerNameEnglish = "New Mgr Eng",
            ManagerMobileNo = "8888888888",
            ManagerEmailId = "newmgr@mail.com"
        };

        // Act
        var outcome = await repo.UpdateWingDetailsAsync(100, dto, updatedBy: 42);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);
        var updatedWing = await context.WingDetailsMast.FindAsync(100);
        Assert.NotNull(updatedWing);
        Assert.Equal("A1", updatedWing.WingName);
        Assert.Equal("New Sec", updatedWing.SecretaryName);
        Assert.Equal("New Sec Eng", updatedWing.SecretaryNameEnglish);
        Assert.Equal("9999999999", updatedWing.SecretaryMobileNo);
        Assert.Equal("newsec@mail.com", updatedWing.SecretaryEmailId);
        Assert.Equal("New Mgr", updatedWing.ManagerName);
        Assert.Equal("New Mgr Eng", updatedWing.ManagerNameEnglish);
        Assert.Equal("8888888888", updatedWing.ManagerMobileNo);
        Assert.Equal("newmgr@mail.com", updatedWing.ManagerEmailId);
        Assert.Equal(42, updatedWing.UpdatedBy);
    }

    [Fact]
    public async Task Repository_UpdateWingDetails_ReturnsWingNotFound_WhenWingDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcWingDetailsDto { WingName = "B" };

        // Act
        var outcome = await repo.UpdateWingDetailsAsync(999, dto, updatedBy: 1);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.WingNotFound, outcome);
    }

    [Fact]
    public async Task Repository_UpdateWingDetails_ReturnsPropertyLocked_WhenPropertyIsLocked()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 10, IsActive = true, MarkedForDeletion = false });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 34, PropertyId = 10, IsActive = true, MarkedForDeletion = false });
        context.WingDetailsMast.Add(new WingDetailsMastEntity
        {
            Id = 100,
            SocietyDetailsMastId = 34,
            WingName = "A",
            IsActive = true,
            MarkedForDeletion = false
        });
        context.PropertyScreenLocks.Add(new PropertyScreenLockEntity
        {
            Id = 1,
            PropertyId = 10,
            IsLocked = true,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);
        var dto = new UpdateApartmentQcWingDetailsDto { SecretaryName = "New Sec" };

        // Act
        var outcome = await repo.UpdateWingDetailsAsync(100, dto, updatedBy: 1);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.PropertyLocked, outcome);
    }

    [Fact]
    public async Task Service_UpdateWingDetails_ReturnsNoFieldsProvided_WhenDtoIsEmpty()
    {
        // Arrange
        var repoMock = new Mock<IApartmentQcTopSectionRepository>();
        var calc = new ApartmentQcTopSectionPerformanceCalculator();
        var service = new ApartmentQcTopSectionService(repoMock.Object, calc);

        // Act
        var outcome = await service.UpdateWingDetailsAsync(100, new UpdateApartmentQcWingDetailsDto(), updatedBy: 1);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.NoFieldsProvided, outcome);
        repoMock.Verify(r => r.UpdateWingDetailsAsync(It.IsAny<int>(), It.IsAny<UpdateApartmentQcWingDetailsDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateWingDetails_DelegatesToRepository_WhenValid()
    {
        // Arrange
        var repoMock = new Mock<IApartmentQcTopSectionRepository>();
        var calc = new ApartmentQcTopSectionPerformanceCalculator();
        var service = new ApartmentQcTopSectionService(repoMock.Object, calc);
        var dto = new UpdateApartmentQcWingDetailsDto { WingName = "C" };

        repoMock.Setup(r => r.UpdateWingDetailsAsync(100, dto, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TopSectionUpdateOutcome.Success);

        // Act
        var outcome = await service.UpdateWingDetailsAsync(100, dto, updatedBy: 1);

        // Assert
        Assert.Equal(TopSectionUpdateOutcome.Success, outcome);
        repoMock.Verify(r => r.UpdateWingDetailsAsync(100, dto, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Controller_UpdateWingDetails_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var serviceMock = new Mock<IApartmentQcTopSectionService>();
        serviceMock.Setup(s => s.UpdateWingDetailsAsync(100, It.IsAny<UpdateApartmentQcWingDetailsDto>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TopSectionUpdateOutcome.Success);

        var controller = CreateController(serviceMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var dto = new UpdateApartmentQcWingDetailsDto { WingName = "A" };

        // Act
        var result = await controller.UpdateWingDetails(100, dto, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Controller_UpdateWingDetails_ReturnsNotFound_WhenWingNotFound()
    {
        // Arrange
        var serviceMock = new Mock<IApartmentQcTopSectionService>();
        serviceMock.Setup(s => s.UpdateWingDetailsAsync(100, It.IsAny<UpdateApartmentQcWingDetailsDto>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TopSectionUpdateOutcome.WingNotFound);

        var controller = CreateController(serviceMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var dto = new UpdateApartmentQcWingDetailsDto { WingName = "A" };

        // Act
        var result = await controller.UpdateWingDetails(100, dto, default);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Controller_UpdateWingDetails_ReturnsLocked_WhenPropertyLocked()
    {
        // Arrange
        var serviceMock = new Mock<IApartmentQcTopSectionService>();
        serviceMock.Setup(s => s.UpdateWingDetailsAsync(100, It.IsAny<UpdateApartmentQcWingDetailsDto>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TopSectionUpdateOutcome.PropertyLocked);

        var controller = CreateController(serviceMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var dto = new UpdateApartmentQcWingDetailsDto { WingName = "A" };

        // Act
        var result = await controller.UpdateWingDetails(100, dto, default);

        // Assert
        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status423Locked, objResult.StatusCode);
    }

    [Fact]
    public async Task Repository_GetAdditionalRevenueTaxDetailsAsync_CalculatesCurrentTaxAndRetroTax_FromPolicyTaxDetails()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var taxTotal = new NtisPlatform.Core.Entities.Master.TaxMasterEntity
        {
            Id = 1,
            TaxCode = "TAXTOTAL",
            TaxName = "Tax Total",
            IsActive = true
        };
        var retroPolicy = new NtisPlatform.Core.Entities.Master.PolicyCodeMasterEntity
        {
            Id = 10,
            IsRetroDemand = true,
            IsActive = true
        };
        var regularPolicy = new NtisPlatform.Core.Entities.Master.PolicyCodeMasterEntity
        {
            Id = 20,
            IsRetroDemand = false,
            IsActive = true
        };

        // 1. Current tax row (TAXTOTAL, IsCurrent = true, IsActive = true, MarkedForDeletion = false)
        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1487316,
            PolicyCodeId = 20,
            TaxId = 1,
            TaxAmount = 2051.00m,
            IsActive = true,
            MarkedForDeletion = false,
            IsCurrent = true
        });

        // Current tax row with IsCurrent = false (should be excluded from CurrentTax)
        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 2,
            PropertyId = 1487316,
            PolicyCodeId = 20,
            TaxId = 1,
            TaxAmount = 500.00m,
            IsActive = true,
            MarkedForDeletion = false,
            IsCurrent = false
        });

        // 2. Retro tax row (TAXTOTAL, PolicyCode.IsRetroDemand = true, IsActive = true, MarkedForDeletion = false)
        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 3,
            PropertyId = 1487316,
            PolicyCodeId = 10,
            TaxId = 1,
            TaxAmount = 2074.00m,
            IsActive = true,
            MarkedForDeletion = false,
            IsCurrent = false
        });

        // 3. Pending current row in TransMast (PolicyCode = OLD_ARREARS, TAXTOTAL)
        var oldArrearsPolicy = new NtisPlatform.Core.Entities.Master.PolicyCodeMasterEntity
        {
            Id = 30,
            PolicyCode = "OLD_ARREARS",
            IsActive = true
        };
        context.TransMast.Add(new TransMastEntity
        {
            Id = 10,
            PropertyId = 1487316,
            PolicyCodeId = 30,
            TaxId = 1,
            TaxAmount = 300.00m,
            IsActive = true
        });

        context.TaxMaster.Add(taxTotal);
        context.PolicyCodeMaster.Add(retroPolicy);
        context.PolicyCodeMaster.Add(regularPolicy);
        context.PolicyCodeMaster.Add(oldArrearsPolicy);
        await context.SaveChangesAsync();

        var repo = new ApartmentQcTopSectionRepository(context);

        // Act
        var (currentTax, retroTax, oldCurrentTax, pendingCurrent) = await repo.GetAdditionalRevenueTaxDetailsAsync(1487316);

        // Assert
        Assert.Equal(2051.00m, currentTax);
        Assert.Equal(2074.00m, retroTax);
        Assert.Equal(300.00m, pendingCurrent);
    }

    [Fact]
    public void Calculator_ComputeAdditionalRevenue_CalculatesPendingDemandAndTotalDemandCorrectly()
    {
        // Arrange
        var calculator = new ApartmentQcTopSectionPerformanceCalculator();
        decimal? currentTax = 2051.00m;
        decimal? retroTax = 2074.00m;
        decimal? oldCurrentTax = 2440.00m;
        decimal? pendingCurrent = 300.00m;

        // Act
        var result = calculator.ComputeAdditionalRevenue(currentTax, retroTax, oldCurrentTax, pendingCurrent);

        // Assert
        Assert.Equal(2051.00m, result.CurrentTax);
        Assert.Equal(2074.00m, result.RetroTax);
        Assert.Equal(4125.00m, result.TotalTax); // 2051 + 2074
        Assert.Equal(300.00m, result.PendingCurrent);
        Assert.Equal(2374.00m, result.PendingDemand); // 2074 (retro) + 300 (pendingCurrent)
        Assert.Equal(4425.00m, result.TotalDemand); // 2051 (currentTax) + 2374 (pendingDemand)
        Assert.Equal(2440.00m, result.OldCurrentTax);
        Assert.Equal(1685.00m, result.DifferenceAmount); // 4125 - 2440
        Assert.Equal(69.06, result.ChangePercent); // 1685 / 2440 (old current tax) * 100, rounded
    }
}


