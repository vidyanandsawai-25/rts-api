using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

public class PropertySignatureRepositoryPendingExportTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetSignAuthorityGridDataAsync_ReturnsAuthorityIdAsClassificationTypeId()
    {
        using var context = CreateContext();
        context.ZoneMaster.Add(new ZoneEntity
        {
            Id = 1,
            ZoneNo = "MM",
            Description = "Main Zone",
            SequenceNo = 1,
            IsActive = true
        });
        context.WardMaster.Add(new WardEntity
        {
            Id = 1,
            WardNo = "MM8",
            ZoneId = 1,
            IsActive = true
        });
        context.PropertyMast.Add(new PropertyEntity
        {
            Id = 10,
            WardId = 1,
            TaxZoneId = 1,
            PropertyNo = "216",
            PartitionNo = "",
            IsActive = true,
            MarkedForDeletion = false
        });
        context.SignAuthorityMaster.Add(new SignAuthorityMasterEntity
        {
            Id = 7,
            AuthorityName = "Clerk Sign",
            SequenceOrder = 1,
            IsActive = true
        });
        context.PropertySignatureDetails.Add(new PropertySignatureDetailsEntity
        {
            Id = 20,
            PropertyId = 10,
            UserId = 1,
            SignAuthorityId = 7,
            IsActive = true
        });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.GetSignAuthorityGridDataAsync(
            new PropertySearchRequestDto { ZoneId = 1 },
            CancellationToken.None);

        var classification = Assert.Single(result.ZoneData.Single().Classifications);
        Assert.Equal(7, classification.TypeId);
        Assert.Equal("Clerk Sign", classification.Type);
        Assert.Equal(1, classification.Structure);
        Assert.Equal(1, classification.Unit);
    }

    [Fact]
    public async Task GetPendingExportAuthoritiesAsync_ReturnsOnlyActiveAuthoritiesOrderedBySequence()
    {
        using var context = CreateContext();
        context.SignAuthorityMaster.AddRange(
            new SignAuthorityMasterEntity
            {
                Id = 2,
                AuthorityName = "Tax Inspector",
                OfficerName = "TI Officer",
                SequenceOrder = 2,
                IsActive = true
            },
            new SignAuthorityMasterEntity
            {
                Id = 1,
                AuthorityName = "Clerk",
                OfficerName = "Clerk Officer",
                SequenceOrder = 1,
                IsActive = true
            },
            new SignAuthorityMasterEntity
            {
                Id = 3,
                AuthorityName = "Inactive",
                OfficerName = "Inactive Officer",
                SequenceOrder = 3,
                IsActive = false
            });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.GetPendingExportAuthoritiesAsync(CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.SignAuthorityId));
        Assert.Equal("Clerk Officer", result[0].OfficerName);
        Assert.Equal("TI Officer", result[1].OfficerName);
    }

    [Fact]
    public async Task GetPendingExportSourceDataAsync_ReturnsStartedActivePropertiesWithSignedAuthorities()
    {
        using var context = CreateContext();
        context.ZoneMaster.AddRange(
            new ZoneEntity { Id = 1, ZoneNo = "MM", Description = "MM", SequenceNo = 1, IsActive = true },
            new ZoneEntity { Id = 2, ZoneNo = "INACTIVE", Description = "Inactive", SequenceNo = 2, IsActive = false });
        context.WardMaster.AddRange(
            new WardEntity { Id = 1, WardNo = "MM8", ZoneId = 1, IsActive = true },
            new WardEntity { Id = 2, WardNo = "MM9", ZoneId = 2, IsActive = true });
        context.PropertyMast.AddRange(
            new PropertyEntity
            {
                Id = 10,
                WardId = 1,
                TaxZoneId = 1,
                PropertyNo = "216",
                PartitionNo = "",
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyEntity
            {
                Id = 11,
                WardId = 1,
                TaxZoneId = 1,
                PropertyNo = "217",
                IsActive = true,
                MarkedForDeletion = true
            },
            new PropertyEntity
            {
                Id = 12,
                WardId = 2,
                TaxZoneId = 1,
                PropertyNo = "218",
                IsActive = true,
                MarkedForDeletion = false
            });
        context.PropertySignatureDetails.AddRange(
            new PropertySignatureDetailsEntity
            {
                Id = 1,
                PropertyId = 10,
                SignAuthorityId = 1,
                UserId = 1,
                NoticeNo = "OLD",
                CreatedDate = new DateTime(2026, 1, 1),
                IsActive = true
            },
            new PropertySignatureDetailsEntity
            {
                Id = 2,
                PropertyId = 10,
                SignAuthorityId = 1,
                UserId = 1,
                NoticeNo = "LATEST",
                CreatedDate = new DateTime(2026, 1, 2),
                IsActive = true
            },
            new PropertySignatureDetailsEntity
            {
                Id = 3,
                PropertyId = 10,
                SignAuthorityId = 2,
                UserId = 1,
                NoticeNo = "TI",
                CreatedDate = new DateTime(2026, 1, 3),
                IsActive = false
            },
            new PropertySignatureDetailsEntity
            {
                Id = 4,
                PropertyId = 11,
                SignAuthorityId = 1,
                UserId = 1,
                NoticeNo = "DELETED_PROPERTY",
                CreatedDate = new DateTime(2026, 1, 1),
                IsActive = true
            },
            new PropertySignatureDetailsEntity
            {
                Id = 5,
                PropertyId = 12,
                SignAuthorityId = 1,
                UserId = 1,
                NoticeNo = "INACTIVE_ZONE",
                CreatedDate = new DateTime(2026, 1, 1),
                IsActive = true
            });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.GetPendingExportSourceDataAsync(CancellationToken.None);

        var row = Assert.Single(result);
        Assert.Equal(10, row.PropertyId);
        Assert.Equal("MM", row.Zone);
        Assert.Equal("MM8-216", row.BuildingNo);
        Assert.Equal("LATEST", row.SrNoticeNo);
        Assert.Equal(new[] { 1 }, row.SignedAuthorityIds);
    }

    [Fact]
    public async Task GetBuildingWiseDataAsync_FiltersByNoticeNoFromSignatureDetails()
    {
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity
        {
            Id = 1,
            WardNo = "MM8",
            ZoneId = 1,
            IsActive = true
        });
        context.PropertyMast.AddRange(
            new PropertyEntity
            {
                Id = 10,
                WardId = 1,
                PropertyNo = "216",
                PartitionNo = "",
                UPICId = "UPIC216",
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyEntity
            {
                Id = 11,
                WardId = 1,
                PropertyNo = "216",
                PartitionNo = "1",
                UPICId = "UPIC216-1",
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyEntity
            {
                Id = 12,
                WardId = 1,
                PropertyNo = "217",
                PartitionNo = "",
                UPICId = "UPIC217",
                IsActive = true,
                MarkedForDeletion = false
            });
        context.PropertyWorkflowDetails.AddRange(
            new PropertyWorkflowDetailsEntity { Id = 101, PropertyId = 10, WorkflowStageId = 1, CreatedDate = DateTime.UtcNow, IsActive = true },
            new PropertyWorkflowDetailsEntity { Id = 102, PropertyId = 11, WorkflowStageId = 1, CreatedDate = DateTime.UtcNow, IsActive = true },
            new PropertyWorkflowDetailsEntity { Id = 103, PropertyId = 12, WorkflowStageId = 1, CreatedDate = DateTime.UtcNow, IsActive = true });
        context.SignAuthorityMaster.Add(new SignAuthorityMasterEntity
        {
            Id = 1,
            AuthorityName = "Clerk",
            AuthorityCode = "CLERK",
            SequenceOrder = 1,
            IsActive = true
        });
        context.PropertySignatureDetails.AddRange(
            new PropertySignatureDetailsEntity
            {
                Id = 201,
                PropertyId = 10,
                SignAuthorityId = 1,
                UserId = 1,
                NoticeNo = "TARGET-NOTICE",
                IsActive = true
            },
            new PropertySignatureDetailsEntity
            {
                Id = 202,
                PropertyId = 12,
                SignAuthorityId = 1,
                UserId = 1,
                NoticeNo = "OTHER-NOTICE",
                IsActive = true
            });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.GetBuildingWiseDataAsync(
            new PropertySignatureBuildingWiseQueryParameters
            {
                WardId = 1,
                WorkflowStageId = 1,
                NoticeNo = "TARGET",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        var row = Assert.Single(result.Items);
        Assert.Equal("MM8-216", row.BuildingNo);
        Assert.Equal("TARGET-NOTICE", row.NoticeNo);
        Assert.Equal(2, row.Units);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPropertyWiseDataAsync_FiltersSearchTypeByFormattedPropertyNo()
    {
        using var context = CreateContext();
        context.WardMaster.Add(new WardEntity
        {
            Id = 1,
            WardNo = "MM8",
            ZoneId = 1,
            IsActive = true
        });
        context.PropertyMast.AddRange(
            new PropertyEntity
            {
                Id = 10,
                WardId = 1,
                PropertyNo = "216",
                PartitionNo = "",
                OwnerName = "Main Owner",
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyEntity
            {
                Id = 11,
                WardId = 1,
                PropertyNo = "216",
                PartitionNo = "1",
                OwnerName = "Partition One Owner",
                IsActive = true,
                MarkedForDeletion = false
            },
            new PropertyEntity
            {
                Id = 12,
                WardId = 1,
                PropertyNo = "216",
                PartitionNo = "2",
                OwnerName = "Partition Two Owner",
                IsActive = true,
                MarkedForDeletion = false
            });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.GetPropertyWiseDataAsync(
            new PropertySignaturePropertyWiseQueryParameters
            {
                PropertyNo = "MM8-216",
                SearchType = "MM8-216-1",
                PageNumber = 1,
                PageSize = 10
            },
            CancellationToken.None);

        var row = Assert.Single(result.Items);
        Assert.Equal(11, row.PropertyId);
        Assert.Equal("MM8-216-1", row.NewPropertyNo);
        Assert.Equal(1, result.TotalCount);
    }
}
