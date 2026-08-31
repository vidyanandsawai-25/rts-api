using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class AliasMasterServiceTests
{
    private readonly Mock<IRepository<AliasMasterEntity, int>> _aliasRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly IMapper _mapper;
    private readonly AliasMasterService _service;

    public AliasMasterServiceTests()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<AliasMasterMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = mapperConfig.CreateMapper();

        _service = new AliasMasterService(_aliasRepoMock.Object, _unitOfWorkMock.Object, _mapper, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetCountsAsync_ReturnsCorrectTotalActiveAndInactiveCounts()
    {
        // Arrange
        var entities = new List<AliasMasterEntity>
        {
            new() { Id = 1, KeyName = "Field1", IsActive = true },
            new() { Id = 2, KeyName = "Field2", IsActive = true },
            new() { Id = 3, KeyName = "Field3", IsActive = false }
        };

        var mockQueryable = entities.BuildMock();
        _aliasRepoMock.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetCountsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.ActiveCount);
        Assert.Equal(1, result.InactiveCount);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResultsWithFilters()
    {
        // Arrange
        var entities = new List<AliasMasterEntity>
        {
            new() { Id = 1, KeyName = "PropertyNo", LabelName = "Property Number", EnglishName = "Prop No", RegionalName = "", HindiName = "", IsActive = true },
            new() { Id = 2, KeyName = "ApplicantName", LabelName = "Applicant Name", EnglishName = "App Name", RegionalName = "", HindiName = "", IsActive = true },
            new() { Id = 3, KeyName = "TaxAmount", LabelName = "Tax Amount", EnglishName = "Tax", RegionalName = "", HindiName = "", IsActive = false }
        };

        var mockQueryable = entities.BuildMock();
        _aliasRepoMock.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new AliasMasterQueryParameters
        {
            SearchTerm = "Property",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("PropertyNo", result.Items.First().KeyName);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenRecordExists()
    {
        // Arrange
        var entity = new AliasMasterEntity { Id = 42, KeyName = "Key42", LabelName = "Label42", IsActive = true };
        _aliasRepoMock.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Key42", result.KeyName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRecordDoesNotExist()
    {
        // Arrange
        _aliasRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((AliasMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationException_WhenKeyNameExists()
    {
        // Arrange
        var entities = new List<AliasMasterEntity>
        {
            new() { Id = 1, KeyName = "ExistingKey", LabelName = "Existing Label" }
        };
        var mockQueryable = entities.BuildMock();
        _aliasRepoMock.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var dto = new CreateAliasMasterDto { KeyName = "ExistingKey", LabelName = "New Label" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.CreateAsync(dto));
        Assert.True(exception.Errors.ContainsKey(nameof(CreateAliasMasterDto.KeyName)));
        Assert.Contains("already has an alias record", exception.Errors[nameof(CreateAliasMasterDto.KeyName)]);
    }

    [Fact]
    public async Task CreateAsync_SuccessfullyCreatesRecord_WhenKeyNameIsUnique()
    {
        // Arrange
        var entities = new List<AliasMasterEntity>();
        var mockQueryable = entities.BuildMock();
        _aliasRepoMock.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _currentUserServiceMock.Setup(u => u.GetCurrentUserId()).Returns(100);

        var dto = new CreateAliasMasterDto { KeyName = "UniqueKey", LabelName = "Unique Label" };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UniqueKey", result.KeyName);
        Assert.Equal("Unique Label", result.LabelName);
        Assert.True(result.IsActive);
        _aliasRepoMock.Verify(r => r.AddAsync(It.Is<AliasMasterEntity>(e => e.KeyName == "UniqueKey" && e.CreatedBy == 100), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenRecordDoesNotExist()
    {
        // Arrange
        _aliasRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((AliasMasterEntity?)null);
        var dto = new UpdateAliasMasterDto { LabelName = "Updated" };

        // Act
        var result = await _service.UpdateAsync(999, dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_SuccessfullyUpdatesRecord_WhenRecordExists()
    {
        // Arrange
        var entity = new AliasMasterEntity { Id = 5, KeyName = "Key5", LabelName = "Label5", IsActive = true };
        _aliasRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _currentUserServiceMock.Setup(u => u.GetCurrentUserId()).Returns(200);

        var dto = new UpdateAliasMasterDto
        {
            LabelName = "New Label",
            EnglishName = "New Eng",
            RegionalName = "New Reg",
            HindiName = "New Hin",
            IsActive = false
        };

        // Act
        var result = await _service.UpdateAsync(5, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Label", entity.LabelName);
        Assert.Equal("New Eng", entity.EnglishName);
        Assert.Equal("New Reg", entity.RegionalName);
        Assert.Equal("New Hin", entity.HindiName);
        Assert.False(entity.IsActive);
        Assert.Equal(200, entity.UpdatedBy);

        _aliasRepoMock.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetActiveStatusAsync_ReturnsFalse_WhenRecordDoesNotExist()
    {
        // Arrange
        _aliasRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((AliasMasterEntity?)null);

        // Act
        var result = await _service.SetActiveStatusAsync(999, true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetActiveStatusAsync_ReturnsTrueWithoutSaving_WhenStatusIsSame()
    {
        // Arrange
        var entity = new AliasMasterEntity { Id = 10, KeyName = "Key10", IsActive = true };
        _aliasRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.SetActiveStatusAsync(10, true);

        // Assert
        Assert.True(result);
        _aliasRepoMock.Verify(r => r.UpdateAsync(It.IsAny<AliasMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetActiveStatusAsync_SuccessfullyTogglesStatus_WhenStatusIsDifferent()
    {
        // Arrange
        var entity = new AliasMasterEntity { Id = 10, KeyName = "Key10", IsActive = true };
        _aliasRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _currentUserServiceMock.Setup(u => u.GetCurrentUserId()).Returns(300);

        // Act
        var result = await _service.SetActiveStatusAsync(10, false);

        // Assert
        Assert.True(result);
        Assert.False(entity.IsActive);
        Assert.Equal(300, entity.UpdatedBy);
        _aliasRepoMock.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveAliasesAsync_ReturnsOnlyActiveAliases()
    {
        // Arrange
        var entities = new List<AliasMasterEntity>
        {
            new() { Id = 1, KeyName = "ActiveKey", IsActive = true, EnglishName = "Active Eng" },
            new() { Id = 2, KeyName = "InactiveKey", IsActive = false, EnglishName = "Inactive Eng" }
        };
        var mockQueryable = entities.BuildMock();
        _aliasRepoMock.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetActiveAliasesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("ActiveKey", result[0].KeyName);
        Assert.Equal("Active Eng", result[0].EnglishName);
    }
}
