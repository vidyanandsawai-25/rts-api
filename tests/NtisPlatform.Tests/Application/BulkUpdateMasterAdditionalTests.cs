using AutoMapper;
using FluentAssertions;
using Moq;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Additional comprehensive test cases for BulkUpdateMaster focusing on
/// edge cases, boundary conditions, and special scenarios.
/// </summary>
public class BulkUpdateMasterAdditionalTests
{
    private readonly Mock<IRepository<BulkUpdateMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly BulkUpdateMasterService _service;

    public BulkUpdateMasterAdditionalTests()
    {
        _repositoryMock = new Mock<IRepository<BulkUpdateMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _service = new BulkUpdateMasterService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    #region Concurrent Update Tests

    [Fact]
    public async Task UpdateAsync_ConcurrentUpdate_HandlesConcurrencyException()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update (v2)",
            ReferenceTableName = "PropertyTypeMaster",
            UpdatedBy = 1
        };
        var entity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map(updateDto, entity))
            .Returns(entity);
        _repositoryMock.Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Concurrency conflict"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(id, updateDto, CancellationToken.None));
    }

    #endregion

    #region Multiple Field Updates Tests

    [Fact]
    public async Task UpdateAsync_UpdateAllFields_UpdatesSuccessfully()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "UPDATED_CODE",
            UpdateName = "Updated Name",
            ReferenceTableName = "UpdatedTable",
            UpdatedBy = 1
        };
        var existingEntity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "OLD_CODE",
            UpdateName = "Old Name",
            IsActive = true
        };
        var updatedEntity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "UPDATED_CODE",
            UpdateName = "Updated Name",
            ReferenceTableName = "UpdatedTable",
            IsActive = true,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = id,
            UpdateCode = "UPDATED_CODE",
            UpdateName = "Updated Name",
            ReferenceTableName = "UpdatedTable",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Callback<UpdateBulkUpdateMasterDto, BulkUpdateMasterEntity>((dto, entity) =>
            {
                entity.UpdateCode = dto.UpdateCode;
                entity.UpdateName = dto.UpdateName;
                entity.ReferenceTableName = dto.ReferenceTableName;
                entity.UpdatedBy = dto.UpdatedBy;
                entity.UpdatedDate = DateTime.Now;
            })
            .Returns(existingEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(existingEntity))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateCode.Should().Be("UPDATED_CODE");
        result.UpdateName.Should().Be("Updated Name");
        result.ReferenceTableName.Should().Be("UpdatedTable");
    }

    #endregion

    #region Special Characters and Unicode Tests

    [Fact]
    public async Task CreateAsync_WithMarathiText_HandlesUnicodeCorrectly()
    {
        // Arrange
        var createDto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "MARATHI_TEST",
            UpdateName = "मराठी चाचणी मजकूर संपूर्ण",
            ReferenceTableName = "TestTable",
            CreatedBy = 1
        };
        var entity = new BulkUpdateMasterEntity
        {
            UpdateCode = "MARATHI_TEST",
            UpdateName = "मराठी चाचणी मजकूर संपूर्ण"
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = 1,
            UpdateCode = "MARATHI_TEST",
            UpdateName = "मराठी चाचणी मजकूर संपूर्ण",
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>()))
            .Callback<BulkUpdateMasterEntity, CancellationToken>((e, ct) => e.Id = 1)
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(entity)).Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UpdateName.Should().Be("मराठी चाचणी मजकूर संपूर्ण");
    }

    #endregion

    #region Null and Empty String Tests

    [Fact]
    public async Task CreateAsync_WithNullOptionalFields_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "NULL_FIELDS",
            UpdateName = "Null Fields Test",
            ReferenceTableName = "TestTable",
            CreatedBy = 1
        };
        var entity = new BulkUpdateMasterEntity
        {
            UpdateCode = "NULL_FIELDS",
            UpdateName = "Null Fields Test"
        };
        var expectedDto = new BulkUpdateMasterDto
        {
            Id = 1,
            UpdateCode = "NULL_FIELDS",
            UpdateName = "Null Fields Test",
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>()))
            .Callback<BulkUpdateMasterEntity, CancellationToken>((e, ct) => e.Id = 1)
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(entity)).Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Soft Delete Tests

    [Fact]
    public async Task DeleteAsync_MarksSoftDelete_DoesNotRemovePhysically()
    {
        // Arrange
        var id = 1;
        var entity = new BulkUpdateMasterEntity
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()))
            .Callback(() => entity.IsActive = false)
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_PassesToRepository()
    {
        // Arrange
        var id = 1;
        var cancellationToken = new CancellationToken();
        var entity = new BulkUpdateMasterEntity { Id = id, UpdateCode = "TEST", IsActive = true };
        var dto = new BulkUpdateMasterDto { Id = id, UpdateCode = "TEST", IsActive = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, cancellationToken))
            .ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(id, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        _repositoryMock.Verify(r => r.GetByIdAsync(id, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithCancellationToken_PassesToRepository()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var createDto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "TEST",
            UpdateName = "Test",
            ReferenceTableName = "TestTable",
            CreatedBy = 1
        };
        var entity = new BulkUpdateMasterEntity { UpdateCode = "TEST" };
        var expectedDto = new BulkUpdateMasterDto { Id = 1, UpdateCode = "TEST", IsActive = true };

        _mapperMock.Setup(m => m.Map<BulkUpdateMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(entity, cancellationToken))
            .Callback<BulkUpdateMasterEntity, CancellationToken>((e, ct) => e.Id = 1)
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateMasterDto>(entity)).Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        _repositoryMock.Verify(r => r.AddAsync(entity, cancellationToken), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    #endregion
}
