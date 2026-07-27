using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Application.DTOs.Master.PropertyCertificateType;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// A protected certificate type (CC/OC/Electric Bill — system-defined, tax-relevant) must not be
/// deletable or deactivatable through standard master maintenance.
/// </summary>
public class PropertyCertificateTypeServiceTests
{
    private static IMapper BuildMapper()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<PropertyCertificateTypeMappingProfile>(),
            NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    [Fact]
    public async Task DeleteAsync_ProtectedType_ThrowsValidationException()
    {
        var entity = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Occupancy Certificate",
            CertificateTypeCode = "OC",
            IsProtected = true,
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(entity, 2);

        var repo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = new PropertyCertificateTypeService(repo.Object, new Mock<IUnitOfWork>().Object, BuildMapper());

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => service.DeleteAsync(2));

        repo.Verify(r => r.DeleteAsync(It.IsAny<PropertyCertificateTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonProtectedType_Succeeds()
    {
        var entity = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Possession Certificate",
            CertificateTypeCode = "POSSESSION",
            IsProtected = false,
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(entity, 4);

        var repo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        repo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = new PropertyCertificateTypeService(repo.Object, unitOfWork.Object, BuildMapper());

        var result = await service.DeleteAsync(4);

        Assert.True(result);
        repo.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }
}
