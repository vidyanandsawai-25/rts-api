using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyDashboard;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyDashboardService : IPropertyDashboardService
{
    private readonly ILogger<PropertyDashboardService> _logger;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _propertyCategoryRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly IRepository<GlobalSurveyWardAllocationEntity, int> _wardAllocationRepository;
    private readonly IRepository<OldWardMasterEntity, int> _oldWardRepository;
    private readonly IRepository<UserRoleAllocationEntity, int> _userRoleAllocationRepository;
    private readonly IRepository<UserRoleMasterEntity, int> _userRoleMasterRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyDetailsRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeMasterRepository;

    public PropertyDashboardService(
        ILogger<PropertyDashboardService> logger,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyCategoryEntity, int> propertyCategoryRepository,
        IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
        IRepository<GlobalSurveyWardAllocationEntity, int> wardAllocationRepository,
        IRepository<OldWardMasterEntity, int> oldWardRepository,
        IRepository<UserRoleAllocationEntity, int> userRoleAllocationRepository,
        IRepository<UserRoleMasterEntity, int> userRoleMasterRepository,
        IRepository<SocietyDetailsEntity, int> societyDetailsRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeMasterRepository)
    {
        _logger = logger;
        _propertyRepository = propertyRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _propertyCategoryRepository = propertyCategoryRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _wardAllocationRepository = wardAllocationRepository;
        _oldWardRepository = oldWardRepository;
        _userRoleAllocationRepository = userRoleAllocationRepository;
        _userRoleMasterRepository = userRoleMasterRepository;
        _societyDetailsRepository = societyDetailsRepository;
        _propertyTypeMasterRepository = propertyTypeMasterRepository;
    }

    public async Task<PropertyDashboardDto> GetAllAsync(PropertyDashboardQueryParameters queryParams, CancellationToken cancellationToken = default)
    {
        var zoneId = queryParams.ZoneId;
        var wardId = queryParams.WardId;
        var userId = queryParams.UserId;

        // Base properties query filtered by active status
        var propertiesQuery = _propertyRepository.GetQueryable()
            .AsNoTracking()
            .Where(pm => pm.IsActive && !pm.MarkedForDeletion);

        var oldPropertiesQuery = _propertyMastOldRepository.GetQueryable()
            .AsNoTracking()
            .Where(pmo => !pmo.MarkedForDeletion && pmo.IsActive);

        // Check User Role (Admin / Manager)
        var userRoles = await (
            from ura in _userRoleAllocationRepository.GetQueryable().AsNoTracking()
            join urm in _userRoleMasterRepository.GetQueryable().AsNoTracking() on ura.UserRoleId equals urm.Id
            where ura.IsActive && ura.UserId == userId && urm.IsActive
            select urm.UserRoleName
        ).ToListAsync(cancellationToken);

        bool isAdminOrManager = userRoles.Any(r =>
            r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Manager", StringComparison.OrdinalIgnoreCase));

        _logger.LogInformation("Getting PropertyDashboard for ZoneId={ZoneId}, WardId={WardId}, UserId={UserId}, IsAdminOrManager={IsAdminOrManager}",
            zoneId, wardId, userId, isAdminOrManager);

        if (isAdminOrManager)
        {
            // Admin or Manager has FULL access to entire data across all wards and zones
            if (wardId > 0)
            {
                propertiesQuery = propertiesQuery.Where(pm => pm.WardId == wardId);
            }

            if (wardId > 0 || zoneId > 0)
            {
                var adminOldWardNos = await (
                    from wa in _wardAllocationRepository.GetQueryable().AsNoTracking()
                    join ow in _oldWardRepository.GetQueryable().AsNoTracking() on wa.OldWardId equals ow.Id
                    where wa.IsActive && ow.IsActive && ow.OldWardNo != null
                          && (zoneId == 0 || wa.ZoneId == zoneId)
                          && (wardId == 0 || wa.WardId == wardId)
                    select ow.OldWardNo!.Trim()
                ).Distinct().ToListAsync(cancellationToken);

                if (adminOldWardNos.Count > 0)
                {
                    oldPropertiesQuery = oldPropertiesQuery.Where(pmo => pmo.OldWardNo != null && adminOldWardNos.Contains(pmo.OldWardNo.Trim()));
                }
            }
        }
        else
        {
            // Regular user (non-Admin / non-Manager): Filter new survey properties created by this user
            propertiesQuery = propertiesQuery.Where(pm => pm.CreatedBy == userId);

            if (wardId > 0)
            {
                propertiesQuery = propertiesQuery.Where(pm => pm.WardId == wardId);
            }

            // Old properties are pre-existing historical master data: filter by user's allocated Wards
            var userAllocationsQuery = _wardAllocationRepository.GetQueryable()
                .AsNoTracking()
                .Where(wa => wa.IsActive && wa.UserId == userId);

            if (zoneId > 0)
            {
                userAllocationsQuery = userAllocationsQuery.Where(wa => wa.ZoneId == zoneId);
            }

            if (wardId > 0)
            {
                userAllocationsQuery = userAllocationsQuery.Where(wa => wa.WardId == wardId);
            }

            var allocatedOldWardNos = await (
                from wa in userAllocationsQuery
                join ow in _oldWardRepository.GetQueryable().AsNoTracking() on wa.OldWardId equals ow.Id
                where ow.IsActive && ow.OldWardNo != null
                select ow.OldWardNo!.Trim()
            ).Distinct().ToListAsync(cancellationToken);

            if (allocatedOldWardNos.Count > 0)
            {
                oldPropertiesQuery = oldPropertiesQuery.Where(pmo => pmo.OldWardNo != null && allocatedOldWardNos.Contains(pmo.OldWardNo.Trim()));
            }
            else
            {
                oldPropertiesQuery = oldPropertiesQuery.Where(pmo => pmo.CreatedBy == userId);
            }
        }

        var categories = await _propertyCategoryRepository.GetQueryable()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.PropertyCategoryName })
            .ToListAsync(cancellationToken);

        int? apartmentCategoryId = categories.FirstOrDefault(c => c.PropertyCategoryName.Contains("Apartment", StringComparison.OrdinalIgnoreCase))?.Id;
        int? individualCategoryId = categories.FirstOrDefault(c => c.PropertyCategoryName.Contains("Individual", StringComparison.OrdinalIgnoreCase))?.Id;
        int? industrialCategoryId = categories.FirstOrDefault(c => c.PropertyCategoryName.Contains("Industrial", StringComparison.OrdinalIgnoreCase) || c.PropertyCategoryName.Contains("Industry", StringComparison.OrdinalIgnoreCase))?.Id;
        int? plotCategoryId = categories.FirstOrDefault(c => c.PropertyCategoryName.Contains("Plot", StringComparison.OrdinalIgnoreCase))?.Id;

        // Fetch properties list matching filters
        var propertyList = await propertiesQuery
            .Select(p => new DashboardPropertyItemDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                PartitionNo = p.PartitionNo,
                PropertySeqNo = p.PropertySeqNo,
                PropertyTypeId = p.PropertyTypeId,
                WingDetailId = p.WingDetailId,
                OpenPlot = p.OpenPlot,
                Type = p.Type,
                PartType = p.PropertyTypeMaster != null ? p.PropertyTypeMaster.PartType : null
            })
            .ToListAsync(cancellationToken);

        var propertyIds = propertyList.Select(p => p.Id).ToList();

        // Assessed property IDs scoped to filtered properties
        var assessedPropertyIds = propertyIds.Count == 0
            ? new List<int>()
            : await _propertyMapDetailRepository.GetQueryable()
                .AsNoTracking()
                .Where(pmd => pmd.IsActive && pmd.Status == "ACTIVE" && pmd.PropertyIdNew != null && propertyIds.Contains(pmd.PropertyIdNew.Value))
                .Select(pmd => pmd.PropertyIdNew!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

        var assessedSet = new HashSet<int>(assessedPropertyIds);

        // 1. Total Old Properties
        var totalOldProperties = await oldPropertiesQuery.CountAsync(cancellationToken);

        // 2. Geo Sequencing Properties
        var geoSequencingProperties = propertyList.Count(p => p.PropertySeqNo.HasValue && p.PropertySeqNo.Value > 0);

        // 3. Property Assessment
        var totalAssessed = propertyList.Count(p => assessedSet.Contains(p.Id));
        var totalUnassessed = propertyList.Count - totalAssessed;

        // Property IDs linked via SocietyDetailsMast.PropertyId scoped to filtered properties
        var propertyIdsWithSociety = propertyIds.Count == 0
            ? new List<int>()
            : await _societyDetailsRepository.GetQueryable()
                .AsNoTracking()
                .Where(sdm => sdm.IsActive && !sdm.MarkedForDeletion && sdm.PropertyId.HasValue && propertyIds.Contains(sdm.PropertyId.Value))
                .Select(sdm => sdm.PropertyId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

        var propertyIdsWithSocietySet = new HashSet<int>(propertyIdsWithSociety);

        // Helper filter functions
        bool IsApartment(DashboardPropertyItemDto p) =>
            (apartmentCategoryId.HasValue && p.CategoryId == apartmentCategoryId.Value) ||
            p.WingDetailId.HasValue ||
            propertyIdsWithSocietySet.Contains(p.Id) ||
            (!string.IsNullOrEmpty(p.Type) && p.Type.Contains("Apartment", StringComparison.OrdinalIgnoreCase));

        bool IsIndividual(DashboardPropertyItemDto p) =>
            (individualCategoryId.HasValue && p.CategoryId == individualCategoryId.Value) ||
            (!string.IsNullOrEmpty(p.Type) && p.Type.Contains("Individual", StringComparison.OrdinalIgnoreCase));

        bool IsIndustrial(DashboardPropertyItemDto p) =>
            (industrialCategoryId.HasValue && p.CategoryId == industrialCategoryId.Value) ||
            (!string.IsNullOrEmpty(p.Type) && (p.Type.Contains("Industrial", StringComparison.OrdinalIgnoreCase) || p.Type.Contains("Industry", StringComparison.OrdinalIgnoreCase)));

        bool IsPlot(DashboardPropertyItemDto p) =>
            (plotCategoryId.HasValue && p.CategoryId == plotCategoryId.Value) ||
            p.OpenPlot == true ||
            (!string.IsNullOrEmpty(p.Type) && p.Type.Contains("Plot", StringComparison.OrdinalIgnoreCase));

        bool IsMainProperty(string? partitionNo) =>
            string.IsNullOrWhiteSpace(partitionNo) || partitionNo.Trim() == "0";

        // Category items
        var apartmentProps = propertyList.Where(p => IsApartment(p)).ToList();
        var individualProps = propertyList.Where(p => IsIndividual(p)).ToList();
        var industrialProps = propertyList.Where(p => IsIndustrial(p)).ToList();
        var plotProps = propertyList.Where(p => IsPlot(p)).ToList();

        // Apartment Details
        var apartmentAssessed = apartmentProps.Count(p => assessedSet.Contains(p.Id));
        var apartmentUnassessed = apartmentProps.Count - apartmentAssessed;
        var apartmentPropertyIds = apartmentProps.Select(p => p.Id).ToList();

        var buildingIdsFromSocietyMast = await _societyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(sdm => sdm.IsActive && !sdm.MarkedForDeletion && sdm.PropertyId.HasValue && apartmentPropertyIds.Contains(sdm.PropertyId.Value))
            .Select(sdm => sdm.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        var totalBuildings = buildingIdsFromSocietyMast
            .Distinct()
            .Count();

        var amenityTypeIds = await _propertyTypeMasterRepository.GetQueryable()
            .AsNoTracking()
            .Where(pt => pt.IsActive && pt.PartType != null && pt.PartType == "Amenity" && (pt.Type == "R" || pt.Type == null))
            .Select(pt => pt.Id)
            .ToListAsync(cancellationToken);

        var amenityTypeSet = new HashSet<int>(amenityTypeIds);

        var totalAmenities = apartmentProps.Count(p =>
            (p.PropertyTypeId.HasValue && amenityTypeSet.Contains(p.PropertyTypeId.Value)) ||
            (p.PartType != null && p.PartType.Equals("Amenity", StringComparison.OrdinalIgnoreCase)));

        // Individual Details
        var individualMain = individualProps.Count(p => IsMainProperty(p.PartitionNo));
        var individualPartition = individualProps.Count - individualMain;
        var individualAssessed = individualProps.Count(p => assessedSet.Contains(p.Id));
        var individualUnassessed = individualProps.Count - individualAssessed;

        // Industrial Details
        var industrialMain = industrialProps.Count(p => IsMainProperty(p.PartitionNo));
        var industrialPartition = industrialProps.Count - industrialMain;
        var industrialAssessed = industrialProps.Count(p => assessedSet.Contains(p.Id));
        var industrialUnassessed = industrialProps.Count - industrialAssessed;

        // Plot Details
        var plotMain = plotProps.Count(p => IsMainProperty(p.PartitionNo));
        var plotPartition = plotProps.Count - plotMain;
        var plotAssessed = plotProps.Count(p => assessedSet.Contains(p.Id));
        var plotUnassessed = plotProps.Count - plotAssessed;

        return new PropertyDashboardDto
        {
            TotalOldProperties = totalOldProperties,
            GeoSequencingProperties = geoSequencingProperties,
            PropertyAssessment = new PropertyAssessmentDto
            {
                AssessedProperties = totalAssessed,
                UnassessedProperties = totalUnassessed
            },
            Apartment = new ApartmentDashboardDto
            {
                TotalProperty = totalBuildings + apartmentProps.Count,
                TotalBuilding = totalBuildings,
                TotalUnits = apartmentProps.Count,
                TotalAmenities = totalAmenities,
                AssessedStatus = new AssessedStatusDto
                {
                    Assessed = apartmentAssessed,
                    Unassessed = (totalBuildings + apartmentProps.Count) - apartmentAssessed
                }
            },
            Individual = new CategoryDashboardDto
            {
                TotalProperty = individualProps.Count,
                MainProperty = individualMain,
                PartitionProperty = individualPartition,
                AssessedStatus = new AssessedStatusDto
                {
                    Assessed = individualAssessed,
                    Unassessed = individualUnassessed
                }
            },
            Industrial = new CategoryDashboardDto
            {
                TotalProperty = industrialProps.Count,
                MainProperty = industrialMain,
                PartitionProperty = industrialPartition,
                AssessedStatus = new AssessedStatusDto
                {
                    Assessed = industrialAssessed,
                    Unassessed = industrialUnassessed
                }
            },
            Plot = new CategoryDashboardDto
            {
                TotalProperty = plotProps.Count,
                MainProperty = plotMain,
                PartitionProperty = plotPartition,
                AssessedStatus = new AssessedStatusDto
                {
                    Assessed = plotAssessed,
                    Unassessed = plotUnassessed
                }
            }
        };
    }
}
