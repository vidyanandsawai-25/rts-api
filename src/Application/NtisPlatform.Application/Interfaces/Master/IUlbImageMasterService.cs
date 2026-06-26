using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IUlbImageMasterService
    : ICommonCrudService<UlbImageMasterEntity, UlbImageMasterDto, CreateUlbImageMasterDto, UpdateUlbImageMasterDto, UlbImageMasterQueryParameters, int>
{
}
