using AutoMapper;
using FluentAssertions;
using Moq;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class BulkUpdateMasterServiceTests
{
    private readonly Mock<IRepository<BulkUpdateMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly BulkUpdateMasterService _service;

    public BulkUpdateMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<BulkUpdateMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _service = new BulkUpdateMasterService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsBulkUpdateMasterDto()
    {
        // Arrange
        var id = 1;
        var entity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(entity))
            .Returns(expectedDto);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = 999;
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedBulkUpdateMasterDto()
    {
        // Arrange
        var createDto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "WARD_UPDATE",
            UpdateName = "Ward Bulk Update",
            ReferenceTableName = "WardMaster",
            CreatedBy = 1
        };
        var entity = new BulkUpdateMasterEntity
        {
            Id = 0,
            UpdateCode = "WARD_UPDATE",
            UpdateName = "Ward Bulk Update",
            ReferenceTableName = "WardMaster",
            CreatedBy = 1
        };
        var savedEntity = new BulkUpdateMasterEntity
        {
            Id = 2,
            UpdateCode = "WARD_UPDATE",
            UpdateName = "Ward Bulk Update",
            ReferenceTableName = "WardMaster",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = 2,
            UpdateCode = "WARD_UPDATE",
            UpdateName = "Ward Bulk Update",
            ReferenceTableName = "WardMaster",
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(It.IsAny<BulkUpdateMasterEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(2);
        result.UpdateCode.Should().Be("WARD_UPDATE");
        result.UpdateName.Should().Be("Ward Bulk Update");
        result.ReferenceTableName.Should().Be("WardMaster");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUpdateCode_ThrowsException()
    {
        // Arrange
        var createDto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE", // Already exists
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            CreatedBy = 1
        };
        var entity = new BulkUpdateMasterEntity { UpdateCode = "PROP_TYPE" };

        _mapperMock.Setup(m => m.Map<BulkUpdateMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate UpdateCode"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithRequiredFieldsOnly_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "ZONE_UPDATE",
            UpdateName = "Zone Update",
            ReferenceTableName = "ZoneMaster",
            IsApprovalRequired = null,
            CreatedBy = 1
        };
        var entity = new BulkUpdateMasterEntity
        {
            UpdateCode = "ZONE_UPDATE",
            UpdateName = "Zone Update",
            ReferenceTableName = "ZoneMaster",
            CreatedBy = 1
        };
        var savedEntity = new BulkUpdateMasterEntity
        {
            Id = 3,
            UpdateCode = "ZONE_UPDATE",
            UpdateName = "Zone Update",
            ReferenceTableName = "ZoneMaster",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = 3,
            UpdateCode = "ZONE_UPDATE",
            UpdateName = "Zone Update",
            ReferenceTableName = "ZoneMaster",
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(It.IsAny<BulkUpdateMasterEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(3);
        result.UpdateCode.Should().Be("ZONE_UPDATE");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidIdAndDto_ReturnsUpdatedBulkUpdateMasterDto()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update (Modified)",
            ReferenceTableName = "PropertyTypeMaster",
            UpdatedBy = 1
        };
        var existingEntity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true
        };
        var updatedEntity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update (Modified)",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update (Modified)",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(It.IsAny<BulkUpdateMasterEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateName.Should().Be("Property Type Update (Modified)");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "NON_EXISTENT",
            UpdateName = "Non Existent",
            ReferenceTableName = "NonExistentTable",
            UpdatedBy = 1
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangeReferenceTableName_UpdatesSuccessfully()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMasterV2", // Changed
            UpdatedBy = 1
        };
        var existingEntity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true
        };
        var updatedEntity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            ReferenceTableName = "PropertyTypeMasterV2",
            IsActive = true,
            UpdatedBy = 1
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            ReferenceTableName = "PropertyTypeMasterV2",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(It.IsAny<BulkUpdateMasterEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ReferenceTableName.Should().Be("PropertyTypeMasterV2");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrueAndSoftDeletes()
    {
        // Arrange
        var id = 1;
        var entity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var id = 999;

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases and Business Logic Tests

    [Fact]
    public async Task CreateAsync_WithLongUpdateName_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "COMPLEX_UPDATE",
            UpdateName = new string('A', 200), // Long update name
            ReferenceTableName = "ComplexTable",
            CreatedBy = 1
        };
        var entity = new BulkUpdateMasterEntity
        {
            UpdateCode = "COMPLEX_UPDATE",
            UpdateName = new string('A', 200),
            ReferenceTableName = "ComplexTable",
            CreatedBy = 1
        };
        var savedEntity = new BulkUpdateMasterEntity
        {
            Id = 4,
            UpdateCode = "COMPLEX_UPDATE",
            UpdateName = new string('A', 200),
            ReferenceTableName = "ComplexTable",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = 4,
            UpdateCode = "COMPLEX_UPDATE",
            UpdateName = new string('A', 200),
            ReferenceTableName = "ComplexTable",
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(It.IsAny<BulkUpdateMasterEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UpdateName.Should().HaveLength(200);
    }

    #endregion
}
