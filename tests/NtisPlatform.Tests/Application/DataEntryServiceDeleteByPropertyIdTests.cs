using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Tests for DataEntryService.DeleteByPropertyIdAsync
/// Verifies transactional cascade-delete of PropertyDetails and all children by PropertyId.
/// </summary>
public class DataEntryServiceDeleteByPropertyIdTests
{
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IRenterDetailService> _renterDetailServiceMock;
    private readonly Mock<IRenterMastService> _renterMastServiceMock;
    private readonly Mock<IRoomWiseSubmissionDetailsService> _roomWiseServiceMock;
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepositoryMock;
    private readonly Mock<IRepository<PropertyCertificateEntity, int>> _propertyCertificateRepositoryMock;
    private readonly Mock<IPropertyRuleApplicationLogService> _ruleLogServiceMock;
    private readonly DataEntryService _service;

    public DataEntryServiceDeleteByPropertyIdTests()
    {
        _repositoryMock = new Mock<IRepository<PropertyDetailsEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _renterDetailServiceMock = new Mock<IRenterDetailService>();
        _renterMastServiceMock = new Mock<IRenterMastService>();
        _roomWiseServiceMock = new Mock<IRoomWiseSubmissionDetailsService>();
        _propertyRepositoryMock = new Mock<IRepository<PropertyEntity, int>>();
        _propertyCertificateRepositoryMock = new Mock<IRepository<PropertyCertificateEntity, int>>();
        _ruleLogServiceMock = new Mock<IPropertyRuleApplicationLogService>();

        _service = new DataEntryService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _renterDetailServiceMock.Object,
            _renterMastServiceMock.Object,
            _roomWiseServiceMock.Object,
            _propertyRepositoryMock.Object,
            _propertyCertificateRepositoryMock.Object,
            _ruleLogServiceMock.Object);

        // Default UnitOfWork setups
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #region DeleteByPropertyIdAsync — No Records

    [Fact]
    public async Task DeleteByPropertyIdAsync_NoPropertyDetails_ReturnsFalse()
    {
        // Arrange
        var emptyList = new List<PropertyDetailsEntity>().BuildMock();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(emptyList);

        // Act
        var result = await _service.DeleteByPropertyIdAsync(999);

        // Assert
        Assert.False(result);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteByPropertyIdAsync — Single PropertyDetails

    [Fact]
    public async Task DeleteByPropertyIdAsync_SinglePropertyDetails_DeletesAllChildrenAndReturnsTrue()
    {
        // Arrange
        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 5, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _renterDetailServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _renterMastServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roomWiseServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ruleLogServiceMock.Setup(s => s.DeleteByPropertyDetailsIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteByPropertyIdAsync(5);

        // Assert
        Assert.True(result);

        // Parent deleted
        _repositoryMock.Verify(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Once);

        // All child services called with entity.Id (10)
        _renterDetailServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _renterMastServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _ruleLogServiceMock.Verify(s => s.DeleteByPropertyDetailsIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);

        // Transaction lifecycle
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteByPropertyIdAsync — Multiple PropertyDetails

    [Fact]
    public async Task DeleteByPropertyIdAsync_MultiplePropertyDetails_DeletesEachWithChildren()
    {
        // Arrange
        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 5, IsActive = true },
            new() { Id = 11, PropertyId = 5, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _renterDetailServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _renterMastServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roomWiseServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ruleLogServiceMock.Setup(s => s.DeleteByPropertyDetailsIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteByPropertyIdAsync(5);

        // Assert
        Assert.True(result);

        // Both parents deleted
        _repositoryMock.Verify(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(11, It.IsAny<CancellationToken>()), Times.Once);

        // Child services called for each entity
        _renterDetailServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _renterDetailServiceMock.Verify(s => s.DeleteByPropertyIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        _renterMastServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _renterMastServiceMock.Verify(s => s.DeleteByPropertyIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseServiceMock.Verify(s => s.DeleteByPropertyIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);

        // SaveChanges called for each entity
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region DeleteByPropertyIdAsync — Inactive Records Filtered

    [Fact]
    public async Task DeleteByPropertyIdAsync_InactiveRecords_AreIgnored()
    {
        // Arrange — one active, one inactive
        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 5, IsActive = true },
            new() { Id = 11, PropertyId = 5, IsActive = false }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _renterDetailServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _renterMastServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roomWiseServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ruleLogServiceMock.Setup(s => s.DeleteByPropertyDetailsIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteByPropertyIdAsync(5);

        // Assert — only entity 10 processed
        Assert.True(result);
        _repositoryMock.Verify(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(11, It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteByPropertyIdAsync — Transaction Rollback On Failure

    [Fact]
    public async Task DeleteByPropertyIdAsync_OnException_RollsBackAndRethrows()
    {
        // Arrange
        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 5, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());

        // Simulate failure during parent delete
        _repositoryMock.Setup(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteByPropertyIdAsync(5));

        // Verify rollback was called
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteByPropertyIdAsync_OnChildServiceFailure_RollsBackAndRethrows()
    {
        // Arrange
        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 5, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Simulate failure in a child service
        _renterDetailServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Child service failure"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.DeleteByPropertyIdAsync(5));

        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteByPropertyIdAsync — Without RuleLogService

    [Fact]
    public async Task DeleteByPropertyIdAsync_WithoutRuleLogService_SkipsRuleLogDeletion()
    {
        // Arrange — create service without ruleLogService
        var serviceWithoutRuleLog = new DataEntryService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _renterDetailServiceMock.Object,
            _renterMastServiceMock.Object,
            _roomWiseServiceMock.Object,
            _propertyRepositoryMock.Object,
            _propertyCertificateRepositoryMock.Object,
            ruleLogService: null);

        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 5, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _renterDetailServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _renterMastServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roomWiseServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await serviceWithoutRuleLog.DeleteByPropertyIdAsync(5);

        // Assert — should succeed without calling rule log service
        Assert.True(result);
        _ruleLogServiceMock.Verify(
            s => s.DeleteByPropertyDetailsIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteByPropertyIdAsync — IsOpenPlot Filtered

    [Fact]
    public async Task DeleteByPropertyIdAsync_WithIsOpenPlotTrue_DoesNotDeletePlotRecordOrRoomWiseService()
    {
        // Arrange — one plot record (IsOpenPlot = true), one normal record (IsOpenPlot = false)
        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 5, IsActive = true, IsOpenPlot = true },
            new() { Id = 11, PropertyId = 5, IsActive = true, IsOpenPlot = false }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _renterDetailServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _renterMastServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roomWiseServiceMock.Setup(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ruleLogServiceMock.Setup(s => s.DeleteByPropertyDetailsIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteByPropertyIdAsync(5);

        // Assert — returns true because entity 11 was deleted
        Assert.True(result);

        // Plot record (Id = 10) was NOT deleted
        _repositoryMock.Verify(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Never);
        _renterDetailServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Never);
        _renterMastServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Never);
        _roomWiseServiceMock.Verify(s => s.DeleteByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Never);

        // Non-plot record (Id = 11) WAS deleted (including cascade deletes)
        _repositoryMock.Verify(r => r.DeleteAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        _renterDetailServiceMock.Verify(s => s.DeleteByPropertyIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        _renterMastServiceMock.Verify(s => s.DeleteByPropertyIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseServiceMock.Verify(s => s.DeleteByPropertyIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        _ruleLogServiceMock.Verify(s => s.DeleteByPropertyDetailsIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteByPropertyIdAsync_AllRecordsAreOpenPlot_ReturnsFalseAndDeletesNothing()
    {
        // Arrange — all records have IsOpenPlot = true
        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 10, PropertyId = 5, IsActive = true, IsOpenPlot = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());

        // Act
        var result = await _service.DeleteByPropertyIdAsync(5);

        // Assert — returns false as no records were deleted
        Assert.False(result);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomWiseServiceMock.Verify(s => s.DeleteByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
