using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure;

public class ApartmentQcTopSectionBelowFlexRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetWorkflowStagesAsync_NoDetailRow_ReturnsNotCompletedWithNullAudit()
    {
        using var context = CreateContext();
        context.PropertyWorkflowStageMaster.Add(new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GIS Verification",
            Description = "Verify GIS mapping",
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionBelowFlexRepository(context);
        var result = await repository.GetWorkflowStagesAsync(propertyId: 100);

        var stage = Assert.Single(result);
        Assert.Equal("GIS Verification", stage.StageName);
        Assert.False(stage.IsCompleted);
        Assert.Null(stage.CreatedBy);
        Assert.Null(stage.CreatedDate);
    }

    [Fact]
    public async Task GetWorkflowStagesAsync_LatestDetailRowWins_WhenMultipleRowsExist()
    {
        using var context = CreateContext();
        context.PropertyWorkflowStageMaster.Add(new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "Assessment",
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        context.PropertyWorkflowDetails.AddRange(
            new PropertyWorkflowDetailsEntity
            {
                Id = 1,
                PropertyId = 100,
                WorkflowStageId = 1,
                CurrentStatus = false,
                IsActive = true,
                CreatedBy = 10,
                CreatedDate = new DateTime(2026, 1, 1)
            },
            new PropertyWorkflowDetailsEntity
            {
                Id = 2,
                PropertyId = 100,
                WorkflowStageId = 1,
                CurrentStatus = true,
                IsActive = true,
                CreatedBy = 20,
                CreatedDate = new DateTime(2026, 2, 1),
                UpdatedBy = 21,
                UpdatedDate = new DateTime(2026, 2, 2)
            });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionBelowFlexRepository(context);
        var result = await repository.GetWorkflowStagesAsync(propertyId: 100);

        var stage = Assert.Single(result);
        Assert.True(stage.IsCompleted);
        Assert.Equal(20, stage.CreatedBy);
        Assert.Equal(21, stage.UpdatedBy);
    }

    [Fact]
    public async Task GetWorkflowStagesAsync_InactiveStage_Excluded()
    {
        using var context = CreateContext();
        context.PropertyWorkflowStageMaster.AddRange(
            new PropertyWorkflowStageMasterEntity { Id = 1, StageName = "Active Stage", DisplayOrder = 1, IsActive = true, CreatedDate = DateTime.Now },
            new PropertyWorkflowStageMasterEntity { Id = 2, StageName = "Inactive Stage", DisplayOrder = 2, IsActive = false, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionBelowFlexRepository(context);
        var result = await repository.GetWorkflowStagesAsync(propertyId: 100);

        var stage = Assert.Single(result);
        Assert.Equal("Active Stage", stage.StageName);
    }

    [Fact]
    public async Task GetCertificateTypesAsync_NoCertificate_ReturnsNotIssued()
    {
        using var context = CreateContext();
        context.PropertyCertificateTypeMasters.Add(new PropertyCertificateTypeMasterEntity
        {
            Id = 1,
            CertificateTypeCode = "CC",
            CertificateTypeName = "Completion Certificate",
            DisplayOrder = 1,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionBelowFlexRepository(context);
        var result = await repository.GetCertificateTypesAsync(propertyId: 100);

        var type = Assert.Single(result);
        Assert.Equal("CC", type.CertificateTypeCode);
        Assert.False(type.IsIssued);
        Assert.Null(type.CertificateNo);
    }

    [Fact]
    public async Task GetCertificateTypesAsync_ActivePropertyLevelCertificate_ReturnsIssuedWithAudit()
    {
        using var context = CreateContext();
        context.PropertyCertificateTypeMasters.Add(new PropertyCertificateTypeMasterEntity
        {
            Id = 1,
            CertificateTypeCode = "OC",
            CertificateTypeName = "Occupancy Certificate",
            DisplayOrder = 1,
            IsActive = true
        });
        var cert = PropertyCertificateEntity.Create(propertyId: 100, certificateTypeId: 1, certificateNo: "OC-2026-001", issueDate: new DateTime(2026, 3, 1));
        cert.CreatedBy = 30;
        cert.CreatedDate = new DateTime(2026, 3, 1);
        context.PropertyCertificates.Add(cert);
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionBelowFlexRepository(context);
        var result = await repository.GetCertificateTypesAsync(propertyId: 100);

        var type = Assert.Single(result);
        Assert.True(type.IsIssued);
        Assert.Equal("OC-2026-001", type.CertificateNo);
        Assert.Equal(30, type.CreatedBy);
    }

    [Fact]
    public async Task GetCertificateTypesAsync_FloorLevelCertificate_ExcludedFromPropertyLevelPanel()
    {
        using var context = CreateContext();
        context.PropertyCertificateTypeMasters.Add(new PropertyCertificateTypeMasterEntity
        {
            Id = 1,
            CertificateTypeCode = "CC",
            CertificateTypeName = "Completion Certificate",
            DisplayOrder = 1,
            IsActive = true
        });
        var floorCert = PropertyCertificateEntity.Create(propertyId: 100, certificateTypeId: 1, certificateNo: "CC-FLOOR-1", issueDate: DateTime.Now, propertyDetailsId: 55);
        context.PropertyCertificates.Add(floorCert);
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionBelowFlexRepository(context);
        var result = await repository.GetCertificateTypesAsync(propertyId: 100);

        var type = Assert.Single(result);
        Assert.False(type.IsIssued);
    }

    [Fact]
    public async Task GetCertificateTypesAsync_InactiveCertificateType_Excluded()
    {
        using var context = CreateContext();
        context.PropertyCertificateTypeMasters.AddRange(
            new PropertyCertificateTypeMasterEntity { Id = 1, CertificateTypeCode = "CC", CertificateTypeName = "Completion Certificate", DisplayOrder = 1, IsActive = true },
            new PropertyCertificateTypeMasterEntity { Id = 2, CertificateTypeCode = "OLD", CertificateTypeName = "Retired Type", DisplayOrder = 2, IsActive = false });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcTopSectionBelowFlexRepository(context);
        var result = await repository.GetCertificateTypesAsync(propertyId: 100);

        var type = Assert.Single(result);
        Assert.Equal("CC", type.CertificateTypeCode);
    }
}
