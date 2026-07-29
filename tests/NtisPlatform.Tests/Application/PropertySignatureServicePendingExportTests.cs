using AutoMapper;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Application;

public class PropertySignatureServicePendingExportTests
{
    private readonly Mock<IPropertySignatureRepository> _repository = new();
    private readonly PropertySignatureService _service;

    public PropertySignatureServicePendingExportTests()
    {
        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile<PropertySignatureMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _service = new PropertySignatureService(
            _repository.Object,
            Mock.Of<IExcelUploadService>(),
            mapperConfig.CreateMapper());
    }

    [Fact]
    public async Task GetPendingExportDataAsync_ReturnsOnlyRowsPendingAtSelectedAuthority()
    {
        _repository
            .Setup(x => x.GetPendingExportAuthoritiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertySignaturePendingExportAuthorityDto>
            {
                new() { SignAuthorityId = 1, AuthorityName = "Clerk", OfficerName = "Clerk Officer", SequenceOrder = 1 },
                new() { SignAuthorityId = 2, AuthorityName = "Tax Inspector", OfficerName = "TI Officer", SequenceOrder = 2 },
                new() { SignAuthorityId = 3, AuthorityName = "Assistant Commissioner", OfficerName = "AC Officer", SequenceOrder = 3 }
            });
        _repository
            .Setup(x => x.GetPendingExportSourceDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertySignaturePendingExportSourceDto>
            {
                new()
                {
                    PropertyId = 10,
                    Zone = "MM",
                    BuildingNo = "MM8-216",
                    SrNoticeNo = "N1",
                    SignedAuthorityIds = new List<int> { 1 }
                },
                new()
                {
                    PropertyId = 11,
                    Zone = "MM",
                    BuildingNo = "MM8-217",
                    SrNoticeNo = "N2",
                    SignedAuthorityIds = new List<int> { 1, 2 }
                },
                new()
                {
                    PropertyId = 12,
                    Zone = "MM",
                    BuildingNo = "MM8-218",
                    SrNoticeNo = "N3",
                    SignedAuthorityIds = new List<int> { 1, 3 }
                },
                new()
                {
                    PropertyId = 13,
                    Zone = "MM",
                    BuildingNo = "MM8-219",
                    SrNoticeNo = "N4",
                    SignedAuthorityIds = new List<int>()
                }
            });

        var result = await _service.GetPendingExportDataAsync(2, CancellationToken.None);

        var row = Assert.Single(result);
        Assert.Equal("MM8-216", row.BuildingNo);
        Assert.Equal("N1", row.SrNoticeNo);
        Assert.Equal("Tax Inspector", row.PendingSignAt);
        Assert.Equal("TI Officer", row.PendingOfficerName);
    }

    [Fact]
    public async Task GetPendingExportDataAsync_WithUnknownAuthority_ReturnsEmptyList()
    {
        _repository
            .Setup(x => x.GetPendingExportAuthoritiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertySignaturePendingExportAuthorityDto>
            {
                new() { SignAuthorityId = 1, AuthorityName = "Clerk", SequenceOrder = 1 }
            });

        var result = await _service.GetPendingExportDataAsync(99, CancellationToken.None);

        Assert.Empty(result);
        _repository.Verify(
            x => x.GetPendingExportSourceDataAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
