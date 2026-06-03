using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Tests.Helpers;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Critical regression tests for PropertyCertificateService bug fixes:
/// 1. ToggleEnabledAsync can re-enable disabled certificates
/// 2. UpdateAsync works on disabled certificates
/// 3. DeleteAsync works on disabled certificates
/// </summary>
public class PropertyCertificateServiceCriticalFixTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private Mock<IUnitOfWork> CreateMockUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return mock;
    }

    #region Critical Fix: ToggleEnabledAsync can re-enable disabled certificates

    [Fact]
    public async Task ToggleEnabledAsync_CanReEnableDisabledCertificate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);

        await context.SaveChangesAsync();

        // Create a disabled certificate (IsActive = false)
        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.Disable(); // Set IsActive to false
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;

        // Act: Re-enable the disabled certificate
        await service.ToggleEnabledAsync(certId, true, 2, CancellationToken.None);

        // Assert: Certificate should now be enabled
        var updated = await context.PropertyCertificates.FindAsync(certId);
        updated.Should().NotBeNull();
        updated!.IsActive.Should().BeTrue("certificate should be re-enabled");
        updated.UpdatedBy.Should().Be(2);
    }

    [Fact]
    public async Task ToggleEnabledAsync_DisableThenEnable_Works()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;

        // Act: Disable then enable
        await service.ToggleEnabledAsync(certId, false, 2, CancellationToken.None);
        await service.ToggleEnabledAsync(certId, true, 2, CancellationToken.None);

        // Assert: Should be enabled at the end
        var result = await context.PropertyCertificates.FindAsync(certId);
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue("certificate should be enabled after disable-enable cycle");
    }

    [Fact]
    public async Task ToggleEnabledAsync_ThrowsException_WhenCertificateMarkedForDeletion()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.MarkForDeletion();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(
            () => service.ToggleEnabledAsync(certificate.Id, true, 2, CancellationToken.None));
    }

    #endregion

    #region Critical Fix: UpdateAsync works on disabled certificates

    [Fact]
    public async Task UpdateAsync_CanUpdateDisabledCertificate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        // Create a disabled certificate
        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.Disable();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;
        var newCertNo = "CERT-002";
        var newDate = DateTime.Now.AddDays(-1); // Use past date to avoid validation error

        // Act: Update the disabled certificate
        await service.UpdateAsync(certId, newCertNo, newDate, 2, CancellationToken.None);

        // Assert: Certificate should be updated
        var updated = await context.PropertyCertificates.FindAsync(certId);
        updated.Should().NotBeNull();
        updated!.CertificateNo.Should().Be(newCertNo);
        updated.IssueDate.Should().Be(newDate);
        updated.UpdatedBy.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsException_WhenCertificateMarkedForDeletion()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.MarkForDeletion();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(
            () => service.UpdateAsync(certificate.Id, "NEW", DateTime.Now, 2, CancellationToken.None));
    }

    #endregion

    #region Critical Fix: DeleteAsync works on disabled certificates

    [Fact]
    public async Task DeleteAsync_CanDeleteDisabledCertificate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        // Create a disabled certificate
        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.Disable();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;

        // Act: Delete the disabled certificate
        await service.DeleteAsync(certId, 2, CancellationToken.None);

        // Assert: Certificate should be marked for deletion
        var deleted = await context.PropertyCertificates.FindAsync(certId);
        deleted.Should().NotBeNull();
        deleted!.MarkedForDeletion.Should().BeTrue();
        deleted.UpdatedBy.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenCertificateAlreadyMarkedForDeletion()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.MarkForDeletion();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        // Act & Assert: Should throw exception because already marked for deletion
        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(
            () => service.DeleteAsync(certificate.Id, 2, CancellationToken.None));
    }

    #endregion

    #region Integration: BulkSave scenario

    [Fact]
    public async Task BulkSave_ReenableExistingInactiveCertificate_Succeeds()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        // Create an inactive certificate
        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.Disable();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;

        // Act: Simulate bulk save re-enabling the certificate
        await service.UpdateAsync(certId, "CERT-002", DateTime.Now.AddDays(-1), 2, CancellationToken.None);
        await service.ToggleEnabledAsync(certId, true, 2, CancellationToken.None);

        // Assert: Certificate should be updated and enabled
        var result = await context.PropertyCertificates.FindAsync(certId);
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
        result.CertificateNo.Should().Be("CERT-002");
    }

    #endregion
}
