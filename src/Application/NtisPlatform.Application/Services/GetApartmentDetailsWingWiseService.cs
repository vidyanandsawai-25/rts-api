using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for retrieving apartment details filtered wing-wise and property-wise.
/// Uses generic <see cref="IRepository{T, TKey}"/> interfaces for table access (following DataEntryService architecture),
/// comparing New Survey and Old Survey property details via PropertyMapDetail table mapping.
/// </summary>
public class GetApartmentDetailsWingWiseService : IGetApartmentDetailsWingWiseService
{
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _categoryRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _assessmentRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<WingDetailsMastEntity, int> _wingDetailsMastRepository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<ZoneEntity, int> _zoneRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<FloorEntity, int> _floorRepository;
    private readonly IRepository<ConstructionTypeEntity, int> _constructionTypeRepository;
    private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepository;
    private readonly IRepository<SubTypeOfUseEntity, int> _subTypeOfUseRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _oldPropertyRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<TransMastEntity, int> _transMastRepository;
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;
    private readonly IRepository<PolicyCodeMasterEntity, int> _policyCodeMasterRepository;
    private readonly IRepository<PolicyTaxDetailsEntity, int> _policyTaxDetailsRepository;
    private readonly IRepository<PolicyTaxDetailsCVEntity, int> _policyTaxDetailsCVRepository;
    private readonly IRepository<PolicyConfigurationEntity, int> _policyConfigurationRepository;
    private readonly IRepository<PropertyPhotoEntity, int> _photoRepository;
    private readonly IRepository<PropertyPhotoOldEntity, int> _photoOldRepository;
    private readonly IRepository<PropertyPhotoTypeEntity, int> _photoTypeRepository;
    private readonly IRepository<DocumentBindingEntity, int> _documentBindingRepository;
    private readonly IRepository<DocumentEntity, int> _documentRepository;
    private readonly IRepository<TransMastOldEntity, int>? _transMastOldRepository;
    private readonly IMapper _mapper;

    public GetApartmentDetailsWingWiseService(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyCategoryEntity, int> categoryRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IRepository<PropertyAssessmentEntity, int> assessmentRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<ZoneEntity, int> zoneRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<FloorEntity, int> floorRepository,
        IRepository<ConstructionTypeEntity, int> constructionTypeRepository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
        IRepository<SubTypeOfUseEntity, int> subTypeOfUseRepository,
        IRepository<PropertyMastOldEntity, int> oldPropertyRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository,
        IRepository<PolicyCodeMasterEntity, int> policyCodeMasterRepository,
        IRepository<PolicyTaxDetailsEntity, int> policyTaxDetailsRepository,
        IRepository<PolicyTaxDetailsCVEntity, int> policyTaxDetailsCVRepository,
        IRepository<PolicyConfigurationEntity, int> policyConfigurationRepository,
        IRepository<PropertyPhotoEntity, int> photoRepository,
        IRepository<PropertyPhotoOldEntity, int> photoOldRepository,
        IRepository<PropertyPhotoTypeEntity, int> photoTypeRepository,
        IRepository<DocumentBindingEntity, int> documentBindingRepository,
        IRepository<DocumentEntity, int> documentRepository,
        IMapper mapper,
        IRepository<TransMastOldEntity, int>? transMastOldRepository = null)
    {
        _propertyRepository = propertyRepository;
        _categoryRepository = categoryRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _assessmentRepository = assessmentRepository;
        _societyRepository = societyRepository;
        _wingDetailsMastRepository = wingDetailsMastRepository;
        _wardRepository = wardRepository;
        _zoneRepository = zoneRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _floorRepository = floorRepository;
        _constructionTypeRepository = constructionTypeRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _subTypeOfUseRepository = subTypeOfUseRepository;
        _oldPropertyRepository = oldPropertyRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _transMastRepository = transMastRepository;
        _taxMasterRepository = taxMasterRepository;
        _yearMasterRepository = yearMasterRepository;
        _policyCodeMasterRepository = policyCodeMasterRepository;
        _policyTaxDetailsRepository = policyTaxDetailsRepository;
        _policyTaxDetailsCVRepository = policyTaxDetailsCVRepository;
        _policyConfigurationRepository = policyConfigurationRepository;
        _photoRepository = photoRepository;
        _photoOldRepository = photoOldRepository;
        _photoTypeRepository = photoTypeRepository;
        _documentBindingRepository = documentBindingRepository;
        _documentRepository = documentRepository;
        _transMastOldRepository = transMastOldRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ApartmentQCComparisonDto>> GetApartmentDetailsWingWiseAsync(
        GetApartmentDetailsWingWiseQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Category Query (Upfront ID resolution to eliminate correlated subquery) ──────
        var apartmentCategoryIds = await _categoryRepository.GetQueryable().AsNoTracking()
            .Where(pcm => PropertyCategoryConstants.ApartmentCategoryNames.Contains(pcm.PropertyCategoryName) && pcm.IsActive)
            .Select(pcm => pcm.Id)
            .ToListAsync(cancellationToken);

        if (apartmentCategoryIds.Count == 0)
        {
            return new PagedResult<ApartmentQCComparisonDto>(
                Array.Empty<ApartmentQCComparisonDto>(), 0, query.PageNumber, query.PageSize);
        }

        var baseQuery = _propertyRepository.GetQueryable()
            .AsNoTracking()
            .Where(pm => pm.IsActive && !pm.MarkedForDeletion && pm.CategoryId.HasValue && apartmentCategoryIds.Contains(pm.CategoryId.Value));

        var searchedProperty = query.PropertyId.HasValue ? await _propertyRepository.GetByIdAsync(query.PropertyId.Value, cancellationToken) : null;

        if (query.WingDetailId.HasValue)
        {
            baseQuery = baseQuery.Where(pm => pm.WingDetailId == query.WingDetailId.Value);
        }
        else if (query.PropertyId.HasValue && searchedProperty != null)
        {
            var targetPropertyNo = searchedProperty.PropertyNo;
            var targetWardId = searchedProperty.WardId;
            var societyQuery = _societyRepository.GetQueryable().AsNoTracking();
            var wingDetailsMastQuery = _wingDetailsMastRepository.GetQueryable().AsNoTracking();
            var propertyQuery = _propertyRepository.GetQueryable().AsNoTracking();

            baseQuery = baseQuery.Where(pm =>
                pm.Id == query.PropertyId.Value
                || (pm.WingDetailId.HasValue && wingDetailsMastQuery.Any(wdm =>
                    wdm.Id == pm.WingDetailId.Value && wdm.IsActive && !wdm.MarkedForDeletion &&
                    societyQuery.Any(s => s.Id == wdm.SocietyDetailsMastId && s.IsActive && !s.MarkedForDeletion &&
                        propertyQuery.Any(sp => sp.Id == s.PropertyId && sp.PropertyNo == targetPropertyNo && sp.WardId == targetWardId && sp.IsActive && !sp.MarkedForDeletion))))
            );
        }

        if (query.WingId.HasValue)
        {
            var societyQuery = _societyRepository.GetQueryable().AsNoTracking();
            var wingDetailsMastQuery = _wingDetailsMastRepository.GetQueryable().AsNoTracking();

            baseQuery = baseQuery.Where(pm =>
                (pm.WingDetailId.HasValue && wingDetailsMastQuery.Any(wdm => wdm.Id == pm.WingDetailId.Value && wdm.IsActive && !wdm.MarkedForDeletion && wdm.WingMasterId == query.WingId.Value))
                ||
                societyQuery.Any(s => s.PropertyId == pm.Id && s.IsActive && !s.MarkedForDeletion &&
                    wingDetailsMastQuery.Any(wdm => wdm.SocietyDetailsMastId == s.Id && wdm.IsActive && !wdm.MarkedForDeletion && wdm.WingMasterId == query.WingId.Value))
            );
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            baseQuery = baseQuery.Where(pm =>
                (pm.PropertyNo != null && pm.PropertyNo.Contains(search)) ||
                (pm.OwnerName != null && pm.OwnerName.Contains(search)) ||
                (pm.OwnerNameEnglish != null && pm.OwnerNameEnglish.Contains(search)) ||
                (pm.FlatOrShopNo != null && pm.FlatOrShopNo.Contains(search)) ||
                (pm.FlatOrShopNoEnglish != null && pm.FlatOrShopNoEnglish.Contains(search)));
        }

        // ── 2. Projection Join (Left join to prevent row dropping when PropertyTypeId is null) ──
        var ptmQuery = _propertyTypeRepository.GetQueryable().AsNoTracking();

        var joined =
            from pm in baseQuery
            join ptm in ptmQuery on pm.PropertyTypeId equals ptm.Id into ptmJ
            from ptm in ptmJ.DefaultIfEmpty()
            select new JoinedPropertyRowDto
            {
                Id = pm.Id,
                TaxZoneId = pm.TaxZoneId,
                WardId = pm.WardId,
                PropertyNo = pm.PropertyNo ?? string.Empty,
                PartitionNo = pm.PartitionNo,
                MobileNo = pm.MobileNo,
                EmailId = pm.EmailId,
                FlatOrShopNo = pm.FlatOrShopNo,
                FlatOrShopName = pm.FlatOrShopName,
                FlatOrShopNoEnglish = pm.FlatOrShopNoEnglish,
                FlatOrShopNameEnglish = pm.FlatOrShopNameEnglish,
                OwnerName = pm.OwnerName,
                OwnerNameEnglish = pm.OwnerNameEnglish,
                OccupierName = pm.OccupierName,
                OccupierNameEnglish = pm.OccupierNameEnglish,
                PartType = ptm != null ? ptm.PartType : null,
                PropertyType = ptm != null ? ptm.Id : 0,
                PropertyTypeName = ptm != null ? ptm.PropertyDescription : null,
                WingDetailId = pm.WingDetailId,
                ApartmentType = pm.Type
            };

        // ── 3. Pagination (Fast Base Query Count) ───────────────────────────
        var totalCount = await baseQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResult<ApartmentQCComparisonDto>(
                Array.Empty<ApartmentQCComparisonDto>(), 0, query.PageNumber, query.PageSize);
        }

        var (pageNumber, pageSize, skip) =
            PagingGuard.Normalize(query.PageNumber, query.PageSize, totalCount);

        var rawRows = await joined
            .OrderBy(x => x.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var propertyIds = rawRows.Select(r => r.Id).ToList();
        var wardIds = rawRows.Select(r => r.WardId).Distinct().ToList();

        // ── 4. Post-Pagination BHK and Wing Resolution ─────────────────────
        var bhkLookup = await _assessmentRepository.GetQueryable().AsNoTracking()
            .Where(d => propertyIds.Contains(d.PropertyId) && d.IsActive && !d.MarkedForDeletion)
            .GroupBy(d => d.PropertyId)
            .Select(g => new { PropertyId = g.Key, BHK = g.OrderByDescending(d => d.CreatedDate).Select(d => d.BHK).FirstOrDefault() })
            .ToDictionaryAsync(x => x.PropertyId, x => x.BHK, cancellationToken);

        var wingDetailIds = rawRows.Where(r => r.WingDetailId.HasValue).Select(r => r.WingDetailId!.Value).Distinct().ToList();
        var directWingNames = wingDetailIds.Count > 0
            ? await _wingDetailsMastRepository.GetQueryable().AsNoTracking()
                .Where(wdm => wingDetailIds.Contains(wdm.Id) && wdm.IsActive && !wdm.MarkedForDeletion)
                .Select(wdm => new { wdm.Id, wdm.WingName })
                .ToDictionaryAsync(wdm => wdm.Id, wdm => wdm.WingName, cancellationToken)
            : new Dictionary<int, string?>();

        var societyWingNames = await (
            from s in _societyRepository.GetQueryable().AsNoTracking()
            where s.PropertyId.HasValue && propertyIds.Contains(s.PropertyId.Value) && s.IsActive && !s.MarkedForDeletion
            join wdm in _wingDetailsMastRepository.GetQueryable().AsNoTracking() on s.Id equals wdm.SocietyDetailsMastId
            where wdm.IsActive && !wdm.MarkedForDeletion
            select new { PropertyId = s.PropertyId!.Value, wdm.WingName, CreatedDate = wdm.CreatedDate }
        ).ToListAsync(cancellationToken);

        var societyWingLookup = societyWingNames
            .GroupBy(x => x.PropertyId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).Select(x => x.WingName).FirstOrDefault());

        foreach (var p in rawRows)
        {
            bhkLookup.TryGetValue(p.Id, out var bhk);
            p.BHK = bhk;

            string? wingName = null;
            if (p.WingDetailId.HasValue)
            {
                directWingNames.TryGetValue(p.WingDetailId.Value, out wingName);
            }
            if (string.IsNullOrEmpty(wingName))
            {
                societyWingLookup.TryGetValue(p.Id, out wingName);
            }
            p.Wing = wingName;
        }

        var mappingList = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
            .Where(pmd => pmd.PropertyIdNew.HasValue && propertyIds.Contains(pmd.PropertyIdNew.Value) && pmd.IsActive)
            .Select(pmd => new
            {
                pmd.Id,
                pmd.PropertyIdNew,
                pmd.PropertyIdOld,
                pmd.PropertyNoOld,
                pmd.IsCurrent
            })
            .ToListAsync(cancellationToken);

        var mappingsByNewProperty = mappingList
            .GroupBy(m => m.PropertyIdNew!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.IsCurrent).ThenByDescending(m => m.Id).ToList());

        // ── 5. Fetch supporting details using IRepository GetQueryable ─────
        var wardZones = await (
            from w in _wardRepository.GetQueryable().AsNoTracking()
            where wardIds.Contains(w.Id)
            join z in _zoneRepository.GetQueryable().AsNoTracking() on w.ZoneId equals z.Id into zJ
            from z in zJ.DefaultIfEmpty()
            select new { w.Id, w.WardNo, ZoneNo = z != null ? z.ZoneNo : null }
        ).ToDictionaryAsync(x => x.Id, x => new { x.WardNo, x.ZoneNo }, cancellationToken);

        var detailsList = await (
            from pd in _propertyDetailsRepository.GetQueryable().AsNoTracking()
            where propertyIds.Contains(pd.PropertyId)
               && pd.IsActive && !pd.MarkedForDeletion
            join fl in _floorRepository.GetQueryable().AsNoTracking() on pd.FloorId equals fl.Id into flJ
            from fl in flJ.DefaultIfEmpty()
            join sfl in _floorRepository.GetQueryable().AsNoTracking() on pd.SubFloorId equals sfl.Id into sflJ
            from sfl in sflJ.DefaultIfEmpty()
            join ct in _constructionTypeRepository.GetQueryable().AsNoTracking() on pd.ConstructionTypeId equals ct.Id into ctJ
            from ct in ctJ.DefaultIfEmpty()
            join tou in _typeOfUseRepository.GetQueryable().AsNoTracking() on pd.TypeOfUseId equals tou.Id into touJ
            from tou in touJ.DefaultIfEmpty()
            join stou in _subTypeOfUseRepository.GetQueryable().AsNoTracking() on pd.SubTypeOfUseId equals stou.Id into stouJ
            from stou in stouJ.DefaultIfEmpty()
            select new FetchDetailRowDto
            {
                Id = pd.Id,
                PropertyId = pd.PropertyId,
                NoOfRooms = pd.NoOfRooms,
                CarpetAreaSqMeter = pd.CarpetAreaSqMeter,
                CarpetAreaSqFeet = pd.CarpetAreaSqFeet,
                BuiltupAreaSqMeter = pd.BuiltupAreaSqMeter,
                BuiltupAreaSqFeet = pd.BuiltupAreaSqFeet,
                Floor = fl != null ? fl.Description : null,
                SubFloor = sfl != null ? sfl.Description : null,
                ConstructionType = ct != null ? ct.Description : null,
                TypeOfUse = tou != null ? tou.Description : null,
                Type = tou != null ? tou.Type : null,
                SubTypeOfUse = stou != null ? stou.Description : null,
                ConstructionYear = pd.ConstructionYear,
                AssessmentYear = pd.AssessmentYear
            }).ToListAsync(cancellationToken);

        var detailsLookup = detailsList
            .GroupBy(d => d.PropertyId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Id).FirstOrDefault());

        // Fetch Old Property Data using direct Index Seeks (avoiding OR across Id / OldPropertyNo)
        var oldPropertyIdsFromMap = mappingList
            .Where(m => m.PropertyIdOld.HasValue)
            .Select(m => m.PropertyIdOld!.Value)
            .Distinct()
            .ToList();

        var mappedOldNos = mappingList.Select(m => m.PropertyNoOld).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        var propertyNos = rawRows.Select(r => r.PropertyNo).Where(n => !string.IsNullOrEmpty(n)).ToList();
        var allOldNos = mappedOldNos.Union(propertyNos).Distinct().ToList();

        var oldPropertiesById = oldPropertyIdsFromMap.Count > 0
            ? await _oldPropertyRepository.GetQueryable().AsNoTracking()
                .Where(o => oldPropertyIdsFromMap.Contains(o.Id) && o.IsActive && !o.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<PropertyMastOldEntity>();

        var foundOldNos = oldPropertiesById
            .Where(o => !string.IsNullOrEmpty(o.OldPropertyNo))
            .Select(o => o.OldPropertyNo!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var remainingOldNos = allOldNos.Where(n => !foundOldNos.Contains(n)).ToList();
        var oldPropertiesByNo = remainingOldNos.Count > 0
            ? await _oldPropertyRepository.GetQueryable().AsNoTracking()
                .Where(o => remainingOldNos.Contains(o.OldPropertyNo!) && o.IsActive && !o.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<PropertyMastOldEntity>();

        var oldPropertyEntities = oldPropertiesById.Concat(oldPropertiesByNo).ToList();

        var oldDataById = oldPropertyEntities.ToDictionary(o => o.Id);
        var oldDataByNo = oldPropertyEntities
            .Where(o => !string.IsNullOrEmpty(o.OldPropertyNo))
            .GroupBy(o => o.OldPropertyNo!)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.CreatedDate).First());

        // Check TaxCalculationMethod from PolicyConfiguration (select PolicyValue from PTIS.PolicyConfiguration where PolicyCode='TaxCalculationMethod')
        var taxCalculationMethod = await _policyConfigurationRepository.GetQueryable().AsNoTracking()
            .Where(p => p.PolicyCode == "TaxCalculationMethod" && p.IsActive)
            .Select(p => p.PolicyValue)
            .FirstOrDefaultAsync(cancellationToken);

        var isRvOnly = string.Equals(taxCalculationMethod?.Trim(), "RV", StringComparison.OrdinalIgnoreCase);
        var retroCalculationType = isRvOnly ? "RV" : "CV";

        // Fetch Retro Demand (TaxAmount where TaxCode='TAXTOTAL') per PropertyId from TransMast joining TaxMaster and PolicyCodeMaster
        var retroTaxLookup = await (
            from tm in _transMastRepository.GetQueryable().AsNoTracking()
            join tx in _taxMasterRepository.GetQueryable().AsNoTracking() on tm.TaxId equals tx.Id
            join pcm in _policyCodeMasterRepository.GetQueryable().AsNoTracking() on tm.PolicyCodeId equals pcm.Id
            where propertyIds.Contains(tm.PropertyId)
               && tx.TaxCode == "TAXTOTAL" && tx.IsActive
               && pcm.IsRetroDemand && pcm.IsActive
               && tm.IsActive && !tm.MarkedForDeletion
               && tm.CalculationType == retroCalculationType
            group tm by tm.PropertyId into g
            select new
            {
                PropertyId = g.Key,
                TotalRetroTax = g.Sum(x => (decimal?)x.TaxAmount) ?? 0m
            }
        ).ToDictionaryAsync(x => x.PropertyId, x => x.TotalRetroTax, cancellationToken);

        // Fetch RV details (CalculationValue / Rateable Value) from PolicyTaxDetails where IsCurrent is true
        var rvTaxLookup = new Dictionary<int, decimal?>();
        var rvTaxRows = await _policyTaxDetailsRepository.GetQueryable().AsNoTracking()
            .Where(x => propertyIds.Contains(x.PropertyId) && x.IsCurrent && x.IsActive && !x.MarkedForDeletion)
            .Select(x => new { x.PropertyId, RateableValue = x.CalculationValue })
            .ToListAsync(cancellationToken);

        foreach (var r in rvTaxRows)
        {
            rvTaxLookup.TryAdd(r.PropertyId, r.RateableValue);
        }

        // Resolve Current Finance Year Id from YearMaster
        var today = DateTime.Today;
        var currentFinanceYearId = await _yearMasterRepository.GetQueryable().AsNoTracking()
            .Where(y => y.IsActive)
            .OrderByDescending(y => y.Year)
            .Select(y => (int?)y.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await _yearMasterRepository.GetQueryable().AsNoTracking()
                .Where(y => y.StartDate <= today && y.EndDate >= today)
                .Select(y => (int?)y.Id)
                .FirstOrDefaultAsync(cancellationToken);

        // Fetch Current Demand (TaxAmount where TaxCode='TAXTOTAL' and FinanceYearId=currentFinanceYearId) per PropertyId from TransMast
        // SELECT TM.[TaxAmount] AS [CurrentDemand] FROM [PTIS].[TransMast] TM
        // INNER JOIN [PTIS].[TaxMaster] TX ON TX.[Id] = TM.[TaxId]
        // WHERE TM.[PropertyId] IN (@propertyIds) AND TM.[FinanceYearId] = @CurrentFinanceYearId AND TX.[TaxCode] = 'TAXTOTAL' AND TM.[IsActive] = 1 AND TM.[MarkedForDeletion] = 0;
        var currentDemandLookup = currentFinanceYearId.HasValue
            ? await (
                from tm in _transMastRepository.GetQueryable().AsNoTracking()
                join tx in _taxMasterRepository.GetQueryable().AsNoTracking() on tm.TaxId equals tx.Id
                where propertyIds.Contains(tm.PropertyId)
                   && tm.FinanceYearId == currentFinanceYearId.Value
                   && tx.TaxCode == "TAXTOTAL" && tx.IsActive
                   && tm.IsActive && !tm.MarkedForDeletion
                   && tm.CalculationType == retroCalculationType
                group tm by tm.PropertyId into g
                select new
                {
                    PropertyId = g.Key,
                    CurrentDemand = g.Sum(x => (decimal?)x.TaxAmount) ?? 0m
                }
            ).ToDictionaryAsync(x => x.PropertyId, x => x.CurrentDemand, cancellationToken)
            : new Dictionary<int, decimal>();

        // If TaxCalculationMethod is 'RV', CV is not checked; otherwise CV details (CalculationValue / Capital Value) are retrieved where IsCurrent is true
        var cvTaxLookup = new Dictionary<int, decimal?>();
        if (!isRvOnly)
        {
            var cvTaxRows = await _policyTaxDetailsCVRepository.GetQueryable().AsNoTracking()
                .Where(x => propertyIds.Contains(x.PropertyId) && x.IsCurrent && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new { x.PropertyId, CapitalValue = x.CalculationValue })
                .ToListAsync(cancellationToken);

            foreach (var r in cvTaxRows)
            {
                cvTaxLookup.TryAdd(r.PropertyId, r.CapitalValue);
            }
        }

        // Fetch Photo Documents (DocumentGuid & PhotoTypeCode for PLAN_PHOTO and PROPERTY_PHOTO)
        var photoDocs = await (
            from pp in _photoRepository.GetQueryable().AsNoTracking()
            where pp.PropertyId.HasValue && propertyIds.Contains(pp.PropertyId.Value) && pp.IsActive && !pp.MarkedForDeletion
            join ppt in _photoTypeRepository.GetQueryable().AsNoTracking() on pp.PhotoTypeId equals ppt.Id
            where ppt.IsActive && (ppt.PhotoTypeCode == "PLAN_PHOTO" || ppt.PhotoTypeCode == "PROPERTY_PHOTO")
            join db in _documentBindingRepository.GetQueryable().AsNoTracking() on pp.DocumentBindingId equals db.Id
            where db.IsActive
            join d in _documentRepository.GetQueryable().AsNoTracking() on db.DocumentId equals d.Id
            where d.IsActive && !d.MarkedForDeletion
            select new
            {
                pp.PropertyId,
                d.DocumentGuid,
                ppt.PhotoTypeCode,
                CreatedDate = pp.CreatedDate
            }
        ).ToListAsync(cancellationToken);

        var photoLookup = photoDocs
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.PhotoTypeCode, StringComparer.OrdinalIgnoreCase)
                      .Select(group => group.OrderByDescending(x => x.CreatedDate).First())
                      .Select(x => new PropertyPhotoDocumentDto
                      {
                          DocumentGuid = x.DocumentGuid,
                          PhotoTypeCode = x.PhotoTypeCode
                      }).ToList()
            );

        // Fetch Old Photo Documents (DocumentGuid & PhotoTypeCode from PropertyPhotoOld)
        var oldPropertyIds = oldPropertyEntities.Select(o => o.Id).Distinct().ToList();

        var oldPhotoDocs = oldPropertyIds.Count > 0
            ? await (
                from pp in _photoOldRepository.GetQueryable().AsNoTracking()
                where oldPropertyIds.Contains(pp.PropertyMastOldId) && pp.IsActive && !pp.MarkedForDeletion
                join ppt in _photoTypeRepository.GetQueryable().AsNoTracking() on pp.PhotoTypeId equals ppt.Id
                where ppt.IsActive && (ppt.PhotoTypeCode == "PLAN_PHOTO" || ppt.PhotoTypeCode == "PROPERTY_PHOTO")
                join db in _documentBindingRepository.GetQueryable().AsNoTracking() on pp.DocumentBindingId equals db.Id
                where db.IsActive
                join d in _documentRepository.GetQueryable().AsNoTracking() on db.DocumentId equals d.Id
                where d.IsActive && !d.MarkedForDeletion
                select new
                {
                    pp.PropertyMastOldId,
                    d.DocumentGuid,
                    ppt.PhotoTypeCode,
                    CreatedDate = pp.CreatedDate
                }
            ).ToListAsync(cancellationToken)
            : new();

        var oldPhotoLookup = oldPhotoDocs
            .GroupBy(x => x.PropertyMastOldId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.PhotoTypeCode, StringComparer.OrdinalIgnoreCase)
                      .Select(group => group.OrderByDescending(x => x.CreatedDate).First())
                      .Select(x => new PropertyPhotoDocumentDto
                      {
                          DocumentGuid = x.DocumentGuid,
                          PhotoTypeCode = x.PhotoTypeCode
                      }).ToList()
            );

        // Fetch Old Tax Details (TransMastOld rows) for old properties
        var oldTaxLookup = new Dictionary<int, List<OldTaxDetailDto>>();
        if (_transMastOldRepository != null && oldPropertyIds.Count > 0)
        {
            var tmoRows = await (
                from tmo in _transMastOldRepository.GetQueryable().AsNoTracking()
                where oldPropertyIds.Contains(tmo.PropertyMastOldId) && tmo.IsActive && !tmo.MarkedForDeletion
                join tx in _taxMasterRepository.GetQueryable().AsNoTracking() on tmo.TaxId equals tx.Id into txJ
                from tx in txJ.DefaultIfEmpty()
                select new OldTaxDetailDto
                {
                    Id = tmo.Id,
                    PropertyMastOldId = tmo.PropertyMastOldId,
                    FinanceYearId = tmo.FinanceYearId,
                    CalculationType = tmo.CalculationType,
                    CalculationValue = tmo.CalculationValue,
                    CalculationAnnualValue = tmo.CalculationAnnualValue,
                    TaxId = tmo.TaxId,
                    TaxName = tx != null ? (tx.TaxNameAlias ?? tx.TaxName) : null,
                    TaxAmount = tmo.TaxAmount
                }
            ).ToListAsync(cancellationToken);

            oldTaxLookup = tmoRows
                .GroupBy(x => x.PropertyMastOldId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // ── 6. Build Comparison DTOs (NewSurvey, OldSurvey, Difference) ─────
        var comparisons = new List<ApartmentQCComparisonDto>(rawRows.Count);

        foreach (var p in rawRows)
        {
            wardZones.TryGetValue(p.WardId, out var wz);

            // Find all mapped old properties for this new property
            List<PropertyMastOldEntity> mappedOldEntities = new();
            if (mappingsByNewProperty.TryGetValue(p.Id, out var linkedMapList) && linkedMapList.Count > 0)
            {
                foreach (var m in linkedMapList)
                {
                    if (m.PropertyIdOld.HasValue && oldDataById.TryGetValue(m.PropertyIdOld.Value, out var oEnt))
                    {
                        if (!mappedOldEntities.Any(x => x.Id == oEnt.Id))
                            mappedOldEntities.Add(oEnt);
                    }
                    else if (!string.IsNullOrEmpty(m.PropertyNoOld) && oldDataByNo.TryGetValue(m.PropertyNoOld, out var oEntByNo))
                    {
                        if (!mappedOldEntities.Any(x => x.Id == oEntByNo.Id))
                            mappedOldEntities.Add(oEntByNo);
                    }
                }
            }

            if (mappedOldEntities.Count == 0 && !string.IsNullOrEmpty(p.PropertyNo) && oldDataByNo.TryGetValue(p.PropertyNo, out var fallbackOld))
            {
                mappedOldEntities.Add(fallbackOld);
            }

            PropertyMastOldEntity? primaryOld = mappedOldEntities.FirstOrDefault();

            retroTaxLookup.TryGetValue(p.Id, out var newRetroTax);
            currentDemandLookup.TryGetValue(p.Id, out var currentDemand);
            rvTaxLookup.TryGetValue(p.Id, out var rateableValue);
            cvTaxLookup.TryGetValue(p.Id, out var capitalValue);
            if (isRvOnly) capitalValue = null;
            photoLookup.TryGetValue(p.Id, out var propertyPhotos);
            detailsLookup.TryGetValue(p.Id, out var primaryDetail);

            var calculationValue = isRvOnly ? rateableValue : (capitalValue ?? rateableValue);
            var totalTaxRV = rateableValue ?? 0m;
            var totalTaxCV = capitalValue ?? 0m;
            var newTaxTotal = currentDemand > 0 ? currentDemand : (calculationValue ?? 0m);

            OldSurveyPropertyDto oldSurvey;
            if (mappedOldEntities.Count == 1)
            {
                var old = mappedOldEntities[0];
                oldSurvey = _mapper.Map<OldSurveyPropertyDto>(old);

                if (oldPhotoLookup.TryGetValue(old.Id, out var oldPhotos) && oldPhotos != null && oldPhotos.Count > 0)
                {
                    oldSurvey.Photos = oldPhotos;
                    oldSurvey.PropertyPhotoDocumentGuid = oldPhotos.FirstOrDefault(x => string.Equals(x.PhotoTypeCode, "PROPERTY_PHOTO", StringComparison.OrdinalIgnoreCase))?.DocumentGuid;
                    oldSurvey.PlanPhotoDocumentGuid = oldPhotos.FirstOrDefault(x => string.Equals(x.PhotoTypeCode, "PLAN_PHOTO", StringComparison.OrdinalIgnoreCase))?.DocumentGuid;
                }

                if (oldTaxLookup.TryGetValue(old.Id, out var oTaxes) && oTaxes != null)
                {
                    oldSurvey.OldTaxDetails = oTaxes;
                }
            }
            else if (mappedOldEntities.Count > 1)
            {
                // MERGE (1 New -> Many Old): Aggregate all mapped old properties
                var firstOld = mappedOldEntities.First();
                oldSurvey = _mapper.Map<OldSurveyPropertyDto>(firstOld);
                oldSurvey.OldPropertyNo = string.Join(", ", mappedOldEntities.Select(o => o.OldPropertyNo).Where(s => !string.IsNullOrEmpty(s)).Distinct());
                oldSurvey.PropertyNo = oldSurvey.OldPropertyNo;

                decimal totalRV = 0m;
                decimal totalTax = 0m;
                decimal totalConstArea = 0m;
                decimal totalBuiltupSqMtr = 0m;
                decimal totalBuiltupSqFt = 0m;

                foreach (var old in mappedOldEntities)
                {
                    totalRV += (decimal)(old.OldRV ?? 0);
                    totalTax += (decimal)(old.OldTotalTax ?? 0);
                    totalConstArea += (decimal)(old.OldConstructionArea ?? 0);
                    totalBuiltupSqMtr += (decimal)(old.OldConstructionArea ?? 0);
                    totalBuiltupSqFt += (decimal)((old.OldConstructionArea ?? 0) * 10.7639);
                }

                oldSurvey.RateableValue = Math.Round(totalRV, 2);
                oldSurvey.TotalTax = Math.Round(totalTax, 2);
                oldSurvey.ConstructionArea = Math.Round(totalConstArea, 2);
                oldSurvey.BuiltupASqMtr = Math.Round(totalBuiltupSqMtr, 2);
                oldSurvey.BuiltupASqFt = Math.Round(totalBuiltupSqFt, 2);

                var mergedPhotos = new List<PropertyPhotoDocumentDto>();
                var mergedOldTaxes = new List<OldTaxDetailDto>();
                foreach (var old in mappedOldEntities)
                {
                    if (oldPhotoLookup.TryGetValue(old.Id, out var oPhotos) && oPhotos != null)
                    {
                        mergedPhotos.AddRange(oPhotos);
                    }
                    if (oldTaxLookup.TryGetValue(old.Id, out var oTaxes) && oTaxes != null)
                    {
                        mergedOldTaxes.AddRange(oTaxes);
                    }
                }
                if (mergedPhotos.Count > 0)
                {
                    oldSurvey.Photos = mergedPhotos;
                    oldSurvey.PropertyPhotoDocumentGuid = mergedPhotos.FirstOrDefault(x => string.Equals(x.PhotoTypeCode, "PROPERTY_PHOTO", StringComparison.OrdinalIgnoreCase))?.DocumentGuid;
                    oldSurvey.PlanPhotoDocumentGuid = mergedPhotos.FirstOrDefault(x => string.Equals(x.PhotoTypeCode, "PLAN_PHOTO", StringComparison.OrdinalIgnoreCase))?.DocumentGuid;
                }
                oldSurvey.OldTaxDetails = mergedOldTaxes;
            }
            else
            {
                oldSurvey = new OldSurveyPropertyDto();
            }

            var newSurveyInput = new NewSurveyMappingInput(
                p,
                wz?.ZoneNo,
                wz?.WardNo,
                primaryDetail,
                primaryOld,
                newRetroTax,
                rateableValue,
                capitalValue,
                calculationValue,
                newTaxTotal,
                totalTaxRV,
                totalTaxCV,
                currentDemand > 0 ? currentDemand : null,
                propertyPhotos);
            var newSurvey = _mapper.Map<NewSurveyPropertyDto>(newSurveyInput);

            if (propertyPhotos != null && propertyPhotos.Count > 0)
            {
                newSurvey.Photos = propertyPhotos;
                newSurvey.PropertyPhotoDocumentGuid = propertyPhotos.FirstOrDefault(x => string.Equals(x.PhotoTypeCode, "PROPERTY_PHOTO", StringComparison.OrdinalIgnoreCase))?.DocumentGuid;
                newSurvey.PlanPhotoDocumentGuid = propertyPhotos.FirstOrDefault(x => string.Equals(x.PhotoTypeCode, "PLAN_PHOTO", StringComparison.OrdinalIgnoreCase))?.DocumentGuid;
            }

            var difference = _mapper.Map<ApartmentQCPropertyDifferenceDto>((newSurvey, oldSurvey));

            comparisons.Add(new ApartmentQCComparisonDto
            {
                NewSurvey = newSurvey,
                OldSurvey = oldSurvey,
                Difference = difference
            });
        }

        // ── 7. Group & Aggregate Multiple New Properties Mapped to Same Old Property ──
        var finalComparisons = new List<ApartmentQCComparisonDto>();
        var groupedByOld = comparisons
            .GroupBy(c => c.OldSurvey?.Id > 0 ? (long?)c.OldSurvey.Id : null)
            .ToList();

        foreach (var group in groupedByOld)
        {
            if (group.Key == null || group.Count() == 1)
            {
                finalComparisons.AddRange(group);
            }
            else
            {
                // Many New -> 1 Old: Aggregate all new properties into one main row with aggregated calculation data
                var items = group.ToList();
                var first = items[0];

                var aggNewSurvey = new NewSurveyPropertyDto
                {
                    Id = first.NewSurvey.Id,
                    PDNId = first.NewSurvey.PDNId,
                    TaxZoneId = first.NewSurvey.TaxZoneId,
                    ZoneNo = first.NewSurvey.ZoneNo,
                    PropertyNo = first.NewSurvey.PropertyNo,
                    WardId = first.NewSurvey.WardId,
                    WardNo = first.NewSurvey.WardNo,
                    MobileNo = first.NewSurvey.MobileNo,
                    EmailId = first.NewSurvey.EmailId,
                    OCNo = first.NewSurvey.OCNo,
                    OCDate = first.NewSurvey.OCDate,
                    FlatOrShopNo = null,
                    FlatOrShopName = null,
                    FlatOrShopNoEnglish = null,
                    FlatOrShopNameEnglish = null,
                    OwnerName = first.NewSurvey.OwnerName,
                    OwnerNameEnglish = first.NewSurvey.OwnerNameEnglish,
                    OccupierName = first.NewSurvey.OccupierName,
                    OccupierNameEnglish = first.NewSurvey.OccupierNameEnglish,
                    PropertyType = first.NewSurvey.PropertyType,
                    PropertyTypeName = first.NewSurvey.PropertyTypeName,
                    RentYearly = first.NewSurvey.RentYearly,
                    RentMonthly = first.NewSurvey.RentMonthly,
                    RenterName = first.NewSurvey.RenterName,
                    RenterNameEnglish = first.NewSurvey.RenterNameEnglish,
                    TypeOfUse = first.NewSurvey.TypeOfUse,
                    Type = first.NewSurvey.Type,
                    SubTypeOfUse = first.NewSurvey.SubTypeOfUse,
                    ApartmentType = first.NewSurvey.ApartmentType,
                    PartType = first.NewSurvey.PartType,
                    Wing = first.NewSurvey.Wing,
                    WingDetailId = first.NewSurvey.WingDetailId,
                    Floor = first.NewSurvey.Floor,
                    SubFloor = first.NewSurvey.SubFloor,
                    ConstructionType = first.NewSurvey.ConstructionType,
                    ConstructionYear = first.NewSurvey.ConstructionYear,
                    AssessmentYear = first.NewSurvey.AssessmentYear,
                    BHK = first.NewSurvey.BHK,
                    Photos = items.SelectMany(x => x.NewSurvey.Photos).GroupBy(x => x.DocumentGuid).Select(g => g.First()).ToList(),
                    PropertyPhotoDocumentGuid = first.NewSurvey.PropertyPhotoDocumentGuid,
                    PlanPhotoDocumentGuid = first.NewSurvey.PlanPhotoDocumentGuid,

                    // Sum of calculation values:
                    NoOfRooms = items.Sum(x => x.NewSurvey.NoOfRooms ?? 0),
                    CarpetASqFt = Math.Round(items.Sum(x => x.NewSurvey.CarpetASqFt ?? 0m), 2),
                    CarpetASqMtr = Math.Round(items.Sum(x => x.NewSurvey.CarpetASqMtr ?? 0m), 2),
                    BuiltupASqFt = Math.Round(items.Sum(x => x.NewSurvey.BuiltupASqFt ?? 0m), 2),
                    BuiltupASqMtr = Math.Round(items.Sum(x => x.NewSurvey.BuiltupASqMtr ?? 0m), 2),
                    RateableValue = Math.Round(items.Sum(x => x.NewSurvey.RateableValue ?? 0m), 2),
                    CapitalValue = Math.Round(items.Sum(x => x.NewSurvey.CapitalValue ?? 0m), 2),
                    CalculationValue = Math.Round(items.Sum(x => x.NewSurvey.CalculationValue ?? 0m), 2),
                    NewTaxTotalRV = Math.Round(items.Sum(x => x.NewSurvey.NewTaxTotalRV), 2),
                    NewTaxTotalCV = Math.Round(items.Sum(x => x.NewSurvey.NewTaxTotalCV), 2),
                    NewTaxTotal = Math.Round(items.Sum(x => x.NewSurvey.NewTaxTotal), 2),
                    CurrentDemand = Math.Round(items.Sum(x => x.NewSurvey.CurrentDemand ?? 0m), 2),
                    RetroTaxTotal = Math.Round(items.Sum(x => x.NewSurvey.RetroTaxTotal ?? 0m), 2)
                };

                var oldSurvey = first.OldSurvey;
                var difference = _mapper.Map<ApartmentQCPropertyDifferenceDto>((aggNewSurvey, oldSurvey));

                finalComparisons.Add(new ApartmentQCComparisonDto
                {
                    NewSurvey = aggNewSurvey,
                    OldSurvey = oldSurvey,
                    Difference = difference
                });
            }
        }

        return new PagedResult<ApartmentQCComparisonDto>(finalComparisons, totalCount, pageNumber, pageSize);
    }
}
