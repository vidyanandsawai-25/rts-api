using System.Linq.Expressions;
using System.Reflection;
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
        var pmQuery = _propertyRepository.GetQueryable().AsNoTracking().Where(x => x.IsActive);

        if (queryParams.PropertyId.HasValue)
        {
            pmQuery = pmQuery.Where(x => x.Id == queryParams.PropertyId.Value);
        }

        int totalCount = await pmQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResult<PropertyMapDetailReturnDto>(new List<PropertyMapDetailReturnDto>(), 0, queryParams.PageNumber, queryParams.PageSize);
        }

        IQueryable<PropertyEntity> pagedPmQuery = pmQuery.OrderBy(x => x.Id);
        if (queryParams.PageSize != -1)
        {
            pagedPmQuery = pagedPmQuery.Skip((queryParams.PageNumber - 1) * queryParams.PageSize).Take(queryParams.PageSize);
        }

        var pagedPms = await pagedPmQuery.ToListAsync(cancellationToken);
        var pmIds = pagedPms.Select(x => x.Id).ToList();

        var pmds = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
            .Where(x => x.PropertyIdNew.HasValue && pmIds.Contains(x.PropertyIdNew.Value) && x.IsActive)
            .Select(x => new
            {
                x.PropertyMapId,
                x.PropertyIdNew,
                x.PropertyIdOld
            })
            .ToListAsync(cancellationToken);
        var pmdMap = pmds.GroupBy(x => x.PropertyIdNew!.Value).ToDictionary(g => g.Key, g => g.First());

        var pmmIds = pmds.Select(x => x.PropertyMapId).Distinct().ToList();
        var pmmMap = pmmIds.Any()
            ? (await _repository.GetQueryable().AsNoTracking().Where(x => pmmIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken)).ToDictionary(x => x.Id)
            : new Dictionary<int, PropertyMapMasterEntity>();

        var pmoIds = pmds.Select(x => x.PropertyIdOld).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var pmoMap = pmoIds.Any()
            ? (await _propertyMastOldRepository.GetQueryable().AsNoTracking().Where(x => pmoIds.Contains(x.Id) && x.IsActive).ToListAsync(cancellationToken)).ToDictionary(x => x.Id)
            : new Dictionary<int, PropertyMastOldEntity>();

        var rawItems = pagedPms.Select(pm =>
        {
            pmdMap.TryGetValue(pm.Id, out var pmd);
            PropertyMapMasterEntity? pmm = (pmd != null && pmmMap.TryGetValue(pmd.PropertyMapId, out var m)) ? m : null;
            PropertyMastOldEntity? pmo = (pmd != null && pmd.PropertyIdOld.HasValue && pmoMap.TryGetValue(pmd.PropertyIdOld.Value, out var o)) ? o : null;

            return new
            {
                PropertyMastOldId = pmo != null ? (int?)pmo.Id : null,
                Dto = MapToReturnDto(pm, pmm, pmo)
            };
        }).ToList();

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
                        CalculationValue = (x.CalculationValue.HasValue ? x.CalculationValue.Value : 0),
                        TaxId = x.TaxId,
                        TaxAmount = x.TaxAmount
                    }).ToList());
            }
        }

        var items = rawItems.Select(x =>
        {
            var dto = x.Dto;
            dto.PropertyDetailsOld = (x.PropertyMastOldId.HasValue && detailsLookup.TryGetValue(x.PropertyMastOldId.Value, out var details)) ? details : new List<PropertyDetailsOldDto>();
            dto.NewPropertyDetails = newDetailsLookup.TryGetValue(x.Dto.PropertyId, out var newDets) ? newDets : new List<NewPropertyDetailDto>();
            dto.TransMastRecords = transMastLookup.TryGetValue(x.Dto.PropertyId, out var tm) ? tm : new List<TransMastDto>();
            dto.TransMastOldRecords = (x.PropertyMastOldId.HasValue && transMastOldLookup.TryGetValue(x.PropertyMastOldId.Value, out var tmo)) ? tmo : new List<TransMastOldDto>();
            return dto;
        }).ToList();

        return new PagedResult<PropertyMapDetailReturnDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    private static PropertyMapDetailReturnDto MapToReturnDto(PropertyEntity pm, PropertyMapMasterEntity? pmm, PropertyMastOldEntity? pmo)
    {
        return new PropertyMapDetailReturnDto
        {
            PropertyId = pm.Id,
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
            NewPropertyInfo = new NewPropertyInfoDto
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
            }
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
            return new List<(int id, OldPropertyInfoDto dto)>();
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
            return new List<(int id, OldPropertyInfoDto dto)>();

        var lambda = Expression.Lambda<Func<PropertyMastOldEntity, bool>>(combinedOr, param);

        var query = oldQuery.Where(x => x.IsActive).Where(lambda);

        var entities = await query
            .OrderBy(x => x.Id)
            .Take(50)
            .ToListAsync(ct);

        if (!entities.Any())
            return new List<(int id, OldPropertyInfoDto dto)>();

        if (hasSearchTerm)
        {
            entities = entities
                .OrderByDescending(x =>
                    ((x.OldWardNo ?? "") + "-" + (x.OldPropertyNo ?? "") + "-" + (x.OldPartitionNo ?? "")) == st ||
                    ((x.OldWardNo ?? "") + "-" + (x.OldPropertyNo ?? "") + "/" + (x.OldPartitionNo ?? "")) == st ||
                    x.OldPartitionNo == st)
                .ThenBy(x => x.Id)
                .Take(20)
                .ToList();
        }
        else
        {
            entities = entities.Take(20).ToList();
        }

        return entities.Select(e => (
            e.Id,
            dto: _mapper.Map<OldPropertyInfoDto>(e)
        )).ToList();
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
