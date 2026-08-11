using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for Assessment dashboard grid assembly and classification rules.
/// </summary>
public class AssessmentStageService : IAssessmentStageService
{
    private const string AssessmentTypeTotal = "Total";
    private const string AssessmentTypeAssessed = "Assessed";
    private const string AssessmentTypeUnassessed = "Unassessed";
    private const string AssessmentTypeRented = "Rented";
    private const string PropertyTypeResidential = "Residential";
    private const string PropertyTypeCommercial = "Commercial";
    private const string PropertyTypeIndustrial = "Industrial";
    private const string PropertyTypeMixedUse = "Mixed Use";
    private const string PropertyTypePublicUtility = "Public Utility";
    private const string PropertyTypeOpenPlots = "Open Plots";
    private const string AssessedClassificationAdditionalConstruction = "Additional Construction";
    private const string AssessedClassificationChangeOfUse = "Change Of Use";
    private const string AssessedClassificationNoChange = "NoChange";
    private const string AssessedClassificationUnderassessed = "Underassessed";
    private const string RentedClassificationOwner = "Owner";
    private const string RentedClassificationRenter = "Renter";
    private const string RentedGrandTotalClassificationType = "All Occupancy Types";
    private const string GrandTotalClassificationType = "Assessed + Unassessed";
    private const string ClerkAuthorityCode = "CLERK";

    private readonly IAssessmentStageRepository _repository;

    public AssessmentStageService(IAssessmentStageRepository repository)
    {
        _repository = repository;
    }

    // Entry point for Assessment grid; routes request to Total, Assessed, Unassessed, or Rented logic.
    public async Task<AssessmentGridResponseDto> GetAssessmentGridDataAsync(PropertySearchRequestDto? searchRequest, string type, CancellationToken cancellationToken = default)
    {
        if (searchRequest?.WorkflowStageId == null)
            return new AssessmentGridResponseDto();

        var requestedType = NormalizeAssessmentType(type);
        if (requestedType == null)
            return new AssessmentGridResponseDto();

        var workflowStageId = searchRequest.WorkflowStageId.Value;
        if (!await _repository.AssessmentWorkflowStageExistsAsync(workflowStageId, cancellationToken))
            return new AssessmentGridResponseDto();

        var assessmentStatusIds = await _repository.GetAssessmentStatusIdsAsync(cancellationToken);
        return requestedType switch
        {
            AssessmentTypeTotal => await GetTotalAssessmentGridDataAsync(workflowStageId, searchRequest, assessmentStatusIds, cancellationToken),
            AssessmentTypeAssessed => await GetAssessedAssessmentGridDataAsync(workflowStageId, searchRequest, assessmentStatusIds, cancellationToken),
            AssessmentTypeUnassessed => await GetUnassessedAssessmentGridDataAsync(workflowStageId, searchRequest, assessmentStatusIds, cancellationToken),
            AssessmentTypeRented => await GetRentedAssessmentGridDataAsync(workflowStageId, searchRequest, cancellationToken),
            _ => await GetAssessmentGridDataByTypeAsync(workflowStageId, searchRequest, requestedType, assessmentStatusIds, cancellationToken)
        };
    }

    // Sends one or more Assessment properties to the Clerk approval queue.
    public async Task<SendToApproveResponseDto> SendToApproveAsync(SendToApproveRequestDto request, CancellationToken cancellationToken = default)
    {
        var requestedPropertyIds = GetRequestedPropertyIds(request);
        var invalidPropertyIds = GetRawRequestedPropertyIds(request).Where(id => id <= 0).Distinct().ToList();

        if (!requestedPropertyIds.Any())
            return CreateSendToApproveResponse(request, 0, Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), invalidPropertyIds, "PropertyIds are required.");

        if (request.UserId <= 0)
            return CreateSendToApproveResponse(request, 0, Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), invalidPropertyIds, "UserId is required.");

        var signAuthorityId = await _repository.GetSignAuthorityIdByCodeAsync(ClerkAuthorityCode, cancellationToken);
        if (signAuthorityId <= 0)
            return CreateSendToApproveResponse(request, 0, Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), invalidPropertyIds, "CLERK signing authority was not found.");

        var existingPropertyIds = await _repository.GetExistingPropertyIdsAsync(requestedPropertyIds, cancellationToken);
        var existingPropertyIdSet = existingPropertyIds.ToHashSet();
        var missingPropertyIds = requestedPropertyIds.Where(id => !existingPropertyIdSet.Contains(id)).ToList();

        var alreadySentPropertyIds = existingPropertyIds.Any()
            ? await _repository.GetExistingPropertySignatureIdsAsync(existingPropertyIds, cancellationToken)
            : new List<int>();
        var alreadySentPropertyIdSet = alreadySentPropertyIds.ToHashSet();

        var propertyIdsToInsert = existingPropertyIds.Where(id => !alreadySentPropertyIdSet.Contains(id)).ToList();
        var savedCount = propertyIdsToInsert.Any()
            ? await _repository.InsertPropertySignaturesAsync(propertyIdsToInsert, request.UserId, signAuthorityId, cancellationToken)
            : 0;

        return CreateSendToApproveResponse(
            request,
            signAuthorityId,
            propertyIdsToInsert,
            missingPropertyIds,
            alreadySentPropertyIds,
            invalidPropertyIds,
            CreateSendToApproveMessage(savedCount, missingPropertyIds.Count, alreadySentPropertyIds.Count, invalidPropertyIds.Count));
    }

    // Builds the Total tab with Assessed, Unassessed, and Rented rows for every zone.
    private async Task<AssessmentGridResponseDto> GetTotalAssessmentGridDataAsync(
        int workflowStageId,
        PropertySearchRequestDto? searchRequest,
        IReadOnlyDictionary<string, int> assessmentStatusIds,
        CancellationToken cancellationToken)
    {
        var properties = await _repository.GetStagePropertiesAsync(workflowStageId, cancellationToken, searchRequest);
        var zoneCounts = GetZoneCounts(properties);
        if (!zoneCounts.Any())
            return new AssessmentGridResponseDto();

        var assessedByZone = assessmentStatusIds.TryGetValue(AssessmentTypeAssessed, out var assessedStatusId)
            ? await GetClassificationByZoneAsync(properties.Where(p => p.AssessmentStatusId == assessedStatusId), AssessmentTypeAssessed, cancellationToken)
            : new Dictionary<int, PropertyClassificationDto>();
        var unassessedByZone = assessmentStatusIds.TryGetValue(AssessmentTypeUnassessed, out var unassessedStatusId)
            ? await GetClassificationByZoneAsync(properties.Where(p => p.AssessmentStatusId == unassessedStatusId), AssessmentTypeUnassessed, cancellationToken)
            : new Dictionary<int, PropertyClassificationDto>();
        var rentedByZone = await GetClassificationByZoneAsync(properties.Where(p => p.IsRented), AssessmentTypeRented, cancellationToken);

        return BuildGridResponse(
            zoneCounts.Select(zone => CreateZoneData(zone, new[]
            {
                assessedByZone.GetValueOrDefault(zone.ZoneId) ?? CreateEmptyClassification(AssessmentTypeAssessed),
                unassessedByZone.GetValueOrDefault(zone.ZoneId) ?? CreateEmptyClassification(AssessmentTypeUnassessed),
                rentedByZone.GetValueOrDefault(zone.ZoneId) ?? CreateEmptyClassification(AssessmentTypeRented)
            })), CalculateTotalRow, CalculateGrandTotalRow);
    }

    // Builds single-type tabs that only need one classification row per zone.
    private async Task<AssessmentGridResponseDto> GetAssessmentGridDataByTypeAsync(
        int workflowStageId,
        PropertySearchRequestDto? searchRequest,
        string assessmentType,
        IReadOnlyDictionary<string, int> assessmentStatusIds,
        CancellationToken cancellationToken)
    {
        var properties = FilterPropertiesByAssessmentType(
            await _repository.GetStagePropertiesAsync(workflowStageId, cancellationToken, searchRequest),
            assessmentType,
            assessmentStatusIds).ToList();
        var zoneCounts = GetZoneCounts(properties);
        if (!zoneCounts.Any())
            return new AssessmentGridResponseDto();

        var classificationByZone = await GetClassificationByZoneAsync(properties, assessmentType, cancellationToken);
        return BuildGridResponse(
            zoneCounts.Select(zone => CreateZoneData(zone, new[]
            {
                classificationByZone.GetValueOrDefault(zone.ZoneId) ?? CreateEmptyClassification(assessmentType)
            })),
            zoneData => CalculateTotalRow(zoneData, new[] { assessmentType }),
            zoneData => CalculateGrandTotalRow(zoneData, new[] { assessmentType }, assessmentType));
    }

    // Builds the Assessed tab with construction/use/RV based classification rows.
    private async Task<AssessmentGridResponseDto> GetAssessedAssessmentGridDataAsync(
        int workflowStageId,
        PropertySearchRequestDto? searchRequest,
        IReadOnlyDictionary<string, int> assessmentStatusIds,
        CancellationToken cancellationToken)
    {
        if (!assessmentStatusIds.TryGetValue(AssessmentTypeAssessed, out var assessedStatusId))
            return new AssessmentGridResponseDto();

        var assessedProperties = await _repository.GetAssessedClassificationPropertiesAsync(workflowStageId, assessedStatusId, cancellationToken, searchRequest);
        if (!assessedProperties.Any())
            return new AssessmentGridResponseDto();

        var classifiedProperties = await ClassifyAssessedPropertiesAsync(assessedProperties, cancellationToken);
        var oldDemandByProperty = await _repository.GetOldDemandByPropertyAsync(classifiedProperties, cancellationToken);
        var currentDemandByProperty = await _repository.GetCurrentDemandByPropertyAsync(classifiedProperties.Select(p => p.PropertyId), cancellationToken);
        var retroDemandByProperty = await _repository.GetRetroDemandByPropertyAsync(classifiedProperties.Select(p => p.PropertyId), cancellationToken);
        var classificationTypes = GetAssessedClassificationTypes();

        return BuildGridResponse(
            classifiedProperties.GroupBy(p => new { p.ZoneId, p.ZoneName, p.ZoneNo }).OrderBy(g => g.Key.ZoneName)
                .Select(zone => CreateAssessedZoneData(zone.Key.ZoneId, zone.Key.ZoneName, zone.Key.ZoneNo, zone, classificationTypes,
                    oldDemandByProperty, currentDemandByProperty, retroDemandByProperty)),
            zoneData => CalculateTotalRow(zoneData, classificationTypes),
            zoneData => CalculateGrandTotalRow(zoneData, classificationTypes, AssessmentTypeAssessed));
    }

    // Builds the Unassessed tab with property-type rows and no old demand.
    private async Task<AssessmentGridResponseDto> GetUnassessedAssessmentGridDataAsync(
        int workflowStageId,
        PropertySearchRequestDto? searchRequest,
        IReadOnlyDictionary<string, int> assessmentStatusIds,
        CancellationToken cancellationToken)
    {
        if (!assessmentStatusIds.TryGetValue(AssessmentTypeUnassessed, out var unassessedStatusId))
            return new AssessmentGridResponseDto();

        var unassessedProperties = await _repository.GetUnassessedPropertiesAsync(workflowStageId, unassessedStatusId, cancellationToken, searchRequest);
        if (!unassessedProperties.Any())
            return new AssessmentGridResponseDto();

        var classifiedProperties = await ClassifyUnassessedPropertiesAsync(unassessedProperties, cancellationToken);
        var currentDemandByProperty = await _repository.GetCurrentDemandByPropertyAsync(classifiedProperties.Select(p => p.PropertyId), cancellationToken);
        var retroDemandByProperty = await _repository.GetRetroDemandByPropertyAsync(classifiedProperties.Select(p => p.PropertyId), cancellationToken);
        var propertyTypes = GetUnassessedPropertyTypes();

        return BuildGridResponse(
            classifiedProperties.GroupBy(p => new { p.ZoneId, p.ZoneName, p.ZoneNo }).OrderBy(g => g.Key.ZoneName)
                .Select(zone => CreateUnassessedZoneData(zone.Key.ZoneId, zone.Key.ZoneName, zone.Key.ZoneNo, zone, propertyTypes,
                    currentDemandByProperty, retroDemandByProperty)),
            zoneData => CalculateUnassessedTotalRow(zoneData, propertyTypes),
            zoneData => CalculateUnassessedGrandTotalRow(zoneData, propertyTypes, AssessmentTypeUnassessed));
    }

    // Builds the Rented tab with Owner and Renter rows based on RenterMast tax liability.
    private async Task<AssessmentGridResponseDto> GetRentedAssessmentGridDataAsync(
        int workflowStageId,
        PropertySearchRequestDto? searchRequest,
        CancellationToken cancellationToken)
    {
        var rentedProperties = (await _repository.GetRentedPropertyDemandDataAsync(workflowStageId, cancellationToken, searchRequest))
            .Select(p => new RentedClassifiedPropertyProjection
            {
                PropertyId = p.PropertyId,
                PartitionNo = p.PartitionNo,
                ZoneId = p.ZoneId,
                ZoneName = p.ZoneName,
                ZoneNo = p.ZoneNo,
                ClassificationType = p.HasRenterTaxLiability ? RentedClassificationRenter : RentedClassificationOwner,
                OldDemand = p.OldDemand,
                CurrentDemand = p.CurrentDemand,
                RetroDemand = p.RetroDemand
            })
            .ToList();

        if (!rentedProperties.Any())
            return new AssessmentGridResponseDto();

        var classificationTypes = GetRentedClassificationTypes();

        return BuildGridResponse(
            rentedProperties.GroupBy(p => new { p.ZoneId, p.ZoneName, p.ZoneNo }).OrderBy(g => g.Key.ZoneName)
                .Select(zone => CreateRentedZoneData(zone.Key.ZoneId, zone.Key.ZoneName, zone.Key.ZoneNo, zone, classificationTypes)),
            zoneData => CalculateTotalRow(zoneData, classificationTypes),
            zoneData => CalculateGrandTotalRow(zoneData, classificationTypes, RentedGrandTotalClassificationType));
    }

    // Creates one classification row per zone with counts and all demand totals.
    private async Task<Dictionary<int, PropertyClassificationDto>> GetClassificationByZoneAsync(
        IEnumerable<AssessmentStagePropertyProjection> properties, string classificationType, CancellationToken cancellationToken)
    {
        var rows = properties.ToList();
        var countsByZone = GetZoneCounts(rows);
        var oldDemandByZone = await _repository.GetOldDemandByZoneAsync(rows, cancellationToken);
        var currentDemandByZone = await _repository.GetCurrentDemandByZoneAsync(rows, cancellationToken);
        var retroDemandByZone = await _repository.GetRetroDemandByZoneAsync(rows, cancellationToken);

        return countsByZone.ToDictionary(
            z => z.ZoneId,
            z => CreateClassification(classificationType, z.StructureCount, z.UnitCount,
                oldDemandByZone.GetValueOrDefault(z.ZoneId),
                currentDemandByZone.GetValueOrDefault(z.ZoneId),
                retroDemandByZone.GetValueOrDefault(z.ZoneId)));
    }

    // Assigns each assessed property to one exclusive classification bucket.
    private async Task<List<AssessedClassifiedPropertyProjection>> ClassifyAssessedPropertiesAsync(
        List<AssessedClassificationPropertyProjection> properties, CancellationToken cancellationToken)
    {
        var propertyIds = properties.Select(p => p.PropertyId).Distinct().ToList();
        var detailsByProperty = (await _repository.GetPropertyUseDetailsAsync(propertyIds, cancellationToken))
            .GroupBy(d => d.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    NewCarpetArea = g.Sum(x => x.CarpetArea),
                    NewUseTypes = g.Select(x => NormalizeUseType(x.TypeOfUseCode, x.Type, x.TypeOfUseDescription))
                        .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()
                });
        var currentRvByProperty = await _repository.GetCurrentRvByPropertyAsync(propertyIds, cancellationToken);

        return properties.Select(property =>
        {
            detailsByProperty.TryGetValue(property.PropertyId, out var details);
            currentRvByProperty.TryGetValue(property.PropertyId, out var currentRv);

            var oldUseType = NormalizeUseType(property.OldUseType);
            var hasAdditionalConstruction = details != null && property.OldConstructionArea.HasValue
                                            && details.NewCarpetArea > property.OldConstructionArea.Value;
            var hasChangeOfUse = !hasAdditionalConstruction && !string.IsNullOrWhiteSpace(oldUseType)
                                 && details?.NewUseTypes.Any(newUseType => newUseType != oldUseType) == true;
            var isUnderassessed = !hasAdditionalConstruction && !hasChangeOfUse && property.OldRV.HasValue
                                  && currentRv != default && Convert.ToDecimal(property.OldRV.Value) != currentRv;

            return new AssessedClassifiedPropertyProjection
            {
                PropertyId = property.PropertyId,
                PropertyMastOldId = property.PropertyMastOldId,
                PartitionNo = property.PartitionNo,
                ZoneId = property.ZoneId,
                ZoneName = property.ZoneName,
                ZoneNo = property.ZoneNo,
                ClassificationType = hasAdditionalConstruction
                    ? AssessedClassificationAdditionalConstruction
                    : hasChangeOfUse
                        ? AssessedClassificationChangeOfUse
                        : isUnderassessed
                            ? AssessedClassificationUnderassessed
                            : AssessedClassificationNoChange
            };
        }).ToList();
    }

    // Assigns each unassessed property to one property-type bucket.
    private async Task<List<UnassessedClassifiedPropertyProjection>> ClassifyUnassessedPropertiesAsync(
        List<UnassessedPropertyProjection> properties, CancellationToken cancellationToken)
    {
        var propertyIds = properties.Select(p => p.PropertyId).Distinct().ToList();
        var mixedPropertyIds = await _repository.GetMixedPropertyIdsAsync(propertyIds, cancellationToken);
        var detailsByProperty = (await _repository.GetPropertyUseDetailsAsync(propertyIds, cancellationToken))
            .GroupBy(x => x.PropertyId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    HasOpenPlot = g.Any(x => x.IsOpenPlot),
                    Types = g.Select(x => x.Type).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),
                    Codes = g.Select(x => x.TypeOfUseCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),
                    Descriptions = g.Select(x => x.TypeOfUseDescription).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()
                });

        return properties.Select(property =>
        {
            detailsByProperty.TryGetValue(property.PropertyId, out var details);
            var propertyType = mixedPropertyIds.Contains(property.PropertyId)
                ? PropertyTypeMixedUse
                : ResolveUnassessedPropertyType(property.IsOpenPlot, details?.HasOpenPlot == true, details?.Types, details?.Codes, details?.Descriptions);

            return new UnassessedClassifiedPropertyProjection
            {
                PropertyId = property.PropertyId,
                PartitionNo = property.PartitionNo,
                ZoneId = property.ZoneId,
                ZoneName = property.ZoneName,
                ZoneNo = property.ZoneNo,
                PropertyType = propertyType
            };
        }).ToList();
    }

    // Applies the selected assessment tab filter to already loaded stage properties.
    private static IEnumerable<AssessmentStagePropertyProjection> FilterPropertiesByAssessmentType(
        IEnumerable<AssessmentStagePropertyProjection> properties, string assessmentType, IReadOnlyDictionary<string, int> assessmentStatusIds)
    {
        return assessmentType switch
        {
            AssessmentTypeAssessed when assessmentStatusIds.TryGetValue(AssessmentTypeAssessed, out var assessedStatusId) =>
                properties.Where(p => p.AssessmentStatusId == assessedStatusId),
            AssessmentTypeUnassessed when assessmentStatusIds.TryGetValue(AssessmentTypeUnassessed, out var unassessedStatusId) =>
                properties.Where(p => p.AssessmentStatusId == unassessedStatusId),
            AssessmentTypeAssessed or AssessmentTypeUnassessed => Enumerable.Empty<AssessmentStagePropertyProjection>(),
            AssessmentTypeRented => properties.Where(p => p.IsRented),
            _ => properties
        };
    }

    // Counts structures and units per zone from filtered properties.
    private static List<AssessmentZoneCountProjection> GetZoneCounts(IEnumerable<AssessmentStagePropertyProjection> properties)
        => properties.GroupBy(p => new { p.ZoneId, p.ZoneName, p.ZoneNo })
            .Select(g => new AssessmentZoneCountProjection
            {
                ZoneId = g.Key.ZoneId,
                ZoneName = g.Key.ZoneName,
                ZoneNo = g.Key.ZoneNo,
                StructureCount = g.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
                UnitCount = g.Count()
            }).OrderBy(z => z.ZoneName).ToList();

    // Creates the common classification DTO and derived demand values.
    private static PropertyClassificationDto CreateClassification(string type, int structure, int unit, decimal oldDemand, decimal currentDemand, decimal retroDemand)
        => new()
        {
            Type = type,
            Structure = structure,
            Unit = unit,
            OldDemand = oldDemand,
            CurrentDemand = currentDemand,
            RetroDemand = retroDemand,
            TotalDemand = oldDemand + currentDemand + retroDemand,
            AdditionalRevenueGenerated = currentDemand - oldDemand
        };

    private static PropertyClassificationDto CreateEmptyClassification(string type)
        => CreateClassification(type, 0, 0, 0m, 0m, 0m);

    private static List<int> GetRawRequestedPropertyIds(SendToApproveRequestDto request)
        => request.PropertyIds ?? new List<int>();

    private static List<int> GetRequestedPropertyIds(SendToApproveRequestDto request)
        => GetRawRequestedPropertyIds(request)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

    private static SendToApproveResponseDto CreateSendToApproveResponse(
        SendToApproveRequestDto request,
        int signAuthorityId,
        IEnumerable<int> insertedPropertyIds,
        IEnumerable<int> missingPropertyIds,
        IEnumerable<int> alreadySentPropertyIds,
        IEnumerable<int> invalidPropertyIds,
        string message)
    {
        var requestedPropertyIds = GetRequestedPropertyIds(request);
        var insertedIds = insertedPropertyIds.Distinct().ToList();
        return new SendToApproveResponseDto
        {
            IsInserted = insertedIds.Any(),
            PropertyId = requestedPropertyIds.FirstOrDefault(),
            PropertyIds = requestedPropertyIds,
            UserId = request.UserId,
            SignAuthorityId = signAuthorityId,
            AuthorityCode = ClerkAuthorityCode,
            RequestedCount = requestedPropertyIds.Count,
            InsertedCount = insertedIds.Count,
            InsertedPropertyIds = insertedIds,
            MissingPropertyIds = missingPropertyIds.Distinct().ToList(),
            AlreadySentPropertyIds = alreadySentPropertyIds.Distinct().ToList(),
            InvalidPropertyIds = invalidPropertyIds.Distinct().ToList(),
            Message = message
        };
    }

    private static string CreateSendToApproveMessage(int insertedCount, int missingCount, int alreadySentCount, int invalidCount)
    {
        if (insertedCount > 0)
            return insertedCount == 1
                ? "1 property has been successfully sent for ULB approval."
                : $"{insertedCount} properties have been successfully sent for ULB approval.";

        if (alreadySentCount > 0 && missingCount == 0 && invalidCount == 0)
            return alreadySentCount == 1
                ? "Property is already sent for approval."
                : "All requested properties are already sent for approval.";

        return $"No properties were sent to approval. {missingCount} missing, {alreadySentCount} already sent, {invalidCount} invalid.";
    }

    // Normalizes user-sent tab type values.
    private static string? NormalizeAssessmentType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return null;

        return type.Trim().ToUpperInvariant() switch
        {
            "TOTAL" => AssessmentTypeTotal,
            "ASSESSED" => AssessmentTypeAssessed,
            "UNASSESSED" => AssessmentTypeUnassessed,
            "RENTED" => AssessmentTypeRented,
            _ => null
        };
    }

    // Returns fixed row order for the Assessed tab.
    private static List<string> GetAssessedClassificationTypes()
        => new()
        {
            AssessedClassificationAdditionalConstruction,
            AssessedClassificationChangeOfUse,
            AssessedClassificationNoChange,
            AssessedClassificationUnderassessed
        };

    // Returns fixed row order for the Unassessed tab.
    private static List<string> GetUnassessedPropertyTypes()
        => new()
        {
            PropertyTypeResidential,
            PropertyTypeCommercial,
            PropertyTypeIndustrial,
            PropertyTypeMixedUse,
            PropertyTypePublicUtility,
            PropertyTypeOpenPlots
        };

    // Returns fixed row order for the Rented tab.
    private static List<string> GetRentedClassificationTypes()
        => new()
        {
            RentedClassificationOwner,
            RentedClassificationRenter
        };

    // Converts TypeOfUse and open-plot flags into the unassessed property-type label.
    private static string ResolveUnassessedPropertyType(
        bool propertyOpenPlot,
        bool detailOpenPlot,
        IEnumerable<string?>? types,
        IEnumerable<string?>? codes,
        IEnumerable<string?>? descriptions)
    {
        var normalizedCodes = codes?.Select(x => x?.Trim().ToUpperInvariant()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new();
        var normalizedTypes = types?.Select(x => x?.Trim().ToUpperInvariant()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new();
        var normalizedDescriptions = descriptions?.Select(x => x?.Trim().ToUpperInvariant()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new();

        if (propertyOpenPlot || detailOpenPlot || normalizedDescriptions.Any(x => x!.Contains("OPEN")))
            return PropertyTypeOpenPlots;
        if (normalizedTypes.Contains("I") || normalizedDescriptions.Any(x => x!.Contains("INDUSTRIAL")))
            return PropertyTypeIndustrial;
        if (normalizedTypes.Contains("N") || normalizedCodes.Contains("PU") || normalizedDescriptions.Any(x => x!.Contains("PUBLIC")))
            return PropertyTypePublicUtility;
        if (normalizedTypes.Contains("R") || normalizedDescriptions.Any(x => x!.Contains("RESIDENTIAL")))
            return PropertyTypeResidential;
        if (normalizedTypes.Contains("C") || normalizedDescriptions.Any(x => x!.Contains("COMMERCIAL")))
            return PropertyTypeCommercial;

        return PropertyTypeResidential;
    }

    // Maps old/new use labels or codes to R, C, or I.
    private static string NormalizeUseType(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var normalized = value.Trim().ToUpperInvariant();
            if (normalized == "R" || normalized.StartsWith("R") || normalized.Contains("RESIDENTIAL"))
                return "R";
            if (normalized == "C" || normalized.StartsWith("C") || normalized.Contains("COMMERCIAL"))
                return "C";
            if (normalized == "I" || normalized.StartsWith("I") || normalized.Contains("INDUSTRIAL"))
                return "I";
        }

        return string.Empty;
    }

    // Builds response and summary rows from zone rows.
    private static AssessmentGridResponseDto BuildGridResponse(
        IEnumerable<AssessmentZoneDataDto> zoneData,
        Func<List<AssessmentZoneDataDto>, AssessmentZoneDataDto> totalRowFactory,
        Func<List<AssessmentZoneDataDto>, AssessmentZoneDataDto> grandTotalRowFactory)
    {
        var zones = zoneData.ToList();
        return new AssessmentGridResponseDto { ZoneData = zones, TotalRow = totalRowFactory(zones), GrandTotalRow = grandTotalRowFactory(zones) };
    }

    // Creates a normal zone row from precomputed zone counts.
    private static AssessmentZoneDataDto CreateZoneData(AssessmentZoneCountProjection zone, IEnumerable<PropertyClassificationDto> classifications)
        => new()
        {
            ZoneId = zone.ZoneId,
            ZoneName = zone.ZoneName,
            ZoneNo = zone.ZoneNo,
            TotalStructure = zone.StructureCount,
            TotalUnit = zone.UnitCount,
            Classifications = classifications.ToList()
        };

    // Creates an assessed zone row by grouping classified properties into fixed row types.
    private static AssessmentZoneDataDto CreateAssessedZoneData(
        int zoneId,
        string zoneName,
        string zoneNo,
        IEnumerable<AssessedClassifiedPropertyProjection> zone,
        List<string> classificationTypes,
        IReadOnlyDictionary<int, decimal> oldDemandByProperty,
        IReadOnlyDictionary<int, decimal> currentDemandByProperty,
        IReadOnlyDictionary<int, decimal> retroDemandByProperty)
    {
        var zoneRows = zone.ToList();
        return new AssessmentZoneDataDto
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            ZoneNo = zoneNo,
            TotalStructure = zoneRows.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
            TotalUnit = zoneRows.Count,
            Classifications = classificationTypes.Select(type => CreateAssessedClassification(
                type,
                zoneRows.Where(p => p.ClassificationType == type),
                oldDemandByProperty,
                currentDemandByProperty,
                retroDemandByProperty)).ToList()
        };
    }

    // Creates an unassessed zone row by grouping properties into property-type rows.
    private static AssessmentZoneDataDto CreateUnassessedZoneData(
        int zoneId, string zoneName, string zoneNo, IEnumerable<UnassessedClassifiedPropertyProjection> zone,
        List<string> propertyTypes, IReadOnlyDictionary<int, decimal> currentDemandByProperty,
        IReadOnlyDictionary<int, decimal> retroDemandByProperty)
    {
        var zoneRows = zone.ToList();
        return new AssessmentZoneDataDto
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            ZoneNo = zoneNo,
            TotalStructure = zoneRows.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
            TotalUnit = zoneRows.Count,
            Classifications = propertyTypes.Select(type => CreateUnassessedClassification(
                type,
                zoneRows.Where(p => p.PropertyType == type),
                currentDemandByProperty,
                retroDemandByProperty)).ToList()
        };
    }

    // Creates a rented zone row by grouping properties into Owner and Renter rows.
    private static AssessmentZoneDataDto CreateRentedZoneData(
        int zoneId,
        string zoneName,
        string zoneNo,
        IEnumerable<RentedClassifiedPropertyProjection> zone,
        List<string> classificationTypes)
    {
        var zoneRows = zone.ToList();
        return new AssessmentZoneDataDto
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            ZoneNo = zoneNo,
            TotalStructure = zoneRows.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
            TotalUnit = zoneRows.Count,
            Classifications = classificationTypes.Select(type => CreateRentedClassification(
                type,
                zoneRows.Where(p => p.ClassificationType == type))).ToList()
        };
    }

    // Builds one assessed classification row from classified properties and demand dictionaries.
    private static PropertyClassificationDto CreateAssessedClassification(
        string type,
        IEnumerable<AssessedClassifiedPropertyProjection> properties,
        IReadOnlyDictionary<int, decimal> oldDemandByProperty,
        IReadOnlyDictionary<int, decimal> currentDemandByProperty,
        IReadOnlyDictionary<int, decimal> retroDemandByProperty)
    {
        var rows = properties.ToList();
        return CreateClassification(
            type,
            rows.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
            rows.Count,
            rows.Sum(p => oldDemandByProperty.GetValueOrDefault(p.PropertyId)),
            rows.Sum(p => currentDemandByProperty.GetValueOrDefault(p.PropertyId)),
            rows.Sum(p => retroDemandByProperty.GetValueOrDefault(p.PropertyId)));
    }

    // Builds one unassessed property-type row without old demand.
    private static PropertyClassificationDto CreateUnassessedClassification(
        string type,
        IEnumerable<UnassessedClassifiedPropertyProjection> properties,
        IReadOnlyDictionary<int, decimal> currentDemandByProperty,
        IReadOnlyDictionary<int, decimal> retroDemandByProperty)
    {
        var rows = properties.ToList();
        var currentDemand = rows.Sum(p => currentDemandByProperty.GetValueOrDefault(p.PropertyId));
        var retroDemand = rows.Sum(p => retroDemandByProperty.GetValueOrDefault(p.PropertyId));
        return new PropertyClassificationDto
        {
            Type = type,
            Structure = rows.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
            Unit = rows.Count,
            OldDemand = null,
            CurrentDemand = currentDemand,
            RetroDemand = retroDemand,
            TotalDemand = currentDemand + retroDemand,
            AdditionalRevenueGenerated = currentDemand + retroDemand
        };
    }

    // Builds one rented Owner/Renter row from classified properties and demand dictionaries.
    private static PropertyClassificationDto CreateRentedClassification(
        string type,
        IEnumerable<RentedClassifiedPropertyProjection> properties)
    {
        var rows = properties.ToList();
        return CreateClassification(
            type,
            rows.Count(p => string.IsNullOrWhiteSpace(p.PartitionNo)),
            rows.Count,
            rows.Sum(p => p.OldDemand),
            rows.Sum(p => p.CurrentDemand),
            rows.Sum(p => p.RetroDemand));
    }

    private static AssessmentZoneDataDto CalculateTotalRow(List<AssessmentZoneDataDto> zoneData)
        => CalculateTotalRow(zoneData, new[] { AssessmentTypeAssessed, AssessmentTypeUnassessed, AssessmentTypeRented });

    // Sums selected classification rows into the Total summary.
    private static AssessmentZoneDataDto CalculateTotalRow(List<AssessmentZoneDataDto> zoneData, IEnumerable<string> classificationTypes)
        => new()
        {
            ZoneName = "TOTAL",
            TotalStructure = zoneData.Sum(z => z.TotalStructure),
            TotalUnit = zoneData.Sum(z => z.TotalUnit),
            Classifications = SumClassificationsByType(zoneData, classificationTypes)
        };

    private static AssessmentZoneDataDto CalculateGrandTotalRow(List<AssessmentZoneDataDto> zoneData)
        => CalculateGrandTotalRow(zoneData, new[] { AssessmentTypeAssessed, AssessmentTypeUnassessed }, GrandTotalClassificationType);

    // Sums selected classification rows into the Grand Total summary.
    private static AssessmentZoneDataDto CalculateGrandTotalRow(List<AssessmentZoneDataDto> zoneData, IEnumerable<string> includedTypes, string totalType)
        => new()
        {
            ZoneName = "GRAND TOTAL",
            TotalStructure = zoneData.Sum(z => z.TotalStructure),
            TotalUnit = zoneData.Sum(z => z.TotalUnit),
            Classifications = new List<PropertyClassificationDto>
            {
                SumClassifications(totalType, zoneData.SelectMany(z => z.Classifications).Where(c => includedTypes.Contains(c.Type)))
            }
        };

    // Sums unassessed property-type rows into the Total summary.
    private static AssessmentZoneDataDto CalculateUnassessedTotalRow(List<AssessmentZoneDataDto> zoneData, IEnumerable<string> propertyTypes)
        => new()
        {
            ZoneName = "TOTAL",
            TotalStructure = zoneData.Sum(z => z.TotalStructure),
            TotalUnit = zoneData.Sum(z => z.TotalUnit),
            Classifications = SumUnassessedClassificationsByType(zoneData, propertyTypes)
        };

    // Sums unassessed rows into the Grand Total summary.
    private static AssessmentZoneDataDto CalculateUnassessedGrandTotalRow(List<AssessmentZoneDataDto> zoneData, IEnumerable<string> includedTypes, string totalType)
        => new()
        {
            ZoneName = "GRAND TOTAL",
            TotalStructure = zoneData.Sum(z => z.TotalStructure),
            TotalUnit = zoneData.Sum(z => z.TotalUnit),
            Classifications = new List<PropertyClassificationDto>
            {
                SumUnassessedClassifications(totalType, zoneData.SelectMany(z => z.Classifications).Where(c => includedTypes.Contains(c.Type)))
            }
        };

    private static List<PropertyClassificationDto> SumClassificationsByType(List<AssessmentZoneDataDto> zoneData, IEnumerable<string> classificationTypes)
        => classificationTypes
            .Select(type => SumClassifications(type, zoneData.SelectMany(z => z.Classifications).Where(c => c.Type == type)))
            .ToList();

    private static List<PropertyClassificationDto> SumUnassessedClassificationsByType(List<AssessmentZoneDataDto> zoneData, IEnumerable<string> propertyTypes)
        => propertyTypes
            .Select(type => SumUnassessedClassifications(type, zoneData.SelectMany(z => z.Classifications).Where(c => c.Type == type)))
            .ToList();

    // Sums demand and count fields for one classification type.
    private static PropertyClassificationDto SumClassifications(string type, IEnumerable<PropertyClassificationDto> classifications)
    {
        var rows = classifications.ToList();
        return CreateClassification(
            type,
            rows.Sum(c => c.Structure),
            rows.Sum(c => c.Unit),
            rows.Sum(c => c.OldDemand ?? 0m),
            rows.Sum(c => c.CurrentDemand),
            rows.Sum(c => c.RetroDemand));
    }

    // Sums unassessed demand and count fields without old demand.
    private static PropertyClassificationDto SumUnassessedClassifications(string type, IEnumerable<PropertyClassificationDto> classifications)
    {
        var rows = classifications.ToList();
        var currentDemand = rows.Sum(c => c.CurrentDemand);
        var retroDemand = rows.Sum(c => c.RetroDemand);
        return new PropertyClassificationDto
        {
            Type = type,
            Structure = rows.Sum(c => c.Structure),
            Unit = rows.Sum(c => c.Unit),
            OldDemand = null,
            CurrentDemand = currentDemand,
            RetroDemand = retroDemand,
            TotalDemand = currentDemand + retroDemand,
            AdditionalRevenueGenerated = currentDemand + retroDemand
        };
    }
}
