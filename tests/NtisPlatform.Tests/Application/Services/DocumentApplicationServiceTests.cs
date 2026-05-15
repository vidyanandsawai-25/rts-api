using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class DocumentApplicationServiceTests
{
    private static DocumentApplicationService Build(
        out Mock<IDocumentService> documentService,
        out Mock<IFileStorageService> fileStorageService,
        out Mock<IUnitOfWork> unitOfWork)
    {
        documentService = new Mock<IDocumentService>();
        fileStorageService = new Mock<IFileStorageService>();
        unitOfWork = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<DocumentApplicationService>>();

        var configValues = new Dictionary<string, string?>
        {
            ["FileStorage:BufferSizeBytes"] = "4096",
            ["FileStorage:MaxFileSizeBytes"] = "1048576"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new DocumentApplicationService(
            documentService.Object,
            fileStorageService.Object,
            unitOfWork.Object,
            configuration,
            logger.Object);
    }

    private static DocumentUploadDto Dto(string? moduleCode = null, string? refTable = null, int? refId = null) =>
        new()
        {
            OwnerUserId = 1,
            DocumentType = "Certificate",
            ModuleCode = moduleCode,
            ReferenceTableName = refTable,
            ReferenceTableId = refId,
            IsPrimaryDocument = true
        };

    #region UploadDocumentAsync

    [Fact]
    public async Task UploadDocumentAsync_Throws_OnInvalidStream()
    {
        var service = Build(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadDocumentAsync(
            null!, "f.pdf", "application/pdf", 1, Dto(), 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_Throws_OnEmptyFileName()
    {
        var service = Build(out _, out _, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadDocumentAsync(
            stream, "", "application/pdf", 4, Dto(), 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_Throws_OnFileNameTooLong()
    {
        var service = Build(out _, out _, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        var longName = new string('a', 256) + ".pdf";
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadDocumentAsync(
            stream, longName, "application/pdf", 4, Dto(), 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_Throws_OnEmptyMimeType()
    {
        var service = Build(out _, out _, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadDocumentAsync(
            stream, "f.pdf", "", 4, Dto(), 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_Throws_OnZeroFileSize()
    {
        var service = Build(out _, out _, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadDocumentAsync(
            stream, "f.pdf", "application/pdf", 0, Dto(), 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_Throws_WhenFileExceedsMaxSize()
    {
        var service = Build(out _, out _, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.UploadDocumentAsync(
            stream, "f.pdf", "application/pdf", 2 * 1024 * 1024, Dto(), 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_Throws_OnNullDto()
    {
        var service = Build(out _, out _, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadDocumentAsync(
            stream, "f.pdf", "application/pdf", 4, null!, 1));
    }

    [Fact]
    public async Task UploadDocumentAsync_Throws_OnZeroUploadedBy()
    {
        var service = Build(out _, out _, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadDocumentAsync(
            stream, "f.pdf", "application/pdf", 4, Dto(), 0));
    }

    [Fact]
    public async Task UploadDocumentAsync_Succeeds_WithoutBinding()
    {
        var service = Build(out var documentService, out var fileStorage, out var uow);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        fileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<Stream>(), "f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/storage/f.pdf");
        documentService.Setup(s => s.CreateDocumentAsync(
            1, 1, It.IsAny<string>(), "f.pdf", ".pdf", "application/pdf", 4,
            "/storage/f.pdf", null, It.IsAny<string>(), "Certificate", It.IsAny<CancellationToken>()))
            .ReturnsAsync((42, Guid.NewGuid()));

        var result = await service.UploadDocumentAsync(stream, "f.pdf", "application/pdf", 4, Dto(), 1);

        Assert.NotNull(result);
        Assert.Equal(42, result.DocumentId);
        Assert.Null(result.DocumentBindingId);
        uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        documentService.Verify(s => s.CreateDocumentBindingAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(),
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadDocumentAsync_Succeeds_WithBinding()
    {
        var service = Build(out var documentService, out var fileStorage, out var uow);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        var dto = Dto(moduleCode: "PROPERTY", refTable: "Property", refId: 99);
        fileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<Stream>(), "f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/storage/f.pdf");
        documentService.Setup(s => s.CreateDocumentAsync(
            1, 1, It.IsAny<string>(), "f.pdf", ".pdf", "application/pdf", 4,
            "/storage/f.pdf", null, It.IsAny<string>(), "Certificate", It.IsAny<CancellationToken>()))
            .ReturnsAsync((42, Guid.NewGuid()));
        documentService.Setup(s => s.CreateDocumentBindingAsync(
            42, "PROPERTY", "Property", 99, null, null, true, null, null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await service.UploadDocumentAsync(stream, "f.pdf", "application/pdf", 4, dto, 1);

        Assert.Equal(42, result.DocumentId);
        Assert.Equal(7, result.DocumentBindingId);
        uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_RollsBackAndDeletesOrphan_OnDocumentCreateFailure()
    {
        var service = Build(out var documentService, out var fileStorage, out var uow);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        fileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<Stream>(), "f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/storage/f.pdf");
        documentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadDocumentAsync(stream, "f.pdf", "application/pdf", 4, Dto(), 1));

        uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(fs => fs.DeleteFileAsync("/storage/f.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_IgnoresCleanupFailure_OnDoubleFailure()
    {
        var service = Build(out var documentService, out var fileStorage, out var uow);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        fileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<Stream>(), "f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/storage/f.pdf");
        fileStorage.Setup(fs => fs.DeleteFileAsync("/storage/f.pdf", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk down"));
        documentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db boom"));

        // The original db exception should propagate; cleanup failure is swallowed
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadDocumentAsync(stream, "f.pdf", "application/pdf", 4, Dto(), 1));
    }

    [Theory]
    [InlineData(null, "Property", 99)]              // missing module
    [InlineData("", "Property", 99)]                // blank module
    [InlineData("lowercase", "Property", 99)]       // invalid module format
    [InlineData("X", "Property", 99)]               // too short module
    [InlineData("PROPERTY", null, 99)]              // missing ref table
    [InlineData("PROPERTY", "", 99)]                // blank ref table
    [InlineData("PROPERTY", "1Bad", 99)]            // ref table starts with digit
    [InlineData("PROPERTY", "Property", null)]      // no id provided
    [InlineData("PROPERTY", "Property", 0)]         // zero id
    public async Task UploadDocumentAsync_SkipsBinding_ForInvalidShouldCreateBindingInputs(string? moduleCode, string? refTable, int? refId)
    {
        var service = Build(out var documentService, out var fileStorage, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        fileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<Stream>(), "f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/storage/f.pdf");
        documentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, Guid.NewGuid()));

        var dto = Dto(moduleCode: moduleCode, refTable: refTable, refId: refId);
        await service.UploadDocumentAsync(stream, "f.pdf", "application/pdf", 4, dto, 1);

        documentService.Verify(s => s.CreateDocumentBindingAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(),
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadDocumentAsync_SkipsBinding_WhenBothIntAndGuidIdsProvided()
    {
        var service = Build(out var documentService, out var fileStorage, out _);
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("data"));
        var dto = Dto(moduleCode: "PROPERTY", refTable: "Property", refId: 1);
        dto.ReferenceTableIdGuid = Guid.NewGuid();
        fileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<Stream>(), "f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/storage/f.pdf");
        documentService.Setup(s => s.CreateDocumentAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, Guid.NewGuid()));

        await service.UploadDocumentAsync(stream, "f.pdf", "application/pdf", 4, dto, 1);

        documentService.Verify(s => s.CreateDocumentBindingAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(),
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetDocumentAsync

    [Fact]
    public async Task GetDocumentAsync_Throws_OnEmptyGuid()
    {
        var service = Build(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetDocumentAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetDocumentAsync_ReturnsNull_WhenServiceReturnsNull()
    {
        var service = Build(out var documentService, out _, out _);
        documentService.Setup(s => s.GetDocumentByGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentEntity?)null);

        var result = await service.GetDocumentAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDocumentAsync_ReturnsMappedDto()
    {
        var service = Build(out var documentService, out _, out _);
        var entity = DocumentEntity.Create(
            uploadedByUserId: 1, fileName: "stored.pdf", originalFileName: "f.pdf",
            fileExtension: ".pdf", mimeType: "application/pdf", fileSizeBytes: 4,
            storagePath: "/storage/f.pdf", documentType: "Certificate");
        documentService.Setup(s => s.GetDocumentByGuidAsync(entity.DocumentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await service.GetDocumentAsync(entity.DocumentGuid);

        Assert.NotNull(result);
        Assert.Equal(entity.DocumentGuid, result!.DocumentGuid);
        Assert.Equal("f.pdf", result.OriginalFileName);
        Assert.Equal("application/pdf", result.MimeType);
    }

    #endregion

    #region DownloadDocumentAsync

    [Fact]
    public async Task DownloadDocumentAsync_Throws_OnEmptyGuid()
    {
        var service = Build(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadDocumentAsync(Guid.Empty, 1));
    }

    [Fact]
    public async Task DownloadDocumentAsync_Throws_OnInvalidUser()
    {
        var service = Build(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadDocumentAsync(Guid.NewGuid(), 0));
    }

    [Fact]
    public async Task DownloadDocumentAsync_ReturnsEmpty_WhenDocumentMissing()
    {
        var service = Build(out var documentService, out _, out _);
        documentService.Setup(s => s.GetDocumentByGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentEntity?)null);

        var (stream, name, mime) = await service.DownloadDocumentAsync(Guid.NewGuid(), 1);

        Assert.Null(stream);
        Assert.Equal(string.Empty, name);
        Assert.Equal(string.Empty, mime);
    }

    [Fact]
    public async Task DownloadDocumentAsync_ReturnsStream_AndIncrementsCount()
    {
        var service = Build(out var documentService, out var fileStorage, out _);
        var entity = DocumentEntity.Create(1, "stored.pdf", "f.pdf", ".pdf", "application/pdf", 4, "/storage/f.pdf");
        documentService.Setup(s => s.GetDocumentByGuidAsync(entity.DocumentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        fileStorage.Setup(fs => fs.ReadFileAsync("/storage/f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        var (stream, name, mime) = await service.DownloadDocumentAsync(entity.DocumentGuid, 1);

        Assert.Same(fileStream, stream);
        Assert.Equal("f.pdf", name);
        Assert.Equal("application/pdf", mime);
        documentService.Verify(s => s.IncrementDownloadCountAsync(entity.DocumentGuid, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadDocumentAsync_DoesNotIncrement_WhenStreamMissing()
    {
        var service = Build(out var documentService, out var fileStorage, out _);
        var entity = DocumentEntity.Create(1, "stored.pdf", "f.pdf", ".pdf", "application/pdf", 4, "/storage/f.pdf");
        documentService.Setup(s => s.GetDocumentByGuidAsync(entity.DocumentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        fileStorage.Setup(fs => fs.ReadFileAsync("/storage/f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var (stream, _, _) = await service.DownloadDocumentAsync(entity.DocumentGuid, 1);

        Assert.Null(stream);
        documentService.Verify(s => s.IncrementDownloadCountAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ViewDocumentAsync

    [Fact]
    public async Task ViewDocumentAsync_Throws_OnEmptyGuid()
    {
        var service = Build(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.ViewDocumentAsync(Guid.Empty));
    }

    [Fact]
    public async Task ViewDocumentAsync_ReturnsEmpty_WhenDocumentMissing()
    {
        var service = Build(out var documentService, out _, out _);
        documentService.Setup(s => s.GetDocumentByGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentEntity?)null);

        var (stream, name, mime) = await service.ViewDocumentAsync(Guid.NewGuid());

        Assert.Null(stream);
        Assert.Equal(string.Empty, name);
        Assert.Equal(string.Empty, mime);
    }

    [Fact]
    public async Task ViewDocumentAsync_ReturnsStream_DoesNotIncrement()
    {
        var service = Build(out var documentService, out var fileStorage, out _);
        var entity = DocumentEntity.Create(1, "stored.pdf", "f.pdf", ".pdf", "application/pdf", 4, "/storage/f.pdf");
        documentService.Setup(s => s.GetDocumentByGuidAsync(entity.DocumentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        fileStorage.Setup(fs => fs.ReadFileAsync("/storage/f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        var (stream, _, _) = await service.ViewDocumentAsync(entity.DocumentGuid);

        Assert.Same(fileStream, stream);
        documentService.Verify(s => s.IncrementDownloadCountAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ViewDocumentAsync_ReturnsMissingStream_WithMetadata()
    {
        var service = Build(out var documentService, out var fileStorage, out _);
        var entity = DocumentEntity.Create(1, "stored.pdf", "f.pdf", ".pdf", "application/pdf", 4, "/storage/f.pdf");
        documentService.Setup(s => s.GetDocumentByGuidAsync(entity.DocumentGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        fileStorage.Setup(fs => fs.ReadFileAsync("/storage/f.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var (stream, name, mime) = await service.ViewDocumentAsync(entity.DocumentGuid);

        Assert.Null(stream);
        Assert.Equal("f.pdf", name);
        Assert.Equal("application/pdf", mime);
    }

    #endregion

    #region DeleteDocumentAsync

    [Fact]
    public async Task DeleteDocumentAsync_Throws_OnEmptyGuid()
    {
        var service = Build(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteDocumentAsync(Guid.Empty, 1));
    }

    [Fact]
    public async Task DeleteDocumentAsync_Throws_OnInvalidUser()
    {
        var service = Build(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteDocumentAsync(Guid.NewGuid(), 0));
    }

    [Fact]
    public async Task DeleteDocumentAsync_ReturnsTrue_WhenServiceSucceeds()
    {
        var service = Build(out var documentService, out _, out _);
        var guid = Guid.NewGuid();
        documentService.Setup(s => s.DeleteDocumentAsync(guid, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Assert.True(await service.DeleteDocumentAsync(guid, 1));
    }

    [Fact]
    public async Task DeleteDocumentAsync_ReturnsFalse_WhenServiceFails()
    {
        var service = Build(out var documentService, out _, out _);
        documentService.Setup(s => s.DeleteDocumentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.False(await service.DeleteDocumentAsync(Guid.NewGuid(), 1));
    }

    #endregion

    #region UpdateDocumentBindingReferenceAsync

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public async Task UpdateDocumentBindingReferenceAsync_Throws_OnInvalidIds(int bindingId, int refId, int userId)
    {
        var service = Build(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateDocumentBindingReferenceAsync(bindingId, refId, userId));
    }

    [Fact]
    public async Task UpdateDocumentBindingReferenceAsync_DelegatesToService()
    {
        var service = Build(out var documentService, out _, out _);
        documentService.Setup(s => s.UpdateDocumentBindingReferenceAsync(5, 9, 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await service.UpdateDocumentBindingReferenceAsync(5, 9, 1);

        documentService.Verify(s => s.UpdateDocumentBindingReferenceAsync(5, 9, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
