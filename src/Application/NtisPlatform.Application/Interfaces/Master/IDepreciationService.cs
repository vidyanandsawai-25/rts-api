using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IDepreciationService : ICommonCrudService<DepreciationMasterEntity, DepreciationDtos, CreateDepreciationDto, UpdateDepreciationDto, DepreciationQueryParameters, int>
{
}
