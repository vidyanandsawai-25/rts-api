using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services;

public class PropertyMapMasterService : BaseCommonCrudService<PropertyMapMasterEntity, PropertyMapMasterDtos, CreatePropertyMapMasterDto, UpdatePropertyMapMasterDto, PropertyMapQueryParameters, int>, IPropertyMapMasterService
{
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly IRepository<PropertyDetailsOldEntity, int> _propertyDetailsOldRepository;
    private readonly IRepository<PropertyDetailsEntity, int>? _propertyDetailsRepository;
    private readonly IServiceProvider? _serviceProvider;

    // Compatibility constructor for unit tests
    public PropertyMapMasterService(
        IRepository<PropertyMapMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _propertyMapDetailRepository = null!;
        _propertyRepository = null!;
        _propertyMastOldRepository = null!;
        _propertyDetailsOldRepository = null!;
        _serviceProvider = null;
    }

    // Main constructor for Dependency Injection
    public PropertyMapMasterService(
        IRepository<PropertyMapMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
        IRepository<PropertyDetailsOldEntity, int> propertyDetailsOldRepository = null!,
        IRepository<PropertyDetailsEntity, int>? propertyDetailsRepository = null,
        IServiceProvider? serviceProvider = null)
        : base(repository, unitOfWork, mapper)
    {
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _propertyRepository = propertyRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _propertyDetailsOldRepository = propertyDetailsOldRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _serviceProvider = serviceProvider;
    }

    // -------------------------------------------------------------------------
    // Existing: simple mapped-properties paged list (unchanged)
    // -------------------------------------------------------------------------

    public async Task<PagedResult<PropertyMapDetailReturnDto>> GetMappedPropertiesAsync(
        PropertyMapDetailQueryParameters queryParams,
        CancellationToken cancellationToken = default)
    {
        var pmdQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking()
            .Where(x => x.IsActive && x.PropertyIdNew.HasValue && x.PropertyIdOld.HasValue);

        if (queryParams.PropertyId.HasValue)
        {
            pmdQuery = pmdQuery.Where(x => x.PropertyIdNew == queryParams.PropertyId.Value);
        }

        int totalCount = await pmdQuery.CountAsync(cancellationToken);

        List<(int? PropertyMastOldId, PropertyMapDetailReturnDto Dto)> rawItems = new();

        if (totalCount > 0)
        {
            IQueryable<PropertyMapDetailEntity> pagedPmdQuery = pmdQuery.OrderBy(x => x.Id);
            if (queryParams.PageSize != -1)
            {
                pagedPmdQuery = pagedPmdQuery.Skip((queryParams.PageNumber - 1) * queryParams.PageSize).Take(queryParams.PageSize);
            }

            var pagedPmds = await pagedPmdQuery.ToListAsync(cancellationToken);

            var pmmIds = pagedPmds.Select(x => x.PropertyMapId).Distinct().ToList();
            var pmIds = pagedPmds.Select(x => x.PropertyIdNew!.Value).Distinct().ToList();
            var pmoIds = pagedPmds.Select(x => x.PropertyIdOld!.Value).Distinct().ToList();

            var pmmMap = pmmIds.Any()
                ? (await _repository.GetQueryable().AsNoTracking().Where(x => pmmIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken)).ToDictionary(x => x.Id)
                : new Dictionary<int, PropertyMapMasterEntity>();

            var pmMap = pmIds.Any()
                ? (await _propertyRepository.GetQueryable().AsNoTracking().Where(x => pmIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken)).ToDictionary(x => x.Id)
                : new Dictionary<int, PropertyEntity>();

            var pmoMap = pmoIds.Any()
                ? (await _propertyMastOldRepository.GetQueryable().AsNoTracking().Where(x => pmoIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken)).ToDictionary(x => x.Id)
                : new Dictionary<int, PropertyMastOldEntity>();

            rawItems = pagedPmds.Select(pmd =>
            {
                pmMap.TryGetValue(pmd.PropertyIdNew!.Value, out var pm);
                pmmMap.TryGetValue(pmd.PropertyMapId, out var pmm);
                pmoMap.TryGetValue(pmd.PropertyIdOld!.Value, out var pmo);

                if (pm == null) return ((int?)null, (PropertyMapDetailReturnDto)null!);

                return ((int?)(pmo?.Id), MapToReturnDto(pm, pmm, pmo));
            }).Where(x => x.Item2 != null).ToList();
        }
        else if (queryParams.PropertyId.HasValue)
        {
            // Fallback: If specific PropertyId was requested but has no old mapping in PropertyMapDetailEntity,
            // fetch the New Property so NewPropertyInfo is still returned!
            var unmappedPm = await _propertyRepository.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == queryParams.PropertyId.Value && x.IsActive, cancellationToken);

            if (unmappedPm != null)
            {
                totalCount = 1;
                rawItems = new List<(int? PropertyMastOldId, PropertyMapDetailReturnDto Dto)>
                {
                    ((int?)null, MapToReturnDto(unmappedPm, null, null))
                };
            }
        }

        if (!rawItems.Any())
        {
            return new PagedResult<PropertyMapDetailReturnDto>(new List<PropertyMapDetailReturnDto>(), totalCount, queryParams.PageNumber, queryParams.PageSize);
        }

        var items = await EnrichPropertyMapDetailsAsync(rawItems, cancellationToken);
        return new PagedResult<PropertyMapDetailReturnDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    private static string? _cachedTaxCalcMethod;
    private static DateTime _taxCalcMethodExpires = DateTime.MinValue;

    private static int? _cachedFinanceYearId;
    private static DateTime _financeYearIdExpires = DateTime.MinValue;

    private static HashSet<int>? _cachedRetroPcmIds;
    private static DateTime _retroPcmIdsExpires = DateTime.MinValue;

    private static readonly SemaphoreSlim _cacheLock = new(1, 1);

    private static async Task<string?> GetCachedTaxCalcMethodAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        if (_cachedTaxCalcMethod != null && DateTime.UtcNow < _taxCalcMethodExpires)
            return _cachedTaxCalcMethod;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedTaxCalcMethod != null && DateTime.UtcNow < _taxCalcMethodExpires)
                return _cachedTaxCalcMethod;

            var policyConfigRepo = sp.GetService<IRepository<PolicyConfigurationEntity, int>>();
            if (policyConfigRepo != null)
            {
                _cachedTaxCalcMethod = await policyConfigRepo.GetQueryable().AsNoTracking()
                    .Where(p => p.PolicyCode == "TaxCalculationMethod" && p.IsActive)
                    .Select(p => p.PolicyValue)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            _taxCalcMethodExpires = DateTime.UtcNow.AddMinutes(10);
            return _cachedTaxCalcMethod;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static async Task<int?> GetCachedFinanceYearIdAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        if (_cachedFinanceYearId.HasValue && DateTime.UtcNow < _financeYearIdExpires)
            return _cachedFinanceYearId;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedFinanceYearId.HasValue && DateTime.UtcNow < _financeYearIdExpires)
                return _cachedFinanceYearId;

            var yearRepo = sp.GetService<IRepository<YearMasterEntity, int>>();
            if (yearRepo != null)
            {
                var today = DateTime.Today;
                _cachedFinanceYearId = await yearRepo.GetQueryable().AsNoTracking()
                    .Where(y => y.IsActive)
                    .OrderByDescending(y => y.Year)
                    .Select(y => (int?)y.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? await yearRepo.GetQueryable().AsNoTracking()
                        .Where(y => y.StartDate <= today && y.EndDate >= today)
                        .Select(y => (int?)y.Id)
                        .FirstOrDefaultAsync(cancellationToken);
            }
            _financeYearIdExpires = DateTime.UtcNow.AddMinutes(10);
            return _cachedFinanceYearId;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static async Task<HashSet<int>> GetCachedRetroPolicyCodeIdsAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        if (_cachedRetroPcmIds != null && DateTime.UtcNow < _retroPcmIdsExpires)
            return _cachedRetroPcmIds;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedRetroPcmIds != null && DateTime.UtcNow < _retroPcmIdsExpires)
                return _cachedRetroPcmIds;

            var pcmRepo = sp.GetService<IRepository<PolicyCodeMasterEntity, int>>();
            if (pcmRepo != null)
            {
                var ids = await pcmRepo.GetQueryable().AsNoTracking()
                    .Where(pcm => pcm.IsRetroDemand && pcm.IsActive)
                    .Select(pcm => pcm.Id)
                    .ToListAsync(cancellationToken);
                _cachedRetroPcmIds = ids.ToHashSet();
            }
            else
            {
                _cachedRetroPcmIds = new HashSet<int>();
            }
            _retroPcmIdsExpires = DateTime.UtcNow.AddMinutes(10);
            return _cachedRetroPcmIds;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Returns a paged list of mapped new properties (New Survey details) based on Old PropertyId.
    /// Excludes old property details and old trans mast records.
    /// </summary>
    public async Task<PagedResult<NewSurveyPropertyDto>> GetMappedNewPropertiesAsync(
        PropertyMapDetailQueryParameters queryParams,
        CancellationToken cancellationToken = default)
    {
        var oldPropertyId = queryParams.OldPropertyId ?? queryParams.PropertyId;

        var pmdQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking()
            .Where(x => x.IsActive && x.PropertyIdNew.HasValue && x.PropertyIdOld.HasValue);

        if (oldPropertyId.HasValue)
        {
            pmdQuery = pmdQuery.Where(x => x.PropertyIdOld == oldPropertyId.Value);
        }

        int totalCount = await pmdQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResult<NewSurveyPropertyDto>(new List<NewSurveyPropertyDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
        }

        IQueryable<PropertyMapDetailEntity> pagedPmdQuery = pmdQuery.OrderBy(x => x.Id);
        if (queryParams.PageSize != -1)
        {
            pagedPmdQuery = pagedPmdQuery.Skip((queryParams.PageNumber - 1) * queryParams.PageSize).Take(queryParams.PageSize);
        }

        var mappedPropertyIds = await pagedPmdQuery.Select(x => x.PropertyIdNew!.Value).ToListAsync(cancellationToken);
        if (mappedPropertyIds.Count == 0)
        {
            return new PagedResult<NewSurveyPropertyDto>(new List<NewSurveyPropertyDto>(), totalCount, queryParams.PageNumber, queryParams.PageSize);
        }

        var sp = _serviceProvider;
        var ptmRepo = sp?.GetService<IRepository<PropertyTypeMasterEntity, int>>();
        var ptmList = ptmRepo != null ? await ptmRepo.GetQueryable().AsNoTracking().ToListAsync(cancellationToken) : new List<PropertyTypeMasterEntity>();
        var ptmDict = ptmList.ToDictionary(x => x.Id);

        var propList = await _propertyRepository.GetQueryable().AsNoTracking()
            .Where(pm => mappedPropertyIds.Contains(pm.Id) && pm.IsActive && !pm.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        var propDict = propList.ToDictionary(x => x.Id);
        var rawRows = new List<JoinedPropertyRowDto>();
        foreach (var id in mappedPropertyIds)
        {
            if (!propDict.TryGetValue(id, out var pm)) continue;
            PropertyTypeMasterEntity? ptm = null;
            if (pm.PropertyTypeId.HasValue)
            {
                ptmDict.TryGetValue(pm.PropertyTypeId.Value, out ptm);
            }

            rawRows.Add(new JoinedPropertyRowDto
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
                PartType = ptm?.PartType,
                PropertyType = ptm?.Id ?? 0,
                PropertyTypeName = ptm?.PropertyDescription,
                WingDetailId = pm.WingDetailId,
                ApartmentType = pm.Type
            });
        }

        if (rawRows.Count == 0)
        {
            return new PagedResult<NewSurveyPropertyDto>(new List<NewSurveyPropertyDto>(), totalCount, queryParams.PageNumber, queryParams.PageSize);
        }

        var propertyIds = rawRows.Select(r => r.Id).ToList();
        var wardIds = rawRows.Select(r => r.WardId).Distinct().ToList();

        // BHK resolution
        var bhkLookup = new Dictionary<int, string?>();
        if (sp != null)
        {
            var assessRepo = sp.GetService<IRepository<PropertyAssessmentEntity, int>>();
            if (assessRepo != null)
            {
                bhkLookup = await assessRepo.GetQueryable().AsNoTracking()
                    .Where(d => propertyIds.Contains(d.PropertyId) && d.IsActive && !d.MarkedForDeletion)
                    .GroupBy(d => d.PropertyId)
                    .Select(g => new { PropertyId = g.Key, BHK = g.OrderByDescending(d => d.CreatedDate).Select(d => d.BHK).FirstOrDefault() })
                    .ToDictionaryAsync(x => x.PropertyId, x => x.BHK, cancellationToken);
            }
        }

        // Wing resolution
        var directWingNames = new Dictionary<int, string?>();
        var societyWingLookup = new Dictionary<int, string?>();
        if (sp != null)
        {
            var wingDetailRepo = sp.GetService<IRepository<WingDetailsMastEntity, int>>();
            var societyRepo = sp.GetService<IRepository<SocietyDetailsEntity, int>>();

            var wingDetailIds = rawRows.Where(r => r.WingDetailId.HasValue).Select(r => r.WingDetailId!.Value).Distinct().ToList();
            if (wingDetailRepo != null && wingDetailIds.Count > 0)
            {
                var wingRows = await wingDetailRepo.GetQueryable().AsNoTracking()
                    .Where(wdm => wingDetailIds.Contains(wdm.Id) && wdm.IsActive && !wdm.MarkedForDeletion)
                    .ToListAsync(cancellationToken);

                var societyIds = wingRows.Select(w => w.SocietyDetailsMastId).Distinct().ToList();
                var societyDict = (societyRepo != null && societyIds.Count > 0)
                    ? (await societyRepo.GetQueryable().AsNoTracking().Where(s => societyIds.Contains(s.Id)).ToListAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.SocietyName)
                    : new Dictionary<int, string?>();

                directWingNames = wingRows.ToDictionary(x => x.Id, x => (string?)x.WingName);
                societyWingLookup = wingRows.Where(x => societyDict.ContainsKey(x.SocietyDetailsMastId))
                    .ToDictionary(x => x.Id, x => societyDict[x.SocietyDetailsMastId]);
            }
        }

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

        // Ward / Zone
        var wardZones = new Dictionary<int, (string? WardNo, string? ZoneNo)>();
        if (sp != null && wardIds.Count > 0)
        {
            var wardRepo = sp.GetService<IRepository<WardEntity, int>>();
            var zoneRepo = sp.GetService<IRepository<ZoneEntity, int>>();

            if (wardRepo != null)
            {
                var wardList = await wardRepo.GetQueryable().AsNoTracking().Where(w => wardIds.Contains(w.Id)).ToListAsync(cancellationToken);
                var zoneIds = wardList.Select(w => w.ZoneId).Distinct().ToList();
                var zoneDict = (zoneRepo != null && zoneIds.Count > 0)
                    ? (await zoneRepo.GetQueryable().AsNoTracking().Where(z => zoneIds.Contains(z.Id)).ToListAsync(cancellationToken)).ToDictionary(z => z.Id, z => z.ZoneNo)
                    : new Dictionary<int, string?>();

                wardZones = wardList.ToDictionary(w => w.Id, w => (
                    WardNo: (string?)w.WardNo,
                    ZoneNo: zoneDict.TryGetValue(w.ZoneId, out var zn) ? zn : null
                ));
            }
        }

        // PropertyDetails
        var detailsLookup = new Dictionary<int, FetchDetailRowDto>();
        if (sp != null)
        {
            var pdRepo = sp.GetService<IRepository<PropertyDetailsEntity, int>>();
            var floorRepo = sp.GetService<IRepository<FloorEntity, int>>();
            var ctRepo = sp.GetService<IRepository<ConstructionTypeEntity, int>>();
            var touRepo = sp.GetService<IRepository<TypeOfUseEntity, int>>();
            var stouRepo = sp.GetService<IRepository<SubTypeOfUseEntity, int>>();

            if (pdRepo != null && floorRepo != null && ctRepo != null && touRepo != null && stouRepo != null)
            {
                var detailsList = await (
                    from pd in pdRepo.GetQueryable().AsNoTracking()
                    where propertyIds.Contains(pd.PropertyId)
                       && pd.IsActive && !pd.MarkedForDeletion
                    join fl in floorRepo.GetQueryable().AsNoTracking() on pd.FloorId equals fl.Id into flJ
                    from fl in flJ.DefaultIfEmpty()
                    join sfl in floorRepo.GetQueryable().AsNoTracking() on pd.SubFloorId equals sfl.Id into sflJ
                    from sfl in sflJ.DefaultIfEmpty()
                    join ct in ctRepo.GetQueryable().AsNoTracking() on pd.ConstructionTypeId equals ct.Id into ctJ
                    from ct in ctJ.DefaultIfEmpty()
                    join tou in touRepo.GetQueryable().AsNoTracking() on pd.TypeOfUseId equals tou.Id into touJ
                    from tou in touJ.DefaultIfEmpty()
                    join stou in stouRepo.GetQueryable().AsNoTracking() on pd.SubTypeOfUseId equals stou.Id into stouJ
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
                    }
                ).ToListAsync(cancellationToken);

                detailsLookup = detailsList
                    .GroupBy(d => d.PropertyId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Id).FirstOrDefault()!);
            }
        }

        // PolicyConfiguration & Retro demand & Tax calculations
        var isRvOnly = false;
        var retroCalculationType = "CV";
        var retroTaxLookup = new Dictionary<int, decimal>();
        var rvTaxLookup = new Dictionary<int, decimal?>();
        var cvTaxLookup = new Dictionary<int, decimal?>();
        var currentDemandLookup = new Dictionary<int, decimal>();

        if (sp != null)
        {
            var taxCalculationMethod = await GetCachedTaxCalcMethodAsync(sp, cancellationToken);
            isRvOnly = string.Equals(taxCalculationMethod?.Trim(), "RV", StringComparison.OrdinalIgnoreCase);
            retroCalculationType = isRvOnly ? "RV" : "CV";

            var currentFinanceYearId = await GetCachedFinanceYearIdAsync(sp, cancellationToken);

            var tmRepo = sp.GetService<IRepository<TransMastEntity, int>>();
            var txRepo = sp.GetService<IRepository<TaxMasterEntity, int>>();
            if (tmRepo != null && txRepo != null)
            {
                var retroPcmIds = await GetCachedRetroPolicyCodeIdsAsync(sp, cancellationToken);

                var tmList = await (
                    from tm in tmRepo.GetQueryable().AsNoTracking()
                    join tx in txRepo.GetQueryable().AsNoTracking() on tm.TaxId equals tx.Id
                    where propertyIds.Contains(tm.PropertyId)
                       && tx.TaxCode == "TAXTOTAL" && tx.IsActive
                       && tm.IsActive && !tm.MarkedForDeletion
                       && tm.CalculationType == retroCalculationType
                    select new
                    {
                        tm.PropertyId,
                        tm.FinanceYearId,
                        tm.PolicyCodeId,
                        tm.TaxAmount
                    }
                ).ToListAsync(cancellationToken);

                foreach (var g in tmList.Where(x => retroPcmIds.Contains(x.PolicyCodeId)).GroupBy(x => x.PropertyId))
                {
                    retroTaxLookup[g.Key] = g.Sum(x => (decimal?)x.TaxAmount) ?? 0m;
                }

                if (currentFinanceYearId.HasValue)
                {
                    foreach (var g in tmList.Where(x => x.FinanceYearId == currentFinanceYearId.Value).GroupBy(x => x.PropertyId))
                    {
                        currentDemandLookup[g.Key] = g.Sum(x => (decimal?)x.TaxAmount) ?? 0m;
                    }
                }
            }

            var ptdRepo = sp.GetService<IRepository<PolicyTaxDetailsEntity, int>>();
            if (ptdRepo != null)
            {
                var rvRows = await ptdRepo.GetQueryable().AsNoTracking()
                    .Where(x => propertyIds.Contains(x.PropertyId) && x.IsCurrent && x.IsActive && !x.MarkedForDeletion)
                    .Select(x => new { x.PropertyId, RateableValue = x.CalculationValue })
                    .ToListAsync(cancellationToken);
                foreach (var r in rvRows) rvTaxLookup.TryAdd(r.PropertyId, r.RateableValue);
            }

            if (!isRvOnly)
            {
                var ptdCvRepo = sp.GetService<IRepository<PolicyTaxDetailsCVEntity, int>>();
                if (ptdCvRepo != null)
                {
                    var cvRows = await ptdCvRepo.GetQueryable().AsNoTracking()
                        .Where(x => propertyIds.Contains(x.PropertyId) && x.IsCurrent && x.IsActive && !x.MarkedForDeletion)
                        .Select(x => new { x.PropertyId, CapitalValue = x.CalculationValue })
                        .ToListAsync(cancellationToken);
                    foreach (var r in cvRows) cvTaxLookup.TryAdd(r.PropertyId, r.CapitalValue);
                }
            }
        }

        // Photos
        var photoLookup = new Dictionary<int, List<PropertyPhotoDocumentDto>>();
        if (sp != null)
        {
            var photoRepo = sp.GetService<IRepository<PropertyPhotoEntity, int>>();
            var photoTypeRepo = sp.GetService<IRepository<PropertyPhotoTypeEntity, int>>();
            var docBindingRepo = sp.GetService<IRepository<DocumentBindingEntity, int>>();
            var docRepo = sp.GetService<IRepository<DocumentEntity, int>>();

            if (photoRepo != null && photoTypeRepo != null && docBindingRepo != null && docRepo != null)
            {
                var photoDocs = await (
                    from pp in photoRepo.GetQueryable().AsNoTracking()
                    where pp.PropertyId.HasValue && propertyIds.Contains(pp.PropertyId.Value) && pp.IsActive && !pp.MarkedForDeletion
                    join ppt in photoTypeRepo.GetQueryable().AsNoTracking() on pp.PhotoTypeId equals ppt.Id
                    where ppt.IsActive && (ppt.PhotoTypeCode == "PLAN_PHOTO" || ppt.PhotoTypeCode == "PROPERTY_PHOTO")
                    join db in docBindingRepo.GetQueryable().AsNoTracking() on pp.DocumentBindingId equals db.Id
                    where db.IsActive
                    join d in docRepo.GetQueryable().AsNoTracking() on db.DocumentId equals d.Id
                    where d.IsActive && !d.MarkedForDeletion
                    select new
                    {
                        pp.PropertyId,
                        d.DocumentGuid,
                        ppt.PhotoTypeCode,
                        CreatedDate = pp.CreatedDate
                    }
                ).ToListAsync(cancellationToken);

                photoLookup = photoDocs
                    .GroupBy(x => x.PropertyId!.Value)
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
            }
        }

        // Map each row to NewSurveyPropertyDto
        var resultList = new List<NewSurveyPropertyDto>(rawRows.Count);
        foreach (var p in rawRows)
        {
            wardZones.TryGetValue(p.WardId, out var wz);
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

            var newSurveyInput = new NewSurveyMappingInput(
                p,
                wz.ZoneNo,
                wz.WardNo,
                primaryDetail,
                null,
                newRetroTax,
                rateableValue,
                capitalValue,
                calculationValue,
                newTaxTotal,
                totalTaxRV,
                totalTaxCV,
                currentDemand > 0 ? currentDemand : null,
                propertyPhotos);

            var dto = _mapper.Map<NewSurveyPropertyDto>(newSurveyInput);
            if (propertyPhotos != null && propertyPhotos.Count > 0)
            {
                dto.Photos = propertyPhotos;
                dto.PropertyPhotoDocumentGuid = propertyPhotos.FirstOrDefault(x => string.Equals(x.PhotoTypeCode, "PROPERTY_PHOTO", StringComparison.OrdinalIgnoreCase))?.DocumentGuid;
                dto.PlanPhotoDocumentGuid = propertyPhotos.FirstOrDefault(x => string.Equals(x.PhotoTypeCode, "PLAN_PHOTO", StringComparison.OrdinalIgnoreCase))?.DocumentGuid;
            }

            resultList.Add(dto);
        }

        return new PagedResult<NewSurveyPropertyDto>(resultList, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    /// <summary>
    /// Returns mapped/unmapped properties filtered society-wise or wing-wise.
    /// </summary>
    public async Task<PagedResult<PropertyMapSocietyReturnDto>> GetMappedPropertiesSocietyWiseAsync(
        PropertyMapSocietyQueryParameters queryParams,
        CancellationToken cancellationToken = default)
    {
        var wingDetailId = queryParams.WingDetailsId;
        var societyDetailId = queryParams.SocietyDetailId;

        if (!wingDetailId.HasValue && !societyDetailId.HasValue)
        {
            return new PagedResult<PropertyMapSocietyReturnDto>(new List<PropertyMapSocietyReturnDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
        }

        IQueryable<PropertyEntity> targetPropsQuery = _propertyRepository.GetQueryable().AsNoTracking()
            .Where(pm => pm.IsActive && !pm.MarkedForDeletion);

        if (wingDetailId.HasValue)
        {
            targetPropsQuery = targetPropsQuery.Where(pm => pm.WingDetailId == wingDetailId.Value);
        }
        else if (societyDetailId.HasValue)
        {
            List<int> wingIds = new();
            int? societyPropertyId = null;

            using (var scope = _serviceProvider?.CreateScope())
            {
                var sp = scope?.ServiceProvider;
                if (sp != null)
                {
                    var wingRepo = sp.GetRequiredService<IRepository<WingDetailsMastEntity, int>>();
                    wingIds = await wingRepo.GetQueryable().AsNoTracking()
                        .Where(w => w.SocietyDetailsMastId == societyDetailId.Value && w.IsActive && !w.MarkedForDeletion)
                        .Select(w => w.Id)
                        .ToListAsync(cancellationToken);

                    var societyRepo = sp.GetRequiredService<IRepository<SocietyDetailsEntity, int>>();
                    societyPropertyId = await societyRepo.GetQueryable().AsNoTracking()
                        .Where(s => s.Id == societyDetailId.Value && s.IsActive && !s.MarkedForDeletion)
                        .Select(s => s.PropertyId)
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }

            targetPropsQuery = targetPropsQuery.Where(pm =>
                (pm.WingDetailId.HasValue && wingIds.Contains(pm.WingDetailId.Value))
                || (societyPropertyId.HasValue && pm.Id == societyPropertyId.Value));
        }

        int totalCount = await targetPropsQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResult<PropertyMapSocietyReturnDto>(new List<PropertyMapSocietyReturnDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
        }

        IQueryable<PropertyEntity> pagedPropsQuery = targetPropsQuery.OrderBy(pm => pm.Id);
        if (queryParams.PageSize != -1)
        {
            pagedPropsQuery = pagedPropsQuery.Skip((queryParams.PageNumber - 1) * queryParams.PageSize).Take(queryParams.PageSize);
        }

        var pagedProperties = await pagedPropsQuery.ToListAsync(cancellationToken);
        var pmIds = pagedProperties.Select(p => p.Id).ToList();

        var pmds = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
            .Where(x => x.IsActive && x.PropertyIdNew.HasValue && pmIds.Contains(x.PropertyIdNew.Value) && x.PropertyIdOld.HasValue)
            .ToListAsync(cancellationToken);

        var pmmIds = pmds.Select(x => x.PropertyMapId).Distinct().ToList();
        var pmoIds = pmds.Select(x => x.PropertyIdOld!.Value).Distinct().ToList();

        var pmmMap = pmmIds.Any()
            ? (await _repository.GetQueryable().AsNoTracking().Where(x => pmmIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken)).ToDictionary(x => x.Id)
            : new Dictionary<int, PropertyMapMasterEntity>();

        var pmoMap = pmoIds.Any()
            ? (await _propertyMastOldRepository.GetQueryable().AsNoTracking().Where(x => pmoIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken)).ToDictionary(x => x.Id)
            : new Dictionary<int, PropertyMastOldEntity>();

        var pmdByNewId = pmds.GroupBy(x => x.PropertyIdNew!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<PropertyMapSocietyReturnDto>();
        foreach (var pm in pagedProperties)
        {
            if (pmdByNewId.TryGetValue(pm.Id, out var mappings) && mappings.Any())
            {
                foreach (var pmd in mappings)
                {
                    pmmMap.TryGetValue(pmd.PropertyMapId, out var pmm);
                    pmoMap.TryGetValue(pmd.PropertyIdOld!.Value, out var pmo);
                    items.Add(MapToSocietyReturnDto(pm, pmm, pmo));
                }
            }
            else
            {
                items.Add(MapToSocietyReturnDto(pm, null, null));
            }
        }

        return new PagedResult<PropertyMapSocietyReturnDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    private async Task<List<PropertyMapDetailReturnDto>> EnrichPropertyMapDetailsAsync(
        List<(int? PropertyMastOldId, PropertyMapDetailReturnDto Dto)> rawItems,
        CancellationToken cancellationToken)
    {
        if (!rawItems.Any())
        {
            return new List<PropertyMapDetailReturnDto>();
        }

        using var scope = _serviceProvider?.CreateScope();
        var sp = scope?.ServiceProvider;

        // Fetch master lookup data ONLY for the paged result items
        var wardIds = rawItems.Where(x => x.Dto.NewPropertyInfo != null).Select(x => x.Dto.NewPropertyInfo!.WardId).Distinct().ToList();
        var taxZoneIds = rawItems.Where(x => x.Dto.NewPropertyInfo != null).Select(x => x.Dto.NewPropertyInfo!.TaxZoneId).Distinct().ToList();
        var propTypeIds = rawItems.Where(x => x.Dto.NewPropertyInfo != null && x.Dto.NewPropertyInfo!.PropertyTypeId.HasValue).Select(x => x.Dto.NewPropertyInfo!.PropertyTypeId!.Value).Distinct().ToList();
        var categoryIds = rawItems.Where(x => x.Dto.NewPropertyInfo != null && x.Dto.NewPropertyInfo!.CategoryId.HasValue).Select(x => x.Dto.NewPropertyInfo!.CategoryId!.Value).Distinct().ToList();

        var wardMap = sp != null && wardIds.Any()
            ? (await sp.GetRequiredService<IRepository<WardEntity, int>>().GetQueryable().AsNoTracking().Where(w => wardIds.Contains(w.Id)).ToListAsync(cancellationToken)).ToDictionary(w => w.Id)
            : new Dictionary<int, WardEntity>();

        var taxZoneMap = sp != null && taxZoneIds.Any()
            ? (await sp.GetRequiredService<IRepository<TaxZoneEntity, int>>().GetQueryable().AsNoTracking().Where(tz => taxZoneIds.Contains(tz.Id)).ToListAsync(cancellationToken)).ToDictionary(t => t.Id)
            : new Dictionary<int, TaxZoneEntity>();

        var propertyTypeMap = sp != null && propTypeIds.Any()
            ? (await sp.GetRequiredService<IRepository<PropertyTypeMasterEntity, int>>().GetQueryable().AsNoTracking().Where(pt => propTypeIds.Contains(pt.Id)).ToListAsync(cancellationToken)).ToDictionary(p => p.Id)
            : new Dictionary<int, PropertyTypeMasterEntity>();

        var categoryMap = sp != null && categoryIds.Any()
            ? (await sp.GetRequiredService<IRepository<PropertyCategoryEntity, int>>().GetQueryable().AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToListAsync(cancellationToken)).ToDictionary(c => c.Id)
            : new Dictionary<int, PropertyCategoryEntity>();

        // Enrich NewPropertyInfo with master descriptions (in-memory)
        foreach (var item in rawItems)
        {
            var info = item.Dto.NewPropertyInfo;
            if (info == null) continue;

            if (wardMap.TryGetValue(info.WardId, out var ward))
            {
                info.WardNo = ward.WardNo;
                info.WardDescription = ward.Description;
            }
            if (taxZoneMap.TryGetValue(info.TaxZoneId, out var tz))
            {
                info.TaxZoneNo = tz.TaxZoneNo;
                info.TaxZoneRemark = tz.Remark;
            }
            if (info.PropertyTypeId.HasValue && propertyTypeMap.TryGetValue(info.PropertyTypeId.Value, out var pt))
                info.PropertyTypeDescription = pt.PropertyDescription;

            if (info.CategoryId.HasValue && categoryMap.TryGetValue(info.CategoryId.Value, out var cat))
                info.CategoryName = cat.PropertyCategoryName;
        }

        var oldIds = rawItems.Select(x => x.PropertyMastOldId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var detailsLookup = new Dictionary<int, List<PropertyDetailsOldDto>>();

        if (oldIds.Any())
        {
            List<PropertyDetailsOldEntity> detailsEntities = new();
            if (sp != null)
            {
                detailsEntities = await sp.GetRequiredService<IRepository<PropertyDetailsOldEntity, int>>().GetQueryable()
                    .AsNoTracking()
                    .Where(x => oldIds.Contains(x.PropertyMastOldId) && x.IsActive && !x.MarkedForDeletion)
                    .ToListAsync(cancellationToken);
            }
            else if (_propertyDetailsOldRepository != null)
            {
                detailsEntities = await _propertyDetailsOldRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(x => oldIds.Contains(x.PropertyMastOldId) && x.IsActive && !x.MarkedForDeletion)
                    .ToListAsync(cancellationToken);
            }

            detailsLookup = detailsEntities
                .GroupBy(x => x.PropertyMastOldId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => _mapper.Map<PropertyDetailsOldDto>(x)).ToList()
                );
        }

        // ── Fetch new property details (PropertyDetails) in a single projected query ──
        var newPropertyIds = rawItems.Where(x => x.Dto.PropertyId > 0).Select(x => x.Dto.PropertyId).Distinct().ToList();
        var newDetailsLookup = new Dictionary<int, List<NewPropertyDetailDto>>();

        if (newPropertyIds.Any() && sp != null)
        {
            var pdRepo2 = sp.GetRequiredService<IRepository<PropertyDetailsEntity, int>>();
            var floorRepo = sp.GetRequiredService<IRepository<FloorEntity, int>>();
            var sfRepo = sp.GetRequiredService<IRepository<SubFloorEntity, int>>();
            var touRepo = sp.GetRequiredService<IRepository<TypeOfUseEntity, int>>();
            var stouRepo = sp.GetRequiredService<IRepository<SubTypeOfUseEntity, int>>();
            var ctRepo = sp.GetRequiredService<IRepository<ConstructionTypeEntity, int>>();

            var pdQuery = pdRepo2.GetQueryable().AsNoTracking()
                               .Where(x => newPropertyIds.Contains(x.PropertyId) && x.IsActive && !x.MarkedForDeletion);
            var floorQ = floorRepo.GetQueryable().AsNoTracking();
            var sfQ = sfRepo.GetQueryable().AsNoTracking();
            var touQ = touRepo.GetQueryable().AsNoTracking();
            var stouQ = stouRepo.GetQueryable().AsNoTracking();
            var ctQ = ctRepo.GetQueryable().AsNoTracking();

            var projected = await (
                from pd in pdQuery
                join fl in floorQ on pd.FloorId equals (int?)fl.Id into flJ
                from fl in flJ.DefaultIfEmpty()
                join sf in sfQ on pd.SubFloorId equals (int?)sf.Id into sfJ
                from sf in sfJ.DefaultIfEmpty()
                join tou in touQ on (int?)pd.TypeOfUseId equals (int?)tou.Id into touJ
                from tou in touJ.DefaultIfEmpty()
                join stou in stouQ on pd.SubTypeOfUseId equals (int?)stou.Id into stouJ
                from stou in stouJ.DefaultIfEmpty()
                join ct in ctQ on pd.ConstructionTypeId equals (int?)ct.Id into ctJ
                from ct in ctJ.DefaultIfEmpty()
                select new
                {
                    PropertyId = pd.PropertyId,
                    Detail = new NewPropertyDetailDto
                    {
                        Id = pd.Id,
                        FloorId = pd.FloorId,
                        FloorCode = fl != null ? fl.FloorCode : null,
                        FloorDescription = fl != null ? fl.Description : null,
                        SubFloorId = pd.SubFloorId,
                        SubFloorCode = sf != null ? sf.SubFloorCode : null,
                        SubFloorDescription = sf != null ? sf.Description : null,
                        TypeOfUseId = pd.TypeOfUseId,
                        TypeOfUseCode = tou != null ? tou.TypeOfUseCode : null,
                        TypeOfUseDescription = tou != null ? tou.Description : null,
                        SubTypeOfUseId = pd.SubTypeOfUseId,
                        SubTypeOfUseDescription = stou != null ? stou.Description : null,
                        ConstructionTypeId = pd.ConstructionTypeId,
                        ConstructionCode = ct != null ? ct.ConstructionCode : null,
                        ConstructionTypeDescription = ct != null ? ct.Description : null,
                        ConstructionYear = pd.ConstructionYear,
                        AssessmentYear = pd.AssessmentYear,
                        CarpetAreaSqMeter = pd.CarpetAreaSqMeter,
                        CarpetAreaSqFeet = pd.CarpetAreaSqFeet,
                        BuiltupAreaSqMeter = pd.BuiltupAreaSqMeter,
                        BuiltupAreaSqFeet = pd.BuiltupAreaSqFeet,
                        NoOfRooms = pd.NoOfRooms,
                        IsRenter = pd.IsRenter,
                        IsTaxable = pd.IsTaxable,
                        IsOpenPlot = tou != null && tou.TypeOfUseCategory != null
                            && tou.TypeOfUseCategory!.TypeOfUseCategoryCode == TypeOfUseConstants.Op
                    }
                }
            ).ToListAsync(cancellationToken);

            newDetailsLookup = projected
                .GroupBy(x => x.PropertyId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Detail).ToList());
        }

        // ── Batch-fetch TransMast & TransMastOld ──────────────────
        var transMastLookup = new Dictionary<int, List<TransMastDto>>();
        var transMastOldLookup = new Dictionary<int, List<TransMastOldDto>>();

        if (sp != null)
        {
            if (newPropertyIds.Any())
            {
                var tmRepo = sp.GetRequiredService<IRepository<TransMastEntity, int>>();
                var tmList = await tmRepo.GetQueryable().AsNoTracking()
                    .Where(x => newPropertyIds.Contains(x.PropertyId) && x.IsActive && x.TaxId == 21)
                    .ToListAsync(cancellationToken);

                transMastLookup = tmList
                    .GroupBy(x => x.PropertyId)
                    .ToDictionary(g => g.Key, g => g.Select(x => new TransMastDto
                    {
                        Id = x.Id,
                        PropertyId = x.PropertyId,
                        FinanceYearId = x.FinanceYearId,
                        CalculationType = x.CalculationType,
                        CalculationValue = x.CalculationValue,
                        TaxId = x.TaxId,
                        TaxAmount = x.TaxAmount
                    }).ToList());
            }

            if (oldIds.Any())
            {
                var tmoRepo = sp.GetRequiredService<IRepository<TransMastOldEntity, int>>();
                var tmoList = await tmoRepo.GetQueryable().AsNoTracking()
                    .Where(x => oldIds.Contains(x.PropertyMastOldId) && x.IsActive)
                    .ToListAsync(cancellationToken);

                transMastOldLookup = tmoList
                    .GroupBy(x => x.PropertyMastOldId)
                    .ToDictionary(g => g.Key, g => g.Select(x => new TransMastOldDto
                    {
                        Id = x.Id,
                        PropertyMastOldId = x.PropertyMastOldId,
                        FinanceYearId = x.FinanceYearId,
                        CalculationType = x.CalculationType,
                        CalculationValue = (x.CalculationValue.HasValue ? x.CalculationValue.Value : 0),
                        TaxId = x.TaxId,
                        TaxAmount = x.TaxAmount
                    }).ToList());
            }
        }

        return rawItems.Select(x =>
        {
            var dto = x.Dto;
            dto.PropertyDetailsOld = (x.PropertyMastOldId.HasValue && detailsLookup.TryGetValue(x.PropertyMastOldId.Value, out var details)) ? details : new List<PropertyDetailsOldDto>();
            dto.NewPropertyDetails = (dto.PropertyId > 0 && newDetailsLookup.TryGetValue(dto.PropertyId, out var newDets)) ? newDets : new List<NewPropertyDetailDto>();
            dto.TransMastRecords = (dto.PropertyId > 0 && transMastLookup.TryGetValue(dto.PropertyId, out var tm)) ? tm : new List<TransMastDto>();
            dto.TransMastOldRecords = (x.PropertyMastOldId.HasValue && transMastOldLookup.TryGetValue(x.PropertyMastOldId.Value, out var tmo)) ? tmo : new List<TransMastOldDto>();
            return dto;
        }).ToList();
    }

    private static PropertyMapDetailReturnDto MapToReturnDto(PropertyEntity? pm, PropertyMapMasterEntity? pmm, PropertyMastOldEntity? pmo)
    {
        return new PropertyMapDetailReturnDto
        {
            PropertyId = pm?.Id ?? 0,
            MappingCategory = pmm != null ? pmm.MappingCategory : string.Empty,
            OldWardNo = pmo?.OldWardNo,
            OldPropertyNo = pmo?.OldPropertyNo,
            OldPartitionNo = pmo?.OldPartitionNo,
            OldEgovNo = pmo?.OldEgovNo,
            OldPropertyTypeId = pmo?.OldPropertyTypeId,
            OldALV = pmo?.OldALV,
            OldRV = pmo?.OldRV,
            OldGeneralTax = pmo?.OldGeneralTax,
            OldTotalTax = pmo?.OldTotalTax,
            OldZoneNo = pmo?.OldZoneNo,
            OldPlotNo = pmo?.OldPlotNo,
            OldCSN = pmo?.OldCSN,
            OldPlotArea = pmo?.OldPlotArea,
            OldConstructionYear = pmo?.OldConstructionYear,
            OldAssessmentYear = pmo?.OldAssessmentYear,
            OldFloor = pmo?.OldFloor,
            OldConstructionTypeOfUseId = pmo?.OldConstructionTypeOfUseId,
            OldUseType = pmo?.OldUseType,
            OldConstructionArea = pmo?.OldConstructionArea,
            OldOwnerName = pmo?.OldOwnerName,
            OldOccupierName = pmo?.OldOccupierName,
            OldAddress = pmo?.OldAddress,
            OldOwnerNameEnglish = pmo?.OldOwnerNameEnglish,
            OldOccupierNameEnglish = pmo?.OldOccupierNameEnglish,
            OldAddressEnglish = pmo?.OldAddressEnglish,
            NoOfOldToilets = pmo?.NoOfOldToilets,
            OldTotalRooms = pmo?.OldTotalRooms,
            OldSocietyName = pmo?.OldSocietyName,
            OldEmailId = pmo?.OldEmailId,
            OldParkingAreaSqFt = pmo?.OldParkingAreaSqFt,
            OldParkingAreaSqMtr = pmo?.OldParkingAreaSqMtr,
            OldAssessmentDate = pmo?.OldAssessmentDate,
            OldFlatOrShopNumber = pmo?.OldFlatOrShopNumber,
            OldWing = pmo?.OldWing,
            OldMobileNo = pmo?.OldMobileNo,
            NewPropertyInfo = pm != null ? new NewPropertyInfoDto
            {
                Id = pm.Id,
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                OwnerName = pm.OwnerName,
                OwnerNameEnglish = pm.OwnerNameEnglish,
                OccupierName = pm.OccupierName,
                OccupierNameEnglish = pm.OccupierNameEnglish,
                Address = pm.Address,
                AddressEnglish = pm.AddressEnglish,
                MobileNo = pm.MobileNo,
                EmailId = pm.EmailId,
                FlatOrShopName = pm.FlatOrShopName,
                FlatOrShopNo = pm.FlatOrShopNo,
                CSN = pm.CSN,
                PlotNo = pm.PlotNo,
                PropertyTypeId = pm.PropertyTypeId,
                WardId = pm.WardId,
                TaxZoneId = pm.TaxZoneId,
                CategoryId = pm.CategoryId
            } : null
        };
    }

    private static PropertyMapSocietyReturnDto MapToSocietyReturnDto(PropertyEntity pm, PropertyMapMasterEntity? pmm, PropertyMastOldEntity? pmo)
    {
        return new PropertyMapSocietyReturnDto
        {
            // New Property Info
            PropertyId = pm.Id,
            PropertyNo = pm.PropertyNo,
            PartitionNo = pm.PartitionNo,
            OwnerName = pm.OwnerName,
            OwnerNameEnglish = pm.OwnerNameEnglish,
            OccupierName = pm.OccupierName,
            OccupierNameEnglish = pm.OccupierNameEnglish,
            Address = pm.Address,
            AddressEnglish = pm.AddressEnglish,
            MobileNo = pm.MobileNo,
            EmailId = pm.EmailId,
            FlatOrShopName = pm.FlatOrShopName,
            FlatOrShopNo = pm.FlatOrShopNo,
            CSN = pm.CSN,
            PlotNo = pm.PlotNo,
            WardId = pm.WardId,
            TaxZoneId = pm.TaxZoneId,
            PropertyTypeId = pm.PropertyTypeId,
            CategoryId = pm.CategoryId,
            WingDetailId = pm.WingDetailId,

            // Mapping & Old Property Info
            MappingCategory = pmm != null ? pmm.MappingCategory : string.Empty,
            OldWardNo = pmo?.OldWardNo,
            OldPropertyNo = pmo?.OldPropertyNo,
            OldPartitionNo = pmo?.OldPartitionNo,
            OldEgovNo = pmo?.OldEgovNo,
            OldPropertyTypeId = pmo?.OldPropertyTypeId,
            OldALV = pmo?.OldALV,
            OldRV = pmo?.OldRV,
            OldGeneralTax = pmo?.OldGeneralTax,
            OldTotalTax = pmo?.OldTotalTax,
            OldZoneNo = pmo?.OldZoneNo,
            OldPlotNo = pmo?.OldPlotNo,
            OldCSN = pmo?.OldCSN,
            OldPlotArea = pmo?.OldPlotArea,
            OldConstructionYear = pmo?.OldConstructionYear,
            OldAssessmentYear = pmo?.OldAssessmentYear,
            OldFloor = pmo?.OldFloor,
            OldConstructionTypeOfUseId = pmo?.OldConstructionTypeOfUseId,
            OldUseType = pmo?.OldUseType,
            OldConstructionArea = pmo?.OldConstructionArea,
            OldOwnerName = pmo?.OldOwnerName,
            OldOccupierName = pmo?.OldOccupierName,
            OldAddress = pmo?.OldAddress,
            OldOwnerNameEnglish = pmo?.OldOwnerNameEnglish,
            OldOccupierNameEnglish = pmo?.OldOccupierNameEnglish,
            OldAddressEnglish = pmo?.OldAddressEnglish,
            NoOfOldToilets = pmo?.NoOfOldToilets,
            OldTotalRooms = pmo?.OldTotalRooms,
            OldSocietyName = pmo?.OldSocietyName,
            OldEmailId = pmo?.OldEmailId,
            OldParkingAreaSqFt = pmo?.OldParkingAreaSqFt,
            OldParkingAreaSqMtr = pmo?.OldParkingAreaSqMtr,
            OldAssessmentDate = pmo?.OldAssessmentDate,
            OldFlatOrShopNumber = pmo?.OldFlatOrShopNumber,
            OldWing = pmo?.OldWing,
            OldMobileNo = pmo?.OldMobileNo
        };
    }

    // -------------------------------------------------------------------------
    // New: multi-field search with match % + MappingDecision
    // -------------------------------------------------------------------------

    public async Task<PropertyMapSearchResultDto> SearchPropertyMappingsAsync(
        PropertyMapDetailQueryParameters q,
        CancellationToken cancellationToken = default)
    {
        bool hasSearchFields = !string.IsNullOrWhiteSpace(q.SearchTerm) ||
                               !string.IsNullOrWhiteSpace(q.OldOwnerName) ||
                               !string.IsNullOrWhiteSpace(q.OldOwnerNameEnglish) ||
                               !string.IsNullOrWhiteSpace(q.OldMobileNo) ||
                               !string.IsNullOrWhiteSpace(q.OldAddress) ||
                               !string.IsNullOrWhiteSpace(q.OldSocietyName) ||
                               !string.IsNullOrWhiteSpace(q.OldOccupierName) ||
                               !string.IsNullOrWhiteSpace(q.OldBuilderName) ||
                               !string.IsNullOrWhiteSpace(q.OldConstructionYear);

        if (!hasSearchFields && !q.PropertyId.HasValue)
        {
            return new PropertyMapSearchResultDto
            {
                OldPropertySuggestions = new List<OldPropertySuggestionDto>(),
                TotalCount = 0,
                PageNumber = q.PageNumber,
                PageSize = q.PageSize
            };
        }

        List<(int id, OldPropertyInfoDto dto)> oldRaw;
        int totalCount;

        if (_serviceProvider != null)
        {
            using var scope = _serviceProvider.CreateScope();
            var pmoRepo = scope.ServiceProvider.GetRequiredService<IRepository<PropertyMastOldEntity, int>>();

            (oldRaw, totalCount) = await FetchOldSuggestionsAsync(
                pmoRepo.GetQueryable().AsNoTracking(),
                q, cancellationToken);
        }
        else
        {
            (oldRaw, totalCount) = await FetchOldSuggestionsAsync(
                _propertyMastOldRepository.GetQueryable().AsNoTracking(),
                q, cancellationToken);
        }

        var oldIds = oldRaw.Select(x => x.id).ToList();
        var detailsLookup = new Dictionary<int, List<PropertyDetailsOldDto>>();
        var mappingLookup = new Dictionary<int, (int PropertyIdNew, string PropertyNoFormatted)>();
        var transMastOldLookup = new Dictionary<int, List<TransMastOldDto>>();

        if (oldIds.Any())
        {
            List<PropertyDetailsOldEntity> detailsEntities;
            if (_serviceProvider != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var pdRepo = scope.ServiceProvider.GetRequiredService<IRepository<PropertyDetailsOldEntity, int>>();
                detailsEntities = await pdRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(x => oldIds.Contains(x.PropertyMastOldId) && x.IsActive && !x.MarkedForDeletion)
                    .ToListAsync(cancellationToken);
            }
            else if (_propertyDetailsOldRepository != null)
            {
                detailsEntities = await _propertyDetailsOldRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(x => oldIds.Contains(x.PropertyMastOldId) && x.IsActive && !x.MarkedForDeletion)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                detailsEntities = new List<PropertyDetailsOldEntity>();
            }

            detailsLookup = detailsEntities
                .GroupBy(x => x.PropertyMastOldId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => _mapper.Map<PropertyDetailsOldDto>(x)).ToList()
                );

            if (_serviceProvider != null)
            {
                using var txScope = _serviceProvider.CreateScope();
                var tmoRepo = txScope.ServiceProvider.GetRequiredService<IRepository<TransMastOldEntity, int>>();
                var tmoList = await tmoRepo.GetQueryable().AsNoTracking()
                    .Where(x => oldIds.Contains(x.PropertyMastOldId) && x.IsActive)
                    .ToListAsync(cancellationToken);

                transMastOldLookup = tmoList
                    .GroupBy(x => x.PropertyMastOldId)
                    .ToDictionary(g => g.Key, g => g.Select(x => new TransMastOldDto
                    {
                        Id = x.Id,
                        PropertyMastOldId = x.PropertyMastOldId,
                        FinanceYearId = x.FinanceYearId,
                        CalculationType = x.CalculationType,
                        CalculationValue = (x.CalculationValue.HasValue ? x.CalculationValue.Value : 0),
                        TaxId = x.TaxId,
                        TaxAmount = x.TaxAmount
                    }).ToList());
            }

            // Fetch mapped new property info using the requested join logic!
            var pmdQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking();
            var pmQuery = _propertyRepository.GetQueryable().AsNoTracking();
            var pmmQuery = _repository.GetQueryable().AsNoTracking();
            var pmoQuery = _propertyMastOldRepository.GetQueryable().AsNoTracking();

            var joinQuery = from pmd in pmdQuery
                            join pm in pmQuery on pmd.PropertyIdNew equals (int?)pm.Id
                            join pmm in pmmQuery on pmd.PropertyMapId equals pmm.Id
                            join pmo in pmoQuery on pmd.PropertyIdOld equals (int?)pmo.Id
                            where pmd.IsActive && pm.IsActive && pmm.IsActive && pmo.IsActive
                            where pmd.PropertyIdOld.HasValue && oldIds.Contains(pmd.PropertyIdOld.Value)
                            select new
                            {
                                PropertyIdOld = pmd.PropertyIdOld.Value,
                                PropertyIdNew = pm.Id,
                                WardNo = pm.Ward != null ? pm.Ward.WardNo : string.Empty,
                                PropertyNo = pm.PropertyNo,
                                PartitionNo = pm.PartitionNo
                            };

            var mappedList = await joinQuery.ToListAsync(cancellationToken);

            foreach (var m in mappedList)
            {
                var wardNo = m.WardNo ?? string.Empty;
                var propertyNo = m.PropertyNo ?? string.Empty;
                var partitionNo = m.PartitionNo ?? string.Empty;

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(wardNo)) parts.Add(wardNo.Trim());
                if (!string.IsNullOrWhiteSpace(propertyNo)) parts.Add(propertyNo.Trim());

                var baseNo = string.Join("-", parts);
                if (!string.IsNullOrWhiteSpace(partitionNo))
                {
                    baseNo += "/" + partitionNo.Trim();
                }

                if (!string.IsNullOrWhiteSpace(baseNo))
                {
                    mappingLookup[m.PropertyIdOld] = (m.PropertyIdNew, baseNo);
                }
            }
        }

        var oldSuggestions = oldRaw.Select(x =>
        {
            var isMapped = mappingLookup.TryGetValue(x.id, out var mapInfo);

            return new OldPropertySuggestionDto
            {
                Id = x.dto.Id,
                OldPropertyNo = x.dto.OldPropertyNo,
                OldOwnerName = x.dto.OldOwnerName,
                OldOwnerNameEnglish = x.dto.OldOwnerNameEnglish,
                OldWardNo = x.dto.OldWardNo,
                OldEgovNo = x.dto.OldEgovNo,
                OldMobileNo = x.dto.OldMobileNo,
                OldPartitionNo = x.dto.OldPartitionNo,
                OldAddress = x.dto.OldAddress,
                OldAddressEnglish = x.dto.OldAddressEnglish,
                OldZoneNo = x.dto.OldZoneNo,
                OldPlotNo = x.dto.OldPlotNo,
                OldCSN = x.dto.OldCSN,
                OldALV = x.dto.OldALV,
                OldRV = x.dto.OldRV,
                OldGeneralTax = x.dto.OldGeneralTax,
                OldTotalTax = x.dto.OldTotalTax,
                OldPlotArea = x.dto.OldPlotArea,
                OldConstructionArea = x.dto.OldConstructionArea,
                OldFloor = x.dto.OldFloor,
                OldUseType = x.dto.OldUseType,
                OldOccupierName = x.dto.OldOccupierName,
                OldOccupierNameEnglish = x.dto.OldOccupierNameEnglish,
                OldSocietyName = x.dto.OldSocietyName,
                OldFlatOrShopNumber = x.dto.OldFlatOrShopNumber,
                OldWing = x.dto.OldWing,
                OldEmailId = x.dto.OldEmailId,
                OldParkingAreaSqFt = x.dto.OldParkingAreaSqFt,
                OldParkingAreaSqMtr = x.dto.OldParkingAreaSqMtr,
                OldPropertyTypeId = x.dto.OldPropertyTypeId,
                OldAssessmentYear = x.dto.OldAssessmentYear,
                OldConstructionYear = x.dto.OldConstructionYear,
                OldConstructionTypeOfUseId = x.dto.OldConstructionTypeOfUseId,
                NoOfOldToilets = x.dto.NoOfOldToilets,
                OldTotalRooms = x.dto.OldTotalRooms,
                OldAssessmentDate = x.dto.OldAssessmentDate,
                IsMapped = isMapped,
                MappedNewPropertyId = isMapped ? mapInfo.PropertyIdNew : null,
                MappedNewPropertyNo = isMapped ? mapInfo.PropertyNoFormatted : null,
                PropertyDetailsOld = detailsLookup.TryGetValue(x.id, out var details) ? details : new List<PropertyDetailsOldDto>(),
                TransMastOldRecords = transMastOldLookup.TryGetValue(x.id, out var tmo) ? tmo : new List<TransMastOldDto>()
            };
        }).ToList();

        return new PropertyMapSearchResultDto
        {
            OldPropertySuggestions = oldSuggestions,
            TotalCount = totalCount,
            PageNumber = q.PageNumber,
            PageSize = q.PageSize
        };
    }

    // -------------------------------------------------------------------------
    // Task B — old property suggestions (PropertyMastOld)
    // -------------------------------------------------------------------------

    private async Task<(List<(int id, OldPropertyInfoDto dto)> items, int totalCount)>
        FetchOldSuggestionsAsync(
            IQueryable<PropertyMastOldEntity> oldQuery,
            PropertyMapDetailQueryParameters q,
            CancellationToken ct)
    {
        var st = q.SearchTerm?.Trim();
        var ownerName = q.OldOwnerName?.Trim();
        var ownerNameEng = q.OldOwnerNameEnglish?.Trim();
        var mobileNo = q.OldMobileNo?.Trim();
        var address = q.OldAddress?.Trim();
        var societyName = q.OldSocietyName?.Trim();
        var occupierName = q.OldOccupierName?.Trim();
        var builderName = q.OldBuilderName?.Trim();
        var constrYear = q.OldConstructionYear?.Trim();

        bool hasSearchTerm = !string.IsNullOrWhiteSpace(st);
        bool hasOwnerName = !string.IsNullOrWhiteSpace(ownerName);
        bool hasOwnerNameEng = !string.IsNullOrWhiteSpace(ownerNameEng);
        bool hasMobileNo = !string.IsNullOrWhiteSpace(mobileNo);
        bool hasAddress = !string.IsNullOrWhiteSpace(address);
        bool hasSocietyName = !string.IsNullOrWhiteSpace(societyName);
        bool hasOccupierName = !string.IsNullOrWhiteSpace(occupierName);
        bool hasBuilderName = !string.IsNullOrWhiteSpace(builderName);
        bool hasConstrYear = !string.IsNullOrWhiteSpace(constrYear);

        if (!hasSearchTerm && !hasOwnerName && !hasOwnerNameEng && !hasMobileNo &&
            !hasAddress && !hasSocietyName && !hasOccupierName && !hasBuilderName && !hasConstrYear)
        {
            return (new List<(int id, OldPropertyInfoDto dto)>(), 0);
        }

        List<int> detailConstrYearMastIds = new();
        if (hasConstrYear)
        {
            if (_serviceProvider != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var pdRepo = scope.ServiceProvider.GetRequiredService<IRepository<PropertyDetailsOldEntity, int>>();
                detailConstrYearMastIds = await pdRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(x => x.IsActive && !x.MarkedForDeletion && x.OldConstructionYear != null && x.OldConstructionYear.Contains(constrYear!))
                    .Select(x => x.PropertyMastOldId)
                    .Distinct()
                    .ToListAsync(ct);
            }

        }

        var param = Expression.Parameter(typeof(PropertyMastOldEntity), "x");
        Expression? combinedOr = null;

        void AddOr(Expression clause)
        {
            combinedOr = combinedOr == null ? clause : Expression.OrElse(combinedOr, clause);
        }

        if (hasSearchTerm)
        {
            bool hasSpecial = st!.Contains('-') || st!.Contains('/');

            var containsWard = BuildContains(param, nameof(PropertyMastOldEntity.OldWardNo), st!);
            var containsProp = BuildContains(param, nameof(PropertyMastOldEntity.OldPropertyNo), st!);
            var containsPart = BuildContains(param, nameof(PropertyMastOldEntity.OldPartitionNo), st!);
            var containsOwner = BuildContains(param, nameof(PropertyMastOldEntity.OldOwnerName), st!);
            var containsOwnerEng = BuildContains(param, nameof(PropertyMastOldEntity.OldOwnerNameEnglish), st!);
            var containsMobile = BuildContains(param, nameof(PropertyMastOldEntity.OldMobileNo), st!);
            var containsAddr = BuildContains(param, nameof(PropertyMastOldEntity.OldAddress), st!);
            var containsAddrEng = BuildContains(param, nameof(PropertyMastOldEntity.OldAddressEnglish), st!);
            var containsSoc = BuildContains(param, nameof(PropertyMastOldEntity.OldSocietyName), st!);
            var containsOcc = BuildContains(param, nameof(PropertyMastOldEntity.OldOccupierName), st!);
            var containsEgov = BuildContains(param, nameof(PropertyMastOldEntity.OldEgovNo), st!);

            Expression stCombined = Expression.OrElse(containsWard, containsProp);
            stCombined = Expression.OrElse(stCombined, containsPart);
            if (hasSpecial)
            {
                var (tWard, tProp, tPart) = ParseSearchTokens(st!);
                if (!string.IsNullOrEmpty(tWard) && !string.IsNullOrEmpty(tProp))
                {
                    var wardAccess = Expression.Property(param, nameof(PropertyMastOldEntity.OldWardNo));
                    var wardEqual = Expression.Equal(wardAccess, Expression.Constant(tWard));

                    var propAccess = Expression.Property(param, nameof(PropertyMastOldEntity.OldPropertyNo));
                    var propNotNull = Expression.NotEqual(propAccess, Expression.Constant(null, typeof(string)));
                    var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
                    var propContains = Expression.Call(propAccess, containsMethod, Expression.Constant(tProp));
                    var propExpr = Expression.AndAlso(propNotNull, propContains);

                    Expression tokenMatch = Expression.AndAlso(wardEqual, propExpr);
                    if (!string.IsNullOrEmpty(tPart))
                    {
                        var partAccess = Expression.Property(param, nameof(PropertyMastOldEntity.OldPartitionNo));
                        var partNotNull = Expression.NotEqual(partAccess, Expression.Constant(null, typeof(string)));
                        var partContains = Expression.Call(partAccess, containsMethod, Expression.Constant(tPart));
                        var partExpr = Expression.AndAlso(partNotNull, partContains);
                        tokenMatch = Expression.AndAlso(tokenMatch, partExpr);
                    }

                    stCombined = Expression.OrElse(stCombined, tokenMatch);
                }
            }
            stCombined = Expression.OrElse(stCombined, containsOwner);
            stCombined = Expression.OrElse(stCombined, containsOwnerEng);
            stCombined = Expression.OrElse(stCombined, containsMobile);
            stCombined = Expression.OrElse(stCombined, containsAddr);
            stCombined = Expression.OrElse(stCombined, containsAddrEng);
            stCombined = Expression.OrElse(stCombined, containsSoc);
            stCombined = Expression.OrElse(stCombined, containsOcc);
            stCombined = Expression.OrElse(stCombined, containsEgov);

            AddOr(stCombined);
        }

        if (hasOwnerName)
            AddOr(BuildContains(param, nameof(PropertyMastOldEntity.OldOwnerName), ownerName!));

        if (hasOwnerNameEng)
            AddOr(BuildContains(param, nameof(PropertyMastOldEntity.OldOwnerNameEnglish), ownerNameEng!));

        if (hasMobileNo)
            AddOr(BuildContains(param, nameof(PropertyMastOldEntity.OldMobileNo), mobileNo!));

        if (hasAddress)
        {
            var containsAddr = BuildContains(param, nameof(PropertyMastOldEntity.OldAddress), address!);
            var containsAddrEng = BuildContains(param, nameof(PropertyMastOldEntity.OldAddressEnglish), address!);
            AddOr(Expression.OrElse(containsAddr, containsAddrEng));
        }

        if (hasSocietyName)
            AddOr(BuildContains(param, nameof(PropertyMastOldEntity.OldSocietyName), societyName!));

        if (hasOccupierName)
        {
            var containsOcc = BuildContains(param, nameof(PropertyMastOldEntity.OldOccupierName), occupierName!);
            var containsOccEng = BuildContains(param, nameof(PropertyMastOldEntity.OldOccupierNameEnglish), occupierName!);
            AddOr(Expression.OrElse(containsOcc, containsOccEng));
        }

        if (hasBuilderName)
            AddOr(BuildContains(param, nameof(PropertyMastOldEntity.OldSocietyName), builderName!));

        if (hasConstrYear)
        {
            var containsYear = BuildContains(param, nameof(PropertyMastOldEntity.OldConstructionYear), constrYear!);
            if (detailConstrYearMastIds.Any())
            {
                var idProp = Expression.Property(param, nameof(PropertyMastOldEntity.Id));
                var containsInList = Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Contains),
                    new[] { typeof(int) },
                    Expression.Constant(detailConstrYearMastIds),
                    idProp
                );
                AddOr(Expression.OrElse(containsYear, containsInList));
            }
            else
            {
                AddOr(containsYear);
            }
        }

        if (combinedOr == null)
            return (new List<(int id, OldPropertyInfoDto dto)>(), 0);

        var lambda = Expression.Lambda<Func<PropertyMastOldEntity, bool>>(combinedOr, param);

        var query = oldQuery.Where(x => x.IsActive).Where(lambda);

        int totalCount = await query.CountAsync(ct);

        if (totalCount == 0)
            return (new List<(int id, OldPropertyInfoDto dto)>(), 0);

        int pageNumber = q.PageNumber < 1 ? 1 : q.PageNumber;
        int pageSize = q.PageSize < 1 ? 10 : q.PageSize;
        int skip = (pageNumber - 1) * pageSize;

        List<PropertyMastOldEntity> entities;
        var searchTermOnly = hasSearchTerm
                             && !hasOwnerName
                             && !hasOwnerNameEng
                             && !hasMobileNo
                             && !hasAddress
                             && !hasSocietyName
                             && !hasOccupierName
                             && !hasBuilderName
                             && !hasConstrYear;

        query = searchTermOnly
            ? query
                .OrderByDescending(x =>
                    ((x.OldWardNo ?? "") + "-" + (x.OldPropertyNo ?? "") + "-" + (x.OldPartitionNo ?? "")) == st ||
                    ((x.OldWardNo ?? "") + "-" + (x.OldPropertyNo ?? "") + "/" + (x.OldPartitionNo ?? "")) == st ||
                    x.OldPropertyNo == st ||
                    x.OldPartitionNo == st ||
                    x.OldEgovNo == st)
                .ThenBy(x => x.Id)
            : query
                .OrderByDescending(x =>
                    // Tier 1: Exact Property No / Ward / Composite Code Match (Highest Priority: 1000 pts)
                    (hasSearchTerm && (
                        ((x.OldWardNo ?? "") + "-" + (x.OldPropertyNo ?? "") + "-" + (x.OldPartitionNo ?? "")) == st ||
                        ((x.OldWardNo ?? "") + "-" + (x.OldPropertyNo ?? "") + "/" + (x.OldPartitionNo ?? "")) == st ||
                        x.OldPropertyNo == st ||
                        x.OldPartitionNo == st ||
                        x.OldEgovNo == st
                    ) ? 1000 : 0)

                    // Tier 2: Exact Parameter Matches (500 pts each)
                    + (hasOwnerName && x.OldOwnerName == ownerName ? 500 : 0)
                    + (hasOwnerNameEng && x.OldOwnerNameEnglish == ownerNameEng ? 500 : 0)
                    + (hasMobileNo && x.OldMobileNo == mobileNo ? 500 : 0)

                    // Tier 3: Unified Search Term Partial Matches (200 pts each)
                    + (hasSearchTerm && x.OldOwnerName != null && x.OldOwnerName.Contains(st!) ? 200 : 0)
                    + (hasSearchTerm && x.OldOwnerNameEnglish != null && x.OldOwnerNameEnglish.Contains(st!) ? 200 : 0)
                    + (hasSearchTerm && x.OldMobileNo != null && x.OldMobileNo.Contains(st!) ? 200 : 0)

                    // Tier 4: Specific Parameter Partial Matches (100 pts each)
                    + (hasOwnerName && x.OldOwnerName != null && x.OldOwnerName.Contains(ownerName!) ? 100 : 0)
                    + (hasOwnerNameEng && x.OldOwnerNameEnglish != null && x.OldOwnerNameEnglish.Contains(ownerNameEng!) ? 100 : 0)
                    + (hasMobileNo && x.OldMobileNo != null && x.OldMobileNo.Contains(mobileNo!) ? 100 : 0)
                    + (hasAddress && x.OldAddress != null && x.OldAddress.Contains(address!) ? 50 : 0)
                    + (hasSocietyName && x.OldSocietyName != null && x.OldSocietyName.Contains(societyName!) ? 50 : 0)
                    + (hasOccupierName && x.OldOccupierName != null && x.OldOccupierName.Contains(occupierName!) ? 50 : 0)
                )
                .ThenBy(x => x.Id);

        entities = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = entities.Select(e => (
            e.Id,
            dto: _mapper.Map<OldPropertyInfoDto>(e)
        )).ToList();

        return (items, totalCount);
    }

    private static (string? ward, string? prop, string? part) ParseSearchTokens(string st)
    {
        if (string.IsNullOrWhiteSpace(st) || !st.Contains('-'))
            return (null, null, null);

        var firstHyphen = st.IndexOf('-');
        var ward = st.Substring(0, firstHyphen).Trim();
        var remainder = st.Substring(firstHyphen + 1).Trim();

        if (string.IsNullOrWhiteSpace(ward) || string.IsNullOrWhiteSpace(remainder))
            return (null, null, null);

        var lastSep = Math.Max(remainder.LastIndexOf('-'), remainder.LastIndexOf('/'));
        if (lastSep > 0 && lastSep < remainder.Length - 1)
        {
            var prop = remainder.Substring(0, lastSep).Trim();
            var part = remainder.Substring(lastSep + 1).Trim();
            return (ward, prop, part);
        }

        return (ward, remainder, null);
    }

    private static readonly MethodInfo StringConcat2Method = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) })!;

    private static Expression ConcatStrings(params Expression[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            return Expression.Constant("");
        if (expressions.Length == 1)
            return expressions[0];

        Expression result = expressions[0];
        for (int i = 1; i < expressions.Length; i++)
        {
            result = Expression.Add(result, expressions[i], StringConcat2Method);
        }
        return result;
    }

    private static Expression BuildContains(ParameterExpression param, string propertyName, string value)
    {
        var propAccess = Expression.Property(param, propertyName);
        var notNull = Expression.NotEqual(propAccess, Expression.Constant(null, typeof(string)));
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        var containsCall = Expression.Call(propAccess, containsMethod, Expression.Constant(value));
        return Expression.AndAlso(notNull, containsCall);
    }
}
