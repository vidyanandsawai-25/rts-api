using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        #region Lease & Renter Query and Activation Methods

        public async Task<List<ShopWiseDetailsDto>> GetShopWiseDetailsByParentAssetIdAsync(int parentAssetId, CancellationToken cancellationToken)
        {
            var results = await (from asset in _repository.GetQueryable()
                                 where asset.ParentAssetId == parentAssetId && !asset.MarkedForDeletion && asset.IsActive
                                 join leaseRent in _leaseRentDetailsRepository.GetQueryable()
                                     on asset.Id equals leaseRent.AssetId into leaseRentGroup
                                 from leaseRent in leaseRentGroup.Where(r => !r.MarkedForDeletion && r.IsActive).DefaultIfEmpty()
                                 join floorDetails in _floorDetailsRepository.GetQueryable()
                                     on asset.Id equals floorDetails.AssetId into floorGroup
                                 from floorDetails in floorGroup.Where(f => !f.MarkedForDeletion && f.IsActive).DefaultIfEmpty()
                                 select new
                                 {
                                     Asset = asset,
                                     LeaseRent = leaseRent,
                                     FloorDetails = floorDetails,
                                     FloorName = floorDetails != null && floorDetails.Floor != null ? floorDetails.Floor.Description : null
                                 })
                                 .AsNoTracking()
                                 .ToListAsync(cancellationToken);

            var shopWiseDetails = new List<ShopWiseDetailsDto>();
            int serialNo = 1;

            foreach (var result in results)
            {
                string agreementPeriod = "N/A";
                if (result.LeaseRent?.LeaseStartDate != null && result.LeaseRent?.LeaseEndDate != null)
                {
                    var fromDate = result.LeaseRent.LeaseStartDate;
                    var toDate = result.LeaseRent.LeaseEndDate.Value;
                    var years = (toDate - fromDate).TotalDays / 365.25;
                    agreementPeriod = $"{fromDate:yyyy-MM-dd}\n{Math.Round(years, 0)} years";
                }

                var shopDetail = new ShopWiseDetailsDto
                {
                    SerialNo = serialNo++,
                    AssetId = result.Asset.AssetNo ?? "N/A",
                    Floor = result.FloorName ?? "N/A",
                    ShopNo = GetShopNumber(result.Asset.AssetName),
                    ShopName = GetShopName(result.Asset.AssetName, result.LeaseRent?.TenantName),
                    Area = result.LeaseRent?.TotalAreaSqFt ?? (decimal?)null,
                    Occupier = result.LeaseRent?.TenantName ?? "Vacant",
                    Contact = result.LeaseRent?.TenantMobile ?? "N/A",
                    AnnualRent = result.LeaseRent?.RentAmount ?? 0,
                    AgreementPeriod = agreementPeriod,
                    Status = GetStatusFromOccupancy(result.Asset.OccupancyStatus),
                    Condition = "N/A"
                };

                shopWiseDetails.Add(shopDetail);
            }

            return shopWiseDetails;
        }

        public async Task ActivateLeaseRentDetailsAsync(List<int> assetIds, DateTime now, CancellationToken cancellationToken)
        {
            var leaseRentDetails = await _leaseRentDetailsRepository.GetQueryable()
                .Where(x => !x.MarkedForDeletion && assetIds.Contains(x.AssetId))
                .ToListAsync(cancellationToken);

            foreach (var leaseRentDetail in leaseRentDetails)
            {
                leaseRentDetail.IsActive = true;
                leaseRentDetail.UpdatedDate = now;
            }
        }

        public async Task<List<AssetLeaseRentDetailsDto>> GetRenterDetailsAsync(List<int> subAssetIds, CancellationToken cancellationToken)
        {
            return await _leaseRentDetailsRepository.GetQueryable()
                .AsNoTracking()
                .Where(r => subAssetIds.Contains(r.AssetId) && r.IsActive && !r.MarkedForDeletion)
                .Select(r => new AssetLeaseRentDetailsDto
                {
                    Id = r.Id,
                    IsActive = r.IsActive,
                    CreatedDate = r.CreatedDate,
                    UpdatedDate = r.UpdatedDate,
                    FloorDetailsId = r.FloorDetailsId,
                    AssetId = r.AssetId,
                    TenantName = r.TenantName ?? string.Empty,
                    GSTNo = r.GSTNo,
                    TotalAreaSqFt = r.TotalAreaSqFt,
                    TenantAadhaarNo = r.TenantAadhaarNo,
                    TenantPanCardNo = r.TenantPanCardNo,
                    TenantMobile = r.TenantMobile ?? string.Empty,
                    TenantEmail = r.TenantEmail,
                    LeaseStartDate = r.LeaseStartDate,
                    LeaseEndDate = r.LeaseEndDate,
                    Duration = r.Duration,
                    PaymentFrequency = r.PaymentFrequency ?? "Monthly",
                    RentAmount = r.RentAmount,
                    SecurityDeposit = r.SecurityDeposit,
                    DepositType = r.DepositType,
                    AgreementId = r.AgreementId,
                    IncrementFrequency = r.IncrementFrequency,
                    IncrementType = r.IncrementType,
                    IncrementValue = r.IncrementValue,
                    IncrementMethod = r.IncrementMethod,
                    Names = new AssetLeaseRentDetailsNamesDto
                    {
                        AssetNo = r.Asset != null ? r.Asset.AssetNo : null,
                        AssetName = r.Asset != null ? r.Asset.AssetName : null
                    }
                })
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region Shop Display Helper Methods

        private static string GetShopNumber(string? assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                return "N/A";

            var parts = assetName.Split(' ', '-', '_');
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out _))
                    return part.Trim();
            }

            return assetName;
        }

        private static string GetShopName(string? assetName, string? renterName)
        {
            if (!string.IsNullOrEmpty(renterName))
                return renterName;

            return assetName ?? "N/A";
        }

        private static string GetStatusFromOccupancy(string? occupancyStatus)
        {
            if (string.IsNullOrEmpty(occupancyStatus))
                return "Unknown";

            return occupancyStatus.ToLower() switch
            {
                "occupied" => "Paid",
                "vacant" => "Vacant",
                "leased" => "Paid",
                "rented" => "Paid",
                _ => occupancyStatus
            };
        }

        #endregion
    }
}
