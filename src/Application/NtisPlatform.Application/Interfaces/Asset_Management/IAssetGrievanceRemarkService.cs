using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Interfaces.Asset_Management
{
    /// <summary>
    /// Service interface for Asset Grievance Remark Master CRUD operations under Asset Management
    /// </summary>
    public interface IAssetGrievanceRemarkService : ICommonCrudService<AssetGrievanceRemarkMasterEntity, AssetGrievanceRemarkDto, CreateAssetGrievanceRemarkDto, UpdateAssetGrievanceRemarkDto, AssetGrievanceRemarkQueryParameters, int>
    {
    }
}
