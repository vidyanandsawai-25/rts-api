using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyDescriptionAndTypeOfUseValidationService : BaseCommonCrudService<PropertyDescriptionAndTypeOfUseValidationEntity, PropertyDescriptionAndTypeOfUseValidationDto, CreatePropertyDescriptionAndTypeOfUseValidationDto, UpdatePropertyDescriptionAndTypeOfUseValidationDto, PropertyDescriptionAndTypeOfUseValidationQueryParameters, int>, IPropertyDescriptionAndTypeOfUseValidationService
{
    public PropertyDescriptionAndTypeOfUseValidationService(
        IRepository<PropertyDescriptionAndTypeOfUseValidationEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
