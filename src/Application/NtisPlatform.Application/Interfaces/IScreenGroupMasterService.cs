using NtisPlatform.Application.DTOs.Master.ScreenGroupMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for ScreenGroupMaster CRUD operations
/// </summary>
public interface IScreenGroupMasterService : ICommonCrudService<ScreenGroupMasterEntity, ScreenGroupMasterDto, CreateScreenGroupMasterDto, UpdateScreenGroupMasterDto, ScreenGroupMasterQueryParameters, int>
{
}
