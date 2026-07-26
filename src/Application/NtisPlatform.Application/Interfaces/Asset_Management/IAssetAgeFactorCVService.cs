using NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>CRUD service for [AMS].[AgeFactorCVMaster].</summary>
public interface IAssetAgeFactorCVService :
    ICommonCrudService<
        AssetAgeFactorCVMasterEntity,
        AssetAgeFactorCVMasterDto,
        CreateAssetAgeFactorCVMasterDto,
        UpdateAssetAgeFactorCVMasterDto,
        AssetAgeFactorCVMasterQueryParameters,
        int>
{
}
