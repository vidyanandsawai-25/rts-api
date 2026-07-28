using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
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
        var pmdQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking();
        var pmQuery = _propertyRepository.GetQueryable().AsNoTracking();
        var pmmQuery = _repository.GetQueryable().AsNoTracking();
        var pmoQuery = _propertyMastOldRepository.GetQueryable().AsNoTracking();

        var rawQuery = from pmd in pmdQuery
                       join pm in pmQuery on pmd.PropertyIdNew equals (int?)pm.Id
                       join pmm in pmmQuery on pmd.PropertyMapId equals pmm.Id
                       join pmo in pmoQuery on pmd.PropertyIdOld equals (int?)pmo.Id
                       where pmd.IsActive && pm.IsActive && pmm.IsActive && pmo.IsActive
                       select new { pmd.PropertyIdNew, pmd.PropertyIdOld, pm, pmm, pmo };

        if (queryParams.PropertyId.HasValue)
        {
            rawQuery = rawQuery.Where(x => x.pm.Id == queryParams.PropertyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var st = queryParams.SearchTerm.Trim();
            bool hasHyphen = st.Contains('-');

            rawQuery = rawQuery.Where(x =>
                (x.pm.PropertyNo != null && x.pm.PropertyNo.Contains(st)) ||
                (x.pmo.OldWardNo != null && x.pmo.OldWardNo.Contains(st)) ||
                (x.pmo.OldPropertyNo != null && x.pmo.OldPropertyNo.Contains(st)) ||
                (x.pmo.OldPartitionNo != null && x.pmo.OldPartitionNo.Contains(st)) ||
                (hasHyphen && (((x.pmo.OldWardNo ?? "") + "-" + (x.pmo.OldPropertyNo ?? "") + "-" + (x.pmo.OldPartitionNo ?? "")).Contains(st))) ||
                (x.pm.OwnerName != null && x.pm.OwnerName.Contains(st)) ||
                (x.pmo.OldOwnerName != null && x.pmo.OldOwnerName.Contains(st)) ||
                (x.pm.OwnerNameEnglish != null && x.pm.OwnerNameEnglish.Contains(st)) ||
                (x.pmo.OldOwnerNameEnglish != null && x.pmo.OldOwnerNameEnglish.Contains(st)) ||
                (x.pm.MobileNo != null && x.pm.MobileNo.Contains(st)) ||
                (x.pmo.OldMobileNo != null && x.pmo.OldMobileNo.Contains(st)) ||
                (x.pm.Address != null && x.pm.Address.Contains(st)) ||
                (x.pmo.OldAddress != null && x.pmo.OldAddress.Contains(st)) ||
                (x.pm.AddressEnglish != null && x.pm.AddressEnglish.Contains(st)) ||
                (x.pmo.OldAddressEnglish != null && x.pmo.OldAddressEnglish.Contains(st)) ||
                (x.pmo.OldSocietyName != null && x.pmo.OldSocietyName.Contains(st)) ||
                (x.pm.FlatOrShopName != null && x.pm.FlatOrShopName.Contains(st)) ||
                (x.pm.OccupierName != null && x.pm.OccupierName.Contains(st)) ||
                (x.pmo.OldOccupierName != null && x.pmo.OldOccupierName.Contains(st))
            );
        }

        int totalCount = await rawQuery.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(queryParams.SortBy))
        {
            bool isDesc = string.Equals(queryParams.SortOrder, "desc", System.StringComparison.OrdinalIgnoreCase);
            switch (queryParams.SortBy.ToLowerInvariant())
            {
                case "propertyid":
                    rawQuery = isDesc ? rawQuery.OrderByDescending(x => x.pm.Id) : rawQuery.OrderBy(x => x.pm.Id);
                    break;
                case "mappingcategory":
                    rawQuery = isDesc ? rawQuery.OrderByDescending(x => x.pmm.MappingCategory) : rawQuery.OrderBy(x => x.pmm.MappingCategory);
                    break;
                case "oldpropertyno":
                    rawQuery = isDesc ? rawQuery.OrderByDescending(x => x.pmo.OldPropertyNo) : rawQuery.OrderBy(x => x.pmo.OldPropertyNo);
                    break;
                case "oldownername":
                    rawQuery = isDesc ? rawQuery.OrderByDescending(x => x.pmo.OldOwnerName) : rawQuery.OrderBy(x => x.pmo.OldOwnerName);
                    break;
                default:
                    rawQuery = rawQuery.OrderBy(x => x.pm.Id);
                    break;
            }
        }
        else
        {
            rawQuery = rawQuery.OrderBy(x => x.pm.Id);
        }

        var pagedQuery = rawQuery;
        if (queryParams.PageSize != -1)
        {
            pagedQuery = rawQuery.Skip((queryParams.PageNumber - 1) * queryParams.PageSize).Take(queryParams.PageSize);
        }

        var rawItems = await pagedQuery
            .Select(x => new
            {
                PropertyMastOldId = x.pmo.Id,
                Dto = new PropertyMapDetailReturnDto
                {
                    PropertyId = x.pm.Id,
                    MappingCategory = x.pmm.MappingCategory,
                    OldWardNo = x.pmo.OldWardNo,
                    OldPropertyNo = x.pmo.OldPropertyNo,
                    OldPartitionNo = x.pmo.OldPartitionNo,
                    OldEgovNo = x.pmo.OldEgovNo,
                    OldPropertyTypeId = x.pmo.OldPropertyTypeId,
                    OldALV = x.pmo.OldALV,
                    OldRV = x.pmo.OldRV,
                    OldGeneralTax = x.pmo.OldGeneralTax,
                    OldTotalTax = x.pmo.OldTotalTax,
                    OldZoneNo = x.pmo.OldZoneNo,
                    OldPlotNo = x.pmo.OldPlotNo,
                    OldCSN = x.pmo.OldCSN,
                    OldPlotArea = x.pmo.OldPlotArea,
                    OldConstructionYear = x.pmo.OldConstructionYear,
                    OldAssessmentYear = x.pmo.OldAssessmentYear,
                    OldFloor = x.pmo.OldFloor,
                    OldConstructionTypeOfUseId = x.pmo.OldConstructionTypeOfUseId,
                    OldUseType = x.pmo.OldUseType,
                    OldConstructionArea = x.pmo.OldConstructionArea,
                    OldOwnerName = x.pmo.OldOwnerName,
                    OldOccupierName = x.pmo.OldOccupierName,
                    OldAddress = x.pmo.OldAddress,
                    OldOwnerNameEnglish = x.pmo.OldOwnerNameEnglish,
                    OldOccupierNameEnglish = x.pmo.OldOccupierNameEnglish,
                    OldAddressEnglish = x.pmo.OldAddressEnglish,
                    NoOfOldToilets = x.pmo.NoOfOldToilets,
                    OldTotalRooms = x.pmo.OldTotalRooms,
                    OldSocietyName = x.pmo.OldSocietyName,
                    OldEmailId = x.pmo.OldEmailId,
                    OldParkingAreaSqFt = x.pmo.OldParkingAreaSqFt,
                    OldParkingAreaSqMtr = x.pmo.OldParkingAreaSqMtr,
                    OldAssessmentDate = x.pmo.OldAssessmentDate,
                    OldFlatOrShopNumber = x.pmo.OldFlatOrShopNumber,
                    OldWing = x.pmo.OldWing,
                    OldMobileNo = x.pmo.OldMobileNo,
                    NewPropertyInfo = new NewPropertyInfoDto
                    {
                        Id = x.pm.Id,
                        PropertyNo = x.pm.PropertyNo,
                        PartitionNo = x.pm.PartitionNo,
                        OwnerName = x.pm.OwnerName,
                        OwnerNameEnglish = x.pm.OwnerNameEnglish,
                        OccupierName = x.pm.OccupierName,
                        OccupierNameEnglish = x.pm.OccupierNameEnglish,
                        Address = x.pm.Address,
                        AddressEnglish = x.pm.AddressEnglish,
                        MobileNo = x.pm.MobileNo,
                        EmailId = x.pm.EmailId,
                        FlatOrShopName = x.pm.FlatOrShopName,
                        FlatOrShopNo = x.pm.FlatOrShopNo,
                        CSN = x.pm.CSN,
                        PlotNo = x.pm.PlotNo,
                        PropertyTypeId = x.pm.PropertyTypeId,
                        WardId = x.pm.WardId,
                        TaxZoneId = x.pm.TaxZoneId,
                        CategoryId = x.pm.CategoryId
                    }
                }
            })
            .ToListAsync(cancellationToken);

        if (!rawItems.Any())
        {
            return new PagedResult<PropertyMapDetailReturnDto>(new List<PropertyMapDetailReturnDto>(), totalCount, queryParams.PageNumber, queryParams.PageSize);
        }

        using var scope = _serviceProvider?.CreateScope();
        var sp = scope?.ServiceProvider;

        // Fetch master lookup data ONLY for the paged result items
        var wardIds = rawItems.Select(x => x.Dto.NewPropertyInfo!.WardId).Distinct().ToList();
        var taxZoneIds = rawItems.Select(x => x.Dto.NewPropertyInfo!.TaxZoneId).Distinct().ToList();
        var propTypeIds = rawItems.Select(x => x.Dto.NewPropertyInfo!.PropertyTypeId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var categoryIds = rawItems.Select(x => x.Dto.NewPropertyInfo!.CategoryId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

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

        var oldIds = rawItems.Select(x => x.PropertyMastOldId).Distinct().ToList();
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
        var newPropertyIds = rawItems.Select(x => x.Dto.PropertyId).Distinct().ToList();
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
                        IsOpenPlot = pd.IsOpenPlot
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
                        CalculationValue = x.CalculationValue,
                        TaxId = x.TaxId,
                        TaxAmount = x.TaxAmount
                    }).ToList());
            }
        }

        var items = rawItems.Select(x =>
        {
            var dto = x.Dto;
            dto.PropertyDetailsOld = detailsLookup.TryGetValue(x.PropertyMastOldId, out var details) ? details : new List<PropertyDetailsOldDto>();
            dto.NewPropertyDetails = newDetailsLookup.TryGetValue(x.Dto.PropertyId, out var newDets) ? newDets : new List<NewPropertyDetailDto>();
            dto.TransMastRecords = transMastLookup.TryGetValue(x.Dto.PropertyId, out var tm) ? tm : new List<TransMastDto>();
            dto.TransMastOldRecords = transMastOldLookup.TryGetValue(x.PropertyMastOldId, out var tmo) ? tmo : new List<TransMastOldDto>();
            return dto;
        }).ToList();

        return new PagedResult<PropertyMapDetailReturnDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    // -------------------------------------------------------------------------
    // New: multi-field search with match % + MappingDecision
    // -------------------------------------------------------------------------

    public async Task<PropertyMapSearchResultDto> SearchPropertyMappingsAsync(
        PropertyMapDetailQueryParameters q,
        CancellationToken cancellationToken = default)
    {
        bool hasSearchFields = !string.IsNullOrWhiteSpace(q.SearchTerm);

        if (!hasSearchFields && !q.PropertyId.HasValue)
        {
            return new PropertyMapSearchResultDto
            {
                OldPropertySuggestions = new List<OldPropertySuggestionDto>()
            };
        }

        List<(int id, OldPropertyInfoDto dto)> oldRaw;

        if (_serviceProvider != null)
        {
            using var scope = _serviceProvider.CreateScope();
            var pmoRepo = scope.ServiceProvider.GetRequiredService<IRepository<PropertyMastOldEntity, int>>();

            oldRaw = await FetchOldSuggestionsAsync(
                pmoRepo.GetQueryable().AsNoTracking(),
                q, cancellationToken);
        }
        else
        {
            oldRaw = await FetchOldSuggestionsAsync(
                _propertyMastOldRepository.GetQueryable().AsNoTracking(),
                q, cancellationToken);
        }

        var oldIds = oldRaw.Select(x => x.id).ToList();
        var detailsLookup = new Dictionary<int, List<PropertyDetailsOldDto>>();
        var mappingLookup = new Dictionary<int, string>();
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
                        CalculationValue = x.CalculationValue,
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
                    mappingLookup[m.PropertyIdOld] = baseNo;
                }
            }
        }

        var oldSuggestions = oldRaw.Select(x => new OldPropertySuggestionDto
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
            IsMapped = mappingLookup.ContainsKey(x.id),
            MappedNewPropertyNo = mappingLookup.TryGetValue(x.id, out var newPropNo) ? newPropNo : null,
            PropertyDetailsOld = detailsLookup.TryGetValue(x.id, out var details) ? details : new List<PropertyDetailsOldDto>(),
            TransMastOldRecords = transMastOldLookup.TryGetValue(x.id, out var tmo) ? tmo : new List<TransMastOldDto>()
        }).ToList();

        return new PropertyMapSearchResultDto
        {
            OldPropertySuggestions = oldSuggestions
        };
    }

    // -------------------------------------------------------------------------
    // Task B — old property suggestions (PropertyMastOld)
    // -------------------------------------------------------------------------

    private async Task<List<(int id, OldPropertyInfoDto dto)>>
        FetchOldSuggestionsAsync(
            IQueryable<PropertyMastOldEntity> oldQuery,
            PropertyMapDetailQueryParameters q,
            CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.SearchTerm))
            return new List<(int id, OldPropertyInfoDto dto)>();

        var query = oldQuery.Where(x => x.IsActive);

        var st = q.SearchTerm.Trim();
        bool hasHyphen = st.Contains('-');

        query = query.Where(x =>
            (x.OldWardNo != null && x.OldWardNo.Contains(st)) ||
            (x.OldPropertyNo != null && x.OldPropertyNo.Contains(st)) ||
            (x.OldPartitionNo != null && x.OldPartitionNo.Contains(st)) ||
            (hasHyphen && (((x.OldWardNo ?? "") + "-" + (x.OldPropertyNo ?? "") + "-" + (x.OldPartitionNo ?? "")).Contains(st))) ||
            (x.OldOwnerName != null && x.OldOwnerName.Contains(st)) ||
            (x.OldOwnerNameEnglish != null && x.OldOwnerNameEnglish.Contains(st)) ||
            (x.OldMobileNo != null && x.OldMobileNo.Contains(st)) ||
            (x.OldAddress != null && x.OldAddress.Contains(st)) ||
            (x.OldAddressEnglish != null && x.OldAddressEnglish.Contains(st)) ||
            (x.OldSocietyName != null && x.OldSocietyName.Contains(st)) ||
            (x.OldOccupierName != null && x.OldOccupierName.Contains(st)) ||
            (x.OldEgovNo != null && x.OldEgovNo.Contains(st))
        );

        var entities = await query
            .OrderBy(x => x.Id)
            .Take(20)
            .ToListAsync(ct);

        if (!entities.Any())
            return new List<(int id, OldPropertyInfoDto dto)>();

        return entities.Select(e => (
            e.Id,
            dto: _mapper.Map<OldPropertyInfoDto>(e)
        )).ToList();
    }
}