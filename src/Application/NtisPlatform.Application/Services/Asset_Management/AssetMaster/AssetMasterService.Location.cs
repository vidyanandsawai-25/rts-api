using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        #region Location Enrichment Methods

        /// <summary>
        /// Resolved location context for an asset: FK ids, address fields, the Zone/Ward short
        /// codes ("No") and their descriptive names, the Mouja name, the nearest landmark, and the
        /// Organization/Department ids resolved to their display names. All of these live on
        /// (or are reached through) AMS.AssetDetails, not AssetMaster.
        /// </summary>
        private sealed record AssetLocationInfo(
            int? ZoneId, int? WardId, int? SubZoneId, int? MoujaId,
            string? AssetWardNo, string? Address, string? PinCode, string? CSN, string? Landmark,
            decimal? Latitude, decimal? Longitude,
            string? ZoneNo, string? ZoneName, string? WardNo, string? WardName, string? MoujaName,
            string? SubZoneNo, string? SubZoneName,
            int? OrganizationId, int? DepartmentId,
            string? OrganizationName, string? DepartmentName);

        /// <summary>
        /// Parent AssetDetails + resolved master-data names, batch-loaded once per distinct parent
        /// so child assets without their own AssetDetails row can fall back to it without a per-child query.
        /// </summary>
        private sealed record ParentFallbackLocation(
            AssetDetailsEntity? Details,
            string? ZoneNo, string? ZoneName,
            string? WardNo, string? WardName,
            string? MoujaName,
            string? SubZoneNo, string? SubZoneName,
            string? OrganizationName);

        /// <summary>
        /// Batch-resolves the location context for the given asset ids from AMS.AssetDetails
        /// (Zone/Ward/Mouja/Organization) plus the owning department off AMS.AssetMaster. A sub-unit
        /// without its own AssetDetails row falls back to its parent's AssetDetails. Keyed by asset id.
        /// Used by every read path so these fields show everywhere.
        /// </summary>
        private async Task<Dictionary<int, AssetLocationInfo>> GetLocationInfoByAssetIdsAsync(
            IReadOnlyCollection<int> assetIds, CancellationToken cancellationToken)
        {
            if (assetIds == null || assetIds.Count == 0)
            {
                return new Dictionary<int, AssetLocationInfo>();
            }

            var query = from a in _repository.GetQueryable()
                        where assetIds.Contains(a.Id)
                        join details in _detailsRepository.GetQueryable() on a.Id equals details.AssetId into detailsGroup
                        from details in detailsGroup.DefaultIfEmpty()
                        join zone in _zoneRepository.GetQueryable() on details.ZoneId equals zone.Id into zoneGroup
                        from zone in zoneGroup.DefaultIfEmpty()
                        join ward in _wardRepository.GetQueryable() on details.WardId equals ward.Id into wardGroup
                        from ward in wardGroup.DefaultIfEmpty()
                        join mouja in _moujaRepository.GetQueryable() on details.MoujaId equals mouja.Id into moujaGroup
                        from mouja in moujaGroup.DefaultIfEmpty()
                        join subZone in _subZoneRepository.GetQueryable() on details.SubZoneId equals subZone.Id into subZoneGroup
                        from subZone in subZoneGroup.DefaultIfEmpty()
                        join org in _organizationRepository.GetQueryable() on details.OrganizationId equals org.Id into orgGroup
                        from org in orgGroup.DefaultIfEmpty()
                        join dept in _departmentRepository.GetQueryable() on a.DepartmentId equals dept.Id into deptGroup
                        from dept in deptGroup.DefaultIfEmpty()
                        select new
                        {
                            a.Id,
                            a.ParentAssetId,
                            a.DepartmentId,
                            Details = details,
                            ZoneNo = zone != null ? zone.ZoneNo.ToString() : null,
                            ZoneName = zone != null ? zone.Description : null,
                            WardNo = ward != null ? ward.WardNo : null,
                            WardName = ward != null ? ward.Description : null,
                            MoujaName = mouja != null ? mouja.MoujaName : null,
                            SubZoneNo = subZone != null ? subZone.SubZoneNo : null,
                            SubZoneName = subZone != null ? subZone.SubZoneName : null,
                            OrganizationName = org != null ? org.OrganizationName : null,
                            DepartmentName = dept != null ? dept.OwningDepartmentName : null
                        };

            var rawLocations = await query.AsNoTracking().ToListAsync(cancellationToken);

            // Names/codes resolved above (ZoneNo, ZoneName, ...) were joined against the asset's OWN
            // AssetDetails row. A child asset never has one, so it falls back to the parent's. Batch-resolve
            // that fallback for every distinct parent in one extra round trip instead of querying per child
            // (previously up to 6 sequential queries — parent details + zone/ward/mouja/subzone/org — for
            // EACH child asset in the batch).
            var parentIdsNeedingFallback = rawLocations
                .Where(a => a.Details == null && a.ParentAssetId.HasValue)
                .Select(a => a.ParentAssetId!.Value)
                .Distinct()
                .ToList();

            var parentLocationLookup = new Dictionary<int, ParentFallbackLocation>();
            if (parentIdsNeedingFallback.Count > 0)
            {
                var parentQuery = from details in _detailsRepository.GetQueryable()
                                   where parentIdsNeedingFallback.Contains(details.AssetId)
                                   join zone in _zoneRepository.GetQueryable() on details.ZoneId equals zone.Id into zoneGroup
                                   from zone in zoneGroup.DefaultIfEmpty()
                                   join ward in _wardRepository.GetQueryable() on details.WardId equals ward.Id into wardGroup
                                   from ward in wardGroup.DefaultIfEmpty()
                                   join mouja in _moujaRepository.GetQueryable() on details.MoujaId equals mouja.Id into moujaGroup
                                   from mouja in moujaGroup.DefaultIfEmpty()
                                   join subZone in _subZoneRepository.GetQueryable() on details.SubZoneId equals subZone.Id into subZoneGroup
                                   from subZone in subZoneGroup.DefaultIfEmpty()
                                   join org in _organizationRepository.GetQueryable() on details.OrganizationId equals org.Id into orgGroup
                                   from org in orgGroup.DefaultIfEmpty()
                                   select new
                                   {
                                       details.AssetId,
                                       Location = new ParentFallbackLocation(
                                           details,
                                           zone != null ? zone.ZoneNo.ToString() : null,
                                           zone != null ? zone.Description : null,
                                           ward != null ? ward.WardNo : null,
                                           ward != null ? ward.Description : null,
                                           mouja != null ? mouja.MoujaName : null,
                                           subZone != null ? subZone.SubZoneNo : null,
                                           subZone != null ? subZone.SubZoneName : null,
                                           org != null ? org.OrganizationName : null)
                                   };

                parentLocationLookup = await parentQuery
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.AssetId, x => x.Location, cancellationToken);
            }

            var result = new Dictionary<int, AssetLocationInfo>();

            foreach (var a in rawLocations)
            {
                var finalDetails = a.Details;
                var zoneNo = a.ZoneNo; var zoneName = a.ZoneName;
                var wardNo = a.WardNo; var wardName = a.WardName;
                var moujaName = a.MoujaName;
                var subZoneNo = a.SubZoneNo; var subZoneName = a.SubZoneName;
                var organizationName = a.OrganizationName;

                if (finalDetails == null && a.ParentAssetId.HasValue &&
                    parentLocationLookup.TryGetValue(a.ParentAssetId.Value, out var parentLocation))
                {
                    finalDetails = parentLocation.Details;
                    zoneNo = parentLocation.ZoneNo;
                    zoneName = parentLocation.ZoneName;
                    wardNo = parentLocation.WardNo;
                    wardName = parentLocation.WardName;
                    moujaName = parentLocation.MoujaName;
                    subZoneNo = parentLocation.SubZoneNo;
                    subZoneName = parentLocation.SubZoneName;
                    organizationName = parentLocation.OrganizationName;
                }

                result[a.Id] = new AssetLocationInfo(
                    ZoneId: finalDetails?.ZoneId,
                    WardId: finalDetails?.WardId,
                    SubZoneId: finalDetails?.SubZoneId,
                    MoujaId: finalDetails?.MoujaId,
                    AssetWardNo: finalDetails?.AssetWardNo,
                    Address: finalDetails?.Address,
                    PinCode: finalDetails?.PinCode,
                    CSN: finalDetails?.CSN,
                    Landmark: finalDetails?.NearestLandmark,
                    Latitude: finalDetails?.Latitude,
                    Longitude: finalDetails?.Longitude,
                    ZoneNo: zoneNo,
                    ZoneName: zoneName,
                    WardNo: wardNo,
                    WardName: wardName,
                    MoujaName: moujaName,
                    SubZoneNo: subZoneNo,
                    SubZoneName: subZoneName,
                    OrganizationId: finalDetails?.OrganizationId,
                    DepartmentId: a.DepartmentId,
                    OrganizationName: organizationName,
                    DepartmentName: a.DepartmentName
                );
            }

            return result;
        }

        private static void ApplyLocation(AssetDetailsDto details, AssetMasterNamesDto names, AssetLocationInfo info)
        {
            if (details == null || names == null || info == null) return;

            details.ZoneId = info.ZoneId;
            details.WardId = info.WardId;
            details.SubZoneId = info.SubZoneId;
            details.SubZoneName = info.SubZoneName;
            details.MoujaId = info.MoujaId;
            details.AssetWardNo = info.AssetWardNo;
            details.Address = info.Address;
            details.PinCode = info.PinCode;
            details.CSN = info.CSN;
            details.NearestLandmark = info.Landmark;
            details.Latitude = info.Latitude;
            details.Longitude = info.Longitude;
            details.OrganizationId = info.OrganizationId ?? 0;
            names.ZoneNo = info.ZoneNo;
            names.ZoneName = info.ZoneName;
            names.WardNo = info.WardNo;
            names.WardName = info.WardName;
            names.MoujaName = info.MoujaName;
            names.SubZoneNo = info.SubZoneNo;
            names.OrganizationName = info.OrganizationName;
            names.DepartmentName = info.DepartmentName;
        }

        /// <summary>Enriches a list of asset DTOs with resolved Zone/Ward/Mouja location context.</summary>
        private async Task EnrichLocationAsync(IReadOnlyList<AssetMasterDto> dtos, CancellationToken cancellationToken)
        {
            if (dtos.Count == 0)
                return;
            var locations = await GetLocationInfoByAssetIdsAsync(
                dtos.Select(d => d.Id).Distinct().ToList(), cancellationToken);
            foreach (var dto in dtos)
            {
                if (!locations.TryGetValue(dto.Id, out var info))
                    continue;
                dto.DepartmentId = info.DepartmentId;
                ApplyLocation(dto.Details, dto.Names, info);
                PopulateFlatProperties(dto);
            }
        }

        private static void PopulateFlatProperties(AssetMasterDto dto)
        {
            // Name-resolution properties — resolved from master-table JOINs via AssetMasterNamesDto
            //dto.OrganizationName  = dto.Names?.OrganizationName;
            //dto.AuthorityName     = dto.Names?.OrganizationName; // Falling back Authority to Organization for now since it is not fetched
            dto.AssetCategoryName = dto.Names?.AssetCategoryName;
            dto.AssetTypeName = dto.Names?.AssetTypeName;
            dto.DepartmentName = dto.Names?.DepartmentName;
            dto.WardName = dto.Names?.WardName;
            dto.WardNo = dto.Names?.WardNo;
            dto.ZoneName = dto.Names?.ZoneName;
            dto.ZoneNo = dto.Names?.ZoneNo;
            dto.MoujaName = dto.Names?.MoujaName;
            dto.SubZoneName = dto.Details?.SubZoneName;
            dto.SubZoneNo = dto.Names?.SubZoneNo;
            dto.AssetCondition = dto.Names?.AssetCondition;
            dto.Address = dto.Details?.Address;
        }

        #endregion
    }
}
