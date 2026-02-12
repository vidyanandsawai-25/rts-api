using NtisPlatform.Application.DTOs.Master.DesignationMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for DesignationMaster CRUD operations
/// </summary>
public interface IDesignationMasterService : ICommonCrudService<DesignationMasterEntity, DesignationMasterDto, CreateDesignationMasterDto, UpdateDesignationMasterDto, DesignationMasterQueryParameters, int>
{
}
