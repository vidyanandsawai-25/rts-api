using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RTSAppeal;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class RtsAppealServiceTests
{
    private readonly Mock<IRepository<RTSAppealApplicationEntity, int>> _appealRepoMock;
    private readonly Mock<IRepository<RTSAppealTypeMasterEntity, int>> _appealTypeRepoMock;
    private readonly Mock<IRepository<RTSAppealFlowStageMasterEntity, int>> _appealStageRepoMock;
    private readonly Mock<IRepository<RTSTrackAppealHistoryEntity, int>> _appealHistoryRepoMock;
    private readonly Mock<IRepository<RTSApplicationDetailsEntity, int>> _applicationRepoMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<RTSAppealService>> _loggerMock;
    private readonly RTSAppealService _service;

    public RtsAppealServiceTests()
    {
        _appealRepoMock = new Mock<IRepository<RTSAppealApplicationEntity, int>>();
        _appealTypeRepoMock = new Mock<IRepository<RTSAppealTypeMasterEntity, int>>();
        _appealStageRepoMock = new Mock<IRepository<RTSAppealFlowStageMasterEntity, int>>();
        _appealHistoryRepoMock = new Mock<IRepository<RTSTrackAppealHistoryEntity, int>>();
        _applicationRepoMock = new Mock<IRepository<RTSApplicationDetailsEntity, int>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<RTSAppealService>>();

        _currentUserServiceMock.Setup(u => u.GetCurrentUserId()).Returns(101);

        SetupApplications();
        SetupAppeals();
        SetupAppealTypes();
        SetupStages();
        SetupAppealHistories();

        _service = new RTSAppealService(
            _appealRepoMock.Object,
            _appealTypeRepoMock.Object,
            _appealStageRepoMock.Object,
            _appealHistoryRepoMock.Object,
            _applicationRepoMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private void SetupApplications(params RTSApplicationDetailsEntity[] apps)
    {
        var mockQuery = apps.ToList().BuildMock();
        _applicationRepoMock.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupAppeals(params RTSAppealApplicationEntity[] appeals)
    {
        var mockQuery = appeals.ToList().BuildMock();
        _appealRepoMock.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupAppealTypes(params RTSAppealTypeMasterEntity[] types)
    {
        var mockQuery = types.ToList().BuildMock();
        _appealTypeRepoMock.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupStages(params RTSAppealFlowStageMasterEntity[] stages)
    {
        var mockQuery = stages.ToList().BuildMock();
        _appealStageRepoMock.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupAppealHistories(params RTSTrackAppealHistoryEntity[] histories)
    {
        var mockQuery = histories.ToList().BuildMock();
        _appealHistoryRepoMock.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    #endregion

    #region 1. Appeal Types

    [Fact]
    public async Task GetAppealTypesAsync_ReturnsActiveTypes()
    {
        // Arrange
        SetupAppealTypes(
            new RTSAppealTypeMasterEntity { Id = 1, Code = "DELAY", AppealTypeName = "Delay in Service Delivery" },
            new RTSAppealTypeMasterEntity { Id = 2, Code = "REJECT", AppealTypeName = "Application Rejection" }
        );

        // Act
        var result = await _service.GetAppealTypesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Code.Should().Be("DELAY");
        result[1].Code.Should().Be("REJECT");
    }

    #endregion

    #region 2. First Appeal Statutory Window & Eligibility

    [Fact]
    public async Task CheckAppealLevelAsync_NonExistentApplication_ReturnsBlocked()
    {
        // Arrange
        SetupApplications();
        SetupAppeals();

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-9999");

        // Assert
        result.CanFileAppeal.Should().BeFalse();
        result.BlockReason.Should().Contain("does not exist");
    }

    [Fact]
    public async Task CheckAppealLevelAsync_ActiveSlaWithinPeriod_ReturnsBlocked()
    {
        // Arrange
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-001",
            ApplicationStatus = "Pending",
            CreatedDate = DateTime.Now.AddDays(-2), // 2 days ago
            IsActive = true,
            Service = new RTSServiceEntity { Sla = "7 Days", ServiceName = "Water Connection" }
        };
        SetupApplications(app);
        SetupAppeals();

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-001");

        // Assert
        result.CanFileAppeal.Should().BeFalse();
        result.BlockReason.Should().Contain("active SLA processing period");
    }

    [Fact]
    public async Task CheckAppealLevelAsync_SlaExpired_Within30Days_ReturnsNormalFirstAppeal()
    {
        // Arrange: SLA is 7 days, created 15 days ago -> elapsed 8 days since SLA expiry
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-001",
            ApplicationStatus = "Pending",
            CreatedDate = DateTime.Now.AddDays(-15),
            IsActive = true,
            Service = new RTSServiceEntity { Sla = "7 Days", ServiceName = "Water Connection" }
        };
        SetupApplications(app);
        SetupAppeals();

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-001");

        // Assert
        result.CanFileAppeal.Should().BeTrue();
        result.AppealLevel.Should().Be("1st Appeal");
        result.IsSecondAppeal.Should().BeFalse();
        result.FilingCategory.Should().Be("Normal");
        result.RequiresDelayJustification.Should().BeFalse();
        result.SuggestedAppealNo.Should().Contain("APP-001-A1");
    }

    [Fact]
    public async Task CheckAppealLevelAsync_SlaExpired_Between31And90Days_ReturnsDelayedFirstAppeal()
    {
        // Arrange: SLA 7 days, created 45 days ago -> expired 38 days ago (>30 and <=90)
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-001",
            ApplicationStatus = "Pending",
            CreatedDate = DateTime.Now.AddDays(-45),
            IsActive = true,
            Service = new RTSServiceEntity { Sla = "7 Days", ServiceName = "Water Connection" }
        };
        SetupApplications(app);
        SetupAppeals();

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-001");

        // Assert
        result.CanFileAppeal.Should().BeTrue();
        result.AppealLevel.Should().Be("1st Appeal");
        result.FilingCategory.Should().Be("Delayed");
        result.RequiresDelayJustification.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAppealLevelAsync_SlaExpired_Beyond90Days_ReturnsBlocked()
    {
        // Arrange: SLA 7 days, created 120 days ago -> expired 113 days ago (>90)
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-001",
            ApplicationStatus = "Pending",
            CreatedDate = DateTime.Now.AddDays(-120),
            IsActive = true,
            Service = new RTSServiceEntity { Sla = "7 Days", ServiceName = "Water Connection" }
        };
        SetupApplications(app);
        SetupAppeals();

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-001");

        // Assert
        result.CanFileAppeal.Should().BeFalse();
        result.BlockReason.Should().Contain("expired");
    }

    [Fact]
    public async Task CheckAppealLevelAsync_RejectedApplication_TriggersFromRejectionDate()
    {
        // Arrange: Rejected 10 days ago -> within 30 days
        var rejectionDate = DateTime.Now.AddDays(-10);
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-002",
            ApplicationStatus = "Rejected",
            CreatedDate = DateTime.Now.AddDays(-20),
            IsActive = true,
            Service = new RTSServiceEntity { Sla = "7 Days", ServiceName = "Zone Certificate" },
            TrackApplicationHistory = new List<TrackApplicationHistoryEntity>
            {
                new TrackApplicationHistoryEntity
                {
                    ApplicationId = 1,
                    Status = "Rejected",
                    Action = "Rejected",
                    CreatedDate = rejectionDate
                }
            }
        };
        SetupApplications(app);
        SetupAppeals();

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-002");

        // Assert
        result.CanFileAppeal.Should().BeTrue();
        result.AppealLevel.Should().Be("1st Appeal");
        result.FilingCategory.Should().Be("Normal");
        result.TriggerReason.Should().Contain("Application Rejected");
    }

    #endregion

    #region 3. Second Appeal Statutory Window & Eligibility

    [Fact]
    public async Task CheckAppealLevelAsync_FirstAppealPending_BlocksSecondAppeal()
    {
        // Arrange: First Appeal exists and is Pending
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-003",
            ApplicationStatus = "Rejected",
            CreatedDate = DateTime.Now.AddDays(-40),
            IsActive = true
        };
        var firstAppeal = new RTSAppealApplicationEntity
        {
            Id = 10,
            ApplicationId = 1,
            AppealNo = "RTS/2026/APP-003-A1",
            AppealLevel = 1,
            AppealStatus = "Pending",
            IsActive = true
        };
        SetupApplications(app);
        SetupAppeals(firstAppeal);

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-003");

        // Assert
        result.CanFileAppeal.Should().BeFalse();
        result.IsSecondAppeal.Should().BeTrue();
        result.BlockReason.Should().Contain("currently Pending review");
    }

    [Fact]
    public async Task CheckAppealLevelAsync_FirstAppealDismissed_Within30Days_AllowsSecondAppeal()
    {
        // Arrange: First appeal rejected 10 days ago
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-003",
            ApplicationStatus = "Rejected",
            CreatedDate = DateTime.Now.AddDays(-60),
            IsActive = true
        };
        var firstAppeal = new RTSAppealApplicationEntity
        {
            Id = 10,
            ApplicationId = 1,
            AppealNo = "RTS/2026/APP-003-A1",
            AppealLevel = 1,
            AppealStatus = "Rejected",
            ActionDate = DateTime.Now.AddDays(-10),
            IsActive = true
        };
        SetupApplications(app);
        SetupAppeals(firstAppeal);

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-003");

        // Assert
        result.CanFileAppeal.Should().BeTrue();
        result.IsSecondAppeal.Should().BeTrue();
        result.AppealLevel.Should().Be("2nd Appeal");
        result.FilingCategory.Should().Be("Normal");
        result.SuggestedAppealNo.Should().Contain("APP-003-A2");
    }

    [Fact]
    public async Task CheckAppealLevelAsync_SecondAppealAlreadyFiled_BlocksFurtherAppeals()
    {
        // Arrange: Second appeal already exists
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-004",
            ApplicationStatus = "Rejected",
            IsActive = true
        };
        var firstAppeal = new RTSAppealApplicationEntity
        {
            Id = 10,
            ApplicationId = 1,
            AppealNo = "RTS/2026/APP-004-A1",
            AppealLevel = 1,
            AppealStatus = "Rejected",
            IsActive = true
        };
        var secondAppeal = new RTSAppealApplicationEntity
        {
            Id = 20,
            ApplicationId = 1,
            AppealNo = "RTS/2026/APP-004-A2",
            AppealLevel = 2,
            AppealStatus = "Approved",
            IsActive = true
        };
        SetupApplications(app);
        SetupAppeals(firstAppeal, secondAppeal);

        // Act
        var result = await _service.CheckAppealLevelAsync("APP-004");

        // Assert
        result.CanFileAppeal.Should().BeFalse();
        result.BlockReason.Should().Contain("Second Appeal");
        result.BlockReason.Should().Contain("No further appeals are permitted");
    }

    #endregion

    #region 4. Submit Appeal

    [Fact]
    public async Task SubmitAppealAsync_ValidNormalAppeal_SavesAndCommits()
    {
        // Arrange
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-100",
            ApplicationStatus = "Rejected",
            ApplicantName = "Jane Doe",
            ApplicantMobileNo = "9876543210",
            CreatedDate = DateTime.Now.AddDays(-10),
            IsActive = true,
            TrackApplicationHistory = new List<TrackApplicationHistoryEntity>
            {
                new TrackApplicationHistoryEntity { ApplicationId = 1, Status = "Rejected", CreatedDate = DateTime.Now.AddDays(-5) }
            }
        };
        SetupApplications(app);
        SetupAppeals();
        SetupStages(new RTSAppealFlowStageMasterEntity { Id = 1, StageOrder = 1, StageName = "First Appellate Authority", UserId = 5 });

        var dto = new CreateRTSAppealApplicationDto
        {
            ApplicationNo = "APP-100",
            AppealTypeId = 1,
            ReasonForAppeal = "Application rejected wrongfully without reason.",
            MobileNo = "9876543210"
        };

        // Act
        var result = await _service.SubmitAppealAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.AppealNo.Should().Contain("APP-100-A1");

        _appealRepoMock.Verify(r => r.AddAsync(It.Is<RTSAppealApplicationEntity>(a =>
            a.ApplicationId == 1 &&
            a.AppealLevel == 1 &&
            a.AppealStatus == "Pending"), It.IsAny<CancellationToken>()), Times.Once);

        _appealHistoryRepoMock.Verify(r => r.AddAsync(It.Is<RTSTrackAppealHistoryEntity>(h =>
            h.ApplicationId == 1 &&
            h.AppealLevel == 1 &&
            h.Action != null && h.Action.Contains("Filed")), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAppealAsync_DelayedWithoutJustification_ThrowsInvalidOperationException()
    {
        // Arrange: SLA expired 40 days ago -> Delayed category
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-101",
            ApplicationStatus = "Pending",
            CreatedDate = DateTime.Now.AddDays(-47),
            IsActive = true,
            Service = new RTSServiceEntity { Sla = "7 Days", ServiceName = "Trade Licence" }
        };
        SetupApplications(app);
        SetupAppeals();

        var dto = new CreateRTSAppealApplicationDto
        {
            ApplicationNo = "APP-101",
            AppealTypeId = 1,
            ReasonForAppeal = "Service SLA expired.",
            MobileNo = "9876543210",
            DelayJustification = null // Missing mandatory justification
        };

        // Act & Assert
        var act = () => _service.SubmitAppealAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*justification for delay (विलंब माफीचे कारण) is mandatory*");
    }

    [Fact]
    public async Task SubmitAppealAsync_DelayedWithJustification_Succeeds()
    {
        // Arrange
        var app = new RTSApplicationDetailsEntity
        {
            Id = 1,
            ApplicationNo = "APP-102",
            ApplicationStatus = "Pending",
            CreatedDate = DateTime.Now.AddDays(-47),
            IsActive = true,
            Service = new RTSServiceEntity { Sla = "7 Days", ServiceName = "Trade Licence" }
        };
        SetupApplications(app);
        SetupAppeals();

        var dto = new CreateRTSAppealApplicationDto
        {
            ApplicationNo = "APP-102",
            AppealTypeId = 1,
            ReasonForAppeal = "Service SLA expired.",
            MobileNo = "9876543210",
            DelayJustification = "Medical emergency prevented timely filing."
        };

        // Act
        var result = await _service.SubmitAppealAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        _appealRepoMock.Verify(r => r.AddAsync(It.Is<RTSAppealApplicationEntity>(a =>
            a.ReasonForComplaint!.Contains("Medical emergency prevented")), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 5. Dashboard Cards

    [Fact]
    public async Task GetAppealDashboardCardsAsync_AggregatesCountsCorrectly()
    {
        // Arrange
        var now = DateTime.Now;
        var appeals = new List<RTSAppealApplicationEntity>
        {
            new RTSAppealApplicationEntity { Id = 1, AppealLevel = 1, AppealStatus = "Pending", CreatedDate = now, IsActive = true },
            new RTSAppealApplicationEntity { Id = 2, AppealLevel = 1, AppealStatus = "Approved", CreatedDate = now.AddDays(-5), IsActive = true },
            new RTSAppealApplicationEntity { Id = 3, AppealLevel = 2, AppealStatus = "Rejected", CreatedDate = now.AddDays(-10), IsActive = true },
            new RTSAppealApplicationEntity { Id = 4, AppealLevel = 2, AppealStatus = "Pending", CreatedDate = now.AddDays(-35), IsActive = true, ReasonForComplaint = "विलंब माफीचे कारण: Illness" },
            new RTSAppealApplicationEntity { Id = 5, AppealLevel = 1, AppealStatus = "Pending", CreatedDate = now, IsActive = false } // Inactive, should be excluded
        };
        SetupAppeals(appeals.ToArray());

        // Act
        var result = await _service.GetAppealDashboardCardsAsync();

        // Assert
        result.TotalAppeals.Should().Be(4);
        result.PendingAppeals.Should().Be(2);
        result.ApprovedAppeals.Should().Be(1);
        result.RejectedAppeals.Should().Be(1);
        result.FirstAppeals.Should().Be(2);
        result.SecondAppeals.Should().Be(2);
        result.OverdueAppeals.Should().Be(1); // 35 days old
        result.DelayedAppeals.Should().Be(1);
        result.TodayAppeals.Should().Be(1);
    }

    #endregion

    #region 6. Officer Adjudication Actions

    [Theory]
    [InlineData("Approve", "Approved")]
    [InlineData("Reject", "Rejected")]
    [InlineData("Return", "Returned")]
    public async Task ProcessAppealOfficerActionAsync_ValidAction_UpdatesStatusAndLogsHistory(string action, string expectedStatus)
    {
        // Arrange
        var appeal = new RTSAppealApplicationEntity
        {
            Id = 50,
            ApplicationId = 1,
            AppealNo = "RTS/2026/APP-001-A1",
            AppealLevel = 1,
            AppealStatus = "Pending",
            IsActive = true
        };
        SetupAppeals(appeal);
        SetupApplications(new RTSApplicationDetailsEntity { Id = 1, ApplicationNo = "APP-001", IsActive = true });
        SetupStages(new RTSAppealFlowStageMasterEntity { Id = 1, StageOrder = 1, UserId = 101, StageName = "First Appellate Authority" });

        var dto = new ProcessRTSAppealActionDto
        {
            AppealId = 50,
            Action = action,
            Remark = $"Official order processed as {action}."
        };

        // Act
        var result = await _service.ProcessAppealOfficerActionAsync(dto);

        // Assert
        result.Should().BeTrue();
        appeal.AppealStatus.Should().Be(expectedStatus);
        appeal.ActionRemarks.Should().Contain(action);

        _appealHistoryRepoMock.Verify(r => r.AddAsync(It.Is<RTSTrackAppealHistoryEntity>(h =>
            h.AppealId == 50 &&
            h.Status == expectedStatus), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAppealOfficerActionAsync_AlreadyResolvedAppeal_ThrowsInvalidOperationException()
    {
        // Arrange
        var appeal = new RTSAppealApplicationEntity
        {
            Id = 51,
            ApplicationId = 1,
            AppealNo = "RTS/2026/APP-001-A1",
            AppealLevel = 1,
            AppealStatus = "Approved", // Already resolved
            IsActive = true
        };
        SetupAppeals(appeal);

        var dto = new ProcessRTSAppealActionDto
        {
            AppealId = 51,
            Action = "Reject",
            Remark = "Attempting to re-adjudicate."
        };

        // Act & Assert
        var act = () => _service.ProcessAppealOfficerActionAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been actioned*");
    }

    [Fact]
    public async Task ProcessAppealOfficerActionAsync_EmptyRemark_ThrowsArgumentException()
    {
        // Arrange
        var dto = new ProcessRTSAppealActionDto
        {
            AppealId = 52,
            Action = "Approve",
            Remark = "" // Empty remark
        };

        // Act & Assert
        var act = () => _service.ProcessAppealOfficerActionAsync(dto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*mandatory*");
    }

    #endregion
}
