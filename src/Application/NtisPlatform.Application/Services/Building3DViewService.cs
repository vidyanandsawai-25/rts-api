using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.Building3DView;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class Building3DViewService : IBuilding3DViewService
{
    private readonly IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<WingEntity, int> _wingRepository;
    private readonly IRepository<WingDetailsMastEntity, int> _wingDetailsMastRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _assessmentRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<FloorEntity, int> _floorRepository;
    private readonly IRepository<PropertyWorkflowDetailsEntity, int> _propertyWorkflowDetailsRepository;
    private readonly IRepository<PropertyWorkflowStageMasterEntity, int> _workflowStageRepository;
    private readonly IRepository<PropertySurveyVisitEntity, int> _propertySurveyVisitRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;

    public Building3DViewService(
        IRepository<PropertyEntity, int> repository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<WingEntity, int> wingRepository,
        IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IRepository<PropertyAssessmentEntity, int> assessmentRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<FloorEntity, int> floorRepository,
        IRepository<PropertyWorkflowDetailsEntity, int> propertyWorkflowDetailsRepository,
        IRepository<PropertyWorkflowStageMasterEntity, int> workflowStageRepository,
        IRepository<PropertySurveyVisitEntity, int> propertySurveyVisitRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository)
    {
        _repository = repository;
        _societyRepository = societyRepository;
        _wingRepository = wingRepository;
        _wingDetailsMastRepository = wingDetailsMastRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _assessmentRepository = assessmentRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _floorRepository = floorRepository;
        _propertyWorkflowDetailsRepository = propertyWorkflowDetailsRepository;
        _workflowStageRepository = workflowStageRepository;
        _propertySurveyVisitRepository = propertySurveyVisitRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
    }

    public async Task<Building3DViewDto?> GetBuilding3DViewAsync(Building3DViewQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var mainProperty = await (
            from property in _repository.GetQueryable().AsNoTracking()
            join society in _societyRepository.GetQueryable().AsNoTracking()
                on property.Id equals society.PropertyId into societyJoin
            from society in societyJoin.DefaultIfEmpty()
            where property.Id == queryParameters.PropertyId
                  && property.IsActive
                  && !property.MarkedForDeletion
            select new
            {
                property.Id,
                property.WardId,
                property.PropertyNo,
                property.PartitionNo,
                property.CategoryId,
                property.OwnerName,
                SocietyDetailId = society != null ? society.Id : (int?)null,
                SocietyName = society != null ? society.SocietyName : null
            }
        ).FirstOrDefaultAsync(cancellationToken);

        // Step 2: Return null early if the main property does not exist
        if (mainProperty == null)
        {
            return null;
        }

        var rawProperties = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(property =>
                    property.WardId == mainProperty.WardId &&
                    property.PropertyNo == mainProperty.PropertyNo &&
                    property.IsActive &&
                    !property.MarkedForDeletion &&
                    (!queryParameters.WingdetailsId.HasValue ||
                    property.WingDetailId == queryParameters.WingdetailsId))
                .Select(property => new
                {
                    property.Id,
                    property.WardId,
                    property.PropertyNo,
                    property.PartitionNo,
                    property.FlatOrShopNo,
                    property.OwnerName,
                    property.PropertyTypeId,
                    property.WingDetailId
                })
                .ToListAsync(cancellationToken);

        var wingDetailIds = rawProperties
            .Where(x => x.WingDetailId.HasValue)
            .Select(x => x.WingDetailId!.Value)
            .Distinct()
            .ToList();

        var wingMasters = await (
                    from wing in _wingRepository.GetQueryable().AsNoTracking()
                    join details in _wingDetailsMastRepository.GetQueryable().AsNoTracking()
                        on wing.Id equals details.WingMasterId
                    where wingDetailIds.Contains(details.Id)
                          && wing.IsActive
                          && details.IsActive
                          && !details.MarkedForDeletion
                    select new
                    {
                        WingId = wing.Id,
                        WingDetailId = details.Id,
                        SocietyDetailId = (int?)details.SocietyDetailsMastId,
                        WingNo = wing.WingNo,
                        WingName = details.WingName
                    }
                ).ToListAsync(cancellationToken);

        var societyDetailIds = wingMasters
            .Select(w => w.SocietyDetailId)
            .Concat(mainProperty.SocietyDetailId.HasValue ? new int?[] { mainProperty.SocietyDetailId.Value } : Array.Empty<int?>())
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var societyDetails = await _societyRepository.GetQueryable()
           .AsNoTracking()
           .Where(x => (societyDetailIds.Contains(x.Id) || x.PropertyId == queryParameters.PropertyId)
                       && x.IsActive && !x.MarkedForDeletion && !string.IsNullOrWhiteSpace(x.SocietyName))
           .Select(x => new
           {
               x.Id,
               x.SocietyName
           })
           .ToListAsync(cancellationToken);

        var societyLookup = societyDetails.ToDictionary(x => x.Id);
        var wingLookup = wingMasters.ToDictionary(x => x.WingDetailId);

        var allProperties = rawProperties.Select(p =>
        {
            int? societyDetailId = null;
            string? societyName = null;

            if (p.WingDetailId.HasValue && wingLookup.TryGetValue(p.WingDetailId.Value, out var wingInfo))
            {
                societyDetailId = wingInfo.SocietyDetailId;
                if (societyDetailId.HasValue && societyLookup.TryGetValue(societyDetailId.Value, out var soc))
                {
                    societyName = soc.SocietyName;
                }
            }
            else if (mainProperty.SocietyDetailId.HasValue)
            {
                societyDetailId = mainProperty.SocietyDetailId;
                societyName = mainProperty.SocietyName;
            }

            return new
            {
                p.Id,
                p.WardId,
                p.PropertyNo,
                p.PartitionNo,
                p.FlatOrShopNo,
                p.OwnerName,
                p.PropertyTypeId,
                p.WingDetailId,
                SocietyDetailId = societyDetailId,
                SocietyName = societyName
            };
        }).ToList();

        // Step 4: Fetch the amenity property type Id from PropertyTypeMaster using the specific criteria: PartType = "Amenity", Type = "R", PropertyDescription = "अॅमिनीटी", IsActive = true
        var amenityPropertyTypeId = await _propertyTypeRepository.GetQueryable()
            .AsNoTracking()
            .Where(ptm => ptm.PartType == PartTypeConstants.Amenity
                       && ptm.Type == PartTypeConstants.Residential
                       && ptm.IsActive)
            .Select(ptm => (int?)ptm.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 5: Exclude the main property itself to get the set of child/sibling properties
        var otherProperties = allProperties
            .Where(p => p.Id != mainProperty.Id)
            .ToList();

        var otherPropertyIds = otherProperties
            .Select(x => x.Id)
            .Distinct()
            .ToList();

        var assessmentLookup = await _assessmentRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => otherPropertyIds.Contains(x.PropertyId) && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(group => group
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.PropertyId,
                    x.BHK,
                    x.UnitGenerationType
                })
                .First())
            .ToDictionaryAsync(x => x.PropertyId, x => new
            {
                x.BHK,
                x.UnitGenerationType
            }, cancellationToken);

        var propertyDetailsLookup = await _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => otherPropertyIds.Contains(x.PropertyId) && x.IsActive && !x.MarkedForDeletion)
            .GroupBy(x => x.PropertyId)
            .Select(group => group
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.PropertyId,
                    x.FloorId
                })
                .First())
            .ToDictionaryAsync(x => x.PropertyId, x => x.FloorId, cancellationToken);

        var floorIds = propertyDetailsLookup.Values
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var floorLookup = await _floorRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => floorIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, x => new { x.Description, x.FloorCode }, cancellationToken);

        // Lookup: Property IDs that have InternalSurveyVerified = true
        var internalSurveyVerifiedPropertyIds = await (
            from wfd in _propertyWorkflowDetailsRepository.GetQueryable().AsNoTracking()
            join wfdm in _workflowStageRepository.GetQueryable().AsNoTracking()
                on wfd.WorkflowStageId equals wfdm.Id
            join psv in _propertySurveyVisitRepository.GetQueryable().AsNoTracking()
                on wfd.Id equals psv.PropertyWorkflowDetailsId
            where otherPropertyIds.Contains(wfd.PropertyId)
                  && psv.InternalSurveyVerified == true
                  && wfd.IsActive
                  && wfdm.IsActive
                  && psv.IsActive
            select wfd.PropertyId
        ).Distinct().ToListAsync(cancellationToken);

        var internalSurveyVerifiedSet = new HashSet<int>(internalSurveyVerifiedPropertyIds);

        // Lookup: Property IDs that are assessed (exist in PropertyMapDetail with ACTIVE status)
        var assessedPropertyIds = await _propertyMapDetailRepository.GetQueryable()
            .AsNoTracking()
            .Where(pmd => pmd.IsActive
                          && pmd.Status == PropertyMapStatus.Active
                          && pmd.PropertyIdNew != null
                          && pmd.PropertyIdOld != null
                          && otherPropertyIds.Contains(pmd.PropertyIdNew.Value))
            .Select(pmd => pmd.PropertyIdNew!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var assessedPropertySet = new HashSet<int>(assessedPropertyIds);

        // Lookup: Property IDs that have submission applied (any non-zero area in PropertyDetails)
        var submissionAppliedPropertyIds = await _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(pd => otherPropertyIds.Contains(pd.PropertyId)
                         && pd.IsActive
                         && !pd.MarkedForDeletion
                         && ((pd.CarpetAreaSqFeet != null && pd.CarpetAreaSqFeet > 0)
                             || (pd.CarpetAreaSqMeter != null && pd.CarpetAreaSqMeter > 0)
                             || (pd.BuiltupAreaSqFeet != null && pd.BuiltupAreaSqFeet > 0)
                             || (pd.BuiltupAreaSqMeter != null && pd.BuiltupAreaSqMeter > 0)))
            .Select(pd => pd.PropertyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var submissionAppliedSet = new HashSet<int>(submissionAppliedPropertyIds);

        // Step 6: Identify main-property-level amenities — properties with no SocietyDetailId whose PropertyTypeId matches the amenity property type
        var mainPropertyAmenities = otherProperties
            .Where(p => p.SocietyDetailId == null)
            .Where(p => amenityPropertyTypeId.HasValue && p.PropertyTypeId == amenityPropertyTypeId.Value)
            .Select(p => new Amenity3DViewDto
            {
                PropertyId = p.Id,
                PropertyNo = p.PropertyNo,
                PartitionNo = p.PartitionNo
            })
            .ToList();

        // Step 6b: Identify main-property-level regular details — properties with no SocietyDetailId whose PropertyTypeId does NOT match the amenity property type
        var mainPropertyDetails = otherProperties
            .Where(p => p.SocietyDetailId == null)
            .Where(p => !amenityPropertyTypeId.HasValue || p.PropertyTypeId != amenityPropertyTypeId.Value)
            .Select(p => new Property3DViewDto
            {
                PropertyId = p.Id,
                PartitionNo = p.PartitionNo,
                FlatOrShopNo = p.FlatOrShopNo,
                OwnerName = p.OwnerName,
                BHK = assessmentLookup.TryGetValue(p.Id, out var assessment) ? assessment.BHK : null,
                UnitGenerationType = assessmentLookup.TryGetValue(p.Id, out var unitAssessment) ? unitAssessment.UnitGenerationType : null,
                FloorId = propertyDetailsLookup.TryGetValue(p.Id, out var floorId) ? floorId : null,
                FloorDescription = propertyDetailsLookup.TryGetValue(p.Id, out var fId) && fId.HasValue && floorLookup.TryGetValue(fId.Value, out var fDesc) ? fDesc.Description : null,
                FloorCode = propertyDetailsLookup.TryGetValue(p.Id, out var fcId) && fcId.HasValue && floorLookup.TryGetValue(fcId.Value, out var floorData) ? floorData.FloorCode : null,
                IsInternalSarveyVerify = internalSurveyVerifiedSet.Contains(p.Id),
                IsAssessed = assessedPropertySet.Contains(p.Id),
                IsSubmissionPending = submissionAppliedSet.Contains(p.Id)
            })
            .ToList();

        // Step 7: Group society-bound properties by SocietyDetailId, then by WingId within each society, and project each wing group into a Society3DViewDto containing separated amenities and regular properties
        var societies = otherProperties
            .Where(p => p.SocietyDetailId != null)
            .GroupBy(p => p.SocietyDetailId)
            .SelectMany(societyGroup =>
            {
                var societyId = societyGroup.Key;

                var societyName = societyId.HasValue && societyLookup.TryGetValue(societyId.Value, out var societyDetail) ? societyDetail.SocietyName ?? "" : "";

                // Step 7 a: Sub-group each society's properties by their WingId
                return societyGroup
                    .GroupBy(p => p.WingDetailId)
                    .Select(wingGroup =>
                    {
                        int? wingId = null;
                        string wingName = string.Empty;

                        if (wingGroup.Key.HasValue && wingLookup.TryGetValue(wingGroup.Key.Value, out var wing))
                        {
                            wingId = wing.WingId;
                            wingName = !string.IsNullOrWhiteSpace(wing.WingName) ? wing.WingName : wing.WingNo ?? string.Empty;
                        }

                        // Step 7 b: Partition wing properties into amenities and regular properties based on whether their PropertyTypeId matches the amenity type
                        var wingProperties = wingGroup.ToList();

                        var amenities = wingProperties
                            .Where(p => amenityPropertyTypeId.HasValue && p.PropertyTypeId == amenityPropertyTypeId.Value)
                            .Select(p => new Amenity3DViewDto
                            {
                                PropertyId = p.Id,
                                PropertyNo = p.PropertyNo,
                                PartitionNo = p.PartitionNo,
                                AmenityName = PartTypeConstants.Amenity
                            })
                            .ToList();

                        var properties = wingProperties
                            .Where(p => !amenityPropertyTypeId.HasValue || p.PropertyTypeId != amenityPropertyTypeId.Value)
                            .Select(p => new Property3DViewDto
                            {
                                PropertyId = p.Id,
                                PartitionNo = p.PartitionNo,
                                FlatOrShopNo = p.FlatOrShopNo,
                                OwnerName = p.OwnerName,
                                BHK = assessmentLookup.TryGetValue(p.Id, out var assessment) ? assessment.BHK : null,
                                UnitGenerationType = assessmentLookup.TryGetValue(p.Id, out var unitAssessment) ? unitAssessment.UnitGenerationType : null,
                                FloorId = propertyDetailsLookup.TryGetValue(p.Id, out var floorId) ? floorId : null,
                                FloorDescription = propertyDetailsLookup.TryGetValue(p.Id, out var fId) && fId.HasValue && floorLookup.TryGetValue(fId.Value, out var fDesc) ? fDesc.Description : null,
                                FloorCode = propertyDetailsLookup.TryGetValue(p.Id, out var fcId) && fcId.HasValue && floorLookup.TryGetValue(fcId.Value, out var floorData) ? floorData.FloorCode : null,
                                IsInternalSarveyVerify = internalSurveyVerifiedSet.Contains(p.Id),
                                IsAssessed = assessedPropertySet.Contains(p.Id),
                                IsSubmissionPending = submissionAppliedSet.Contains(p.Id)
                            })
                            .ToList();
                        return new Society3DViewDto
                        {
                            SocietyId = societyId,
                            SocietyName = societyName,
                            WingId = wingId,
                            WingName = wingName,
                            Properties = properties,
                            Amenities = amenities
                        };
                    });
            })
            .ToList();

        // Step 8: Assemble the final DTO from the main property details and the computed collections
        var result = new Building3DViewDto
        {
            MainPropertyId = mainProperty.Id,
            PropertyNo = mainProperty.PropertyNo,
            PropertyName = mainProperty.OwnerName,
            MainPropertyDetails = mainPropertyDetails,
            MainPropertyAmenities = mainPropertyAmenities,
            Societies = societies
        };

        return result;
    }
}
