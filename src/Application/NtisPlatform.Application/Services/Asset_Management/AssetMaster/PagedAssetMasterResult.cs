using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services.Asset_Management;

public class PagedAssetMasterResult : PagedResult<AssetMasterDto>
{
    public decimal? TotalCapitalValue { get; set; }
    public int? ActiveAssetsCount { get; set; }

    public PagedAssetMasterResult(IEnumerable<AssetMasterDto> items, int totalCount, int pageNumber, int pageSize)
        : base(items, totalCount, pageNumber, pageSize)
    {
    }
}
