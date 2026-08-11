using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Tests for RoomWiseSubmissionDetailsService.DeleteByPropertyIdAsync
/// Verifies that soft-deleting by PropertyDetailsId also cascades to child RoomWiseMinusData records.
/// </summary>
public class RoomWiseSubmissionDetailsServiceDeleteTests
{
    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>> _repositoryMock;
    private readonly Mock<IRepository<RoomWiseMinusDataEntity, int>> _roomWiseMinusRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly RoomWiseSubmissionDetailsService _service;

    public RoomWiseSubmissionDetailsServiceDeleteTests()
    {
        _repositoryMock = new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();
        _roomWiseMinusRepositoryMock = new Mock<IRepository<RoomWiseMinusDataEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();

        _service = new RoomWiseSubmissionDetailsService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _roomWiseMinusRepositoryMock.Object);
    }

    #region DeleteByPropertyIdAsync — No Records

    [Fact]
    public async Task DeleteByPropertyIdAsync_NoSubmissions_DoesNotDeleteAnything()
    {
        // Arrange
        var emptyList = new List<RoomWiseSubmissionDetailsEntity>().BuildMock();

        _repositoryMock.Setup(r => r.GetQueryable()).Returns(emptyList);

        // Act
        await _service.DeleteByPropertyIdAsync(999);

        // Assert — no delete calls should have been made
        _repositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _roomWiseMinusRepositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteByPropertyIdAsync — With Submissions, No Minus Data

    [Fact]
    public async Task DeleteByPropertyIdAsync_WithSubmissions_NoMinusData_DeletesSubmissionsOnly()
    {
        // Arrange
        var submissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 10, PropertyDetailsId = 5, IsActive = true },
            new() { Id = 11, PropertyDetailsId = 5, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(submissions.BuildMock());

        var emptyMinus = new List<RoomWiseMinusDataEntity>().BuildMock();
        _roomWiseMinusRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(emptyMinus);

        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteByPropertyIdAsync(5);

        // Assert — both submissions soft-deleted, no minus deletions
        _repositoryMock.Verify(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseMinusRepositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteByPropertyIdAsync — With Submissions And Minus Data

    [Fact]
    public async Task DeleteByPropertyIdAsync_WithMinusData_DeletesMinusRecordsBeforeSubmissions()
    {
        // Arrange
        var submissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 10, PropertyDetailsId = 5, IsActive = true }
        };

        var minusRecords = new List<RoomWiseMinusDataEntity>
        {
            new() { Id = 100, RoomWiseSubmissionId = 10, IsActive = true },
            new() { Id = 101, RoomWiseSubmissionId = 10, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(submissions.BuildMock());

        _roomWiseMinusRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(minusRecords.BuildMock());

        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roomWiseMinusRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteByPropertyIdAsync(5);

        // Assert — both minus records and parent submission deleted
        _roomWiseMinusRepositoryMock.Verify(r => r.DeleteAsync(100, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseMinusRepositoryMock.Verify(r => r.DeleteAsync(101, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteByPropertyIdAsync_MultipleSubmissions_EachCascadesToOwnMinusData()
    {
        // Arrange — two submissions, each with their own minus records
        var submissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 10, PropertyDetailsId = 5, IsActive = true },
            new() { Id = 11, PropertyDetailsId = 5, IsActive = true }
        };

        var allMinusRecords = new List<RoomWiseMinusDataEntity>
        {
            new() { Id = 100, RoomWiseSubmissionId = 10, IsActive = true },
            new() { Id = 200, RoomWiseSubmissionId = 11, IsActive = true },
            new() { Id = 201, RoomWiseSubmissionId = 11, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(submissions.BuildMock());

        _roomWiseMinusRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(allMinusRecords.BuildMock());

        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roomWiseMinusRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteByPropertyIdAsync(5);

        // Assert — submission 10's minus
        _roomWiseMinusRepositoryMock.Verify(r => r.DeleteAsync(100, It.IsAny<CancellationToken>()), Times.Once);
        // submission 11's minus records
        _roomWiseMinusRepositoryMock.Verify(r => r.DeleteAsync(200, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseMinusRepositoryMock.Verify(r => r.DeleteAsync(201, It.IsAny<CancellationToken>()), Times.Once);
        // both submissions
        _repositoryMock.Verify(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(11, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteByPropertyIdAsync — Inactive Records Ignored

    [Fact]
    public async Task DeleteByPropertyIdAsync_InactiveSubmissions_AreIgnored()
    {
        // Arrange — one active, one inactive submission for the same PropertyDetailsId
        var submissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 10, PropertyDetailsId = 5, IsActive = true },
            new() { Id = 11, PropertyDetailsId = 5, IsActive = false }  // should be filtered
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(submissions.BuildMock());

        var emptyMinus = new List<RoomWiseMinusDataEntity>().BuildMock();
        _roomWiseMinusRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(emptyMinus);

        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteByPropertyIdAsync(5);

        // Assert — only the active submission (Id=10) should be deleted
        _repositoryMock.Verify(r => r.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(11, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteByPropertyIdAsync_InactiveMinusRecords_AreIgnored()
    {
        // Arrange
        var submissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 10, PropertyDetailsId = 5, IsActive = true }
        };

        var minusRecords = new List<RoomWiseMinusDataEntity>
        {
            new() { Id = 100, RoomWiseSubmissionId = 10, IsActive = true },
            new() { Id = 101, RoomWiseSubmissionId = 10, IsActive = false }  // should be filtered
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(submissions.BuildMock());

        _roomWiseMinusRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(minusRecords.BuildMock());

        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _roomWiseMinusRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteByPropertyIdAsync(5);

        // Assert — only active minus record deleted
        _roomWiseMinusRepositoryMock.Verify(r => r.DeleteAsync(100, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseMinusRepositoryMock.Verify(r => r.DeleteAsync(101, It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteByPropertyIdAsync — CancellationToken Propagation

    [Fact]
    public async Task DeleteByPropertyIdAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = cts.Token;

        var submissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 10, PropertyDetailsId = 5, IsActive = true }
        };

        var minusRecords = new List<RoomWiseMinusDataEntity>
        {
            new() { Id = 100, RoomWiseSubmissionId = 10, IsActive = true }
        };

        _repositoryMock.Setup(r => r.GetQueryable())
            .Returns(submissions.BuildMock());
        _roomWiseMinusRepositoryMock.Setup(r => r.GetQueryable())
            .Returns(minusRecords.BuildMock());

        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), token))
            .Returns(Task.CompletedTask);
        _roomWiseMinusRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<int>(), token))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteByPropertyIdAsync(5, token);

        // Assert — verify the token was forwarded
        _roomWiseMinusRepositoryMock.Verify(r => r.DeleteAsync(100, token), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(10, token), Times.Once);
    }

    #endregion
}
