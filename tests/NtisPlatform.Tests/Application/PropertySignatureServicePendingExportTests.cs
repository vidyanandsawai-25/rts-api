using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.DTOs.PropertySignature;
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
            mapperConfig.CreateMapper(),
            Mock.Of<ILogger<PropertySignatureService>>());
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

    [Fact]
    public async Task GetPendingSignsAsync_GroupsUnitsAndSumsDemandBySignatureProperty()
    {
        _repository
            .Setup(x => x.GetSignAuthorityIdByUserRoleAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repository
            .Setup(x => x.GetPendingSignSourceDataAsync(2, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertySignaturePendingSignSourceDto>
            {
                new()
                {
                    SignatureId = 11,
                    PropertyId = 110,
                    SignAuthorityId = 2,
                    UnitPropertyId = 110,
                    WardNo = "WE3",
                    PropertyNo = "15",
                    PartitionNo = "2",
                    SrNoticeNo = "WE0300150002",
                    SignStatus = "Pending",
                    AuthorityCode = "TI",
                    UnitDemand = 50m
                },
                new()
                {
                    SignatureId = 10,
                    PropertyId = 100,
                    SignAuthorityId = 2,
                    UnitPropertyId = 100,
                    WardNo = "WE2",
                    PropertyNo = "4",
                    PartitionNo = null,
                    SrNoticeNo = "WE0200040000",
                    SignStatus = "Pending",
                    AuthorityCode = "TI",
                    UnitDemand = 100m
                },
                new()
                {
                    SignatureId = 10,
                    PropertyId = 100,
                    SignAuthorityId = 2,
                    UnitPropertyId = 101,
                    WardNo = "WE2",
                    PropertyNo = "4",
                    PartitionNo = null,
                    SrNoticeNo = "WE0200040000",
                    SignStatus = "Pending",
                    AuthorityCode = "TI",
                    UnitDemand = 250m
                },
                new()
                {
                    SignatureId = 12,
                    PropertyId = 120,
                    SignAuthorityId = 2,
                    UnitPropertyId = 120,
                    WardNo = "WE3",
                    PropertyNo = "15",
                    PartitionNo = "1",
                    SrNoticeNo = "WE0300150001",
                    SignStatus = "Pending",
                    AuthorityCode = "TI",
                    UnitDemand = 75m
                }
            });

        var result = await _service.GetPendingSignsAsync(
            new PropertySignaturePendingSignsQueryParameters { UserId = 5 },
            CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(new[] { "WE2-4", "WE3-15-1", "WE3-15-2" }, result.Items.Select(row => row.StructureName));
        var row = result.Items.First();
        Assert.Equal(100, row.PropertyId);
        Assert.Equal(2, row.SignAuthorityId);
        Assert.Equal("WE2-4", row.StructureName);
        Assert.Equal("WE0200040000", row.SrNoticeNo);
        Assert.Equal(2, row.NoOfUnits);
        Assert.Equal(350m, row.Demand);
        Assert.Equal("Pending", row.SignStatus);
        Assert.Equal("TI", row.AuthorityCode);
    }

    [Fact]
    public async Task GetPendingSignsAsync_AppliesPaginationDefaultsAndRequestedPage()
    {
        _repository
            .Setup(x => x.GetSignAuthorityIdByUserRoleAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repository
            .Setup(x => x.GetPendingSignSourceDataAsync(2, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(1, 12)
                .Select(propertyNo => new PropertySignaturePendingSignSourceDto
                {
                    SignatureId = propertyNo,
                    PropertyId = propertyNo,
                    SignAuthorityId = 2,
                    UnitPropertyId = propertyNo,
                    WardNo = "WE2",
                    PropertyNo = propertyNo.ToString(),
                    SrNoticeNo = $"N{propertyNo}",
                    AuthorityCode = "TI"
                })
                .ToList());

        var result = await _service.GetPendingSignsAsync(
            new PropertySignaturePendingSignsQueryParameters { UserId = 5, PageNumber = 2, PageSize = 5 },
            CancellationToken.None);

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(new[] { 6, 7, 8, 9, 10 }, result.Items.Select(row => row.PropertyId));
    }

    [Fact]
    public async Task GetPendingSignsAsync_PassesSearchTermFilterToRepository()
    {
        _repository
            .Setup(x => x.GetSignAuthorityIdByUserRoleAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repository
            .Setup(x => x.GetPendingSignSourceDataAsync(2,"WE020004", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertySignaturePendingSignSourceDto>());

        await _service.GetPendingSignsAsync(
            new PropertySignaturePendingSignsQueryParameters
            {
                UserId = 5,
                SearchTerm = "WE020004"
            },
            CancellationToken.None);

        _repository.Verify(
            x => x.GetPendingSignSourceDataAsync(2, "WE020004", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPendingSignsAsync_WhenUserRoleHasNoMappedAuthority_ThrowsClearMessage()
    {
        _repository
            .Setup(x => x.GetSignAuthorityIdByUserRoleAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetPendingSignsAsync(
            new PropertySignaturePendingSignsQueryParameters { UserId = 5 },
            CancellationToken.None));

        Assert.Equal("No active PTIS sign authority role is mapped for UserId 5.", exception.Message);
        _repository.Verify(
            x => x.GetPendingSignSourceDataAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateSignAsync_ApprovesCurrentStatusAndCreatesNextPendingStage()
    {
        _repository
            .Setup(x => x.GetUpdateSignSourceAsync(5, 100, 1, "PendingToClerk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertySignatureUpdateSignSourceDto
            {
                SignatureId = 10,
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                AuthorityCode = "CLERK",
                SignStatus = "PendingToClerk",
                IsActive = true
            });
        SetupValidUpdateSignReferences();
        _repository
            .Setup(x => x.GetAuthoritiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SignAuthorityDto>
            {
                new() { Id = 1, AuthorityCode = "CLERK", AuthorityName = "Clerk", SequenceOrder = 1 },
                new() { Id = 2, AuthorityCode = "TI", AuthorityName = "Tax Inspector", SequenceOrder = 2 }
            });
        _repository
            .Setup(x => x.SignatureExistsAsync(100, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository
            .Setup(x => x.UpdateSignAsync(It.IsAny<PropertySignatureUpdateSignCommandDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateSignAsync(
            new PropertySignatureUpdateSignRequestDto
            {
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                AuthorityCode = "CLERK",
                SignStatus = "PendingToClerk"
            },
            CancellationToken.None);

        Assert.Equal("ApprovedByClerk", result.UpdatedSignStatus);
        Assert.Equal(2, result.NextSignAuthorityId);
        Assert.Equal("PendingToTI", result.NextSignStatus);
        _repository.Verify(x => x.UpdateSignAsync(
            It.Is<PropertySignatureUpdateSignCommandDto>(command =>
                command.SignatureId == 10
                && command.UserId == 5
                && command.PropertyId == 100
                && command.SignAuthorityId == 1
                && command.IsActive
                && command.UpdatedBy == 5
                && command.UpdatedSignStatus == "ApprovedByClerk"
                && command.NextSignAuthorityId == 2
                && command.NextSignStatus == "PendingToTI"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateSignAsync_FinalAuthorityApprovesWithoutNextInsert()
    {
        _repository
            .Setup(x => x.GetUpdateSignSourceAsync(5, 100, 5, "PendingToACD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertySignatureUpdateSignSourceDto
            {
                SignatureId = 10,
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 5,
                AuthorityCode = "ACD",
                SignStatus = "PendingToACD",
                IsActive = true
            });
        SetupValidUpdateSignReferences();
        _repository
            .Setup(x => x.GetAuthoritiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SignAuthorityDto>
            {
                new() { Id = 5, AuthorityCode = "ACD", AuthorityName = "Additional Commissioner", SequenceOrder = 5 }
            });
        _repository
            .Setup(x => x.UpdateSignAsync(It.IsAny<PropertySignatureUpdateSignCommandDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateSignAsync(
            new PropertySignatureUpdateSignRequestDto
            {
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 5,
                AuthorityCode = "ACD",
                SignStatus = "PendingToACD"
            },
            CancellationToken.None);

        Assert.Equal("ApprovedByACD", result.UpdatedSignStatus);
        Assert.Null(result.NextSignAuthorityId);
        Assert.Null(result.NextSignStatus);
        _repository.Verify(x => x.SignatureExistsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSignAsync_WithMismatchedAuthorityCode_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateSignAsync(
            new PropertySignatureUpdateSignRequestDto
            {
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                AuthorityCode = "TI",
                SignStatus = "PendingToClerk"
            },
            CancellationToken.None));

        _repository.Verify(x => x.GetUpdateSignSourceAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateSignAsync_WithUnknownUserId_ThrowsClearNotFoundMessage()
    {
        _repository
            .Setup(x => x.GetUpdateSignReferenceStatusAsync(5, 100, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, true, true));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateSignAsync(
            new PropertySignatureUpdateSignRequestDto
            {
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                AuthorityCode = "CLERK",
                SignStatus = "PendingToClerk"
            },
            CancellationToken.None));

        Assert.Equal("The UserId 5 was not found or is inactive.", exception.Message);
    }

    [Fact]
    public async Task UpdateSignAsync_WithUnknownPropertyId_ThrowsClearNotFoundMessage()
    {
        _repository
            .Setup(x => x.GetUpdateSignReferenceStatusAsync(5, 100, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, false, true));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateSignAsync(
            new PropertySignatureUpdateSignRequestDto
            {
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                AuthorityCode = "CLERK",
                SignStatus = "PendingToClerk"
            },
            CancellationToken.None));

        Assert.Equal("The PropertyId 100 was not found or is inactive.", exception.Message);
    }

    [Fact]
    public async Task UpdateSignAsync_WithUnknownSignAuthorityId_ThrowsClearNotFoundMessage()
    {
        _repository
            .Setup(x => x.GetUpdateSignReferenceStatusAsync(5, 100, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, true, false));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateSignAsync(
            new PropertySignatureUpdateSignRequestDto
            {
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                AuthorityCode = "CLERK",
                SignStatus = "PendingToClerk"
            },
            CancellationToken.None));

        Assert.Equal("The SignAuthorityId 1 was not found or is inactive.", exception.Message);
    }

    [Fact]
    public async Task UpdateSignAsync_WhenPendingRecordMissing_ThrowsClearMessage()
    {
        SetupValidUpdateSignReferences();
        _repository
            .Setup(x => x.GetUpdateSignSourceAsync(5, 100, 1, "PendingToClerk", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySignatureUpdateSignSourceDto?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateSignAsync(
            new PropertySignatureUpdateSignRequestDto
            {
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                AuthorityCode = "CLERK",
                SignStatus = "PendingToClerk"
            },
            CancellationToken.None));

        Assert.Equal(
            "No active pending signature record found for UserId 5, PropertyId 100, SignAuthorityId 1, and SignStatus PendingToClerk.",
            exception.Message);
    }

    private void SetupValidUpdateSignReferences()
    {
        _repository
            .Setup(x => x.GetUpdateSignReferenceStatusAsync(5, 100, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, true, true));
    }
}
