using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for PropertyService to achieve 100% code coverage
/// </summary>
public class PropertyServiceComprehensiveTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPropertyRepository> _mockPropertyRepository;
    private readonly PropertyService _service;
    private readonly Mock<ILogger<PropertyService>> _mockLogger;
    private readonly Mock<IOptions<FeatureFlagsOptions>> _mockFeatureFlags;

    public PropertyServiceComprehensiveTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockPropertyRepository = new Mock<IPropertyRepository>();
        _mockLogger = new Mock<ILogger<PropertyService>>();
        _mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();

        // Setup feature flag to allow property deletion without payment validation
        _mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = true
        });

        _service = new PropertyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockPropertyRepository.Object,
            _mockLogger.Object,
            _mockFeatureFlags.Object, new Mock<IRepository<NtisPlatform.Core.Entities.WardEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyCategoryEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.SocietyDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.RoomWiseSubmissionDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyAssessmentEntity, int>>().Object);
    }

    [Fact]
    public async Task GetOldDetailsAsync_CallsRepository()
    {
        var expectedDto = new PropertyOldDetailsDto
        {
            PropertyId = 549357,
            OldWardNo = "10"
        };

        _mockPropertyRepository
            .Setup(r => r.GetOldDetailsAsync(549357, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var result = await _service.GetOldDetailsAsync(549357);

        Assert.NotNull(result);
        Assert.Equal(549357, result.PropertyId);
        Assert.Equal("10", result.OldWardNo);
        _mockPropertyRepository.Verify(r => r.GetOldDetailsAsync(549357, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOldDetailsAsync_WithCancellationToken_PassesToken()
    {
        var cts = new CancellationTokenSource();
        var expectedDto = new PropertyOldDetailsDto { PropertyId = 549357 };

        _mockPropertyRepository
            .Setup(r => r.GetOldDetailsAsync(549357, cts.Token))
            .ReturnsAsync(expectedDto);

        var result = await _service.GetOldDetailsAsync(549357, cts.Token);

        Assert.NotNull(result);
        _mockPropertyRepository.Verify(r => r.GetOldDetailsAsync(549357, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_CallsRepository()
    {
        var dto = new UpdatePropertyOldDetailsDto
        {
            OldWardNo = "10"
        };

        var expectedResult = new PropertyOldDetailsDto
        {
            PropertyId = 549357,
            OldWardNo = "10"
        };

        _mockPropertyRepository
            .Setup(r => r.UpdateOldDetailsAsync(549357, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _service.UpdateOldDetailsAsync(549357, dto);

        Assert.NotNull(result);
        Assert.Equal("10", result.OldWardNo);
        _mockPropertyRepository.Verify(r => r.UpdateOldDetailsAsync(549357, dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOldDetailsAsync_WithCancellationToken_PassesToken()
    {
        var cts = new CancellationTokenSource();
        var dto = new UpdatePropertyOldDetailsDto();
        var expectedResult = new PropertyOldDetailsDto { PropertyId = 549357 };

        _mockPropertyRepository
            .Setup(r => r.UpdateOldDetailsAsync(549357, dto, cts.Token))
            .ReturnsAsync(expectedResult);

        var result = await _service.UpdateOldDetailsAsync(549357, dto, cts.Token);

        Assert.NotNull(result);
        _mockPropertyRepository.Verify(r => r.UpdateOldDetailsAsync(549357, dto, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetSocietyDetailsAsync_CallsRepository()
    {
        var expectedDto = new PropertySocietyDetailsDto
        {
            PropertyId = 549357,
            SocietyName = "ABC Society"
        };

        _mockPropertyRepository
            .Setup(r => r.GetSocietyDetailsAsync(549357, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var result = await _service.GetSocietyDetailsAsync(549357);

        Assert.NotNull(result);
        Assert.Equal("ABC Society", result.SocietyName);
        _mockPropertyRepository.Verify(r => r.GetSocietyDetailsAsync(549357, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSocietyDetailsAsync_CallsRepository()
    {
        var dto = new UpdatePropertySocietyDetailsDto
        {
            SocietyName = "Updated Society"
        };

        var expectedResult = new PropertySocietyDetailsDto
        {
            PropertyId = 549357,
            SocietyName = "Updated Society"
        };

        _mockPropertyRepository
            .Setup(r => r.UpdateSocietyDetailsAsync(549357, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _service.UpdateSocietyDetailsAsync(549357, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated Society", result.SocietyName);
        _mockPropertyRepository.Verify(r => r.UpdateSocietyDetailsAsync(549357, dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetKycDetailsAsync_WithCancellationToken_PassesToken()
    {
        var cts = new CancellationTokenSource();
        var expectedDto = new PropertyKycDetailsDto { PropertyId = 549357 };

        _mockPropertyRepository
            .Setup(r => r.GetKycDetailsAsync(549357, cts.Token))
            .ReturnsAsync(expectedDto);

        var result = await _service.GetKycDetailsAsync(549357, cts.Token);

        Assert.NotNull(result);
        _mockPropertyRepository.Verify(r => r.GetKycDetailsAsync(549357, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateKycDetailsAsync_WithCancellationToken_PassesToken()
    {
        var cts = new CancellationTokenSource();
        var dto = new UpdatePropertyKycDetailsDto();
        var expectedResult = new PropertyKycDetailsDto { PropertyId = 549357 };

        _mockPropertyRepository
            .Setup(r => r.UpdateKycDetailsAsync(549357, dto, cts.Token))
            .ReturnsAsync(expectedResult);

        var result = await _service.UpdateKycDetailsAsync(549357, dto, cts.Token);

        Assert.NotNull(result);
        _mockPropertyRepository.Verify(r => r.UpdateKycDetailsAsync(549357, dto, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetBasicDetailsAsync_WithCancellationToken_PassesToken()
    {
        var cts = new CancellationTokenSource();
        var expectedDto = new PropertyBasicDetailsDto { PropertyId = 549357 };

        _mockPropertyRepository
            .Setup(r => r.GetBasicDetailsAsync(549357, cts.Token))
            .ReturnsAsync(expectedDto);

        var result = await _service.GetBasicDetailsAsync(549357, cts.Token);

        Assert.NotNull(result);
        _mockPropertyRepository.Verify(r => r.GetBasicDetailsAsync(549357, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateBasicDetailsAsync_WithCancellationToken_PassesToken()
    {
        var cts = new CancellationTokenSource();
        var dto = new UpdatePropertyBasicDetailsDto { WardId = 79, TaxZoneId = 10 };
        var expectedResult = new PropertyBasicDetailsDto { PropertyId = 549357 };

        _mockPropertyRepository
            .Setup(r => r.UpdateBasicDetailsAsync(549357, dto, cts.Token))
            .ReturnsAsync(expectedResult);

        var result = await _service.UpdateBasicDetailsAsync(549357, dto, cts.Token);

        Assert.NotNull(result);
        _mockPropertyRepository.Verify(r => r.UpdateBasicDetailsAsync(549357, dto, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetSocietyDetailsAsync_WithCancellationToken_PassesToken()
    {
        var cts = new CancellationTokenSource();
        var expectedDto = new PropertySocietyDetailsDto { PropertyId = 549357 };

        _mockPropertyRepository
            .Setup(r => r.GetSocietyDetailsAsync(549357, cts.Token))
            .ReturnsAsync(expectedDto);

        var result = await _service.GetSocietyDetailsAsync(549357, cts.Token);

        Assert.NotNull(result);
        _mockPropertyRepository.Verify(r => r.GetSocietyDetailsAsync(549357, cts.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateSocietyDetailsAsync_WithCancellationToken_PassesToken()
    {
        var cts = new CancellationTokenSource();
        var dto = new UpdatePropertySocietyDetailsDto();
        var expectedResult = new PropertySocietyDetailsDto { PropertyId = 549357 };

        _mockPropertyRepository
            .Setup(r => r.UpdateSocietyDetailsAsync(549357, dto, cts.Token))
            .ReturnsAsync(expectedResult);

        var result = await _service.UpdateSocietyDetailsAsync(549357, dto, cts.Token);

        Assert.NotNull(result);
        _mockPropertyRepository.Verify(r => r.UpdateSocietyDetailsAsync(549357, dto, cts.Token), Times.Once);
    }
}


