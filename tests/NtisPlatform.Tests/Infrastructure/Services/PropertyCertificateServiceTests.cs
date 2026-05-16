using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Tests.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive tests for PropertyCertificateService to achieve 100% line and branch coverage
/// </summary>
public class PropertyCertificateServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly PropertyCertificateService _service;

    public PropertyCertificateServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Configure mock to actually call SaveChanges on the context
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => await _context.SaveChangesAsync(ct));

        _service = new PropertyCertificateService(_context, _mockUnitOfWork.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var property = EntityTestHelpers.CreatePropertyEntity(id: 1);
        var certificateType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(id: 1);

        _context.PropertyMast.Add(property);
        _context.PropertyCertificateTypeMasters.Add(certificateType);
        _context.SaveChanges();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var service = new PropertyCertificateService(_context, _mockUnitOfWork.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidParameters_CreatesEntity()
    {
        // Act
        var result = await _service.CreateAsync(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: DateTime.Now,
            createdBy: 1);

        // Assert
        Assert.True(result > 0);
        var entity = await _context.PropertyCertificates.FirstOrDefaultAsync(x => x.Id == result);
        Assert.NotNull(entity);
        Assert.Equal(1, entity.PropertyId);
        Assert.Equal(1, entity.CertificateTypeId);
        Assert.Equal("CERT-001", entity.CertificateNo);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidPropertyId_ThrowsPropertyNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<PropertyNotFoundException>(() =>
            _service.CreateAsync(
                propertyId: 999,
                certificateTypeId: 1,
                certificateNo: "CERT-001",
                issueDate: DateTime.Now,
                createdBy: 1));

        Assert.Contains("Property", exception.Message);
        Assert.Equal(999, exception.EntityId);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCertificateTypeId_ThrowsCertificateTypeNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<CertificateTypeNotFoundException>(() =>
            _service.CreateAsync(
                propertyId: 1,
                certificateTypeId: 999,
                certificateNo: "CERT-001",
                issueDate: DateTime.Now,
                createdBy: 1));

        Assert.Contains("CertificateType", exception.Message);
        Assert.Equal(999, exception.EntityId);
    }

    [Fact]
    public async Task CreateAsync_WithNullCertificateNo_CreatesEntity()
    {
        // Act
        var result = await _service.CreateAsync(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: null,
            issueDate: DateTime.Now,
            createdBy: 1);

        // Assert
        Assert.True(result > 0);
        var entity = await _context.PropertyCertificates.FirstOrDefaultAsync(x => x.Id == result);
        Assert.NotNull(entity);
        Assert.Null(entity.CertificateNo);
    }

    [Fact]
    public async Task CreateAsync_WithNullIssueDate_CreatesEntity()
    {
        // Act
        var result = await _service.CreateAsync(
            propertyId: 1,
            certificateTypeId: 1,
            certificateNo: "CERT-001",
            issueDate: null,
            createdBy: 1);

        // Assert
        Assert.True(result > 0);
        var entity = await _context.PropertyCertificates.FirstOrDefaultAsync(x => x.Id == result);
        Assert.NotNull(entity);
        Assert.Null(entity.IssueDate);
    }

    #endregion

    #region CreateWithDocumentAsync Tests

    [Fact]
    public async Task CreateWithDocumentAsync_WithValidParameters_CreatesEntity()
    {
        // Act
        var result = await _service.CreateWithDocumentAsync(
            propertyId: 1,
            certificateTypeId: 1,
            documentBindingId: 100,
            certificateNo: "CERT-002",
            issueDate: DateTime.Now,
            createdBy: 1);

        // Assert
        Assert.True(result > 0);
        var entity = await _context.PropertyCertificates.FirstOrDefaultAsync(x => x.Id == result);
        Assert.NotNull(entity);
        Assert.Equal(100, entity.DocumentBindingId);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWithDocumentAsync_WithInvalidPropertyId_ThrowsPropertyNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<PropertyNotFoundException>(() =>
            _service.CreateWithDocumentAsync(
                propertyId: 999,
                certificateTypeId: 1,
                documentBindingId: 100,
                certificateNo: "CERT-002",
                issueDate: DateTime.Now,
                createdBy: 1));

        Assert.Contains("Property", exception.Message);
    }

    [Fact]
    public async Task CreateWithDocumentAsync_WithInvalidCertificateTypeId_ThrowsCertificateTypeNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<CertificateTypeNotFoundException>(() =>
            _service.CreateWithDocumentAsync(
                propertyId: 1,
                certificateTypeId: 999,
                documentBindingId: 100,
                certificateNo: "CERT-002",
                issueDate: DateTime.Now,
                createdBy: 1));

        Assert.Contains("CertificateType", exception.Message);
    }

    #endregion

    #region UpdateDocumentBindingAsync Tests

    [Fact]
    public async Task UpdateDocumentBindingAsync_WithValidId_UpdatesBinding()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => await _context.SaveChangesAsync(ct));

        // Act
        await _service.UpdateDocumentBindingAsync(entity.Id, 200, 1);

        // Assert
        var updatedEntity = await _context.PropertyCertificates.FirstOrDefaultAsync(x => x.Id == entity.Id);
        Assert.NotNull(updatedEntity);
        Assert.Equal(200, updatedEntity.DocumentBindingId);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_WithInvalidId_ThrowsPropertyCertificateNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(() =>
            _service.UpdateDocumentBindingAsync(999, 200, 1));

        Assert.Contains("PropertyCertificate", exception.Message);
        Assert.Equal(999, exception.EntityId);
    }

    [Fact]
    public async Task UpdateDocumentBindingAsync_WithInactiveEntity_ThrowsPropertyCertificateNotFoundException()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        entity.IsActive = false;
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(() =>
            _service.UpdateDocumentBindingAsync(entity.Id, 200, 1));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsEntity()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithInactiveEntity_ReturnsNull()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        entity.IsActive = false;
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithMarkedForDeletion_ReturnsNull()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1,
            markedForDeletion: true);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByIdAsync with IncludeOptions Tests

    [Fact]
    public async Task GetByIdAsync_WithIncludeNone_ReturnsEntityWithoutNavigationProperties()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(entity.Id, PropertyCertificateIncludeOptions.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.CertificateType);
        Assert.Null(result.DocumentBinding);
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeCertificateType_ReturnsEntityWithCertificateType()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(entity.Id, PropertyCertificateIncludeOptions.CertificateType);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CertificateType);
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeDocumentBinding_ReturnsEntityWithDocumentBinding()
    {
        // Arrange
        var binding = EntityTestHelpers.CreateDocumentBindingEntity(documentId: 1);
        _context.DocumentBindings.Add(binding);
        await _context.SaveChangesAsync();

        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1,
            documentBindingId: binding.Id);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(entity.Id, PropertyCertificateIncludeOptions.DocumentBinding);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DocumentBinding);
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeDocument_ReturnsEntityWithDocumentBindingAndDocument()
    {
        // Arrange
        var document = EntityTestHelpers.CreateDocumentEntity();
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var binding = EntityTestHelpers.CreateDocumentBindingEntity(documentId: document.Id);
        _context.DocumentBindings.Add(binding);
        await _context.SaveChangesAsync();

        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1,
            documentBindingId: binding.Id);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(
            entity.Id,
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DocumentBinding);
        Assert.NotNull(result.DocumentBinding.Document);
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeAll_ReturnsEntityWithAllNavigationProperties()
    {
        // Arrange
        var document = EntityTestHelpers.CreateDocumentEntity();
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var binding = EntityTestHelpers.CreateDocumentBindingEntity(documentId: document.Id);
        _context.DocumentBindings.Add(binding);
        await _context.SaveChangesAsync();

        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1,
            documentBindingId: binding.Id);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(entity.Id, PropertyCertificateIncludeOptions.All);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CertificateType);
        Assert.NotNull(result.DocumentBinding);
        Assert.NotNull(result.DocumentBinding.Document);
    }

    #endregion

    #region GetByPropertyIdAsync Tests

    [Fact]
    public async Task GetByPropertyIdAsync_WithExistingProperty_ReturnsEntities()
    {
        // Arrange
        var entity1 = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        var entity2 = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        _context.PropertyCertificates.AddRange(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByPropertyIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_WithNoEntities_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetByPropertyIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_FiltersInactiveEntities()
    {
        // Arrange
        var entity1 = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        var entity2 = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        entity2.IsActive = false;
        _context.PropertyCertificates.AddRange(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByPropertyIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_FiltersMarkedForDeletion()
    {
        // Arrange
        var entity1 = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        var entity2 = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1,
            markedForDeletion: true);
        _context.PropertyCertificates.AddRange(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByPropertyIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    #endregion

    #region GetByPropertyIdAsync with IncludeOptions Tests

    [Fact]
    public async Task GetByPropertyIdAsync_WithIncludeNone_ReturnsEntitiesWithoutNavigationProperties()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByPropertyIdAsync(1, PropertyCertificateIncludeOptions.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result[0].CertificateType);
        Assert.Null(result[0].DocumentBinding);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_WithIncludeCertificateType_ReturnsEntitiesWithCertificateType()
    {
        // Arrange
        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByPropertyIdAsync(1, PropertyCertificateIncludeOptions.CertificateType);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.NotNull(result[0].CertificateType);
    }

    [Fact]
    public async Task GetByPropertyIdAsync_WithIncludeAll_ReturnsEntitiesWithAllNavigationProperties()
    {
        // Arrange
        var document = EntityTestHelpers.CreateDocumentEntity();
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var binding = EntityTestHelpers.CreateDocumentBindingEntity(documentId: document.Id);
        _context.DocumentBindings.Add(binding);
        await _context.SaveChangesAsync();

        var entity = EntityTestHelpers.CreatePropertyCertificateEntity(
            propertyId: 1,
            certificateTypeId: 1,
            documentBindingId: binding.Id);
        _context.PropertyCertificates.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByPropertyIdAsync(1, PropertyCertificateIncludeOptions.All);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.NotNull(result[0].CertificateType);
        Assert.NotNull(result[0].DocumentBinding);
        Assert.NotNull(result[0].DocumentBinding!.Document);
    }

    #endregion

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}
