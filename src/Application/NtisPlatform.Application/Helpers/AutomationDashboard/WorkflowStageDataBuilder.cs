using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Helpers.AutomationDashboard;

/// <summary>
/// Common data builder for all Automation Dashboard stages.
/// Provides methods for building zone, division, and ward data DTOs.
/// </summary>
public static class WorkflowStageDataBuilder
{
    #region GeoSequencing Data Builders

    /// <summary>
    /// Builds GeoSequencing zone data DTO
    /// </summary>
    public static GeoSequencingZoneDataDto BuildGeoSequencingZoneData(
        int zoneId,
        string zoneName,
        string zoneNo,
        int registeredCount,
        List<GeoSequencingStagePropertyProjection> properties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,
        Dictionary<string, int> statusIdsByName)
    {
        var propertyTypeBreakdown = WorkflowStagePropertyTypeBuilder.Build(
            properties,
            propertyUseGroups,
            p => p.PropertyId,
            p => p.PropertyTypeCode);

        var assessmentBreakdown = WorkflowStageAssessmentStatusBuilder.BuildBreakdown(
            properties,
            statusIdsByName,
            p => p.AssessmentStatusId,
            p => p.PartitionNo,
            p => p.PropertyCategoryName);

        return new GeoSequencingZoneDataDto
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            ZoneNo = zoneNo,
            RegisteredProperties = registeredCount,
            GeoSequencedProperties = new GeoSequencedPropertiesDto
            {
                StructureCount = WorkflowStageAssessmentStatusBuilder.CountStructures(properties, p => p.PartitionNo),
                UnitCount = properties.Count
            },
            PropertyTypeBreakdown = MapToPropertyTypesBreakdownDto(propertyTypeBreakdown),
            AssessmentStatusBreakdown = MapToAssessmentStatusBreakdownDto(assessmentBreakdown)
        };
    }

    /// <summary>
    /// Builds GeoSequencing ward data DTO
    /// </summary>
    public static GeoSequencingWardDataDto BuildGeoSequencingWardData(
        int wardId,
        string wardNo,
        int registeredCount,
        List<GeoSequencingStagePropertyProjection> properties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,
        Dictionary<string, int> statusIdsByName)
    {
        var propertyTypeBreakdown = WorkflowStagePropertyTypeBuilder.Build(
            properties,
            propertyUseGroups,
            p => p.PropertyId,
            p => p.PropertyTypeCode);

        var assessmentBreakdown = WorkflowStageAssessmentStatusBuilder.BuildBreakdown(
            properties,
            statusIdsByName,
            p => p.AssessmentStatusId,
            p => p.PartitionNo,
            p => p.PropertyCategoryName);

        return new GeoSequencingWardDataDto
        {
            WardId = wardId,
            WardNo = wardNo,
            RegisteredProperties = registeredCount,
            GeoSequencedProperties = new GeoSequencedPropertiesDto
            {
                StructureCount = WorkflowStageAssessmentStatusBuilder.CountStructures(properties, p => p.PartitionNo),
                UnitCount = properties.Count
            },
            PropertyTypeBreakdown = MapToPropertyTypesBreakdownDto(propertyTypeBreakdown),
            AssessmentStatusBreakdown = MapToAssessmentStatusBreakdownDto(assessmentBreakdown)
        };
    }

    /// <summary>
    /// Calculates GeoSequencing zone totals
    /// </summary>
    public static GeoSequencingZoneDataDto CalculateGeoSequencingZoneTotals(List<GeoSequencingZoneDataDto> zones)
    {
        return new GeoSequencingZoneDataDto
        {
            ZoneName = "TOTAL",
            RegisteredProperties = WorkflowStageTotalsCalculator.Sum(zones, z => z.RegisteredProperties),
            GeoSequencedProperties = new GeoSequencedPropertiesDto
            {
                StructureCount = WorkflowStageTotalsCalculator.Sum(zones, z => z.GeoSequencedProperties.StructureCount),
                UnitCount = WorkflowStageTotalsCalculator.Sum(zones, z => z.GeoSequencedProperties.UnitCount)
            },
            PropertyTypeBreakdown = MapToPropertyTypesBreakdownDto(
                WorkflowStageTotalsCalculator.SumPropertyTypeBreakdown(
                    zones,
                    z => MapFromPropertyTypesBreakdownDto(z.PropertyTypeBreakdown))),
            AssessmentStatusBreakdown = MapToAssessmentStatusBreakdownDto(
                WorkflowStageTotalsCalculator.SumAssessmentStatusBreakdown(
                    zones,
                    z => MapFromAssessmentStatusBreakdownDto(z.AssessmentStatusBreakdown)))
        };
    }

    /// <summary>
    /// Calculates GeoSequencing ward totals
    /// </summary>
    public static GeoSequencingWardDataDto CalculateGeoSequencingWardTotals(List<GeoSequencingWardDataDto> wards)
    {
        return new GeoSequencingWardDataDto
        {
            WardNo = "TOTAL",
            RegisteredProperties = WorkflowStageTotalsCalculator.Sum(wards, w => w.RegisteredProperties),
            GeoSequencedProperties = new GeoSequencedPropertiesDto
            {
                StructureCount = WorkflowStageTotalsCalculator.Sum(wards, w => w.GeoSequencedProperties.StructureCount),
                UnitCount = WorkflowStageTotalsCalculator.Sum(wards, w => w.GeoSequencedProperties.UnitCount)
            },
            PropertyTypeBreakdown = MapToPropertyTypesBreakdownDto(
                WorkflowStageTotalsCalculator.SumPropertyTypeBreakdown(
                    wards,
                    w => MapFromPropertyTypesBreakdownDto(w.PropertyTypeBreakdown))),
            AssessmentStatusBreakdown = MapToAssessmentStatusBreakdownDto(
                WorkflowStageTotalsCalculator.SumAssessmentStatusBreakdown(
                    wards,
                    w => MapFromAssessmentStatusBreakdownDto(w.AssessmentStatusBreakdown)))
        };
    }

    /// <summary>
    /// Checks if GeoSequencing ward has summary data
    /// </summary>
    public static bool HasGeoSequencingWardSummaryData(GeoSequencingWardDataDto ward)
        => GetGeoSequencingWardSummaryScore(ward) > 0;

    public static int GetGeoSequencingWardSummaryScore(GeoSequencingWardDataDto ward)
        => ward.GeoSequencedProperties.StructureCount
           + ward.GeoSequencedProperties.UnitCount
           + ward.PropertyTypeBreakdown.Residential
           + ward.PropertyTypeBreakdown.NonResidential
           + ward.PropertyTypeBreakdown.Mixed
           + ward.PropertyTypeBreakdown.PublicUtility
           + ward.PropertyTypeBreakdown.UnderConstruction
           + ward.AssessmentStatusBreakdown.Assessed.StructureCount
           + ward.AssessmentStatusBreakdown.Assessed.UnitCount
           + ward.AssessmentStatusBreakdown.Unassessed.StructureCount
           + ward.AssessmentStatusBreakdown.Unassessed.UnitCount
           + ward.AssessmentStatusBreakdown.NewlyAssessedFound.StructureCount
           + ward.AssessmentStatusBreakdown.NewlyAssessedFound.UnitCount
           + ward.AssessmentStatusBreakdown.AssessmentInProcess.StructureCount
           + ward.AssessmentStatusBreakdown.AssessmentInProcess.UnitCount;

    #endregion

    #region InternalSurvey Data Builders

    /// <summary>
    /// Builds InternalSurvey division data DTO
    /// </summary>
    public static InternalSurveyDivisionDataDto BuildInternalSurveyDivisionData(
        int divisionId,
        string divisionName,
        string zoneNo,
        List<InternalSurveyStagePropertyProjection> geoSequencingProperties,
        List<InternalSurveyStagePropertyProjection> internalSurveyProperties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,
        int assessedStatusId,
        int unassessedStatusId,
        int photoCount)
    {
        if (!internalSurveyProperties.Any())
        {
            return new InternalSurveyDivisionDataDto
            {
                DivisionId = divisionId,
                DivisionName = divisionName,
                ZoneNo = zoneNo,
                GeoSequencingProperties = new GeoSequencingPropertiesDto
                {
                    Structure = WorkflowStageAssessmentStatusBuilder.CountStructures(geoSequencingProperties, p => p.PartitionNo),
                    Unit = geoSequencingProperties.Count
                }
            };
        }

        var propertyTypeBreakdown = WorkflowStagePropertyTypeBuilder.Build(
            internalSurveyProperties,
            propertyUseGroups,
            p => p.PropertyId,
            p => p.PropertyTypeCode);

        return new InternalSurveyDivisionDataDto
        {
            DivisionId = divisionId,
            DivisionName = divisionName,
            ZoneNo = zoneNo,
            GeoSequencingProperties = new GeoSequencingPropertiesDto
            {
                Structure = WorkflowStageAssessmentStatusBuilder.CountStructures(geoSequencingProperties, p => p.PartitionNo),
                Unit = geoSequencingProperties.Count
            },
            SurveyProperties = new SurveyPropertiesDto
            {
                Structure = WorkflowStageAssessmentStatusBuilder.CountStructures(internalSurveyProperties, p => p.PartitionNo),
                Unit = internalSurveyProperties.Count
            },
            PropertyType = MapToPropertyTypesBreakdownDto(propertyTypeBreakdown),
            AssessedProperties = MapToAssessedPropertiesDto(
                WorkflowStageAssessmentStatusBuilder.GetStatusCounts(
                    internalSurveyProperties,
                    assessedStatusId,
                    p => p.AssessmentStatusId,
                    p => p.PartitionNo)),
            UnassessedProperties = MapToUnassessedPropertiesDto(
                WorkflowStageAssessmentStatusBuilder.GetStatusCounts(
                    internalSurveyProperties,
                    unassessedStatusId,
                    p => p.AssessmentStatusId,
                    p => p.PartitionNo)),
            NewlyAssessedFound = new NewlyAssessedFoundDto(),
            AssessmentInprocess = new AssessmentInprocessDto(),
            PhotoCount = photoCount
        };
    }

    /// <summary>
    /// Builds InternalSurvey ward data DTO
    /// </summary>
    public static InternalSurveyWardDataDto BuildInternalSurveyWardData(
        int wardId,
        string wardNo,
        List<InternalSurveyStagePropertyProjection> geoSequencingProperties,
        List<InternalSurveyStagePropertyProjection> internalSurveyProperties,
        Dictionary<int, PropertyUseGroup> propertyUseGroups,
        int assessedStatusId,
        int unassessedStatusId,
        int photoCount)
    {
        var wardData = new InternalSurveyWardDataDto
        {
            WardId = wardId,
            WardNo = wardNo,
            GeoSequencingProperties = new GeoSequencingPropertiesDto
            {
                Structure = WorkflowStageAssessmentStatusBuilder.CountStructures(geoSequencingProperties, p => p.PartitionNo),
                Unit = geoSequencingProperties.Count
            }
        };

        if (!internalSurveyProperties.Any())
            return wardData;

        var propertyTypeBreakdown = WorkflowStagePropertyTypeBuilder.Build(
            internalSurveyProperties,
            propertyUseGroups,
            p => p.PropertyId,
            p => p.PropertyTypeCode);

        wardData.SurveyProperties = new SurveyPropertiesDto
        {
            Structure = WorkflowStageAssessmentStatusBuilder.CountStructures(internalSurveyProperties, p => p.PartitionNo),
            Unit = internalSurveyProperties.Count
        };
        wardData.PropertyType = MapToPropertyTypesBreakdownDto(propertyTypeBreakdown);
        wardData.AssessedProperties = MapToAssessedPropertiesDto(
            WorkflowStageAssessmentStatusBuilder.GetStatusCounts(
                internalSurveyProperties,
                assessedStatusId,
                p => p.AssessmentStatusId,
                p => p.PartitionNo));
        wardData.UnassessedProperties = MapToUnassessedPropertiesDto(
            WorkflowStageAssessmentStatusBuilder.GetStatusCounts(
                internalSurveyProperties,
                unassessedStatusId,
                p => p.AssessmentStatusId,
                p => p.PartitionNo));
        wardData.NewlyAssessedFound = new NewlyAssessedFoundDto();
        wardData.AssessmentInprocess = new AssessmentInprocessDto();
        wardData.PhotoCount = photoCount;

        return wardData;
    }

    #endregion

    #region Mapping Helpers

    private static PropertyTypesBreakdownDto MapToPropertyTypesBreakdownDto(PropertyTypeBreakdown breakdown)
        => new()
        {
            Residential = breakdown.Residential,
            NonResidential = breakdown.NonResidential,
            Mixed = breakdown.Mixed,
            PublicUtility = breakdown.PublicUtility,
            UnderConstruction = breakdown.UnderConstruction
        };

    private static PropertyTypeBreakdown MapFromPropertyTypesBreakdownDto(PropertyTypesBreakdownDto dto)
        => new()
        {
            Residential = dto.Residential,
            NonResidential = dto.NonResidential,
            Mixed = dto.Mixed,
            PublicUtility = dto.PublicUtility,
            UnderConstruction = dto.UnderConstruction
        };

    private static AssessmentStatusBreakdownDto MapToAssessmentStatusBreakdownDto(AssessmentStatusBreakdown breakdown)
        => new()
        {
            Assessed = new StructureUnitCountDto
            {
                StructureCount = breakdown.Assessed.StructureCount,
                UnitCount = breakdown.Assessed.UnitCount
            },
            Unassessed = new StructureUnitCountDto
            {
                StructureCount = breakdown.Unassessed.StructureCount,
                UnitCount = breakdown.Unassessed.UnitCount
            },
            NewlyAssessedFound = new StructureUnitCountDto
            {
                StructureCount = breakdown.NewlyAssessedFound.StructureCount,
                UnitCount = breakdown.NewlyAssessedFound.UnitCount
            },
            AssessmentInProcess = new StructureUnitCountDto
            {
                StructureCount = breakdown.AssessmentInProcess.StructureCount,
                UnitCount = breakdown.AssessmentInProcess.UnitCount
            }
        };

    private static AssessmentStatusBreakdown MapFromAssessmentStatusBreakdownDto(AssessmentStatusBreakdownDto dto)
        => new()
        {
            Assessed = new StructureUnitCount
            {
                StructureCount = dto.Assessed.StructureCount,
                UnitCount = dto.Assessed.UnitCount
            },
            Unassessed = new StructureUnitCount
            {
                StructureCount = dto.Unassessed.StructureCount,
                UnitCount = dto.Unassessed.UnitCount
            },
            NewlyAssessedFound = new StructureUnitCount
            {
                StructureCount = dto.NewlyAssessedFound.StructureCount,
                UnitCount = dto.NewlyAssessedFound.UnitCount
            },
            AssessmentInProcess = new StructureUnitCount
            {
                StructureCount = dto.AssessmentInProcess.StructureCount,
                UnitCount = dto.AssessmentInProcess.UnitCount
            }
        };

    private static AssessedPropertiesSimpleDto MapToAssessedPropertiesDto(StructureUnitCount count)
        => new()
        {
            Structure = count.StructureCount,
            Units = count.UnitCount
        };

    private static UnassessedPropertiesDto MapToUnassessedPropertiesDto(StructureUnitCount count)
        => new()
        {
            Structure = count.StructureCount,
            Units = count.UnitCount
        };

    #endregion
}
