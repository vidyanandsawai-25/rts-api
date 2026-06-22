using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Services.Master;

public class TypeOfUseByPropertyTypeService 
    : BaseCommonCrudService<TypeOfUseEntity, TypeOfUseByPropertyTypeResponseDto, CreateTypeOfUseDto, UpdateTypeOfUseDto, TypeOfUseQueryParameters, int>, 
      ITypeOfUseByPropertyTypeService
{
    private readonly ITypeOfUseByPropertyTypeRepository _typeOfUseRepository;

    public TypeOfUseByPropertyTypeService(
        ITypeOfUseByPropertyTypeRepository repository, 
        IUnitOfWork unitOfWork, 
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _typeOfUseRepository = repository;
    }

    public override async Task<TypeOfUseByPropertyTypeResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Treat 'id' as 'propertyTypeId'
        var entities = await _typeOfUseRepository.GetTypeOfUseByPropertyTypeIdAsync(id, cancellationToken);
        
        var dto = new TypeOfUseByPropertyTypeResponseDto();
        dto.AddRange(_mapper.Map<IEnumerable<TypeOfUseByPropertyTypeItemDto>>(entities));
        
        return dto;
    }

    public async Task<IEnumerable<TypeOfUseByPropertyTypeResponseDto>> GetTypeOfUseByPropertyTypeIdAsync(int propertyTypeId, CancellationToken cancellationToken)
    {
        var entities = await _typeOfUseRepository.GetTypeOfUseByPropertyTypeIdAsync(propertyTypeId, cancellationToken);
        var itemDtos = _mapper.Map<IEnumerable<TypeOfUseByPropertyTypeItemDto>>(entities);
        
        var dto = new TypeOfUseByPropertyTypeResponseDto();
        dto.AddRange(itemDtos);
        
        return new List<TypeOfUseByPropertyTypeResponseDto> { dto };
    }
}
