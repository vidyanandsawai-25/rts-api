using NtisPlatform.Application.DTOs.Master.ULBMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for ULB Master operations
/// </summary>
public interface IULBMasterService : ICommonCrudService<ULBMasterEntity, ULBMasterDto, CreateULBMasterDto, UpdateULBMasterDto, ULBMasterQueryParameters, int>
{
    // Add any custom methods specific to ULBMaster here if needed
}
