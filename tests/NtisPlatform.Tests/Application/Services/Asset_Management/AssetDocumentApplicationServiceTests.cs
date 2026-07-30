using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.IO;
using NtisPlatform.Application.DTOs.Document;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

public class AssetDocumentApplicationServiceTests
{
    private static AssetDocumentApplicationService CreateService(
        out Mock<IAssetDocumentService> documentService,
        out Mock<IUnitOfWork> unitOfWork,
        out Mock<IRepository<AssetDocumentDefinitionEntity, int>> documentDefinitionRepo,
        out Mock<IRepository<AssetMasterEntity, int>> assetMasterRepo)
    {
        return CreateServiceFull(
            out documentService,
            out unitOfWork,
            out documentDefinitionRepo,
            out assetMasterRepo,
            out _,
            out _);
    }

    private static AssetDocumentApplicationService CreateServiceFull(
        out Mock<IAssetDocumentService> documentService,
        out Mock<IUnitOfWork> unitOfWork,
        out Mock<IRepository<AssetDocumentDefinitionEntity, int>> documentDefinitionRepo,
        out Mock<IRepository<AssetMasterEntity, int>> assetMasterRepo,
        out Mock<IDocumentApplicationService> globalDocService,
        out Mock<IModuleLookupService> moduleLookupService)
    {
        documentService = new Mock<IAssetDocumentService>();
        unitOfWork = new Mock<IUnitOfWork>();
        documentDefinitionRepo = new Mock<IRepository<AssetDocumentDefinitionEntity, int>>();
        assetMasterRepo = new Mock<IRepository<AssetMasterEntity, int>>();
        globalDocService = new Mock<IDocumentApplicationService>();
        
        var mockConfig = new Mock<IConfiguration>();
        // Return null/empty sections so FileValidationHelper defaults kick in
        var mockSection = new Mock<IConfigurationSection>();
        mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockSection.Object);
        var fileValidationHelper = new FileValidationHelper(mockConfig.Object);

        moduleLookupService = new Mock<IModuleLookupService>();
        moduleLookupService.Setup(s => s.GetDepartmentAndModuleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 2));

        var logger = new Mock<ILogger<AssetDocumentApplicationService>>();

        return new AssetDocumentApplicationService(
            documentService.Object,
            unitOfWork.Object,
            documentDefinitionRepo.Object,
            assetMasterRepo.Object,
            globalDocService.Object,
            fileValidationHelper,
            moduleLookupService.Object,
            logger.Object);
    }

    [Fact]
    public async Task GetDocumentsByAssetAsync_Throws_WhenAssetIdInvalid()
    {
        var service = CreateService(out _, out _, out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetDocumentsByAssetAsync(0));
    }

    [Fact]
    public async Task GetDocumentsByAssetAsync_ReturnsMappedDtos()
    {
        var service = CreateService(out var documentService, out _, out _, out _);
        var entity = AssetDocumentEntity.Create(10, 1);

        documentService.Setup(s => s.GetLatestByAssetIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentEntity> { entity });

        var result = await service.GetDocumentsByAssetAsync(10);

        Assert.Single(result);
        Assert.Equal(10, result[0].AssetId);
    }

    [Fact]
    public async Task GetGroupedDocumentsByAssetAsync_Throws_WhenAssetNotFound()
    {
        var service = CreateService(out _, out _, out _, out var assetRepo);
        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetGroupedDocumentsByAssetAsync(10));
    }

    [Fact]
    public async Task GetGroupedDocumentsByAssetAsync_ReturnsGroupedGallery_WhenAssetExists()
    {
        var service = CreateService(out var documentService, out _, out var documentDefinitionRepo, out var assetRepo);
        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity { AssetCategoryId = 5, AssetTypeId = 6 } });

        documentDefinitionRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetDocumentDefinitionEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentDefinitionEntity>
            {
                new AssetDocumentDefinitionEntity { Id = 1, DocumentCode = "DOC", DocumentName = "Document Definition", AssetCategoryId = 5, AssetTypeId = 6, DisplayOrder = 1 }
            });

        var existingDoc = AssetDocumentEntity.Create(10, 1);
        documentService.Setup(s => s.GetLatestByAssetIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentEntity> { existingDoc });

        var result = await service.GetGroupedDocumentsByAssetAsync(10);

        Assert.Equal(10, result.AssetId);
        Assert.Single(result.DocumentTypes);
        Assert.True(result.DocumentTypes[0].HasDocument);
        Assert.Equal(1, result.DocumentTypes[0].DocumentCount);
    }

    [Fact]
    public async Task GetDocumentTypesWithStatusAsync_Throws_WhenAssetNotFound()
    {
        var service = CreateService(out _, out _, out _, out var assetRepo);
        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetDocumentTypesWithStatusAsync(10));
    }

    [Fact]
    public async Task GetDocumentTypesWithStatusAsync_ReturnsTypesWithStatusAndDocumentData()
    {
        var service = CreateService(out var documentService, out _, out var documentDefinitionRepo, out var assetRepo);

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        documentDefinitionRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetDocumentDefinitionEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentDefinitionEntity>
            {
                new AssetDocumentDefinitionEntity { Id = 1, DocumentCode = "DOC", DocumentName = "Document Definition" },
                new AssetDocumentDefinitionEntity { Id = 2, DocumentCode = "DOC2", DocumentName = "Document Definition 2" }
            });

        var docGuid = Guid.NewGuid();
        var doc = AssetDocumentEntity.CreateWithDocument(10, 1, 100);
        var binding = new DocumentBindingEntity
        {
            Id = 100,
            Document = new DocumentEntity
            {
                DocumentGuid = docGuid,
                OriginalFileName = "document.pdf",
                MimeType = "application/pdf",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        typeof(AssetDocumentEntity).GetProperty(nameof(AssetDocumentEntity.DocumentBinding))!
            .SetValue(doc, binding);

        documentService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentEntity> { doc });

        var result = await service.GetDocumentTypesWithStatusAsync(10);

        Assert.Equal(2, result.Count);
        var first = result.First(r => r.DocumentDefinitionId == 1);
        Assert.True(first.HasDocument);
        Assert.Equal(docGuid, first.DocumentGuid);
        Assert.Equal("document.pdf", first.FileName);
        Assert.Equal("application/pdf", first.MimeType);

        var second = result.First(r => r.DocumentDefinitionId == 2);
        Assert.False(second.HasDocument);
        Assert.Null(second.DocumentGuid);
    }

    [Fact]
    public async Task GetDocumentTypesWithStatusAsync_ChecksGetSafeMethods_WhenDocumentInactiveOrDeleted()
    {
        var service = CreateService(out var documentService, out _, out var documentDefinitionRepo, out var assetRepo);

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        documentDefinitionRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetDocumentDefinitionEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentDefinitionEntity>
            {
                new AssetDocumentDefinitionEntity { Id = 1, DocumentCode = "DOC", DocumentName = "Document Definition" }
            });

        var doc = AssetDocumentEntity.CreateWithDocument(10, 1, 100);
        
        // Setup cases where GetSafeDocumentGuid returns null:
        // 1. binding is null
        documentService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentEntity> { doc });

        var result1 = await service.GetDocumentTypesWithStatusAsync(10);
        Assert.Null(result1[0].DocumentGuid);

        // 2. document is inactive/deleted
        var bindingInactive = new DocumentBindingEntity
        {
            Id = 100,
            Document = new DocumentEntity
            {
                DocumentGuid = Guid.NewGuid(),
                IsActive = false
            }
        };
        typeof(AssetDocumentEntity).GetProperty(nameof(AssetDocumentEntity.DocumentBinding))!.SetValue(doc, bindingInactive);

        var result2 = await service.GetDocumentTypesWithStatusAsync(10);
        Assert.Null(result2[0].DocumentGuid);
    }

    [Fact]
    public async Task BulkSaveAllAsync_ExecutesBulkSave_AndDisablesExistingDocument()
    {
        var service = CreateService(out var documentService, out var unitOfWork, out var documentDefinitionRepo, out var assetRepo);

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        documentDefinitionRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetDocumentDefinitionEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentDefinitionEntity>());

        var existingDoc = AssetDocumentEntity.Create(10, 1);
        documentService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentEntity> { existingDoc });

        var bulkDto = new AssetDocumentBulkSaveDto
        {
            AssetId = 10,
            Documents = new List<AssetDocumentItemDto>
            {
                new AssetDocumentItemDto
                {
                    DocumentDefinitionId = 1,
                    IsEnabled = false
                }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, 42);

        Assert.Equal(1, response.DisabledCount);
        documentService.Verify(s => s.ToggleEnabledAsync(existingDoc.Id, false, 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkSaveAllAsync_CreatesAndUpdatesDocuments_WhenIsEnabledIsTrue()
    {
        var service = CreateService(out var documentService, out var unitOfWork, out var documentDefinitionRepo, out var assetRepo);

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        documentDefinitionRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetDocumentDefinitionEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentDefinitionEntity>());

        // 1. Doc definition 1: Doesn't exist, will be created
        // 2. Doc definition 2: Exists but inactive, will be updated and enabled
        // 3. Doc definition 3: Exists and active, will be updated
        var existingDoc2 = AssetDocumentEntity.Create(10, 2);
        var existingDoc3 = AssetDocumentEntity.Create(10, 3);
        
        typeof(AssetDocumentEntity).GetProperty(nameof(AssetDocumentEntity.Id))!.SetValue(existingDoc2, 202);
        typeof(AssetDocumentEntity).GetProperty(nameof(AssetDocumentEntity.Id))!.SetValue(existingDoc3, 203);
        typeof(AssetDocumentEntity).GetProperty(nameof(AssetDocumentEntity.IsActive))!.SetValue(existingDoc2, false);
        typeof(AssetDocumentEntity).GetProperty(nameof(AssetDocumentEntity.IsActive))!.SetValue(existingDoc3, true);

        documentService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentEntity> { existingDoc2, existingDoc3 });

        documentService.Setup(s => s.CreateAsync(10, 1, 1, "Remarks1", 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(101);

        var bulkDto = new AssetDocumentBulkSaveDto
        {
            AssetId = 10,
            Documents = new List<AssetDocumentItemDto>
            {
                new AssetDocumentItemDto { DocumentDefinitionId = 1, IsEnabled = true, DisplayOrder = 1, Remarks = "Remarks1" },
                new AssetDocumentItemDto { DocumentDefinitionId = 2, IsEnabled = true, DisplayOrder = 2, Remarks = "Remarks2" },
                new AssetDocumentItemDto { DocumentDefinitionId = 3, IsEnabled = true, DisplayOrder = 3, Remarks = "Remarks3" }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, 42);

        Assert.Equal(3, response.EnabledCount);
        documentService.Verify(s => s.CreateAsync(10, 1, 1, "Remarks1", 42, It.IsAny<CancellationToken>()), Times.Once);
        documentService.Verify(s => s.ToggleEnabledAsync(101, true, 42, It.IsAny<CancellationToken>()), Times.Once);
        
        documentService.Verify(s => s.UpdateAsync(202, 2, "Remarks2", 42, It.IsAny<CancellationToken>()), Times.Once);
        documentService.Verify(s => s.ToggleEnabledAsync(202, true, 42, It.IsAny<CancellationToken>()), Times.Once);

        documentService.Verify(s => s.UpdateAsync(203, 3, "Remarks3", 42, It.IsAny<CancellationToken>()), Times.Once);
        documentService.Verify(s => s.ToggleEnabledAsync(203, true, 42, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveAllAsync_CapturesItemException_InErrorsList()
    {
        var service = CreateService(out var documentService, out var unitOfWork, out var documentDefinitionRepo, out var assetRepo);

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetMasterEntity> { new AssetMasterEntity() });

        documentDefinitionRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetDocumentDefinitionEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentDefinitionEntity>());

        documentService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentEntity>());

        documentService.Setup(s => s.CreateAsync(10, 1, It.IsAny<int?>(), It.IsAny<string?>(), 42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var bulkDto = new AssetDocumentBulkSaveDto
        {
            AssetId = 10,
            Documents = new List<AssetDocumentItemDto>
            {
                new AssetDocumentItemDto
                {
                    DocumentDefinitionId = 1,
                    IsEnabled = true
                }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, 42);

        Assert.Single(response.Errors);
        Assert.Contains("DB error", response.Errors[0]);
    }

    [Fact]
    public async Task BulkSaveAllAsync_RollsBackTransaction_OnUnhandledException()
    {
        var service = CreateService(out var documentService, out var unitOfWork, out var documentDefinitionRepo, out var assetRepo);

        documentService.Setup(s => s.GetLatestByAssetIdIncludingInactiveAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetDocumentEntity>());

        assetRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AssetMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fatal error"));

        var bulkDto = new AssetDocumentBulkSaveDto { AssetId = 10 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkSaveAllAsync(bulkDto, 42));

        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public async Task SaveWithUploadAsync_Throws_WhenInputsAreInvalid(int assetId, int docDefId, int userId)
    {
        var service = CreateServiceFull(out _, out _, out _, out _, out _, out _);
        var request = new AssetDocumentSaveWithUploadDto
        {
            AssetId = assetId,
            DocumentDefinitionId = docDefId
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveWithUploadAsync(request, userId));
    }

    [Fact]
    public async Task SaveWithUploadAsync_Throws_WhenFileIsNull()
    {
        var service = CreateServiceFull(out _, out _, out _, out _, out _, out _);
        var request = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            DocumentFile = null
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveWithUploadAsync(request, 42));
    }

    [Fact]
    public async Task SaveWithUploadAsync_Throws_WhenFileIsEmpty()
    {
        var service = CreateServiceFull(out _, out _, out _, out _, out _, out _);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        
        var request = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            DocumentFile = mockFile.Object
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveWithUploadAsync(request, 42));
    }

    [Fact]
    public async Task SaveWithUploadAsync_Throws_WhenRemarksTooLong()
    {
        var service = CreateServiceFull(out _, out _, out _, out _, out _, out _);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);

        var request = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            DocumentFile = mockFile.Object,
            Remarks = new string('a', 501)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveWithUploadAsync(request, 42));
    }

    [Fact]
    public async Task SaveWithUploadAsync_Throws_WhenFileFormatIsInvalid()
    {
        // Mock config setting to only allow .pdf, and feed in a .exe file
        var mockDocumentService = new Mock<IAssetDocumentService>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockDocumentDefinitionRepo = new Mock<IRepository<AssetDocumentDefinitionEntity, int>>();
        var mockAssetMasterRepo = new Mock<IRepository<AssetMasterEntity, int>>();
        var mockGlobalDocService = new Mock<IDocumentApplicationService>();
        var mockDeptMasterRepo = new Mock<IRepository<DepartmentMasterEntity, int>>();
        var mockModuleMasterRepo = new Mock<IRepository<ModuleMasterEntity, int>>();
        
        var mockConfig = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockSection.Object);
        var fileValidationHelper = new FileValidationHelper(mockConfig.Object);

        var service = new AssetDocumentApplicationService(
            mockDocumentService.Object,
            mockUnitOfWork.Object,
            mockDocumentDefinitionRepo.Object,
            mockAssetMasterRepo.Object,
            mockGlobalDocService.Object,
            fileValidationHelper,
            new Mock<IModuleLookupService>().Object,
            new Mock<ILogger<AssetDocumentApplicationService>>().Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("malicious.exe");
        mockFile.Setup(f => f.ContentType).Returns("application/x-msdownload");

        var request = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            DocumentFile = mockFile.Object
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveWithUploadAsync(request, 42));
    }

    [Fact]
    public async Task SaveWithUploadAsync_Throws_WhenDeptOrModuleNotFound()
    {
        var service = CreateServiceFull(
            out var docService,
            out _,
            out _,
            out _,
            out _,
            out var moduleLookupService);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("doc.pdf");
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");

        var request = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            DocumentFile = mockFile.Object
        };

        docService.Setup(s => s.CreateAsync(10, 1, It.IsAny<int?>(), It.IsAny<string?>(), 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500);

        docService.Setup(s => s.GetByIdAsync(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssetDocumentEntity.Create(10, 1));

        // Department/Module resolution throws
        moduleLookupService.Setup(s => s.GetDepartmentAndModuleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Asset department or module not found."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveWithUploadAsync(request, 42));
    }

    [Fact]
    public async Task SaveWithUploadAsync_SavesSuccessfully_WithNewOrExistingDocument()
    {
        var service = CreateServiceFull(
            out var docService,
            out _,
            out _,
            out _,
            out var globalDocService,
            out var moduleLookupService);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("doc.pdf");
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[100]));

        moduleLookupService.Setup(r => r.GetDepartmentAndModuleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((10, 20));

        // Case 1: ExistingDocumentId is provided
        var request1 = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            ExistingDocumentId = 500,
            DocumentFile = mockFile.Object
        };

        var entity = AssetDocumentEntity.Create(10, 1);
        docService.Setup(s => s.GetByIdAsync(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result1 = await service.SaveWithUploadAsync(request1, 42);

        docService.Verify(s => s.CreateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        globalDocService.Verify(g => g.UploadDocumentAsync(
            It.IsAny<Stream>(),
            "doc.pdf",
            "application/pdf",
            100,
            It.Is<DocumentUploadDto>(d => d.ReferenceTableId == 500 && d.DepartmentId == 10 && d.ModuleId == 20),
            42,
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(10, result1.AssetId);

        // Case 2: New document (ExistingDocumentId is null)
        var request2 = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            DocumentFile = mockFile.Object
        };

        docService.Setup(s => s.CreateAsync(10, 1, It.IsAny<int?>(), It.IsAny<string?>(), 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(600);
        docService.Setup(s => s.GetByIdAsync(600, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result2 = await service.SaveWithUploadAsync(request2, 42);
        
        docService.Verify(s => s.CreateAsync(10, 1, It.IsAny<int?>(), It.IsAny<string?>(), 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveWithUploadAsync_Throws_WhenExistingDocumentAssetIdMismatches()
    {
        var service = CreateServiceFull(out var docService, out _, out _, out _, out _, out _);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("doc.pdf");
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");

        var request = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            ExistingDocumentId = 500,
            DocumentFile = mockFile.Object
        };

        // Entity has AssetId = 20 (mismatch)
        docService.Setup(s => s.GetByIdAsync(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssetDocumentEntity.Create(20, 1));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SaveWithUploadAsync(request, 42));
        Assert.Equal("AssetId", ex.ParamName);
    }

    [Fact]
    public async Task SaveWithUploadAsync_Throws_WhenExistingDocumentDefinitionIdMismatches()
    {
        var service = CreateServiceFull(out var docService, out _, out _, out _, out _, out _);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.FileName).Returns("doc.pdf");
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");

        var request = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 10,
            DocumentDefinitionId = 1,
            ExistingDocumentId = 500,
            DocumentFile = mockFile.Object
        };

        // Entity has DocumentDefinitionId = 2 (mismatch)
        docService.Setup(s => s.GetByIdAsync(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssetDocumentEntity.Create(10, 2));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SaveWithUploadAsync(request, 42));
        Assert.Equal("DocumentDefinitionId", ex.ParamName);
    }

    [Fact]
    public void TestDtoGettersAndSetters()
    {
        var guid = Guid.NewGuid();
        var itemDto = new AssetDocumentItemDto
        {
            DocumentDefinitionId = 10,
            IsEnabled = true,
            DisplayOrder = 1,
            Remarks = "Some remarks",
            ExistingDocumentId = 100,
            ExistingDocumentGuid = guid
        };

        Assert.Equal(10, itemDto.DocumentDefinitionId);
        Assert.True(itemDto.IsEnabled);
        Assert.Equal(1, itemDto.DisplayOrder);
        Assert.Equal("Some remarks", itemDto.Remarks);
        Assert.Equal(100, itemDto.ExistingDocumentId);
        Assert.Equal(guid, itemDto.ExistingDocumentGuid);

        var responseDto = new AssetDocumentBulkSaveResponseDto
        {
            AssetId = 5,
            TotalProcessed = 1,
            EnabledCount = 1,
            DisabledCount = 0,
            UpdatedDocumentTypes = new List<AssetDocumentTypeWithStatusDto>(),
            Errors = new List<string> { "error" }
        };

        Assert.Equal(5, responseDto.AssetId);
        Assert.Equal(1, responseDto.TotalProcessed);
        Assert.Equal(1, responseDto.EnabledCount);
        Assert.Equal(0, responseDto.DisabledCount);
        Assert.Empty(responseDto.UpdatedDocumentTypes);
        Assert.Single(responseDto.Errors);

        var galleryDto = new AssetDocumentGalleryDto
        {
            AssetId = 2,
            TotalDocuments = 3,
            DocumentTypes = new List<AssetDocumentTypeGroupDto>()
        };

        Assert.Equal(2, galleryDto.AssetId);
        Assert.Equal(3, galleryDto.TotalDocuments);
        Assert.Empty(galleryDto.DocumentTypes);

        var uploadResponse = new AssetDocumentUploadResponseDto
        {
            DocumentId = 1,
            DocumentGuid = guid,
            DocumentBindingId = 2,
            AssetId = 3,
            DocumentDefinitionId = 4,
            DisplayOrder = 5,
            Remarks = "Remarks",
            FileName = "file.txt",
            FileSizeBytes = 1024,
            StoragePath = "/path"
        };

        Assert.Equal(1, uploadResponse.DocumentId);
        Assert.Equal(guid, uploadResponse.DocumentGuid);
        Assert.Equal(2, uploadResponse.DocumentBindingId);
        Assert.Equal(3, uploadResponse.AssetId);
        Assert.Equal(4, uploadResponse.DocumentDefinitionId);
        Assert.Equal(5, uploadResponse.DisplayOrder);
        Assert.Equal("Remarks", uploadResponse.Remarks);
        Assert.Equal("file.txt", uploadResponse.FileName);
        Assert.Equal(1024, uploadResponse.FileSizeBytes);
        Assert.Equal("/path", uploadResponse.StoragePath);

        var saveUploadDto = new AssetDocumentSaveWithUploadDto
        {
            AssetId = 1,
            ExistingDocumentId = 2,
            DocumentDefinitionId = 3,
            DisplayOrder = 4,
            Remarks = "Remarks",
            IsEnabled = true,
            DocumentFile = null
        };

        Assert.Equal(1, saveUploadDto.AssetId);
        Assert.Equal(2, saveUploadDto.ExistingDocumentId);
        Assert.Equal(3, saveUploadDto.DocumentDefinitionId);
        Assert.Equal(4, saveUploadDto.DisplayOrder);
        Assert.Equal("Remarks", saveUploadDto.Remarks);
        Assert.True(saveUploadDto.IsEnabled);
        Assert.Null(saveUploadDto.DocumentFile);
    }
}

