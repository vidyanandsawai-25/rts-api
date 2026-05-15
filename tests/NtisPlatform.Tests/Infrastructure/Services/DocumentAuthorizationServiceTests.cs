using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class DocumentAuthorizationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DocumentAuthorizationService _service;

    public DocumentAuthorizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new DocumentAuthorizationService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private DocumentEntity NewDocument(int uploadedBy, int? ownerUserId = null, bool isActive = true)
    {
        var doc = DocumentEntity.Create(
            uploadedByUserId: uploadedBy,
            fileName: "stored.pdf",
            originalFileName: "f.pdf",
            fileExtension: ".pdf",
            mimeType: "application/pdf",
            fileSizeBytes: 1,
            storagePath: "/storage/f.pdf");

        if (ownerUserId.HasValue)
            doc.TransferOwnership(ownerUserId.Value);

        if (!isActive)
            doc.MarkForDeletion(uploadedBy);

        _context.Documents.Add(doc);
        return doc;
    }

    [Fact]
    public async Task CanAccessDocumentAsync_ReturnsTrue_WhenUserIsOwner()
    {
        var doc = NewDocument(uploadedBy: 1, ownerUserId: 99);
        await _context.SaveChangesAsync();

        Assert.True(await _service.CanAccessDocumentAsync(doc.DocumentGuid, 99));
    }

    [Fact]
    public async Task CanAccessDocumentAsync_ReturnsTrue_WhenUserIsUploader()
    {
        var doc = NewDocument(uploadedBy: 7);
        await _context.SaveChangesAsync();

        Assert.True(await _service.CanAccessDocumentAsync(doc.DocumentGuid, 7));
    }

    [Fact]
    public async Task CanAccessDocumentAsync_ReturnsFalse_WhenDocumentInactive()
    {
        var doc = NewDocument(uploadedBy: 1, isActive: false);
        await _context.SaveChangesAsync();

        Assert.False(await _service.CanAccessDocumentAsync(doc.DocumentGuid, 1));
    }

    [Fact]
    public async Task CanAccessDocumentAsync_ReturnsFalse_WhenDocumentMissing()
    {
        Assert.False(await _service.CanAccessDocumentAsync(Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task CanAccessDocumentAsync_ReturnsTrue_WhenBindingAuthorizesUser()
    {
        var doc = NewDocument(uploadedBy: 1);
        var binding = DocumentBindingEntity.CreateWithIntReference(1, "PROPERTY", "Property", 99);
        binding.SetAuthorizationContext("ACCESS", 50);
        // Need to use reflection to assign DocumentId after save; instead use BindingNavigationProperty by attaching binding
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        // Re-create binding with the real document id
        var binding2 = DocumentBindingEntity.CreateWithIntReference(doc.Id, "PROPERTY", "Property", 99);
        binding2.SetAuthorizationContext("ACCESS", 50);
        _context.DocumentBindings.Add(binding2);
        await _context.SaveChangesAsync();

        Assert.True(await _service.CanAccessDocumentAsync(doc.DocumentGuid, 50));
    }

    [Fact]
    public async Task CanAccessDocumentAsync_ReturnsFalse_WhenBindingDoesNotAuthorize()
    {
        var doc = NewDocument(uploadedBy: 1);
        await _context.SaveChangesAsync();
        var binding = DocumentBindingEntity.CreateWithIntReference(doc.Id, "PROPERTY", "Property", 99);
        binding.SetAuthorizationContext("ACCESS", 25);
        _context.DocumentBindings.Add(binding);
        await _context.SaveChangesAsync();

        Assert.False(await _service.CanAccessDocumentAsync(doc.DocumentGuid, 50));
    }

    [Fact]
    public async Task CanModifyDocumentAsync_ReturnsTrue_WhenUserIsOwner()
    {
        var doc = NewDocument(uploadedBy: 1, ownerUserId: 99);
        await _context.SaveChangesAsync();

        Assert.True(await _service.CanModifyDocumentAsync(doc.DocumentGuid, 99));
    }

    [Fact]
    public async Task CanModifyDocumentAsync_ReturnsTrue_WhenUserIsUploader()
    {
        var doc = NewDocument(uploadedBy: 7);
        await _context.SaveChangesAsync();

        Assert.True(await _service.CanModifyDocumentAsync(doc.DocumentGuid, 7));
    }

    [Fact]
    public async Task CanModifyDocumentAsync_ReturnsFalse_ForOtherUsers()
    {
        var doc = NewDocument(uploadedBy: 1);
        await _context.SaveChangesAsync();

        Assert.False(await _service.CanModifyDocumentAsync(doc.DocumentGuid, 999));
    }

    [Fact]
    public async Task CanModifyDocumentAsync_ReturnsFalse_WhenDocumentMissing()
    {
        Assert.False(await _service.CanModifyDocumentAsync(Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_ReturnsFalse_WhenBindingMissing()
    {
        Assert.False(await _service.CanAccessDocumentBindingAsync(99, 1));
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_ReturnsTrue_WhenUserIsOwner()
    {
        var doc = NewDocument(uploadedBy: 1, ownerUserId: 42);
        await _context.SaveChangesAsync();
        var binding = DocumentBindingEntity.CreateWithIntReference(doc.Id, "PROPERTY", "Property", 99);
        _context.DocumentBindings.Add(binding);
        await _context.SaveChangesAsync();

        Assert.True(await _service.CanAccessDocumentBindingAsync(binding.Id, 42));
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_ReturnsTrue_WhenAuthRefMatches()
    {
        var doc = NewDocument(uploadedBy: 1);
        await _context.SaveChangesAsync();
        var binding = DocumentBindingEntity.CreateWithIntReference(doc.Id, "PROPERTY", "Property", 99);
        binding.SetAuthorizationContext("ACCESS", 50);
        _context.DocumentBindings.Add(binding);
        await _context.SaveChangesAsync();

        Assert.True(await _service.CanAccessDocumentBindingAsync(binding.Id, 50));
    }

    [Fact]
    public async Task CanAccessDocumentBindingAsync_ReturnsFalse_ForUnrelatedUser()
    {
        var doc = NewDocument(uploadedBy: 1);
        await _context.SaveChangesAsync();
        var binding = DocumentBindingEntity.CreateWithIntReference(doc.Id, "PROPERTY", "Property", 99);
        _context.DocumentBindings.Add(binding);
        await _context.SaveChangesAsync();

        Assert.False(await _service.CanAccessDocumentBindingAsync(binding.Id, 999));
    }
}
