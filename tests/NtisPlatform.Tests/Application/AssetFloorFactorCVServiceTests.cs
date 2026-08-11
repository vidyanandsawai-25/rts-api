using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;
using AppValidationException = NtisPlatform.Application.Exceptions.ValidationException;
using SystemValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class AssetFloorFactorCVServiceTests
{
    private readonly Mock<IRepository<AssetFloorFactorCVEntity, int>> _repositoryMock = new();
    private readonly Mock<IRepository<FloorEntity, int>> _floorRepoMock = new();
    private readonly Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>> _yearRangeRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IReferenceValidationService> _referenceValidatorMock = new();
    private readonly IMapper _mapper;

    public AssetFloorFactorCVServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetFloorFactorCVMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _referenceValidatorMock
            .Setup(x => x.ValidateReferencesAsync<AssetFloorFactorCVEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());
    }

    private AssetFloorFactorCVService CreateService() =>
        new(_repositoryMock.Object, _floorRepoMock.Object, _yearRangeRepoMock.Object, _unitOfWorkMock.Object, _mapper, _referenceValidatorMock.Object);

    #region Entity & DTO Property Coverage

    [Fact]
    public void EntityAndDtos_Properties_GetSet_WorksCorrectly()
    {
        var now = DateTime.UtcNow;
        var floorObj = new FloorEntity { Id = 10 };
        var yearRangeObj = new AssetAssessmentYearRangeMasterCVEntity { Id = 5 };

        var entity = new AssetFloorFactorCVEntity
        {
            Id = 1,
            FloorId = 10,
            Floor = floorObj,
            YearRangeCVId = 5,
            YearRangeCV = yearRangeObj,
            FactorWithLift = 1.5m,
            FactorWithoutLift = 1.2m,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.FloorId);
        Assert.Same(floorObj, entity.Floor);
        Assert.Equal(5, entity.YearRangeCVId);
        Assert.Same(yearRangeObj, entity.YearRangeCV);
        Assert.Equal(1.5m, entity.FactorWithLift);
        Assert.Equal(1.2m, entity.FactorWithoutLift);
        Assert.True(entity.IsActive);

        var dto = new AssetFloorFactorCVDto
        {
            Id = 1,
            FloorId = 10,
            FloorDescription = "Ground Floor",
            YearRangeCVId = 5,
            FactorWithLift = 1.5m,
            FactorWithoutLift = 1.2m,
            IsActive = true,
            CreatedDate = now,
            MarkedForDeletion = false
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.FloorId);
        Assert.Equal("Ground Floor", dto.FloorDescription);
        Assert.Equal(5, dto.YearRangeCVId);

        var updateDto = new UpdateAssetFloorFactorCVDto
        {
            FloorId = 10,
            YearRangeCVId = 5,
            FactorWithLift = 1.6m,
            FactorWithoutLift = 1.3m,
            IsActive = true
        };

        Assert.Equal(1.6m, updateDto.FactorWithLift);

        var qp = new AssetFloorFactorCVQueryParameters
        {
            FloorId = 10,
            YearRangeCVId = 5,
            SearchTerm = "First",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };

        Assert.Equal(10, qp.FloorId);
        Assert.Equal(5, qp.YearRangeCVId);
        Assert.Equal("First", qp.SearchTerm);
    }

    [Fact]
    public void CreateDto_Validation_Passes()
    {
        var dto = new CreateAssetFloorFactorCVDto
        {
            FloorId = 10,
            YearRangeCVId = 5,
            FactorWithLift = 1.2m,
            FactorWithoutLift = 1.0m,
            IsActive = true
        };

        var results = new List<SystemValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
    }

    #endregion

    #region Service Operations

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var list = new List<AssetFloorFactorCVEntity>
        {
            new() { Id = 1, FloorId = 10, YearRangeCVId = 5, FactorWithLift = 1.2m, FactorWithoutLift = 1.0m, IsActive = true }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var qp = new AssetFloorFactorCVQueryParameters { FloorId = 10, YearRangeCVId = 5 };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithFloorNavigationSet_PopulatesFloorDescription()
    {
        // ApplyIncludes eager-loads Floor so GetAll can enrich the response with FloorDescription.
        var floor = new FloorEntity { Id = 10, Description = "Ground Floor" };
        var list = new List<AssetFloorFactorCVEntity>
        {
            new() { Id = 1, FloorId = 10, Floor = floor, YearRangeCVId = 5, FactorWithLift = 1.2m, FactorWithoutLift = 1.0m, IsActive = true }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var qp = new AssetFloorFactorCVQueryParameters();

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal("Ground Floor", item.FloorDescription);
    }

    [Fact]
    public async Task GetAllAsync_WithFloorDescriptionNull_ReturnsEmptyStringNotNull()
    {
        // FloorEntity.Description is a nullable string - a loaded Floor whose Description is null
        // must still coalesce to string.Empty rather than leaking null into the DTO.
        var floor = new FloorEntity { Id = 10, Description = null };
        var list = new List<AssetFloorFactorCVEntity>
        {
            new() { Id = 1, FloorId = 10, Floor = floor, YearRangeCVId = 5, FactorWithLift = 1.2m, FactorWithoutLift = 1.0m, IsActive = true }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var qp = new AssetFloorFactorCVQueryParameters();

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(string.Empty, item.FloorDescription);
    }

    [Fact]
    public async Task GetAllAsync_WithoutFloorNavigationSet_FloorDescriptionIsEmpty()
    {
        // Guards the null-conditional in AssetFloorFactorCVMappingProfile - a missing/unloaded
        // Floor navigation must not throw a NullReferenceException during mapping.
        var list = new List<AssetFloorFactorCVEntity>
        {
            new() { Id = 1, FloorId = 10, Floor = null, YearRangeCVId = 5, FactorWithLift = 1.2m, FactorWithoutLift = 1.0m, IsActive = true }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var qp = new AssetFloorFactorCVQueryParameters();

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal(string.Empty, item.FloorDescription);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDto()
    {
        var entity = new AssetFloorFactorCVEntity { Id = 1, FloorId = 10, YearRangeCVId = 5, FactorWithLift = 1.2m, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10, result.FloorId);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsDto()
    {
        var floors = new List<FloorEntity> { new() { Id = 10, IsActive = true } };
        _floorRepoMock.Setup(r => r.GetQueryable()).Returns(floors.BuildMockDbSet().Object);

        var yearRanges = new List<AssetAssessmentYearRangeMasterCVEntity> { new() { Id = 5, MarkedForDeletion = false } };
        _yearRangeRepoMock.Setup(r => r.GetQueryable()).Returns(yearRanges.BuildMockDbSet().Object);

        var existingList = new List<AssetFloorFactorCVEntity>();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<AssetFloorFactorCVEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetFloorFactorCVEntity entity, CancellationToken _) => { entity.Id = 1; return entity; });

        var service = CreateService();
        var dto = new CreateAssetFloorFactorCVDto { FloorId = 10, YearRangeCVId = 5, FactorWithLift = 1.2m, FactorWithoutLift = 1.0m };

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10, result.FloorId);
        Assert.Equal(5, result.YearRangeCVId);
    }

    [Fact]
    public async Task CreateAsync_InvalidFloor_ThrowsValidationException()
    {
        _floorRepoMock.Setup(r => r.GetQueryable()).Returns(new List<FloorEntity>().BuildMockDbSet().Object);

        var service = CreateService();
        var dto = new CreateAssetFloorFactorCVDto { FloorId = 99, YearRangeCVId = 5, FactorWithLift = 1.2m, FactorWithoutLift = 1.0m };

        await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_InvalidYearRangeCV_ThrowsValidationException()
    {
        var floors = new List<FloorEntity> { new() { Id = 10, IsActive = true } };
        _floorRepoMock.Setup(r => r.GetQueryable()).Returns(floors.BuildMockDbSet().Object);
        _yearRangeRepoMock.Setup(r => r.GetQueryable()).Returns(new List<AssetAssessmentYearRangeMasterCVEntity>().BuildMockDbSet().Object);

        var service = CreateService();
        var dto = new CreateAssetFloorFactorCVDto { FloorId = 10, YearRangeCVId = 99, FactorWithLift = 1.2m, FactorWithoutLift = 1.0m };

        await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Duplicate_ThrowsValidationException()
    {
        var floors = new List<FloorEntity> { new() { Id = 10, IsActive = true } };
        _floorRepoMock.Setup(r => r.GetQueryable()).Returns(floors.BuildMockDbSet().Object);

        var yearRanges = new List<AssetAssessmentYearRangeMasterCVEntity> { new() { Id = 5, MarkedForDeletion = false } };
        _yearRangeRepoMock.Setup(r => r.GetQueryable()).Returns(yearRanges.BuildMockDbSet().Object);

        var existingList = new List<AssetFloorFactorCVEntity>
        {
            new() { Id = 1, FloorId = 10, YearRangeCVId = 5, MarkedForDeletion = false }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);

        var service = CreateService();
        var dto = new CreateAssetFloorFactorCVDto { FloorId = 10, YearRangeCVId = 5, FactorWithLift = 1.2m, FactorWithoutLift = 1.0m };

        await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesSuccessfully()
    {
        var existingEntity = new AssetFloorFactorCVEntity { Id = 1, FloorId = 10, YearRangeCVId = 5, FactorWithLift = 1.0m, IsActive = true };
        var list = new List<AssetFloorFactorCVEntity> { existingEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var floors = new List<FloorEntity> { new() { Id = 10, IsActive = true } };
        _floorRepoMock.Setup(r => r.GetQueryable()).Returns(floors.BuildMockDbSet().Object);

        var yearRanges = new List<AssetAssessmentYearRangeMasterCVEntity> { new() { Id = 5, MarkedForDeletion = false } };
        _yearRangeRepoMock.Setup(r => r.GetQueryable()).Returns(yearRanges.BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetFloorFactorCVDto { FloorId = 10, YearRangeCVId = 5, FactorWithLift = 1.5m, FactorWithoutLift = 1.2m, IsActive = true };

        var result = await service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1.5m, result.FactorWithLift);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidFloor_ThrowsValidationException()
    {
        var existingEntity = new AssetFloorFactorCVEntity { Id = 1, FloorId = 10, YearRangeCVId = 5, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _floorRepoMock.Setup(r => r.GetQueryable()).Returns(new List<FloorEntity>().BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetFloorFactorCVDto { FloorId = 99, YearRangeCVId = 5, FactorWithLift = 1.5m, FactorWithoutLift = 1.2m, IsActive = true };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_InvalidYearRange_ThrowsValidationException()
    {
        var existingEntity = new AssetFloorFactorCVEntity { Id = 1, FloorId = 10, YearRangeCVId = 5, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        var floors = new List<FloorEntity> { new() { Id = 10, IsActive = true } };
        _floorRepoMock.Setup(r => r.GetQueryable()).Returns(floors.BuildMockDbSet().Object);
        _yearRangeRepoMock.Setup(r => r.GetQueryable()).Returns(new List<AssetAssessmentYearRangeMasterCVEntity>().BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetFloorFactorCVDto { FloorId = 10, YearRangeCVId = 99, FactorWithLift = 1.5m, FactorWithoutLift = 1.2m, IsActive = true };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateFloorAndYearRange_ThrowsValidationException()
    {
        var existingEntity = new AssetFloorFactorCVEntity { Id = 1, FloorId = 10, YearRangeCVId = 5, IsActive = true };
        var otherEntity = new AssetFloorFactorCVEntity { Id = 2, FloorId = 20, YearRangeCVId = 5, IsActive = true };
        var list = new List<AssetFloorFactorCVEntity> { existingEntity, otherEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var floors = new List<FloorEntity> { new() { Id = 20, IsActive = true } };
        _floorRepoMock.Setup(r => r.GetQueryable()).Returns(floors.BuildMockDbSet().Object);

        var yearRanges = new List<AssetAssessmentYearRangeMasterCVEntity> { new() { Id = 5, MarkedForDeletion = false } };
        _yearRangeRepoMock.Setup(r => r.GetQueryable()).Returns(yearRanges.BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetFloorFactorCVDto { FloorId = 20, YearRangeCVId = 5, FactorWithLift = 1.5m, FactorWithoutLift = 1.2m, IsActive = true };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Deactivation_WithReferences_ThrowsValidationException()
    {
        var existingEntity = new AssetFloorFactorCVEntity { Id = 1, FloorId = 10, YearRangeCVId = 5, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _referenceValidatorMock
            .Setup(x => x.ValidateReferencesAsync<AssetFloorFactorCVEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Id", "In use"));

        var service = CreateService();
        var updateDto = new UpdateAssetFloorFactorCVDto { FloorId = 10, YearRangeCVId = 5, FactorWithLift = 1.5m, FactorWithoutLift = 1.2m, IsActive = false };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesSuccessfully()
    {
        var entity = new AssetFloorFactorCVEntity { Id = 1, FloorId = 10 };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
