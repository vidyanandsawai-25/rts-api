using NtisPlatform.Application.DTOs.Asset_Management.AssetUseFactorCVMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management;

/// <summary>CRUD service for [AMS].[UseFactorCVMaster].</summary>
public interface IAssetUseFactorCVService :
    ICommonCrudService<
        AssetUseFactorCVMasterEntity,
        AssetUseFactorCVMasterDto,
        CreateAssetUseFactorCVMasterDto,
        UpdateAssetUseFactorCVMasterDto,
        AssetUseFactorCVMasterQueryParameters,
        int>
{
}
