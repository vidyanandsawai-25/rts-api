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
                },
                AssessedProperties = new AssessedPropertiesSimpleDto { StatusId = assessedStatusId },
                UnassessedProperties = new UnassessedPropertiesDto { StatusId = unassessedStatusId }
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
            },
            AssessedProperties = new AssessedPropertiesSimpleDto { StatusId = assessedStatusId },
            UnassessedProperties = new UnassessedPropertiesDto { StatusId = unassessedStatusId }
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

    public static InternalSurveyDivisionDataDto CalculateInternalSurveyDivisionTotals(List<InternalSurveyDivisionDataDto> divisionData)
        => new()
        {
            DivisionName = "TOTAL",
            GeoSequencingProperties = new GeoSequencingPropertiesDto
            {
                Structure = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.GeoSequencingProperties.Structure),
                Unit = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.GeoSequencingProperties.Unit)
            },
            SurveyProperties = new SurveyPropertiesDto
            {
                Structure = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.SurveyProperties.Structure),
                Unit = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.SurveyProperties.Unit)
            },
            PropertyType = new PropertyTypesBreakdownDto
            {
                Residential = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.PropertyType.Residential),
                NonResidential = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.PropertyType.NonResidential),
                Mixed = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.PropertyType.Mixed),
                PublicUtility = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.PropertyType.PublicUtility),
                UnderConstruction = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.PropertyType.UnderConstruction)
            },
            AssessedProperties = new AssessedPropertiesSimpleDto
            {
                StatusId = divisionData.Select(d => d.AssessedProperties.StatusId).FirstOrDefault(id => id > 0),
                Structure = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.AssessedProperties.Structure),
                Units = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.AssessedProperties.Units)
            },
            UnassessedProperties = new UnassessedPropertiesDto
            {
                StatusId = divisionData.Select(d => d.UnassessedProperties.StatusId).FirstOrDefault(id => id > 0),
                Structure = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.UnassessedProperties.Structure),
                Units = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.UnassessedProperties.Units)
            },
            NewlyAssessedFound = new NewlyAssessedFoundDto
            {
                Structure = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.NewlyAssessedFound.Structure),
                Unit = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.NewlyAssessedFound.Unit)
            },
            AssessmentInprocess = new AssessmentInprocessDto
            {
                Structure = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.AssessmentInprocess.Structure),
                Unit = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.AssessmentInprocess.Unit)
            },
            PhotoCount = WorkflowStageTotalsCalculator.Sum(divisionData, d => d.PhotoCount)
        };

    public static InternalSurveyWardDataDto CalculateInternalSurveyWardTotals(List<InternalSurveyWardDataDto> wardData)
        => new()
        {
            WardNo = "TOTAL",
            GeoSequencingProperties = new GeoSequencingPropertiesDto
            {
                Structure = WorkflowStageTotalsCalculator.Sum(wardData, w => w.GeoSequencingProperties.Structure),
                Unit = WorkflowStageTotalsCalculator.Sum(wardData, w => w.GeoSequencingProperties.Unit)
            },
            SurveyProperties = new SurveyPropertiesDto
            {
                Structure = WorkflowStageTotalsCalculator.Sum(wardData, w => w.SurveyProperties.Structure),
                Unit = WorkflowStageTotalsCalculator.Sum(wardData, w => w.SurveyProperties.Unit)
            },
            PropertyType = new PropertyTypesBreakdownDto
            {
                Residential = WorkflowStageTotalsCalculator.Sum(wardData, w => w.PropertyType.Residential),
                NonResidential = WorkflowStageTotalsCalculator.Sum(wardData, w => w.PropertyType.NonResidential),
                Mixed = WorkflowStageTotalsCalculator.Sum(wardData, w => w.PropertyType.Mixed),
                PublicUtility = WorkflowStageTotalsCalculator.Sum(wardData, w => w.PropertyType.PublicUtility),
                UnderConstruction = WorkflowStageTotalsCalculator.Sum(wardData, w => w.PropertyType.UnderConstruction)
            },
            AssessedProperties = new AssessedPropertiesSimpleDto
            {
                StatusId = wardData.Select(w => w.AssessedProperties.StatusId).FirstOrDefault(id => id > 0),
                Structure = WorkflowStageTotalsCalculator.Sum(wardData, w => w.AssessedProperties.Structure),
                Units = WorkflowStageTotalsCalculator.Sum(wardData, w => w.AssessedProperties.Units)
            },
            UnassessedProperties = new UnassessedPropertiesDto
            {
                StatusId = wardData.Select(w => w.UnassessedProperties.StatusId).FirstOrDefault(id => id > 0),
                Structure = WorkflowStageTotalsCalculator.Sum(wardData, w => w.UnassessedProperties.Structure),
                Units = WorkflowStageTotalsCalculator.Sum(wardData, w => w.UnassessedProperties.Units)
            },
            NewlyAssessedFound = new NewlyAssessedFoundDto
            {
                Structure = WorkflowStageTotalsCalculator.Sum(wardData, w => w.NewlyAssessedFound.Structure),
                Unit = WorkflowStageTotalsCalculator.Sum(wardData, w => w.NewlyAssessedFound.Unit)
            },
            AssessmentInprocess = new AssessmentInprocessDto
            {
                Structure = WorkflowStageTotalsCalculator.Sum(wardData, w => w.AssessmentInprocess.Structure),
                Unit = WorkflowStageTotalsCalculator.Sum(wardData, w => w.AssessmentInprocess.Unit)
            },
            PhotoCount = WorkflowStageTotalsCalculator.Sum(wardData, w => w.PhotoCount)
        };

    public static int GetInternalSurveyWardSummaryScore(InternalSurveyWardDataDto ward)
        => ward.GeoSequencingProperties.Structure
           + ward.GeoSequencingProperties.Unit
           + ward.SurveyProperties.Structure
           + ward.SurveyProperties.Unit
           + ward.PropertyType.Residential
           + ward.PropertyType.NonResidential
           + ward.PropertyType.Mixed
           + ward.PropertyType.PublicUtility
           + ward.PropertyType.UnderConstruction
           + ward.AssessedProperties.Structure
           + ward.AssessedProperties.Units
           + ward.UnassessedProperties.Structure
           + ward.UnassessedProperties.Units
           + ward.NewlyAssessedFound.Structure
           + ward.NewlyAssessedFound.Unit
           + ward.AssessmentInprocess.Structure
           + ward.AssessmentInprocess.Unit
           + ward.PhotoCount;

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
                StatusId = breakdown.Assessed.StatusId,
                StructureCount = breakdown.Assessed.StructureCount,
                UnitCount = breakdown.Assessed.UnitCount
            },
            Unassessed = new StructureUnitCountDto
            {
                StatusId = breakdown.Unassessed.StatusId,
                StructureCount = breakdown.Unassessed.StructureCount,
                UnitCount = breakdown.Unassessed.UnitCount
            },
            NewlyAssessedFound = new StructureUnitCountDto
            {
                StatusId = breakdown.NewlyAssessedFound.StatusId,
                StructureCount = breakdown.NewlyAssessedFound.StructureCount,
                UnitCount = breakdown.NewlyAssessedFound.UnitCount
            },
            AssessmentInProcess = new StructureUnitCountDto
            {
                StatusId = breakdown.AssessmentInProcess.StatusId,
                StructureCount = breakdown.AssessmentInProcess.StructureCount,
                UnitCount = breakdown.AssessmentInProcess.UnitCount
            }
        };

    private static AssessmentStatusBreakdown MapFromAssessmentStatusBreakdownDto(AssessmentStatusBreakdownDto dto)
        => new()
        {
            Assessed = new StructureUnitCount
            {
                StatusId = dto.Assessed.StatusId,
                StructureCount = dto.Assessed.StructureCount,
                UnitCount = dto.Assessed.UnitCount
            },
            Unassessed = new StructureUnitCount
            {
                StatusId = dto.Unassessed.StatusId,
                StructureCount = dto.Unassessed.StructureCount,
                UnitCount = dto.Unassessed.UnitCount
            },
            NewlyAssessedFound = new StructureUnitCount
            {
                StatusId = dto.NewlyAssessedFound.StatusId,
                StructureCount = dto.NewlyAssessedFound.StructureCount,
                UnitCount = dto.NewlyAssessedFound.UnitCount
            },
            AssessmentInProcess = new StructureUnitCount
            {
                StatusId = dto.AssessmentInProcess.StatusId,
                StructureCount = dto.AssessmentInProcess.StructureCount,
                UnitCount = dto.AssessmentInProcess.UnitCount
            }
        };

    private static AssessedPropertiesSimpleDto MapToAssessedPropertiesDto(StructureUnitCount count)
        => new()
        {
            StatusId = count.StatusId,
            Structure = count.StructureCount,
            Units = count.UnitCount
        };

    private static UnassessedPropertiesDto MapToUnassessedPropertiesDto(StructureUnitCount count)
        => new()
        {
            StatusId = count.StatusId,
            Structure = count.StructureCount,
            Units = count.UnitCount
        };

    #endregion
}
