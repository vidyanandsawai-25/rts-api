using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class PropertyCertificateServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly PropertyCertificateService _service;

    public PropertyCertificateServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => _context.SaveChangesAsync(ct));
        _service = new PropertyCertificateService(_context, _unitOfWork.Object);
    }

    public void Dispose() => _context.Dispose();

    private async Task<int> SeedPropertyAsync(bool active = true)
    {
        var property = new PropertyEntity { Id = 1, WardId = 1, IsActive = active };
        _context.PropertyMast.Add(property);
        await _context.SaveChangesAsync();
        return property.Id;
    }

    private async Task<int> SeedCertificateTypeAsync(bool active = true)
    {
        var type = new PropertyCertificateTypeMasterEntity { Id = 5, IsActive = active };
        _context.PropertyCertificateTypeMasters.Add(type);
        await _context.SaveChangesAsync();
        return type.Id;
    }

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_Throws_WhenPropertyMissing()
    {
        await SeedCertificateTypeAsync();
        await Assert.ThrowsAsync<PropertyNotFoundException>(() =>
            _service.CreateAsync(99, 5, "CERT-1", DateTime.Now, 1));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenCertificateTypeMissing()
    {
        await SeedPropertyAsync();
        await Assert.ThrowsAsync<CertificateTypeNotFoundException>(() =>
            _service.CreateAsync(1, 99, "CERT-1", DateTime.Now, 1));
    }

    [Fact]
    public async Task CreateAsync_CreatesEntity()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();

        var id = await _service.CreateAsync(1, 5, "CERT-1", new DateTime(2024, 1, 1), 7);

        var saved = await _context.PropertyCertificates.SingleAsync();
        Assert.Equal(id, saved.Id);
        Assert.Equal(7, saved.CreatedBy);
    }

    #endregion

    #region CreateWithDocumentAsync

    [Fact]
    public async Task CreateWithDocumentAsync_Throws_WhenPropertyMissing()
    {
        await SeedCertificateTypeAsync();
        await Assert.ThrowsAsync<PropertyNotFoundException>(() =>
            _service.CreateWithDocumentAsync(99, 5, 10, null, null, 1));
    }

    [Fact]
    public async Task CreateWithDocumentAsync_Throws_WhenCertificateTypeMissing()
    {
        await SeedPropertyAsync();
        await Assert.ThrowsAsync<CertificateTypeNotFoundException>(() =>
            _service.CreateWithDocumentAsync(1, 99, 10, null, null, 1));
    }

    [Fact]
    public async Task CreateWithDocumentAsync_CreatesEntity()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();

        var id = await _service.CreateWithDocumentAsync(1, 5, 10, "CERT-1", new DateTime(2024, 1, 1), 7);

        var saved = await _context.PropertyCertificates.SingleAsync();
        Assert.Equal(id, saved.Id);
        Assert.Equal(10, saved.DocumentBindingId);
    }

    #endregion

    #region UpdateDocumentBindingAsync

    [Fact]
    public async Task UpdateDocumentBindingAsync_Throws_WhenCertificateMissing()
    {
        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(() =>
            _service.UpdateDocumentBindingAsync(99, 10, 1));
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_UpdatesBinding()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();
        var id = await _service.CreateAsync(1, 5, "CERT-1", null, 7);

        await _service.UpdateDocumentBindingAsync(id, 20, 8);

        var updated = await _context.PropertyCertificates.SingleAsync();
        Assert.Equal(20, updated.DocumentBindingId);
        Assert.Equal(8, updated.UpdatedBy);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        Assert.Null(await _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenFound()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();
        var id = await _service.CreateAsync(1, 5, "CERT-1", null, 7);

        var result = await _service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeOptions_None_ReturnsEntity()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();
        var id = await _service.CreateAsync(1, 5, "CERT-1", null, 7);

        var result = await _service.GetByIdAsync(id, PropertyCertificateIncludeOptions.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeOptions_All_AppliesIncludes()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();
        var id = await _service.CreateAsync(1, 5, "CERT-1", null, 7);

        var result = await _service.GetByIdAsync(id, PropertyCertificateIncludeOptions.CertificateType);

        Assert.NotNull(result);
    }

    #endregion

    #region GetByPropertyIdAsync

    [Fact]
    public async Task GetByPropertyIdAsync_ReturnsEmpty_WhenNoCertificates()
    {
        var result = await _service.GetByPropertyIdAsync(99);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_ReturnsCertificatesForProperty()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();
        await _service.CreateAsync(1, 5, "CERT-1", null, 7);
        await _service.CreateAsync(1, 5, "CERT-2", null, 7);

        var result = await _service.GetByPropertyIdAsync(1);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_WithIncludeOptions_None_ReturnsAll()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();
        await _service.CreateAsync(1, 5, "CERT-1", null, 7);

        var result = await _service.GetByPropertyIdAsync(1, PropertyCertificateIncludeOptions.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_WithIncludeOptions_All_AppliesIncludes()
    {
        await SeedPropertyAsync();
        await SeedCertificateTypeAsync();
        await _service.CreateAsync(1, 5, "CERT-1", null, 7);

        var result = await _service.GetByPropertyIdAsync(1, PropertyCertificateIncludeOptions.All);

        Assert.Single(result);
    }

    #endregion
}
