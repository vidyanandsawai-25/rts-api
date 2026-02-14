using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for ScreenMaster CRUD operations
/// </summary>
public interface IScreenMasterService : ICommonCrudService<ScreenMasterEntity, ScreenMasterDto, CreateScreenMasterDto, UpdateScreenMasterDto, ScreenMasterQueryParameters, int>
{
}
