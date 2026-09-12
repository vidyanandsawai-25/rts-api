using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class SocietyDetailsService : BaseCommonCrudService<SocietyDetailsEntity, SocietyDetailsDto, CreateSocietyDetailsDto, UpdateSocietyDetailsDto, SocietyDetailsQueryParameters, int>, ISocietyDetailsService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<PropertyEntity, int>? _propertyRepository;
    private readonly IRepository<WardEntity, int>? _wardRepository;
    private readonly IRepository<SocietyWingDetailsEntity, int>? _societyWingRepository;
    private readonly IRepository<WingDetailsMastEntity, int>? _wingDetailsMasterRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int>? _propertyTypeRepository;

    public SocietyDetailsService(
        IRepository<SocietyDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator,
        IRepository<PropertyEntity, int>? propertyRepository = null,
        IRepository<WardEntity, int>? wardRepository = null,
        IRepository<SocietyWingDetailsEntity, int>? societyWingRepository = null,
        IRepository<WingDetailsMastEntity, int>? wingDetailsMasterRepository = null,
        IRepository<PropertyTypeMasterEntity, int>? propertyTypeRepository = null)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _propertyRepository = propertyRepository;
        _wardRepository = wardRepository;
        _societyWingRepository = societyWingRepository;
        _wingDetailsMasterRepository = wingDetailsMasterRepository;
        _propertyTypeRepository = propertyTypeRepository;
    }

    public async Task<SocietySummaryDto?> GetSocietySummaryAsync(
        SocietySummaryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.WardNo) ||
            string.IsNullOrWhiteSpace(request.PropertyNo) ||
            _propertyRepository == null ||
            _wardRepository == null ||
            _societyWingRepository == null)
        {
            return null;
        }

        var wardNoTrim = request.WardNo.Trim();
        var propertyNoTrim = request.PropertyNo.Trim();
        var partitionNoTrim = request.PartitionNo?.Trim();
        var isPartitionEmpty = string.IsNullOrWhiteSpace(partitionNoTrim);

        var targetProperty = await (
            from property in _propertyRepository.GetQueryable().AsNoTracking()
            join ward in _wardRepository.GetQueryable().AsNoTracking().Where(w => w.IsActive)
                on property.WardId equals ward.Id
            where ward.WardNo == wardNoTrim
               && property.PropertyNo == propertyNoTrim
               && property.IsActive
               && !property.MarkedForDeletion
               && (isPartitionEmpty
                   ? string.IsNullOrWhiteSpace(property.PartitionNo)
                   : property.PartitionNo == partitionNoTrim)
            select new { PropertyId = property.Id, WardId = property.WardId }
        ).FirstOrDefaultAsync(cancellationToken);

        if (targetProperty == null)
        {
            return null;
        }

        var targetPropertyId = targetProperty.PropertyId;
        var targetWardId = targetProperty.WardId;

        var societyDetailsList = await _repository.GetQueryable()
            .AsNoTracking()
            .Where(s => s.PropertyId.HasValue
                     && s.PropertyId.Value == targetPropertyId
                     && s.IsActive
                     && !s.MarkedForDeletion)
            .Select(s => new
            {
                s.Id,
                s.SocietyName,
                s.BuilderName,
                s.SocietyAddress
            })
            .ToListAsync(cancellationToken);

        var societyDetailIds = societyDetailsList.Select(s => s.Id).ToList();

        var wingList = await _societyWingRepository.GetQueryable()
            .AsNoTracking()
            .Where(w => w.IsActive &&
                ((w.PropertyId.HasValue && w.PropertyId.Value == targetPropertyId) ||
                 (w.SocietyDetailId.HasValue && societyDetailIds.Contains(w.SocietyDetailId.Value))))
            .Select(w => new
            {
                w.Id,
                w.SocietyDetailId,
                w.WingDetailsMastId,
                NoOfRowHouse = w.NoOfRowHouse ?? 0
            })
            .ToListAsync(cancellationToken);

        var wingDetailIds = wingList
            .Select(w => w.WingDetailsMastId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // Calculate NoOfShop and NoOfFlat from PTIS.PropertyMast (PropertyEntity) joined with PTIS.PropertyTypeMaster based on WingDetailId or WardId/PropertyNo:
        // SUM(CASE WHEN UPPER(ptm.[Type]) = 'C' THEN 1 ELSE 0 END) AS TotalShop
        // SUM(CASE WHEN UPPER(ptm.[Type]) = 'R' THEN 1 ELSE 0 END) AS TotalFlat
        int noOfShop = 0;
        int noOfFlat = 0;

        if (_propertyTypeRepository != null)
        {
            if (wingDetailIds.Count > 0)
            {
                var counts = await (
                    from pm in _propertyRepository.GetQueryable().AsNoTracking()
                    join ptm in _propertyTypeRepository.GetQueryable().AsNoTracking()
                        on pm.PropertyTypeId equals ptm.Id
                    where pm.IsActive && !pm.MarkedForDeletion &&
                          ptm.IsActive &&
                          pm.WingDetailId.HasValue && wingDetailIds.Contains(pm.WingDetailId.Value)
                    group ptm by 1 into g
                    select new
                    {
                        NoOfFlat = g.Count(ptm => ptm.Type != null && ptm.Type.ToUpper() == "R"),
                        NoOfShop = g.Count(ptm => ptm.Type != null && ptm.Type.ToUpper() == "C")
                    }
                ).FirstOrDefaultAsync(cancellationToken);

                if (counts != null)
                {
                    noOfFlat = counts.NoOfFlat;
                    noOfShop = counts.NoOfShop;
                }
            }
            else
            {
                var counts = await (
                    from pm in _propertyRepository.GetQueryable().AsNoTracking()
                    join ptm in _propertyTypeRepository.GetQueryable().AsNoTracking()
                        on pm.PropertyTypeId equals ptm.Id
                    where pm.WardId == targetWardId
                       && pm.PropertyNo == propertyNoTrim
                       && pm.IsActive
                       && !pm.MarkedForDeletion
                       && ptm.IsActive
                    group ptm by 1 into g
                    select new
                    {
                        NoOfFlat = g.Count(ptm => ptm.Type != null && ptm.Type.ToUpper() == "R"),
                        NoOfShop = g.Count(ptm => ptm.Type != null && ptm.Type.ToUpper() == "C")
                    }
                ).FirstOrDefaultAsync(cancellationToken);

                if (counts != null)
                {
                    noOfFlat = counts.NoOfFlat;
                    noOfShop = counts.NoOfShop;
                }
            }
        }
        else
        {
            var propertyRecords = await _propertyRepository.GetQueryable()
                .AsNoTracking()
                .Where(p => p.WardId == targetWardId
                         && p.PropertyNo == propertyNoTrim
                         && p.IsActive
                         && !p.MarkedForDeletion)
                .Select(p => new
                {
                    HasShop = !string.IsNullOrWhiteSpace(p.FlatOrShopNo),
                    HasFlat = !string.IsNullOrWhiteSpace(p.PartitionNo)
                })
                .ToListAsync(cancellationToken);

            noOfShop = propertyRecords.Count(p => p.HasShop);
            noOfFlat = propertyRecords.Count(p => p.HasFlat);
        }

        var activeSocietyIdsWithWings = wingList
            .Select(w => w.SocietyDetailId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var bestSociety = societyDetailsList
            .OrderByDescending(s => activeSocietyIdsWithWings.Contains(s.Id) ? 1 : 0)
            .ThenByDescending(s => !string.IsNullOrWhiteSpace(s.SocietyName) ? 1 : 0)
            .ThenByDescending(s => s.Id)
            .FirstOrDefault();

        var distinctWings = wingList
            .GroupBy(w => w.Id)
            .Select(g => g.First())
            .ToList();

        int noOfRowHouse = distinctWings.Sum(x => x.NoOfRowHouse);

        int totalWingCount = 0;
        if (bestSociety != null && _wingDetailsMasterRepository != null)
        {
            totalWingCount = await _wingDetailsMasterRepository.GetQueryable()
                .AsNoTracking()
                .CountAsync(w => w.SocietyDetailsMastId == bestSociety.Id && w.IsActive && !w.MarkedForDeletion, cancellationToken);
        }

        if (totalWingCount == 0 && distinctWings.Count > 0)
        {
            totalWingCount = distinctWings.Count;
        }

        int totalAmenityCount = 0;
        if (_propertyTypeRepository != null)
        {
            totalAmenityCount = await (
                from pm in _propertyRepository.GetQueryable().AsNoTracking()
                join ptm in _propertyTypeRepository.GetQueryable().AsNoTracking()
                    on pm.PropertyTypeId equals ptm.Id
                where pm.WardId == targetWardId &&
                      pm.PropertyNo != null &&
                      pm.PropertyNo == propertyNoTrim &&
                      pm.IsActive &&
                      !pm.MarkedForDeletion &&
                      ptm.IsActive &&
                      ptm.PartType == "Amenity"
                select pm.Id
            ).CountAsync(cancellationToken);
        }

        return new SocietySummaryDto
        {
            SocietyName = bestSociety?.SocietyName,
            BuilderName = bestSociety?.BuilderName,
            SocietyAddress = bestSociety?.SocietyAddress,
            TotalWingCount = totalWingCount,
            NoOfFlat = noOfFlat,
            NoOfShop = noOfShop,
            NoOfRowHouse = noOfRowHouse,
            TotalAmenityCount = totalAmenityCount
        };
    }

    /// <summary>
    /// Validates deactivation (IsActive change from true to false) for SocietyDetailsEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        SocietyDetailsEntity currentEntity,
        SocietyDetailsEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<SocietyDetailsEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates delete operation for SocietyDetailsEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        SocietyDetailsEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<SocietyDetailsEntity>(id, cancellationToken);
    }
}
