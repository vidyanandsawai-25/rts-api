using NtisPlatform.Application.DTOs.Master.PropertyCertificateType;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyCertificateTypeService : ICommonCrudService<PropertyCertificateTypeMasterEntity, PropertyCertificateTypeDto, CreatePropertyCertificateTypeDto, UpdatePropertyCertificateTypeDto, PropertyCertificateTypeQueryParameters, int>
{

}
