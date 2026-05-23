using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using Moq;
using Xunit;
using AutoMapper;
using NtisPlatform.Application.DTOs.Range;
using Microsoft.EntityFrameworkCore;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for PropertyService to achieve 100% code coverage
/// </summary>
public class PropertyServiceTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPropertyRepository> _mockPropertyRepository;
    private readonly PropertyService _service;

    public PropertyServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockPropertyRepository = new Mock<IPropertyRepository>();

        _service = new PropertyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockPropertyRepository.Object);
    }

    #region GetBasicDetailsAsync Tests

    [Fact]
    public async Task GetBasicDetailsAsync_ReturnsBasicDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyBasicDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetBasicDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetBasicDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
        _mockPropertyRepository.Verify(x => x.GetBasicDetailsAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateBasicDetailsAsync Tests

    [Fact]
    public async Task UpdateBasicDetailsAsync_UpdatesAndReturnsBasicDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertyBasicDetailsDto();
        var expectedDto = new PropertyBasicDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateBasicDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateBasicDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
        _mockPropertyRepository.Verify(x => x.UpdateBasicDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetSocietyDetailsAsync Tests

    [Fact]
    public async Task GetSocietyDetailsAsync_ReturnsSocietyDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertySocietyDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetSocietyDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetSocietyDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateSocietyDetailsAsync Tests

    [Fact]
    public async Task UpdateSocietyDetailsAsync_UpdatesAndReturnsSocietyDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertySocietyDetailsDto();
        var expectedDto = new PropertySocietyDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateSocietyDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateSocietyDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetKycDetailsAsync Tests

    [Fact]
    public async Task GetKycDetailsAsync_ReturnsKycDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyKycDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetKycDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetKycDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateKycDetailsAsync Tests

    [Fact]
    public async Task UpdateKycDetailsAsync_UpdatesAndReturnsKycDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertyKycDetailsDto();
        var expectedDto = new PropertyKycDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateKycDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateKycDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateOldDetailsAsync Tests

    [Fact]
    public async Task UpdateOldDetailsAsync_UpdatesAndReturnsOldDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertyOldDetailsDto();
        var expectedDto = new PropertyOldDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateOldDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateOldDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetOldDetailsAsync Tests

    [Fact]
    public async Task GetOldDetailsAsync_ReturnsOldDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyOldDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetOldDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetOldDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetTaxDetailsAsync Tests

    [Fact]
    public async Task GetTaxDetailsAsync_ReturnsTaxDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyTaxDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetTaxDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetTaxDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetTaxDetailsCVAsync Tests

    [Fact]
    public async Task GetTaxDetailsCVAsync_ReturnsTaxDetailsCV()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyTaxDetailsCVDto();
        _mockPropertyRepository
            .Setup(x => x.GetTaxDetailsCVAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetTaxDetailsCVAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetOldTaxesDetailsAsync Tests

    [Fact]
    public async Task GetOldTaxesDetailsAsync_ReturnsOldTaxesDetails()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyOldTaxesDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetOldTaxesDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetOldTaxesDetailsAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateOldTaxesDetailsAsync Tests

    [Fact]
    public async Task UpdateOldTaxesDetailsAsync_UpdatesAndReturnsOldTaxesDetails()
    {
        // Arrange
        var propertyId = 1;
        var dto = new UpdatePropertyOldTaxesDetailsDto();
        var expectedDto = new PropertyOldTaxesDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateOldTaxesDetailsAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateOldTaxesDetailsAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetFloorDetailsOldAsync Tests

    [Fact]
    public async Task GetFloorDetailsOldAsync_ReturnsFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var expectedDto = new PropertyDetailsOldListDto();
        _mockPropertyRepository
            .Setup(x => x.GetFloorDetailsOldAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetFloorDetailsOldAsync(propertyId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetFloorDetailsOldByIdAsync Tests

    [Fact]
    public async Task GetFloorDetailsOldByIdAsync_ReturnsFloorDetailsOldById()
    {
        // Arrange
        var propertyId = 1;
        var floorId = 2;
        var expectedDto = new PropertyDetailsOldDto();
        _mockPropertyRepository
            .Setup(x => x.GetFloorDetailsOldByIdAsync(propertyId, floorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetFloorDetailsOldByIdAsync(propertyId, floorId);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region AddFloorDetailsOldAsync Tests

    [Fact]
    public async Task AddFloorDetailsOldAsync_AddsAndReturnsFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var dto = new AddPropertyDetailsOldDto();
        var expectedDto = new PropertyDetailsOldDto();
        _mockPropertyRepository
            .Setup(x => x.AddFloorDetailsOldAsync(propertyId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.AddFloorDetailsOldAsync(propertyId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region UpdateFloorDetailsOldAsync Tests

    [Fact]
    public async Task UpdateFloorDetailsOldAsync_UpdatesAndReturnsFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var floorId = 2;
        var dto = new UpdatePropertyDetailsOldDto();
        var expectedDto = new PropertyDetailsOldDto();
        _mockPropertyRepository
            .Setup(x => x.UpdateFloorDetailsOldAsync(propertyId, floorId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.UpdateFloorDetailsOldAsync(propertyId, floorId, dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region DeleteFloorDetailsOldAsync Tests

    [Fact]
    public async Task DeleteFloorDetailsOldAsync_DeletesFloorDetailsOld()
    {
        // Arrange
        var propertyId = 1;
        var floorId = 2;
        _mockPropertyRepository
            .Setup(x => x.DeleteFloorDetailsOldAsync(propertyId, floorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteFloorDetailsOldAsync(propertyId, floorId);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region GetApartmentPropertyTaxDetailsAsync Tests

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsAsync_ReturnsApartmentPropertyTaxDetails()
    {
        // Arrange
        var dto = new PropertyApartmentTaxRequestDto();
        var expectedDto = new PropertyTaxApartmentDetailsDto();
        _mockPropertyRepository
            .Setup(x => x.GetApartmentPropertyTaxDetailsAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetApartmentPropertyTaxDetailsAsync(dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetApartmentPropertyTaxDetailsCVAsync Tests

    [Fact]
    public async Task GetApartmentPropertyTaxDetailsCVAsync_ReturnsApartmentPropertyTaxDetailsCV()
    {
        // Arrange
        var dto = new PropertyApartmentTaxRequestDto();
        var expectedDto = new PropertyTaxApartmentDetailsCVDto();
        _mockPropertyRepository
            .Setup(x => x.GetApartmentPropertyTaxDetailsCVAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _service.GetApartmentPropertyTaxDetailsCVAsync(dto);

        // Assert
        Assert.Equal(expectedDto, result);
    }

    #endregion

    #region GetGenerateBuildingStructureAsync Tests

    [Fact]
    public async Task GetGenerateBuildingStructureAsync_ReturnsGenerateBuildingStructure()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto();
        var expectedList = new List<BuildingGenerateStructureDto>();
        _mockPropertyRepository
            .Setup(x => x.GetGenerateBuildingStructureAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // Act
        var result = await _service.GetGenerateBuildingStructureAsync(dto);

        // Assert
        Assert.Equal(expectedList, result);
    }

    #endregion

    #region CreatePropertiesFromRangeAsync Tests

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.CreatePropertiesFromRangeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithNullTemplate_ReturnsError()
    {
        // Arrange
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = null,
            RangeFrom = "1",
            RangeTo = "3"
        };

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains("Template cannot be null.", result.Errors);
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithValidRequest_CreatesProperties()
    {
        // Arrange
        // Note: RangeFrom and RangeTo must be numeric values without prefix/suffix
        // because the code uses Convert.ToInt32(rangeValues[i]) for PropertySeqNo
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2",
            Prefix = null,
            Suffix = null,
            StartSequenceNo = 1
        };

        var response1 = new CreateNewPropertyResponseDto { Success = true };
        var response2 = new CreateNewPropertyResponseDto { Success = true };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response1);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithCancellationRequested_RollsBackAndReturnsError()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, cts.Token);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Operation cancelled"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithEmptyRangeValue_ThrowsArgumentException()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "",
            RangeTo = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithFailedResponse_RollsBackAndReturnsError()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        var response = new CreateNewPropertyResponseDto { Success = false, Message = "Property already exists" };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockPropertyRepository
            .Setup(x => x.IsPropertyExists(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithNullResponse_ReturnsError()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateNewPropertyResponseDto?)null);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Repository returned null response"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithDbUpdateException_ReturnsError()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Database error"));

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Database error"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithOperationCanceledException_ReturnsError()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Operation cancelled"));

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Operation cancelled"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithArgumentException_ReturnsError()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid argument"));

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Invalid argument"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithGenericException_ReturnsError()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid operation"));

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Invalid operation"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithRollbackFailure_IncludesRollbackError()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Rollback failed"));

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Rollback error"));
    }

    [Fact]
    public async Task CreatePropertiesFromRangeAsync_WithPropertyExists_IncludesProperMessage()
    {
        // Arrange
        var template = new CreateNewPropertyDto { WardId = 1 };
        var request = new RangeCreateRequest<CreateNewPropertyDto>
        {
            Template = template,
            RangeFrom = "1",
            RangeTo = "2"
        };

        var response = new CreateNewPropertyResponseDto { Success = false, Message = "Property already exists" };

        _mockPropertyRepository
            .Setup(x => x.CreateNewPropertyAsync(It.IsAny<CreateNewPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockPropertyRepository
            .Setup(x => x.IsPropertyExists(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(true);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
    }

    #endregion

    #region BulkCreateAsync Tests

    [Fact]
    public async Task BulkCreateAsync_WithEmptyArray_ReturnsEmptyResult()
    {
        // Arrange
        var items = Array.Empty<CreateBulkPropertyDto>();

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task BulkCreateAsync_WithValidItems_ReturnsSuccessResult()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-002", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var response1 = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };
        var response2 = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 2 };

        _mockPropertyRepository
            .SetupSequence(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response1)
            .ReturnsAsync(response2);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(2, result.Results.Count);
        Assert.True(result.AllSucceeded);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task BulkCreateAsync_WithEmptyPropertyNo_RollsBackAndReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("PropertyNo is required"));
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithWhitespacePropertyNo_RollsBackAndReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "   ", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("PropertyNo is required"));
    }

    [Fact]
    public async Task BulkCreateAsync_WithFailedRepositoryResponse_RollsBackAndReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var response = new CreateBulkPropertyResponseDto { Success = false, Message = "Property already exists" };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Property already exists"));
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithNullRepositoryResponse_RollsBackAndReturnsError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBulkPropertyResponseDto?)null);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithException_RollsBackAndReturnsTransactionError()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Transaction failed"));
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithPartialSuccess_RollsBackOnFirstFailure()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-002", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var successResponse = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };
        var failureResponse = new CreateBulkPropertyResponseDto { Success = false, Message = "Duplicate property" };

        _mockPropertyRepository
            .SetupSequence(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResponse)
            .ReturnsAsync(failureResponse);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(2, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate property"));
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_WithSingleItem_ReturnsSuccessResult()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var response = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Single(result.Results);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_WithMultipleItems_CommitsOnAllSuccess()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-002", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 },
            new CreateBulkPropertyDto { PropertyNo = "PROP-003", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var response = new CreateBulkPropertyResponseDto { Success = true, PropertyId = 1 };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(3, result.Results.Count);
        Assert.Null(result.Errors);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_FailureResponseWithNullMessage_HandlesGracefully()
    {
        // Arrange
        var items = new[]
        {
            new CreateBulkPropertyDto { PropertyNo = "PROP-001", WardId = 1, TaxZoneId = 1, PropertyTypeId = 1, CategoryId = 1 }
        };

        var response = new CreateBulkPropertyResponseDto { Success = false, Message = null };

        _mockPropertyRepository
            .Setup(x => x.CreateBulkPropertyAsync(It.IsAny<CreateBulkPropertyDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Unknown error"));
    }

    #endregion
}
