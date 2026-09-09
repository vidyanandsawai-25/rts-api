using System.Data;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Read-only repository for the ApartmentQC top-section panel.
/// Every query is AsNoTracking — this feature never writes.
/// </summary>
public sealed class ApartmentQcTopSectionRepository : IApartmentQcTopSectionRepository
{
    private readonly ApplicationDbContext _context;

    public ApartmentQcTopSectionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PropertyTopSectionRawData?> GetPropertyAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken = default)
    {
        var propertyId = await ResolvePropertyIdAsync(query, cancellationToken);
        if (propertyId is null)
        {
            return null;
        }

        var joined =
            from pm in _context.PropertyMast.AsNoTracking()
            where pm.Id == propertyId.Value && !pm.MarkedForDeletion
            join tzm in _context.TaxZoneMaster.AsNoTracking() on pm.TaxZoneId equals tzm.Id into tzmj
            from tzm in tzmj.DefaultIfEmpty()
            join wm in _context.WardMaster.AsNoTracking() on pm.WardId equals wm.Id into wmj
            from wm in wmj.DefaultIfEmpty()
            join zm in _context.ZoneMaster.AsNoTracking() on wm.ZoneId equals zm.Id into zmj
            from zm in zmj.DefaultIfEmpty()
            join mm in _context.MoujaEntity.AsNoTracking() on pm.MoujaId equals mm.Id into mmj
            from mm in mmj.DefaultIfEmpty()
            join pcm in _context.PropertyCategoryMaster.AsNoTracking() on pm.CategoryId equals pcm.Id into pcmj
            from pcm in pcmj.DefaultIfEmpty()
            join ptm in _context.PropertyTypeMasters.AsNoTracking() on pm.PropertyTypeId equals ptm.Id into ptmj
            from ptm in ptmj.DefaultIfEmpty()
            let assessment = _context.PropertyMastDetails.AsNoTracking()
                .Where(a => a.PropertyId == pm.Id && a.IsActive && !a.MarkedForDeletion)
                .OrderByDescending(a => a.CreatedDate)
                .FirstOrDefault()
            let society = _context.SocietyDetailsMast.AsNoTracking()
                .Where(s => s.PropertyId == pm.Id && s.IsActive)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefault()
            let isLocked = _context.PropertyScreenLocks.AsNoTracking()
                .Any(l => l.PropertyId == pm.Id && l.IsLocked && l.IsActive && !l.MarkedForDeletion)
            let ownerCategory = _context.OwnerTypeMaster.AsNoTracking()
                .Where(o => assessment != null && o.Id == assessment.OwnerTypeId && o.IsActive)
                .Select(o => o.OwnerType)
                .FirstOrDefault()
            // SOCIETY_PLACE is the closest existing PropertyPhotoType to "society main building
            // photo" - there is no dedicated "building" code at SOCIETY scope (only per-WING has
            // one, WING_BUILDING, which doesn't fit a property that spans multiple wings).
            let societyBuildingPhotoGuid = (
                from photo in _context.PropertyPhotos.AsNoTracking()
                join photoType in _context.PropertyPhotoTypes.AsNoTracking() on photo.PhotoTypeId equals photoType.Id
                join binding in _context.DocumentBindings.AsNoTracking() on photo.DocumentBindingId equals binding.Id
                join doc in _context.Documents.AsNoTracking() on binding.DocumentId equals doc.Id
                where society != null
                    && photo.EntityType == "S" && photo.SocietyDetailId == society.Id
                    && photoType.PhotoTypeCode == "SOCIETY_PLACE"
                    && photo.IsLatest && photo.IsActive && !photo.MarkedForDeletion
                    && binding.IsActive && !binding.MarkedForDeletion
                    && doc.IsActive && !doc.MarkedForDeletion
                select (Guid?)doc.DocumentGuid
            ).FirstOrDefault()
            select new PropertyTopSectionRawData
            {
                Id = pm.Id,
                Upic = pm.UPICId ?? string.Empty,
                IsActive = pm.IsActive,
                IsLocked = isLocked,
                WardId = pm.WardId,
                WardNo = wm != null ? wm.WardNo : null,
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                OwnerName = pm.OwnerName,
                OwnerNameEnglish = pm.OwnerNameEnglish,
                OccupierName = pm.OccupierName,
                OccupierNameEnglish = pm.OccupierNameEnglish,
                PlotNo = pm.PlotNo,
                Csn = pm.CSN,
                Address = pm.Address,
                PinCode = pm.PinCode,
                MobileNo = pm.MobileNo,
                AlternateMobileNo = pm.AlternateMobileNo,
                EmailId = pm.EmailId,
                AadharCardNo = assessment != null ? assessment.AdharCardNo : null,
                PropertyCategoryName = pcm != null ? pcm.PropertyCategoryName : null,
                PropertyDescription = ptm != null ? ptm.PropertyDescription : null,
                TaxZoneNo = tzm != null ? tzm.TaxZoneNo : null,
                TaxZoneRemark = tzm != null ? tzm.Remark : null,
                DivisionName = zm != null ? zm.Description : null,
                MoujaName = mm != null ? mm.MoujaName : null,
                SecretaryName = society != null ? society.SecretaryName : null,
                SecretaryNameEnglish = society != null ? society.SecretaryNameEnglish : null,
                SecretaryMobileNo = society != null ? society.SecretaryMobileNo : null,
                SecretaryEmailId = society != null ? society.SecretaryEmailId : null,
                ManagerName = society != null ? society.ManagerName : null,
                ManagerNameEnglish = society != null ? society.ManagerNameEnglish : null,
                ManagerMobileNo = society != null ? society.ManagerMobileNo : null,
                ManagerEmailId = society != null ? society.ManagerEmailId : null,
                SocietyName = society != null ? society.SocietyName : null,
                SocietyNameEnglish = society != null ? society.SocietyNameEnglish : null,
                SocietyAddress = society != null ? society.SocietyAddress : null,
                SocietyAddressEnglish = society != null ? society.SocietyAddressEnglish : null,
                SocietyEmailId = society != null ? society.SocietyEmailId : null,
                LandOwnerName = society != null ? society.LandOwnerName : null,
                LandOwnerNameEnglish = society != null ? society.LandOwnerNameEnglish : null,
                BuilderName = society != null ? society.BuilderName : null,
                BuilderNameEnglish = society != null ? society.BuilderNameEnglish : null,
                BuilderMobileNo = society != null ? society.BuilderMobileNo : null,
                OwnerCategory = ownerCategory,
                SocietyBuildingPhotoGuid = societyBuildingPhotoGuid,
            };

        return await joined.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves a PropertyMast.Id from whichever identifier <paramref name="query"/> supplies,
    /// checked in precedence order. WingDetailsId and SocietyId are resolved the same
    /// direct-FK-or-via-society way as <c>GetApartmentDetailsWingWiseService</c>, since
    /// PropertyMast.SocietyDetailId/WingDetailId and SocietyDetailsMast.PropertyId are two
    /// directions of the same relationship and either may be the one that's populated.
    /// </summary>
    private async Task<int?> ResolvePropertyIdAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken)
    {
        if (query.PropertyId is int propertyId)
        {
            return propertyId;
        }

        if (!string.IsNullOrWhiteSpace(query.Upic))
        {
            return await _context.PropertyMast.AsNoTracking()
                .Where(p => p.UPICId == query.Upic && !p.MarkedForDeletion)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (query.WardId is int wardId && !string.IsNullOrWhiteSpace(query.PropertyNo))
        {
            return await _context.PropertyMast.AsNoTracking()
                .Where(p => p.WardId == wardId && p.PropertyNo == query.PropertyNo && !p.MarkedForDeletion
                    && (query.PartitionNo == null || p.PartitionNo == query.PartitionNo))
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (query.WingDetailsId is int wingDetailsId)
        {
            var wing = await _context.WingDetailsMast.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == wingDetailsId, cancellationToken);
            if (wing is null)
            {
                return null;
            }

            return await _context.PropertyMast.AsNoTracking()
                    .Where(p => !p.MarkedForDeletion && p.WingDetailId == wingDetailsId)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? await _context.SocietyDetailsMast.AsNoTracking()
                    .Where(s => s.Id == wing.SocietyDetailsMastId && s.PropertyId != null)
                    .Select(s => s.PropertyId)
                    .FirstOrDefaultAsync(cancellationToken);
        }

        if (query.SocietyId is int societyId)
        {
            return await _context.SocietyDetailsMast.AsNoTracking()
                .Where(s => s.Id == societyId && s.PropertyId != null)
                .Select(s => s.PropertyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    public async Task<(double? Ft, double? Mtr)> GetCarpetAreaTotalsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var result = await _context.PropertyDetails
            .AsNoTracking()
            .Where(d => d.PropertyId == propertyId && d.IsActive && !d.MarkedForDeletion)
            .GroupBy(_ => 1)
            .Select(g => new { Ft = g.Sum(d => d.CarpetAreaSqFeet ?? 0), Mtr = g.Sum(d => d.CarpetAreaSqMeter ?? 0) })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? (null, null) : (result.Ft, result.Mtr);
    }

    public async Task<(double? Ft, double? Mtr)> GetBuiltUpAreaTotalsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var result = await _context.PropertyDetails
            .AsNoTracking()
            .Where(d => d.PropertyId == propertyId && d.IsActive && !d.MarkedForDeletion)
            .GroupBy(_ => 1)
            .Select(g => new { Ft = g.Sum(d => d.BuiltupAreaSqFeet ?? 0), Mtr = g.Sum(d => d.BuiltupAreaSqMeter ?? 0) })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? (null, null) : (result.Ft, result.Mtr);
    }

    public async Task<(double? Ft, double? Mtr)> GetPlotAreaTotalsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var result = await _context.PropertyDetails
            .AsNoTracking()
            .Where(d => d.PropertyId == propertyId && d.IsActive && !d.MarkedForDeletion)
            .GroupBy(_ => 1)
            .Select(g => new { Ft = g.Sum(d => d.BuiltupAreaSqFeet ?? 0), Mtr = g.Sum(d => d.BuiltupAreaSqMeter ?? 0) })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null ? (null, null) : (result.Ft, result.Mtr);
    }

    /// <summary>
    /// Gets the current tax, retrospective tax, pre-merge old current tax, and pending current tax details for the property.
    /// Uses TAXTOTAL tax code filter.
    /// </summary>
    public async Task<(decimal? CurrentTax, decimal? RetroTax, decimal? OldCurrentTax, decimal? PendingCurrent)> GetAdditionalRevenueTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // 1. Get currentTax from PolicyTaxDetails (TAXTOTAL and IsCurrent = 1)
        // select ptd.TaxAmount as currentTax from ptis.PolicyTaxDetails PTD
        // inner join ptis.PropertyMast PM on PTD.PropertyId=PM.Id
        // inner join ptis.taxmaster TX on tx.id=Ptd.TaxId
        // where PTD.IsActive=1 and ptd.MarkedForDeletion=0 and UPPER(TX.[TaxCode]) = 'TAXTOTAL' and pm.Id=@propertyId and Ptd.IsCurrent=1
        var currentTax = await (
            from ptd in _context.PolicyTaxDetails.AsNoTracking()
            join tx in _context.TaxMaster.AsNoTracking() on ptd.TaxId equals tx.Id
            where ptd.PropertyId == propertyId
                  && tx.TaxCode != null && tx.TaxCode.ToUpper() == "TAXTOTAL"
                  && ptd.IsActive
                  && !ptd.MarkedForDeletion
                  && ptd.IsCurrent
            select ptd.TaxAmount
        ).SumAsync(cancellationToken);

        // 2. Get retroTax from PolicyTaxDetails (PolicyCodeMaster.IsRetroDemand = 1 and TAXTOTAL)
        // select ptd.taxamount as RetroTax from ptis.PolicyTaxDetails PTD 
        // inner join ptis.PolicyCodeMaster PCM on ptd.PolicyCodeId = pcm.id
        // inner join ptis.TaxMaster tx on ptd.TaxId=tx.id
        // where pcm.IsRetroDemand=1 and UPPER(TX.[TaxCode]) = 'TAXTOTAL' and ptd.PropertyId=@propertyId
        var retroTax = await (
            from ptd in _context.PolicyTaxDetails.AsNoTracking()
            join pcm in _context.PolicyCodeMaster.AsNoTracking() on ptd.PolicyCodeId equals pcm.Id
            join tx in _context.TaxMaster.AsNoTracking() on ptd.TaxId equals tx.Id
            where ptd.PropertyId == propertyId
                  && tx.TaxCode != null && tx.TaxCode.ToUpper() == "TAXTOTAL"
                  && pcm.IsRetroDemand
            select (decimal?)ptd.TaxAmount
        ).SumAsync(cancellationToken);

        // 3. Get pendingCurrent from TransMast (PolicyCode = 'OLD_ARREARS' and TAXTOTAL)
        // select TaxAmount as pendingcurrent from ptis.TransMast tm
        // join ptis.PolicyCodeMaster pcm on tm.PolicyCodeId = pcm.Id
        // join ptis.taxmaster tx on tx.id=tm.TaxId
        // where pcm.PolicyCode='OLD_ARREARS' and UPPER(TX.[TaxCode]) = 'TAXTOTAL' and tm.PropertyId=@propertyId
        var pendingCurrent = await (
            from tm in _context.TransMast.AsNoTracking()
            join pcm in _context.PolicyCodeMaster.AsNoTracking() on tm.PolicyCodeId equals pcm.Id
            join tx in _context.TaxMaster.AsNoTracking() on tm.TaxId equals tx.Id
            where tm.PropertyId == propertyId
                  && pcm.PolicyCode == "OLD_ARREARS"
                  && tx.TaxCode != null && tx.TaxCode.ToUpper() == "TAXTOTAL"
            select (decimal?)tm.TaxAmount
        ).SumAsync(cancellationToken);

        // 4. Get oldCurrentTax from TransMastOld for pre-merge properties
        // select TMO.TaxAmount as Old_Current_Tax from ptis.TransMastOld TMO inner join ptis.PropertyMapDetail PMD on PMD.PropertyIdOld = TMO.PropertyMastOldId inner join ptis.taxmaster TX on TX.id=TMO.TaxId where PMD.PropertyIdNew=@propertyId and UPPER(TX.taxcode)='TAXTOTAL'
        var oldCurrentTax = await (
            from tmo in _context.TransMastOld.AsNoTracking()
            join pmd in _context.PropertyMapDetails.AsNoTracking() on tmo.PropertyMastOldId equals pmd.PropertyIdOld
            join tx in _context.TaxMaster.AsNoTracking() on tmo.TaxId equals tx.Id
            where pmd.PropertyIdNew == propertyId
                  && tx.TaxCode != null && tx.TaxCode.ToUpper() == "TAXTOTAL"
                  && tmo.IsActive
                  && !tmo.MarkedForDeletion
                  && pmd.IsActive
            select (decimal?)tmo.TaxAmount
        ).SumAsync(cancellationToken);

        return (currentTax, retroTax, oldCurrentTax, pendingCurrent);
    }

    public async Task<List<SocietyWingSummaryDto>> GetSocietyWingsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // 1. Resolve societyId in a single step (direct property link with fallback to wing link)
        var societyId = await _context.SocietyDetailsMast
            .AsNoTracking()
            .Where(s => s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion)
            .OrderByDescending(s => s.CreatedDate)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await (from pm in _context.PropertyMast.AsNoTracking()
                      join w in _context.WingDetailsMast.AsNoTracking() on pm.WingDetailId equals w.Id
                      where pm.Id == propertyId && !pm.MarkedForDeletion && w.IsActive && !w.MarkedForDeletion
                      select (int?)w.SocietyDetailsMastId).FirstOrDefaultAsync(cancellationToken);

        if (societyId is null)
        {
            return new List<SocietyWingSummaryDto>();
        }

        return await _context.WingDetailsMast
            .AsNoTracking()
            .Where(w => w.SocietyDetailsMastId == societyId.Value && w.IsActive && !w.MarkedForDeletion)
            .OrderBy(w => w.WingName)
            .Select(w => new SocietyWingSummaryDto
            {
                WingDetailId = w.Id,
                WingMasterId = w.WingMasterId,
                WingName = w.WingName,
                SecretaryName = w.SecretaryName,
                SecretaryNameEnglish = w.SecretaryNameEnglish,
                SecretaryMobileNo = w.SecretaryMobileNo,
                SecretaryEmailId = w.SecretaryEmailId,
                ManagerName = w.ManagerName,
                ManagerNameEnglish = w.ManagerNameEnglish,
                ManagerMobileNo = w.ManagerMobileNo,
                ManagerEmailId = w.ManagerEmailId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TopSectionUpdateOutcome> UpdateTopSectionAsync(
        int propertyId,
        UpdateApartmentQcTopSectionDto dto,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var property = await _context.PropertyMast
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (property is null)
        {
            return TopSectionUpdateOutcome.PropertyNotFound;
        }

        var isLocked = await _context.PropertyScreenLocks
            .AnyAsync(l => l.PropertyId == propertyId && l.IsLocked && l.IsActive && !l.MarkedForDeletion, cancellationToken);

        if (isLocked)
        {
            return TopSectionUpdateOutcome.PropertyLocked;
        }

        var now = DateTime.Now;

        // 1. Update PropertyMast fields with trimming
        if (dto.OwnerName != null) property.OwnerName = dto.OwnerName.Trim();
        if (dto.OwnerNameEnglish != null) property.OwnerNameEnglish = dto.OwnerNameEnglish.Trim();
        if (dto.OccupierName != null) property.OccupierName = dto.OccupierName.Trim();
        if (dto.OccupierNameEnglish != null) property.OccupierNameEnglish = dto.OccupierNameEnglish.Trim();
        if (dto.MobileNo != null) property.MobileNo = dto.MobileNo.Trim();
        if (dto.AlternateMobileNo != null) property.AlternateMobileNo = dto.AlternateMobileNo.Trim();
        if (dto.EmailId != null) property.EmailId = dto.EmailId.Trim();
        if (dto.Address != null) property.Address = dto.Address.Trim();
        if (dto.PinCode != null) property.PinCode = dto.PinCode.Trim();
        if (dto.PlotNo != null) property.PlotNo = dto.PlotNo.Trim();
        if (dto.SurveyNo != null) property.CSN = dto.SurveyNo.Trim();
        if (dto.MoujaId.HasValue) property.MoujaId = dto.MoujaId;
        if (dto.TaxZoneId.HasValue) property.TaxZoneId = dto.TaxZoneId.Value;
        if (dto.CategoryId.HasValue) property.CategoryId = dto.CategoryId;
        if (dto.PropertyTypeId.HasValue) property.PropertyTypeId = dto.PropertyTypeId;

        property.UpdatedBy = updatedBy;
        property.UpdatedDate = now;

        // 2. Update PropertyMastDetails (AadharCardNo, OwnerTypeId)
        if (dto.AadharNo != null || dto.OwnerTypeId.HasValue)
        {
            var assessment = await _context.PropertyMastDetails
                .Where(a => a.PropertyId == propertyId && a.IsActive && !a.MarkedForDeletion)
                .OrderByDescending(a => a.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (assessment != null)
            {
                if (dto.AadharNo != null) assessment.AdharCardNo = dto.AadharNo.Trim();
                if (dto.OwnerTypeId.HasValue) assessment.OwnerTypeId = dto.OwnerTypeId;
                assessment.UpdatedBy = updatedBy;
                assessment.UpdatedDate = now;
            }
            else
            {
                var newAssessment = new NtisPlatform.Core.Entities.PropertyAssessmentEntity
                {
                    PropertyId = propertyId,
                    AdharCardNo = dto.AadharNo?.Trim(),
                    OwnerTypeId = dto.OwnerTypeId,
                    IsActive = true,
                    CreatedBy = updatedBy,
                    CreatedDate = now
                };
                _context.PropertyMastDetails.Add(newAssessment);
            }
        }

        // 3. Update SocietyDetailsMast & cascade to WingDetailsMast
        var hasSecretaryUpdates = dto.SecretaryName != null ||
                                  dto.SecretaryNameEnglish != null ||
                                  dto.SecretaryMobileNo != null ||
                                  dto.SecretaryEmailId != null;

        var hasManagerUpdates = dto.ManagerName != null ||
                                dto.ManagerNameEnglish != null ||
                                dto.ManagerMobileNo != null ||
                                dto.ManagerEmailId != null;

        var hasAdministrativeUpdates = hasSecretaryUpdates || hasManagerUpdates;
        var hasSocietyUpdates = hasAdministrativeUpdates
                                || dto.SocietyName != null
                                || dto.SocietyNameEnglish != null
                                || dto.SocietyAddress != null
                                || dto.SocietyAddressEnglish != null
                                || dto.SocietyEmailId != null
                                || dto.LandOwnerName != null
                                || dto.LandOwnerNameEnglish != null
                                || dto.BuilderName != null
                                || dto.BuilderNameEnglish != null
                                || dto.BuilderMobileNo != null;

        if (hasSocietyUpdates)
        {
            var society = await _context.SocietyDetailsMast
                .Where(s => s.PropertyId == propertyId && s.IsActive && !s.MarkedForDeletion)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (society != null)
            {
                if (dto.SocietyName != null) society.SocietyName = dto.SocietyName.Trim();
                if (dto.SocietyNameEnglish != null) society.SocietyNameEnglish = dto.SocietyNameEnglish.Trim();
                if (dto.SocietyAddress != null) society.SocietyAddress = dto.SocietyAddress.Trim();
                if (dto.SocietyAddressEnglish != null) society.SocietyAddressEnglish = dto.SocietyAddressEnglish.Trim();
                if (dto.SocietyEmailId != null) society.SocietyEmailId = dto.SocietyEmailId.Trim();
                if (dto.LandOwnerName != null) society.LandOwnerName = dto.LandOwnerName.Trim();
                if (dto.LandOwnerNameEnglish != null) society.LandOwnerNameEnglish = dto.LandOwnerNameEnglish.Trim();
                if (dto.BuilderName != null) society.BuilderName = dto.BuilderName.Trim();
                if (dto.BuilderNameEnglish != null) society.BuilderNameEnglish = dto.BuilderNameEnglish.Trim();
                if (dto.BuilderMobileNo != null) society.BuilderMobileNo = dto.BuilderMobileNo.Trim();
                if (dto.SecretaryName != null) society.SecretaryName = dto.SecretaryName.Trim();
                if (dto.SecretaryNameEnglish != null) society.SecretaryNameEnglish = dto.SecretaryNameEnglish.Trim();
                if (dto.SecretaryMobileNo != null) society.SecretaryMobileNo = dto.SecretaryMobileNo.Trim();
                if (dto.SecretaryEmailId != null) society.SecretaryEmailId = dto.SecretaryEmailId.Trim();
                if (dto.ManagerName != null) society.ManagerName = dto.ManagerName.Trim();
                if (dto.ManagerNameEnglish != null) society.ManagerNameEnglish = dto.ManagerNameEnglish.Trim();
                if (dto.ManagerMobileNo != null) society.ManagerMobileNo = dto.ManagerMobileNo.Trim();
                if (dto.ManagerEmailId != null) society.ManagerEmailId = dto.ManagerEmailId.Trim();
                society.UpdatedBy = updatedBy;
                society.UpdatedDate = now;

                // 4. Cascade to selected child wings (independently for Secretary and Manager)
                var secretaryWingIds = dto.SecretaryTargetWingDetailIds;
                var managerWingIds = dto.ManagerTargetWingDetailIds;

                var allTargetWingIds = (secretaryWingIds ?? Enumerable.Empty<int>())
                    .Union(managerWingIds ?? Enumerable.Empty<int>())
                    .Distinct()
                    .ToList();

                if (allTargetWingIds.Count > 0)
                {
                    var targetWings = await _context.WingDetailsMast
                        .Where(w => w.SocietyDetailsMastId == society.Id
                                    && allTargetWingIds.Contains(w.Id)
                                    && w.IsActive
                                    && !w.MarkedForDeletion)
                        .ToListAsync(cancellationToken);

                    foreach (var wing in targetWings)
                    {
                        var appliesToSecretary = hasSecretaryUpdates && (secretaryWingIds?.Contains(wing.Id) == true);
                        var appliesToManager = hasManagerUpdates && (managerWingIds?.Contains(wing.Id) == true);

                        if (appliesToSecretary)
                        {
                            if (dto.SecretaryName != null) wing.SecretaryName = dto.SecretaryName.Trim();
                            if (dto.SecretaryNameEnglish != null) wing.SecretaryNameEnglish = dto.SecretaryNameEnglish.Trim();
                            if (dto.SecretaryMobileNo != null) wing.SecretaryMobileNo = dto.SecretaryMobileNo.Trim();
                            if (dto.SecretaryEmailId != null) wing.SecretaryEmailId = dto.SecretaryEmailId.Trim();
                        }

                        if (appliesToManager)
                        {
                            if (dto.ManagerName != null) wing.ManagerName = dto.ManagerName.Trim();
                            if (dto.ManagerNameEnglish != null) wing.ManagerNameEnglish = dto.ManagerNameEnglish.Trim();
                            if (dto.ManagerMobileNo != null) wing.ManagerMobileNo = dto.ManagerMobileNo.Trim();
                            if (dto.ManagerEmailId != null) wing.ManagerEmailId = dto.ManagerEmailId.Trim();
                        }

                        if (appliesToSecretary || appliesToManager)
                        {
                            wing.UpdatedBy = updatedBy;
                            wing.UpdatedDate = now;
                        }
                    }
                }
            }
            else
            {
                var newSociety = new NtisPlatform.Core.Entities.SocietyDetailsEntity
                {
                    PropertyId = propertyId,
                    SocietyName = dto.SocietyName?.Trim(),
                    SocietyNameEnglish = dto.SocietyNameEnglish?.Trim(),
                    SocietyAddress = dto.SocietyAddress?.Trim(),
                    SocietyAddressEnglish = dto.SocietyAddressEnglish?.Trim(),
                    SocietyEmailId = dto.SocietyEmailId?.Trim(),
                    LandOwnerName = dto.LandOwnerName?.Trim(),
                    LandOwnerNameEnglish = dto.LandOwnerNameEnglish?.Trim(),
                    BuilderName = dto.BuilderName?.Trim(),
                    BuilderNameEnglish = dto.BuilderNameEnglish?.Trim(),
                    BuilderMobileNo = dto.BuilderMobileNo?.Trim(),
                    SecretaryName = dto.SecretaryName?.Trim(),
                    SecretaryNameEnglish = dto.SecretaryNameEnglish?.Trim(),
                    SecretaryMobileNo = dto.SecretaryMobileNo?.Trim(),
                    SecretaryEmailId = dto.SecretaryEmailId?.Trim(),
                    ManagerName = dto.ManagerName?.Trim(),
                    ManagerNameEnglish = dto.ManagerNameEnglish?.Trim(),
                    ManagerMobileNo = dto.ManagerMobileNo?.Trim(),
                    ManagerEmailId = dto.ManagerEmailId?.Trim(),
                    IsActive = true,
                    CreatedBy = updatedBy,
                    CreatedDate = now
                };
                _context.SocietyDetailsMast.Add(newSociety);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return TopSectionUpdateOutcome.Success;
    }

    public async Task<TopSectionUpdateOutcome> UpdateWingDetailsAsync(
        int wingDetailId,
        UpdateApartmentQcWingDetailsDto dto,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        var wing = await _context.WingDetailsMast
            .FirstOrDefaultAsync(w => w.Id == wingDetailId && w.IsActive && !w.MarkedForDeletion, cancellationToken);

        if (wing is null)
        {
            return TopSectionUpdateOutcome.WingNotFound;
        }

        var isLocked = await (
            from s in _context.SocietyDetailsMast.AsNoTracking()
            join l in _context.PropertyScreenLocks.AsNoTracking() on s.PropertyId equals l.PropertyId
            where s.Id == wing.SocietyDetailsMastId && s.IsActive && !s.MarkedForDeletion
                  && l.IsLocked && l.IsActive && !l.MarkedForDeletion
            select l.Id
        ).AnyAsync(cancellationToken);

        if (isLocked)
        {
            return TopSectionUpdateOutcome.PropertyLocked;
        }

        var now = DateTime.Now;

        if (dto.WingName != null) wing.WingName = dto.WingName.Trim();
        if (dto.SecretaryName != null) wing.SecretaryName = dto.SecretaryName.Trim();
        if (dto.SecretaryNameEnglish != null) wing.SecretaryNameEnglish = dto.SecretaryNameEnglish.Trim();
        if (dto.SecretaryMobileNo != null) wing.SecretaryMobileNo = dto.SecretaryMobileNo.Trim();
        if (dto.SecretaryEmailId != null) wing.SecretaryEmailId = dto.SecretaryEmailId.Trim();
        if (dto.ManagerName != null) wing.ManagerName = dto.ManagerName.Trim();
        if (dto.ManagerNameEnglish != null) wing.ManagerNameEnglish = dto.ManagerNameEnglish.Trim();
        if (dto.ManagerMobileNo != null) wing.ManagerMobileNo = dto.ManagerMobileNo.Trim();
        if (dto.ManagerEmailId != null) wing.ManagerEmailId = dto.ManagerEmailId.Trim();

        wing.UpdatedBy = updatedBy;
        wing.UpdatedDate = now;

        await _context.SaveChangesAsync(cancellationToken);
        return TopSectionUpdateOutcome.Success;
    }
}


