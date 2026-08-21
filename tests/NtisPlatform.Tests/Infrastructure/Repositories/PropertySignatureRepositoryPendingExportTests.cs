using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
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
    public async Task GetSignAuthorityIdByUserRoleAsync_ReturnsAuthorityIdForActivePtisRole()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.UserMasters.Add(new UserEntity
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = createdDate
        });
        context.DepartmentMasters.Add(new DepartmentMasterEntity
        {
            Id = 10,
            DepartmentCode = "PTIS",
            DepartmentName = "PTIS",
            IsActive = true,
            CreatedDate = createdDate
        });
        context.UserRoleMasterEntity.Add(new UserRoleMasterEntity
        {
            Id = 20,
            UserRoleName = "Tax Inspector",
            DepartmentId = 10,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.UserRoleAllocation.Add(new UserRoleAllocationEntity
        {
            Id = 30,
            UserId = 1,
            UserRoleId = 20,
            DepartmentId = 10,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.SignAuthorityMaster.Add(new SignAuthorityMasterEntity
        {
            Id = 2,
            AuthorityName = "Tax Inspector",
            AuthorityCode = "TI",
            SequenceOrder = 2,
            IsActive = true,
            CreatedDate = createdDate
        });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.GetSignAuthorityIdByUserRoleAsync(1, CancellationToken.None);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task UpdateSignAsync_UpdatesCurrentRowAndInsertsNextPendingRow()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.PropertySignatureDetails.Add(new PropertySignatureDetailsEntity
        {
            Id = 10,
            UserId = 5,
            PropertyId = 100,
            SignAuthorityId = 1,
            NoticeNo = "N100",
            SignStatus = "PendingToClerk",
            IsActive = true,
            CreatedDate = createdDate,
            CreatedBy = 5
        });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.UpdateSignAsync(
            new PropertySignatureUpdateSignCommandDto
            {
                SignatureId = 10,
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                NoticeNo = "N100",
                IsActive = true,
                UpdatedBy = 5,
                UpdatedSignStatus = "ApprovedByClerk",
                NextSignAuthorityId = 2,
                NextSignStatus = "PendingToTI"
            },
            CancellationToken.None);

        Assert.True(result);
        var current = await context.PropertySignatureDetails.SingleAsync(x => x.Id == 10);
        Assert.Equal("ApprovedByClerk", current.SignStatus);
        Assert.Equal(5, current.UpdatedBy);

        var next = await context.PropertySignatureDetails.SingleAsync(x => x.PropertyId == 100 && x.SignAuthorityId == 2);
        Assert.Equal(5, next.UserId);
        Assert.Equal("N100", next.NoticeNo);
        Assert.Equal("PendingToTI", next.SignStatus);
        Assert.True(next.IsActive);
    }

    [Fact]
    public async Task UpdateSignAsync_WhenNextRowAlreadyExists_UpdatesCurrentOnly()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.PropertySignatureDetails.AddRange(
            new PropertySignatureDetailsEntity
            {
                Id = 10,
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                NoticeNo = "N100",
                SignStatus = "PendingToClerk",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertySignatureDetailsEntity
            {
                Id = 11,
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 2,
                NoticeNo = "N100",
                SignStatus = "PendingToTI",
                IsActive = true,
                CreatedDate = createdDate
            });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.UpdateSignAsync(
            new PropertySignatureUpdateSignCommandDto
            {
                SignatureId = 10,
                UserId = 5,
                PropertyId = 100,
                SignAuthorityId = 1,
                NoticeNo = "N100",
                IsActive = true,
                UpdatedBy = 5,
                UpdatedSignStatus = "ApprovedByClerk",
                NextSignAuthorityId = 2,
                NextSignStatus = "PendingToTI"
            },
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(2, await context.PropertySignatureDetails.CountAsync(x => x.PropertyId == 100));
        Assert.Equal("ApprovedByClerk", (await context.PropertySignatureDetails.SingleAsync(x => x.Id == 10)).SignStatus);
    }

    [Fact]
    public async Task GetUpdateSignSourceAsync_ReturnsOnlyMatchingActivePendingRecord()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.SignAuthorityMaster.Add(new SignAuthorityMasterEntity
        {
            Id = 1,
            AuthorityName = "Clerk",
            AuthorityCode = "CLERK",
            SequenceOrder = 1,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertySignatureDetails.Add(new PropertySignatureDetailsEntity
        {
            Id = 10,
            UserId = 5,
            PropertyId = 100,
            SignAuthorityId = 1,
            NoticeNo = "N100",
            SignStatus = "PendingToClerk",
            IsActive = true,
            CreatedDate = createdDate
        });
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(context, Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.GetUpdateSignSourceAsync(5, 100, 1, "pendingtoclerk", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10, result!.SignatureId);
        Assert.Equal("CLERK", result.AuthorityCode);
        Assert.Equal("N100", result.NoticeNo);
    }

    [Fact]
    public async Task GetPendingSignSourceDataAsync_ReturnsSignatureRowsWithUnitsDemandAndAuthorityCode()
    {
        using var context = CreateContext();
        var createdDate = new DateTime(2026, 1, 1);
        context.WardMaster.Add(new WardEntity
        {
            Id = 1,
            WardNo = "WE2",
            ZoneId = 1,
            IsActive = true,
            CreatedDate = createdDate
        });
        context.PropertyMast.AddRange(
            new PropertyEntity
            {
                Id = 100,
                WardId = 1,
                TaxZoneId = 1,
                PropertyNo = "4",
                PartitionNo = "",
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyEntity
            {
                Id = 101,
                WardId = 1,
                TaxZoneId = 1,
                PropertyNo = "4",
                PartitionNo = "1",
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            },
            new PropertyEntity
            {
                Id = 102,
                WardId = 1,
                TaxZoneId = 1,
                PropertyNo = "4",
                PartitionNo = "2",
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = createdDate
            });
        context.SignAuthorityMaster.AddRange(
            new SignAuthorityMasterEntity
            {
                Id = 1,
                AuthorityName = "Clerk",
                AuthorityCode = "CLERK",
                SequenceOrder = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new SignAuthorityMasterEntity
            {
                Id = 2,
                AuthorityName = "Tax Inspector",
                AuthorityCode = "TI",
                SequenceOrder = 2,
                IsActive = true,
                CreatedDate = createdDate
            });
        context.PropertySignatureDetails.AddRange(
            new PropertySignatureDetailsEntity
            {
                Id = 20,
                PropertyId = 100,
                UserId = 5,
                SignAuthorityId = 2,
                NoticeNo = "WE0200040000",
                SignStatus = "Pending",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertySignatureDetailsEntity
            {
                Id = 21,
                PropertyId = 100,
                UserId = 99,
                SignAuthorityId = 1,
                NoticeNo = "MMMAJOR40001B7P",
                SignStatus = "Approved",
                IsActive = true,
                CreatedDate = createdDate
            },
            new PropertySignatureDetailsEntity
            {
                Id = 22,
                PropertyId = 101,
                UserId = 5,
                SignAuthorityId = 2,
                NoticeNo = "WE0200040000A",
                SignStatus = "Pending",
                IsActive = true,
                CreatedDate = createdDate
            });
        context.TaxMaster.AddRange(
            new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "TaxTotal",
                TaxName = "TaxTotal",
                TaxCategoryId = 1,
                IsActive = true,
                CreatedDate = createdDate
            },
            new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "GENERAL",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                IsActive = true,
                CreatedDate = createdDate
            });
        context.TransMast.AddRange(
            CreateTransMast(1, 100, 1, 100m, createdDate),
            CreateTransMast(2, 100, 2, 999m, createdDate),
            CreateTransMast(3, 101, 1, 250m, createdDate));
        await context.SaveChangesAsync();
        var repository = new PropertySignatureRepository(
            context,
            Mock.Of<ILogger<PropertySignatureRepository>>());

        var result = await repository.GetPendingSignSourceDataAsync(2, null, CancellationToken.None);

        Assert.Equal(6, result.Count);
        Assert.All(result, row =>
        {
            Assert.Equal(2, row.SignAuthorityId);
            Assert.Equal("WE2", row.WardNo);
            Assert.Equal("4", row.PropertyNo);
            Assert.Equal("Pending", row.SignStatus);
            Assert.Equal("TI", row.AuthorityCode);
        });
        Assert.Equal(new[] { 20, 22 }, result.Select(row => row.SignatureId).Distinct().OrderBy(id => id));
        Assert.Equal(new[] { 100, 101 }, result.Select(row => row.PropertyId).Distinct().OrderBy(id => id));
        Assert.Equal(700m, result.Sum(row => row.UnitDemand));
        Assert.Contains(result, row => row.UnitPropertyId == 102 && row.UnitDemand == 0m);

        var filteredResult = await repository.GetPendingSignSourceDataAsync(
            2,
            "we0200040000",
            CancellationToken.None);

        Assert.Equal(3, filteredResult.Count);
        Assert.All(filteredResult, row => Assert.Equal("WE0200040000", row.SrNoticeNo));

        var emptyResult = await repository.GetPendingSignSourceDataAsync(
            2,
            "WE020004000",
            CancellationToken.None);

        Assert.Empty(emptyResult);
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

    private static TransMastEntity CreateTransMast(
        int id,
        int propertyId,
        int taxId,
        decimal taxAmount,
        DateTime createdDate)
        => new()
        {
            Id = id,
            PropertyId = propertyId,
            FinanceYearId = 1,
            CalculationType = "RV",
            CalculationValue = 0,
            TaxId = taxId,
            TaxAmount = taxAmount,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = createdDate
        };
}
