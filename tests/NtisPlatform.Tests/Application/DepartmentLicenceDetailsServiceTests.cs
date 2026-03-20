using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.DepartmentLicenceDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for DepartmentLicenceDetailsService
/// </summary>
public class DepartmentLicenceDetailsServiceTests
{
    private readonly Mock<IRepository<DepartmentLicenceDetailsEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly DepartmentLicenceDetailsService _service;

    public DepartmentLicenceDetailsServiceTests()
    {
        _repositoryMock = new Mock<IRepository<DepartmentLicenceDetailsEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new DepartmentLicenceDetailsService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = new DepartmentLicenceDetailsEntity
        {
            DepartmentLicenceId = 1,
            DepartmentMasterId = 1,
            LicenceStartDate = new DateTime(2025, 1, 1),
            LicenceEndDate = new DateTime(2026, 1, 1),
            LicenceDuration = "1 Year",
            IsActive = true
        };

        var dto = new DepartmentLicenceDetailsDto
        {
            DepartmentLicenceId = 1,
            DepartmentMasterId = 1,
            LicenceStartDate = new DateTime(2025, 1, 1),
            LicenceEndDate = new DateTime(2026, 1, 1),
            LicenceDuration = "1 Year",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<DepartmentLicenceDetailsDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DepartmentLicenceId);
        Assert.Equal(1, result.DepartmentMasterId);
        Assert.Equal("1 Year", result.LicenceDuration);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentLicenceDetailsEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAndReturnsDto()
    {
        // Arrange
        var createDto = new CreateDepartmentLicenceDetailsDto
        {
            DepartmentMasterId = 1,
            LicenceStartDate = new DateTime(2025, 1, 1),
            LicenceEndDate = new DateTime(2026, 1, 1),
            LicenceDuration = "1 Year",
            IsActive = true
        };

        var entity = new DepartmentLicenceDetailsEntity
        {
            DepartmentLicenceId = 1,
            DepartmentMasterId = 1,
            LicenceStartDate = new DateTime(2025, 1, 1),
            LicenceEndDate = new DateTime(2026, 1, 1),
            LicenceDuration = "1 Year",
            IsActive = true
        };

        var returnDto = new DepartmentLicenceDetailsDto
        {
            DepartmentLicenceId = 1,
            DepartmentMasterId = 1,
            LicenceStartDate = new DateTime(2025, 1, 1),
            LicenceEndDate = new DateTime(2026, 1, 1),
            LicenceDuration = "1 Year",
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<DepartmentLicenceDetailsEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<DepartmentLicenceDetailsDto>(entity))
            .Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DepartmentMasterId);
        Assert.Equal("1 Year", result.LicenceDuration);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<DepartmentLicenceDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdateDepartmentLicenceDetailsDto
        {
            DepartmentMasterId = 1,
            LicenceStartDate = new DateTime(2025, 1, 1),
            LicenceEndDate = new DateTime(2027, 1, 1), // Extended
            LicenceDuration = "2 Years",
            IsActive = true
        };

        var existingEntity = new DepartmentLicenceDetailsEntity
        {
            DepartmentLicenceId = 1,
            DepartmentMasterId = 1,
            LicenceStartDate = new DateTime(2025, 1, 1),
            LicenceEndDate = new DateTime(2026, 1, 1),
            LicenceDuration = "1 Year"
        };

        var returnDto = new DepartmentLicenceDetailsDto
        {
            DepartmentLicenceId = 1,
            DepartmentMasterId = 1,
            LicenceStartDate = new DateTime(2025, 1, 1),
            LicenceEndDate = new DateTime(2027, 1, 1),
            LicenceDuration = "2 Years",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity))
            .Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<DepartmentLicenceDetailsDto>(existingEntity))
            .Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2 Years", result.LicenceDuration);
        Assert.Equal(new DateTime(2027, 1, 1), result.LicenceEndDate);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var entity = new DepartmentLicenceDetailsEntity
        {
            DepartmentLicenceId = 1,
            DepartmentMasterId = 1
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Business Logic Tests


    #endregion
}
