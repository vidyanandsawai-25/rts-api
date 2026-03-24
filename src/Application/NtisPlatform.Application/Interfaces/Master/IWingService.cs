using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IWingService : ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>
{
}