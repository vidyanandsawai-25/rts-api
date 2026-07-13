using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for UlbConfigService to achieve 100% code coverage
/// </summary>
public class UlbConfigServiceTests
{
    [Fact]
    public async Task GetUlbConfigAsync_NoActiveUlb_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        // Add inactive ULB
        context.ULBMasters.Add(new ULBMasterEntity
        {
            Id = 1,
            UlbCode = "ULB001",
            UlbName = "Test ULB",
            IsActive = false
        });
        await context.SaveChangesAsync();

        var mockRepo = new Mock<IRepository<ULBMasterEntity>>();
        mockRepo.Setup(r => r.GetQueryable()).Returns(context.ULBMasters.AsQueryable());
        var mockImageRepo = new Mock<IRepository<UlbImageMasterEntity>>();
        mockImageRepo.Setup(r => r.GetQueryable()).Returns(context.UlbImageMasters.AsQueryable());
        var mockDocRepo = new Mock<IRepository<DocumentEntity>>();
        mockDocRepo.Setup(r => r.GetQueryable()).Returns(context.Documents.AsQueryable());

        var service = new UlbConfigService(mockRepo.Object, mockImageRepo.Object, mockDocRepo.Object);
        var result = await service.GetUlbConfigAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUlbConfigAsync_ActiveUlbExists_ReturnsDto()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.ULBMasters.Add(new ULBMasterEntity
        {
            Id = 1,
            UlbCode = "ULB001",
            UlbName = "Test ULB",
            UlbNameLocal = "??????? ??????",
            UlbLogo = "logo.png",
            EmailId = "ulb@example.com",
            MobileNo = "9876543210",
            WebsiteUrl = "https://ulb.example.com",
            UlbAddress = "123 Main Street",
            State = "Maharashtra",
            District = "Mumbai",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var mockRepo = new Mock<IRepository<ULBMasterEntity>>();
        mockRepo.Setup(r => r.GetQueryable()).Returns(context.ULBMasters.AsQueryable());
        var mockImageRepo = new Mock<IRepository<UlbImageMasterEntity>>();
        mockImageRepo.Setup(r => r.GetQueryable()).Returns(context.UlbImageMasters.AsQueryable());
        var mockDocRepo = new Mock<IRepository<DocumentEntity>>();
        mockDocRepo.Setup(r => r.GetQueryable()).Returns(context.Documents.AsQueryable());

        var service = new UlbConfigService(mockRepo.Object, mockImageRepo.Object, mockDocRepo.Object);
        var result = await service.GetUlbConfigAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result.UlbId);
        Assert.Equal("ULB001", result.UlbCode);
        Assert.Equal("Test ULB", result.UlbName);
        Assert.Equal("??????? ??????", result.UlbNameLocal);
        Assert.Equal("logo.png", result.UlbLogo);
        Assert.Equal("ulb@example.com", result.EmailId);
        Assert.Equal("9876543210", result.MobileNo);
        Assert.Equal("https://ulb.example.com", result.WebsiteUrl);
        Assert.Equal("123 Main Street", result.UlbAddress);
        Assert.Equal("Maharashtra", result.State);
        Assert.Equal("Mumbai", result.District);
    }

    [Fact]
    public async Task GetUlbConfigAsync_MultipleActiveUlbs_ReturnsFirstOne()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.ULBMasters.Add(new ULBMasterEntity
        {
            Id = 2,
            UlbCode = "ULB002",
            UlbName = "Second ULB",
            IsActive = true
        });

        context.ULBMasters.Add(new ULBMasterEntity
        {
            Id = 1,
            UlbCode = "ULB001",
            UlbName = "First ULB",
            IsActive = true
        });

        await context.SaveChangesAsync();

        var mockRepo = new Mock<IRepository<ULBMasterEntity>>();
        mockRepo.Setup(r => r.GetQueryable()).Returns(context.ULBMasters.AsQueryable());
        var mockImageRepo = new Mock<IRepository<UlbImageMasterEntity>>();
        mockImageRepo.Setup(r => r.GetQueryable()).Returns(context.UlbImageMasters.AsQueryable());
        var mockDocRepo = new Mock<IRepository<DocumentEntity>>();
        mockDocRepo.Setup(r => r.GetQueryable()).Returns(context.Documents.AsQueryable());

        var service = new UlbConfigService(mockRepo.Object, mockImageRepo.Object, mockDocRepo.Object);
        var result = await service.GetUlbConfigAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result.UlbId);
        Assert.Equal("First ULB", result.UlbName);
    }

    [Fact]
    public async Task GetUlbConfigAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.ULBMasters.Add(new ULBMasterEntity
        {
            Id = 1,
            UlbCode = "ULB001",
            UlbName = "Test ULB",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var mockRepo = new Mock<IRepository<ULBMasterEntity>>();
        mockRepo.Setup(r => r.GetQueryable()).Returns(context.ULBMasters.AsQueryable());
        var mockImageRepo = new Mock<IRepository<UlbImageMasterEntity>>();
        mockImageRepo.Setup(r => r.GetQueryable()).Returns(context.UlbImageMasters.AsQueryable());
        var mockDocRepo = new Mock<IRepository<DocumentEntity>>();
        mockDocRepo.Setup(r => r.GetQueryable()).Returns(context.Documents.AsQueryable());

        var service = new UlbConfigService(mockRepo.Object, mockImageRepo.Object, mockDocRepo.Object);
        var cts = new CancellationTokenSource();

        var result = await service.GetUlbConfigAsync(cts.Token);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUlbConfigAsync_EmptyDatabase_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var mockRepo = new Mock<IRepository<ULBMasterEntity>>();
        mockRepo.Setup(r => r.GetQueryable()).Returns(context.ULBMasters.AsQueryable());
        var mockImageRepo = new Mock<IRepository<UlbImageMasterEntity>>();
        mockImageRepo.Setup(r => r.GetQueryable()).Returns(context.UlbImageMasters.AsQueryable());
        var mockDocRepo = new Mock<IRepository<DocumentEntity>>();
        mockDocRepo.Setup(r => r.GetQueryable()).Returns(context.Documents.AsQueryable());

        var service = new UlbConfigService(mockRepo.Object, mockImageRepo.Object, mockDocRepo.Object);
        var result = await service.GetUlbConfigAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUlbConfigAsync_ActiveUlbAndBackgroundExists_ReturnsDtoWithBackground()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        // Add active ULB
        context.ULBMasters.Add(new ULBMasterEntity
        {
            Id = 1,
            UlbCode = "ULB001",
            UlbName = "Test ULB",
            IsActive = true
        });

        // Add active background document
        var doc = DocumentEntity.Create(
            uploadedByUserId: 1,
            fileName: "bg.jpg",
            originalFileName: "bg.jpg",
            fileExtension: ".jpg",
            mimeType: "image/jpeg",
            fileSizeBytes: 1000,
            storagePath: "uploads/bg.jpg",
            documentType: "Background"
        );
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        // Add UlbImageMaster pointing to that Document
        context.UlbImageMasters.Add(new UlbImageMasterEntity
        {
            Id = 1,
            ImageType = "Background",
            ImageId = doc.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var mockRepo = new Mock<IRepository<ULBMasterEntity>>();
        mockRepo.Setup(r => r.GetQueryable()).Returns(context.ULBMasters.AsQueryable());

        var mockImageRepo = new Mock<IRepository<UlbImageMasterEntity>>();
        mockImageRepo.Setup(r => r.GetQueryable()).Returns(context.UlbImageMasters.AsQueryable());

        var mockDocRepo = new Mock<IRepository<DocumentEntity>>();
        mockDocRepo.Setup(r => r.GetQueryable()).Returns(context.Documents.AsQueryable());

        var service = new UlbConfigService(mockRepo.Object, mockImageRepo.Object, mockDocRepo.Object);
        var result = await service.GetUlbConfigAsync();

        Assert.NotNull(result);
        Assert.Equal($"/api/UlbImageMaster/{doc.DocumentGuid}/view", result.UlbBackground);
    }
}
