using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Reporting;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for ReportWorkerService.AuthenticateAsync — the worker SLT handshake.
/// Covers SLT validation, requestId/claim matching, single-use consumption, org isolation,
/// and concurrent-consumption handling.
/// </summary>
public class ReportWorkerServiceAuthenticateTests
{
    private readonly Mock<IReportingRepository<ReportRequestEntity, Guid>> _requestRepositoryMock;
    private readonly Mock<IReportingUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<ReportDefinitionEntity, int>> _definitionRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IDocumentApplicationService> _documentServiceMock;
    private readonly Mock<IReportDataProvider> _dataProviderMock;
    private readonly ReportDefinitionCacheService _cache;
    private readonly ReportingOptions _options;
    private readonly ReportWorkerService _service;

    private const string ReportCode = "REP001";
    private const string ProviderCode = "TestProvider";

    public ReportWorkerServiceAuthenticateTests()
    {
        _requestRepositoryMock = new Mock<IReportingRepository<ReportRequestEntity, Guid>>();
        _unitOfWorkMock = new Mock<IReportingUnitOfWork>();
        _definitionRepositoryMock = new Mock<IRepository<ReportDefinitionEntity, int>>();
        _tokenServiceMock = new Mock<ITokenService>();
        _documentServiceMock = new Mock<IDocumentApplicationService>();

        _dataProviderMock = new Mock<IReportDataProvider>();
        _dataProviderMock.Setup(p => p.ProviderCode).Returns(ProviderCode);
        _dataProviderMock.Setup(p => p.GetDataAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<object>());

        _cache = new ReportDefinitionCacheService(Mock.Of<ILogger<ReportDefinitionCacheService>>());
        _cache.Load(
            new List<ReportDefinitionEntity>
            {
                new()
                {
                    Id = 1,
                    ReportCode = ReportCode,
                    ReportName = "Test Report",
                    TemplateFile = "test.rpt",
                    DataProviderCode = ProviderCode,
                    IsActive = true,
                },
            },
            new Dictionary<int, IReadOnlyList<ReportParameterDefinitionEntity>>());

        _options = new ReportingOptions { OrganizationId = 0, LltMinutes = 45 };

        _service = new ReportWorkerService(
            _requestRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cache,
            _definitionRepositoryMock.Object,
            new[] { _dataProviderMock.Object },
            _tokenServiceMock.Object,
            _documentServiceMock.Object,
            Options.Create(_options),
            Mock.Of<ILogger<ReportWorkerService>>());
    }

    private static ReportRequestEntity BuildValidEntity(Guid requestId, int? organizationId = null) => new()
    {
        ReportRequestId = requestId,
        ReportCode = ReportCode,
        RequestedByUserId = 7,
        OrganizationId = organizationId,
        SltConsumed = false,
        SltExpiresAt = DateTime.Now.AddMinutes(5),
        ParametersJson = null,
    };

    private void SeedRequests(params ReportRequestEntity[] entities) =>
        _requestRepositoryMock.Setup(r => r.GetQueryable()).Returns(entities.ToList().BuildMock());

    [Fact]
    public async Task AuthenticateAsync_EmptyToken_ReturnsNull()
    {
        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "", ReportRequestId = Guid.NewGuid() };

        var result = await _service.AuthenticateAsync(request);

        Assert.Null(result);
        _tokenServiceMock.Verify(t => t.ValidateShortLivedToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidOrExpiredJwt_ReturnsNull()
    {
        var requestId = Guid.NewGuid();
        _tokenServiceMock.Setup(t => t.ValidateShortLivedToken("bad-token")).Returns(((Guid, int)?)null);

        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "bad-token", ReportRequestId = requestId };

        var result = await _service.AuthenticateAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_BodyRequestIdDoesNotMatchSltClaim_ReturnsNull()
    {
        var bodyRequestId = Guid.NewGuid();
        var claimedRequestId = Guid.NewGuid();
        _tokenServiceMock.Setup(t => t.ValidateShortLivedToken("slt")).Returns((claimedRequestId, 7));

        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "slt", ReportRequestId = bodyRequestId };

        var result = await _service.AuthenticateAsync(request);

        Assert.Null(result);
        _requestRepositoryMock.Verify(r => r.GetQueryable(), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_RequestNotFound_ReturnsNull()
    {
        var requestId = Guid.NewGuid();
        _tokenServiceMock.Setup(t => t.ValidateShortLivedToken("slt")).Returns((requestId, 7));
        SeedRequests(); // empty

        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "slt", ReportRequestId = requestId };

        var result = await _service.AuthenticateAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_SltAlreadyConsumed_ReturnsNull()
    {
        var requestId = Guid.NewGuid();
        var entity = BuildValidEntity(requestId);
        entity.SltConsumed = true;
        _tokenServiceMock.Setup(t => t.ValidateShortLivedToken("slt")).Returns((requestId, 7));
        SeedRequests(entity);

        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "slt", ReportRequestId = requestId };

        var result = await _service.AuthenticateAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_SltExpired_ReturnsNull()
    {
        var requestId = Guid.NewGuid();
        var entity = BuildValidEntity(requestId);
        entity.SltExpiresAt = DateTime.Now.AddMinutes(-1);
        _tokenServiceMock.Setup(t => t.ValidateShortLivedToken("slt")).Returns((requestId, 7));
        SeedRequests(entity);

        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "slt", ReportRequestId = requestId };

        var result = await _service.AuthenticateAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_OrganizationMismatch_ReturnsNull()
    {
        _options.OrganizationId = 100;
        var requestId = Guid.NewGuid();
        var entity = BuildValidEntity(requestId, organizationId: 200);
        _tokenServiceMock.Setup(t => t.ValidateShortLivedToken("slt")).Returns((requestId, 7));
        SeedRequests(entity);

        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "slt", ReportRequestId = requestId };

        var result = await _service.AuthenticateAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_ConcurrentConsumption_DiscardsChangesAndReturnsNull()
    {
        var requestId = Guid.NewGuid();
        var entity = BuildValidEntity(requestId);
        _tokenServiceMock.Setup(t => t.ValidateShortLivedToken("slt")).Returns((requestId, 7));
        SeedRequests(entity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "slt", ReportRequestId = requestId };

        var result = await _service.AuthenticateAsync(request);

        Assert.Null(result);
        _unitOfWorkMock.Verify(u => u.DiscardChanges(), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidSlt_ReturnsLongLivedTokenAndReportMetadata()
    {
        var requestId = Guid.NewGuid();
        var entity = BuildValidEntity(requestId);
        _tokenServiceMock.Setup(t => t.ValidateShortLivedToken("slt")).Returns((requestId, 7));
        SeedRequests(entity);
        _tokenServiceMock.Setup(t => t.GenerateReportWorkerToken(requestId, 7, _options.LltMinutes))
            .Returns("llt-token");

        var request = new WorkerAuthenticateRequestDto { ShortLivedToken = "slt", ReportRequestId = requestId };

        var result = await _service.AuthenticateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("llt-token", result!.LongLivedToken);
        Assert.Equal("test.rpt", result.ReportName);
        Assert.Equal(ProviderCode, result.DataProviderCode);
        Assert.Equal("pdf", result.OutputFormat);
        Assert.True(entity.SltConsumed);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
