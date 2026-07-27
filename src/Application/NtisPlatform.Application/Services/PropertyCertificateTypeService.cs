using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyCertificateType;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyCertificateTypeService : BaseCommonCrudService<PropertyCertificateTypeMasterEntity, PropertyCertificateTypeDto, CreatePropertyCertificateTypeDto, UpdatePropertyCertificateTypeDto, PropertyCertificateTypeQueryParameters, int>, IPropertyCertificateTypeService
{
    public PropertyCertificateTypeService(
        IRepository<PropertyCertificateTypeMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    /// <summary>
    /// Protected certificate types (CC/OC/Electric Bill — system-defined, tax-relevant) cannot be
    /// removed through standard master maintenance.
    /// </summary>
    protected override Task<ValidationResult> ValidateForDeleteAsync(
        int id, PropertyCertificateTypeMasterEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity.IsProtected)
        {
            return Task.FromResult(ValidationResult.Failure(
                nameof(entity.IsProtected),
                $"Certificate type '{entity.CertificateTypeName}' is protected and cannot be deleted."));
        }

        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Protected certificate types cannot be deactivated either — deactivating would silently hide
    /// a type with real tax consequences from the Building Permission tab.
    /// </summary>
    protected override Task<ValidationResult> ValidateForDeactivationAsync(
        int id, PropertyCertificateTypeMasterEntity currentEntity, PropertyCertificateTypeMasterEntity updatedEntity, CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsProtected)
        {
            return Task.FromResult(ValidationResult.Failure(
                nameof(currentEntity.IsProtected),
                $"Certificate type '{currentEntity.CertificateTypeName}' is protected and cannot be deactivated."));
        }

        return Task.FromResult(ValidationResult.Success());
    }
}
