using NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>CRUD service for [AMS].[NatureFactorCVMaster].</summary>
public interface IAssetNatureFactorCVService :
    ICommonCrudService<
        AssetNatureFactorCVMasterEntity,
        AssetNatureFactorCVMasterDto,
        CreateAssetNatureFactorCVMasterDto,
        UpdateAssetNatureFactorCVMasterDto,
        AssetNatureFactorCVMasterQueryParameters,
        int>
{
}
