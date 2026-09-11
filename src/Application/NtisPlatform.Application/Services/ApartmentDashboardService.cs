using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Property.ApartmentQC.ApartmentDashboard;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;


namespace NtisPlatform.Application.Services;

public class ApartmentDashboardService : IApartmentDashboardService
{
    private readonly ILogger<ApartmentDashboardService> _logger;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<WingDetailsMastEntity, int> _wingDetailsRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;
    private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepository;
    private readonly IRepository<TypeOfUseCategoryEntity, int> _typeOfUseCategoryRepository;
    private readonly IRepository<PropertySocialDetailsEntity, int> _propertySocialDetailsRepository;
    private readonly IRepository<SocialAttributeEntity, int> _socialAttributeRepository;
    private readonly IRepository<PropertyWorkflowDetailsEntity, int> _propertyWorkflowRepository;
    private readonly IRepository<PropertySurveyVisitEntity, int> _propertySurveyVisitRepository;

    public ApartmentDashboardService(
        ILogger<ApartmentDashboardService> logger,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<WingDetailsMastEntity, int> wingDetailsRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IRepository<TypeOfUseEntity, int> typeOfUseRepository,
        IRepository<TypeOfUseCategoryEntity, int> typeOfUseCategoryRepository,
        IRepository<PropertySocialDetailsEntity, int> propertySocialDetailsRepository,
        IRepository<SocialAttributeEntity, int> socialAttributeRepository,
        IRepository<PropertyWorkflowDetailsEntity, int> propertyWorkflowRepository,
        IRepository<PropertySurveyVisitEntity, int> propertySurveyVisitRepository)
    {
        _logger = logger;
        _propertyRepository = propertyRepository;
        _wingDetailsRepository = wingDetailsRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _typeOfUseCategoryRepository = typeOfUseCategoryRepository;
        _propertySocialDetailsRepository = propertySocialDetailsRepository;
        _socialAttributeRepository = socialAttributeRepository;
        _propertyWorkflowRepository = propertyWorkflowRepository;
        _propertySurveyVisitRepository = propertySurveyVisitRepository;
    }

    public async Task<ApartmentDashboardDto> GetAllAsync(ApartmentDashboardQueryParameters queryParams, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Apartment dashboard requested for SocietyDetailsId: {SocietyDetailsId}, WingId: {WingId}", queryParams.SocietyDetailsId, queryParams.WingId);

        var societyId = queryParams.SocietyDetailsId;
        var wingId = queryParams.WingId;

        // Read-only IQueryable sources
        var properties = _propertyRepository.GetQueryable().AsNoTracking();
        var wings = _wingDetailsRepository.GetQueryable().AsNoTracking();
        var propertyMapDetails = _propertyMapDetailRepository.GetQueryable().AsNoTracking();
        var propertyDetails = _propertyDetailsRepository.GetQueryable().AsNoTracking();
        var propertyTypes = _propertyTypeRepository.GetQueryable().AsNoTracking();
        var typeOfUses = _typeOfUseRepository.GetQueryable().AsNoTracking();
        var typeOfUseCategories = _typeOfUseCategoryRepository.GetQueryable().AsNoTracking();
        var propertySocialDetails = _propertySocialDetailsRepository.GetQueryable().AsNoTracking();
        var socialAttributes = _socialAttributeRepository.GetQueryable().AsNoTracking();
        var propertyWorkflows = _propertyWorkflowRepository.GetQueryable().AsNoTracking();
        var propertySurveyVisits = _propertySurveyVisitRepository.GetQueryable().AsNoTracking();

        //  Base Properties
        // Filter as early as possible.
        var baseProperties =
            from pm in properties
            join wdm in wings
                on pm.WingDetailId equals wdm.Id
            where pm.IsActive && !pm.MarkedForDeletion
                  && wdm.IsActive && !wdm.MarkedForDeletion
                  && wdm.SocietyDetailsMastId == societyId
                  && (wingId == 0 || wdm.WingMasterId == wingId)
            select new
            {
                PropertyId = pm.Id,
                pm.PropertyTypeId,
                wdm.WingMasterId
            };

        //  Assessed Properties
        // DISTINCT PropertyId ensures one property = one dashboard row.
        var assessedPropertyIds = propertyMapDetails.Where(x => x.IsActive && x.Status == "ACTIVE").Select(x => x.PropertyIdNew).Distinct();

        //  Amenity Property Types
        var amenityPropertyTypeIds = propertyTypes.Where(x => x.IsActive && x.PartType == "Amenity").Select(x => x.Id);

        //  Parking Properties
        var parkingPropertyIds =
            (
                from pd in propertyDetails
                join tou in typeOfUses on pd.TypeOfUseId equals tou.Id
                join touc in typeOfUseCategories on tou.TypeOfUseCategoryId equals touc.Id
                where pd.IsActive && !pd.MarkedForDeletion
                      && tou.IsActive && touc.IsActive
                      && touc.TypeOfUseCategoryName == "PARKING"
                select pd.PropertyId
            )
            .Distinct();


        // 5. Internal Survey Verified Properties
        var surveyVerifiedPropertyIds =
        (
            from workflow in propertyWorkflows
            join visit in propertySurveyVisits
                on workflow.Id equals visit.PropertyWorkflowDetailsId
            where workflow.IsActive == true 
                  && visit.IsActive == true
                  && visit.InternalSurveyVerified == true
            select workflow.PropertyId
        )
        .Distinct();

        // Submission Complete Properties
        var submissionCompletePropertyIds = propertyDetails.Where(pd => pd.IsActive && !pd.MarkedForDeletion && (pd.CarpetAreaSqMeter ?? 0) > 0 && (pd.BuiltupAreaSqMeter ?? 0) > 0)
            .Select(pd => pd.PropertyId).Distinct();

        //  Lift details
        // Pre-filter before joining with base properties.
        var liftDetails =
            from psd in propertySocialDetails
            join sam in socialAttributes
                on psd.SocialAttributeId equals sam.Id
            where psd.IsActive && !psd.MarkedForDeletion
                  && sam.IsActive
                  && sam.SocialAttributeCode == "HAS_LIFT"
            select new
            {
                psd.PropertyId,
                Value = psd.IntValue
            };

        //  One dashboard SQL query
        var dashboard = await baseProperties
        .GroupBy(_ => 1)
        .Select(g => new
        {
            SocietyId = societyId,
            TotalProperties = g.Count(),
            TotalWings = g.Select(x => x.WingMasterId).Distinct().Count(),
            Assessed = g.Count(x => assessedPropertyIds.Any(id => id == x.PropertyId)),
            TotalAmenities = g.Count(x => amenityPropertyTypeIds.Any(id => id == x.PropertyTypeId)),
            TotalParking = g.Count(x => parkingPropertyIds.Any(id => id == x.PropertyId)),
            InternalSurveyVerified = g.Count(x => surveyVerifiedPropertyIds.Any(id => id == x.PropertyId)),
            InternalSurveyVerifiedAssessed = g.Count(x => surveyVerifiedPropertyIds.Any(id => id == x.PropertyId) &&
                                             assessedPropertyIds.Any(id => id == x.PropertyId)),
            SubmissionComplete = g.Count(x => submissionCompletePropertyIds.Any(id => id == x.PropertyId)),
            SubmissionCompleteAssessed = g.Count(x => submissionCompletePropertyIds.Any(id => id == x.PropertyId) &&
                                         assessedPropertyIds.Any(id => id == x.PropertyId)),
            TotalFloors =
                    (
                        from pd in propertyDetails
                        where pd.IsActive && !pd.MarkedForDeletion && pd.FloorId != null && g.Any(bp => bp.PropertyId == pd.PropertyId)
                        select pd.FloorId
                    ).Distinct().Count(),
            TotalLifts =
                    (
                        from lift in liftDetails
                        where g.Any(bp => bp.PropertyId == lift.PropertyId)
                        select (int?)lift.Value
                    ).Sum() ?? 0
        })
        .SingleOrDefaultAsync(cancellationToken);

        if (dashboard == null)
        {
            return new ApartmentDashboardDto { SocietyId = societyId };
        }

        var totalProperties = dashboard.TotalProperties;
        var assessed = dashboard.Assessed;
        var unassessed = totalProperties - assessed;
        var internalSurveyVerified = dashboard.InternalSurveyVerified;
        var internalSurveyPending = totalProperties - internalSurveyVerified;
        var internalSurveyVerifiedAssessed = dashboard.InternalSurveyVerifiedAssessed;
        var internalSurveyVerifiedUnassessed = internalSurveyVerified - internalSurveyVerifiedAssessed;
        var submissionComplete = dashboard.SubmissionComplete;
        var submissionPending = totalProperties - submissionComplete;
        var submissionCompleteAssessed = dashboard.SubmissionCompleteAssessed;
        var submissionCompleteUnassessed = submissionComplete - submissionCompleteAssessed;


        var result = new ApartmentDashboardDto
        {
            SocietyId = dashboard.SocietyId,
            // Property
            TotalProperties = totalProperties,
            TotalWings = dashboard.TotalWings,
            TotalFloors = dashboard.TotalFloors,
            // Assessment
            Assessed = assessed,
            Unassessed = unassessed,
            AssessedPercentage = CalculatePercentage(assessed, totalProperties),
            UnassessedPercentage = CalculatePercentage(unassessed, totalProperties),
            // Society
            TotalAmenities = dashboard.TotalAmenities,
            TotalParking = dashboard.TotalParking,
            TotalLifts = dashboard.TotalLifts,
            // Internal Survey
            InternalSurveyVerified = internalSurveyVerified,
            InternalSurveyPending = internalSurveyPending,
            InternalSurveyVerifiedPercentage = CalculatePercentage(internalSurveyVerified, totalProperties),
            InternalSurveyPendingPercentage = CalculatePercentage(internalSurveyPending, totalProperties),
            InternalSurveyVerifiedAssessed = internalSurveyVerifiedAssessed,
            InternalSurveyVerifiedUnassessed = internalSurveyVerifiedUnassessed,
            InternalSurveyVerifiedAssessedPercentage = CalculatePercentage(internalSurveyVerifiedAssessed, internalSurveyVerified),
            InternalSurveyVerifiedUnassessedPercentage = CalculatePercentage(internalSurveyVerifiedUnassessed, internalSurveyVerified),
            // Submission
            SubmissionComplete = submissionComplete,
            SubmissionPending = submissionPending,
            SubmissionCompletePercentage = CalculatePercentage(submissionComplete, totalProperties),
            SubmissionPendingPercentage = CalculatePercentage(submissionPending, totalProperties),
            SubmissionCompleteAssessed = submissionCompleteAssessed,
            SubmissionCompleteUnassessed = submissionCompleteUnassessed,
            SubmissionCompleteAssessedPercentage = CalculatePercentage(submissionCompleteAssessed, submissionComplete),
            SubmissionCompleteUnassessedPercentage = CalculatePercentage(submissionCompleteUnassessed, submissionComplete)
        };
        return result;
    }

    private static decimal CalculatePercentage(int value, int total)
    {
        if (total <= 0) return 0m;
        return Math.Round(value * 100m / total, 2, MidpointRounding.AwayFromZero);
    }
}
