using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces;

public interface ITypeOfUseByPropertyTypeService : ICommonCrudService<TypeOfUseEntity, TypeOfUseByPropertyTypeResponseDto, CreateTypeOfUseDto, UpdateTypeOfUseDto, TypeOfUseQueryParameters, int>
{
    Task<IEnumerable<TypeOfUseByPropertyTypeResponseDto>> GetTypeOfUseByPropertyTypeIdAsync(int propertyTypeId, CancellationToken cancellationToken);
}

