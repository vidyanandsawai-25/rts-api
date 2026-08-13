using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services
{
    /// <summary>
    /// Service for Automation Dashboard operations.
    /// Orchestrates calls to dedicated stage repositories.
    /// </summary>
    public class AutomationDashboardService : IAutomationDashboardService
    {
        private const string AssessedStatusName = "ASSESSED";
        private const string UnassessedStatusName = "UNASSESSED";

        private readonly IAutomationDashboardRepository _dashboardRepository;
        private readonly IGeoSequencingStageService _geoSequencingStageService;
        private readonly IInternalSurveyStageService _internalSurveyStageService;
        private readonly IDataEntryStageService _dataEntryStageService;
        private readonly IAssessmentStageService _assessmentStageService;

        public AutomationDashboardService(
            IAutomationDashboardRepository dashboardRepository,
            IGeoSequencingStageService geoSequencingStageService,
            IInternalSurveyStageService internalSurveyStageService,
            IDataEntryStageService dataEntryStageService,
            IAssessmentStageService assessmentStageService)
        {
            _dashboardRepository = dashboardRepository;
            _geoSequencingStageService = geoSequencingStageService;
            _internalSurveyStageService = internalSurveyStageService;
            _dataEntryStageService = dataEntryStageService;
            _assessmentStageService = assessmentStageService;
        }

        #region Public API Methods

        /// <summary>
        /// Gets main dashboard cards (Previously Registered, Assessment Approved, Additional Revenue).
        /// </summary>
        public async Task<MainCardsResponseDto> GetMainCardsAsync()
        {
            var assessmentStatusIds = await _dashboardRepository.ReadAssessmentStatusIdsAsync();

            var previouslyRegistered = await _dashboardRepository.ReadPreviouslyRegisteredBreakdownAsync();
            var assessed = await _dashboardRepository.ReadPropertyBreakdownByAssessmentStatusAsync(
                assessmentStatusIds.GetValueOrDefault(AssessedStatusName),
                includeDemand: true);
            var unassessed = await _dashboardRepository.ReadPropertyBreakdownByAssessmentStatusAsync(
                assessmentStatusIds.GetValueOrDefault(UnassessedStatusName));
            var additionalRevenue = await _dashboardRepository.ReadAcdApprovedPropertyBreakdownAsync();

            return new MainCardsResponseDto
            {
                PreviouslyRegistered = MapDashboardCardBreakdown(previouslyRegistered),
                AssessmentApproved = new AssessmentApprovedDto
                {
                    Assessed = MapDashboardCardBreakdown(assessed),
                    Unassessed = MapDashboardCardBreakdown(unassessed)
                },
                AdditionalRevenueGenerated = MapDashboardCardBreakdown(additionalRevenue)
            };
        }

        public async Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync()
        {
            var stages = await _dashboardRepository.ReadWorkflowStagesAsync();
            var countsByStageId = await _dashboardRepository.ReadWorkflowStageCountsAsync(
                stages.Select(s => s.WorkflowStageId));

            return stages.Select(stage =>
            {
                countsByStageId.TryGetValue(stage.WorkflowStageId, out var counts);

                return new WorkflowStageCardDto
                {
                    Id = stage.WorkflowStageId,
                    StageName = stage.StageName,
                    StructureCount = counts?.StructureCount ?? 0,
                    UnitCount = counts?.UnitCount ?? 0
                };
            }).ToList();
        }

        public Task<GeoSequencingGridResponseDto> GetGeoSequencingGridDataAsync(
            DashboardGridQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => _geoSequencingStageService.GetGeoSequencingGridDataAsync(queryParameters, cancellationToken);

        public Task<GeoSequencingWardWiseSummaryResponseDto> GetGeoSequencingWardWiseSummaryAsync(
            WardWiseSummaryQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => _geoSequencingStageService.GetGeoSequencingWardWiseSummaryAsync(queryParameters, cancellationToken);

        public Task<InternalSurveyGridResponseDto> GetInternalSurveyGridDataAsync(
            DashboardGridQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => _internalSurveyStageService.GetInternalSurveyGridDataAsync(queryParameters, cancellationToken);

        public Task<InternalSurveyWardWiseSummaryResponseDto> GetInternalSurveyWardWiseSummaryAsync(
            WardWiseSummaryQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => _internalSurveyStageService.GetInternalSurveyWardWiseSummaryAsync(queryParameters, cancellationToken);

        public Task<DataEntryGridResponseDto> GetDataEntryGridDataAsync(
            DashboardGridQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => _dataEntryStageService.GetDataEntryGridDataAsync(queryParameters, cancellationToken);

        public Task<DataEntryWardWiseSummaryResponseDto> GetDataEntryWardWiseSummaryAsync(
            WardWiseSummaryQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => _dataEntryStageService.GetDataEntryWardWiseSummaryAsync(queryParameters, cancellationToken);

        public Task<AssessmentGridResponseDto> GetAssessmentGridDataAsync(
            AssessmentGridQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => _assessmentStageService.GetAssessmentGridDataAsync(queryParameters, cancellationToken);

        public Task<SendToApproveResponseDto> SendToApproveAsync(
            SendToApproveRequestDto request,
            CancellationToken cancellationToken = default)
            => _assessmentStageService.SendToApproveAsync(request, cancellationToken);

        /// <summary>
        /// Tracks property status across all workflow stages.
        /// </summary>
        public Task<List<TrackStageStatusDto>> TrackStageStatusAsync(
            int propertyId,
            CancellationToken cancellationToken = default)
            => GetTrackStageStatusAsync(propertyId, cancellationToken);

        #endregion

        #region SubGrid Operations

        public Task<SubGridPDDataDto> GetSubGridDataAsync(SubGridQueryParameters queryParameters,CancellationToken cancellationToken = default)
            => GetSubGridResponseAsync(() => _dashboardRepository.GetSubGridDataAsync(queryParameters, cancellationToken));

        public Task<SubGridPDDataDto> GetWardSubGridDataAsync(
            WardSubGridQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
            => GetSubGridResponseAsync(() => _dashboardRepository.GetSubGridDataAsync(queryParameters, cancellationToken));

        public Task<PendingAssessmentSubGridPDDataDto> GetPendingAssessmentPropsAsync(
            PendingAssessmentQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
            => GetPendingAssessmentSubGridResponseAsync(() => _dashboardRepository.GetPendingAssessmentPropsAsync(queryParameters, cancellationToken));

        #endregion

        #region Private Helper Methods

        private async Task<SubGridPDDataDto> GetSubGridResponseAsync(
            Func<Task<SubGridDataProjection>> fetchSnapshot)
            => BuildSubGridResponse(await fetchSnapshot());

        private async Task<List<TrackStageStatusDto>> GetTrackStageStatusAsync(
            int propertyId,
            CancellationToken cancellationToken)
        {
            var stages = await _dashboardRepository.ReadWorkflowStageCompletionsAsync(propertyId, cancellationToken);

            return stages.Select(stage => new TrackStageStatusDto
            {
                WorkflowStageId = stage.WorkflowStageId,
                StageName = stage.StageName,
                DisplayOrder = stage.DisplayOrder,
                IsCompleted = stage.IsCompleted ? 1 : 0
            }).ToList();
        }

        private static DashboardCardBreakdownDto MapDashboardCardBreakdown(DashboardCardBreakdownProjection projection)
            => new()
            {
                PropertyCount = projection.PropertyCount,
                StructureCount = projection.StructureCount,
                UnitCount = projection.UnitCount,
                Demand = projection.Demand
            };

        private async Task<PendingAssessmentSubGridPDDataDto> GetPendingAssessmentSubGridResponseAsync(
            Func<Task<SubGridDataProjection>> fetchSnapshot)
            => BuildPendingAssessmentSubGridResponse(await fetchSnapshot());

        private SubGridPDDataDto BuildSubGridResponse(SubGridDataProjection snapshot)
        {
            var propertyDtos = BuildSubGridPropertyDetails(snapshot)
                .OrderBy(x => x.PropertyNo)
                .ToList();

            return new SubGridPDDataDto
            {
                WorkflowStageId = snapshot.WorkflowStageId,
                WorkflowStageName = snapshot.WorkflowStageName,
                ZoneId = snapshot.ZoneId,
                ZoneName = snapshot.ZoneName,
                ZoneNo = snapshot.ZoneNo,
                WardId = snapshot.WardId,
                WardNo = snapshot.WardNo,
                Properties = propertyDtos,
                TotalCount = snapshot.TotalCount
            };
        }

        private PendingAssessmentSubGridPDDataDto BuildPendingAssessmentSubGridResponse(SubGridDataProjection snapshot)
        {
            var propertyDtos = BuildPendingAssessmentSubGridPropertyDetails(snapshot)
                .OrderBy(x => x.PropertyNo)
                .ToList();

            return new PendingAssessmentSubGridPDDataDto
            {
                WorkflowStageId = snapshot.WorkflowStageId,
                WorkflowStageName = snapshot.WorkflowStageName,
                ZoneId = snapshot.ZoneId,
                ZoneName = snapshot.ZoneName,
                ZoneNo = snapshot.ZoneNo,
                WardId = snapshot.WardId,
                WardNo = snapshot.WardNo,
                Properties = propertyDtos,
                TotalCount = snapshot.TotalCount
            };
        }

        private List<SubGridPropertyDetailsDto> BuildSubGridPropertyDetails(SubGridDataProjection snapshot)
            => BuildSubGridPropertyRows(snapshot)
                .Select(row => row.Property)
                .ToList();

        private List<PendingAssessmentSubGridPropertyDetailsDto> BuildPendingAssessmentSubGridPropertyDetails(SubGridDataProjection snapshot)
            => BuildSubGridPropertyRows(snapshot)
                .Select(row =>
                {
                    var property = row.Property;
                    return new PendingAssessmentSubGridPropertyDetailsDto
                    {
                        PropertyId = property.PropertyId,
                        WardId = property.WardId,
                        WardNo = property.WardNo,
                        PropertyNo = property.PropertyNo,
                        Category = property.Category,
                        PropertyDescription = property.PropertyDescription,
                        PropertyType = property.PropertyType,
                        OwnerName = property.OwnerName,
                        OccupierName = property.OccupierName,
                        MobileNo = property.MobileNo,
                        Address = property.Address,
                        FlatOrShopName = property.FlatOrShopName,
                        WingName = property.WingName,
                        AssessmentStatus = property.AssessmentStatus,
                        FloorCount = property.FloorCount,
                        DocumentGuid = property.DocumentGuid,
                        PlanDocumentGuid = property.PlanDocumentGuid,
                        AdditionalRevenue = property.AdditionalRevenue,
                        PropertyDetailsComparison = property.PropertyDetailsComparison,
                        QcChecklist = row.QcChecklist
                    };
                })
                .ToList();

        private List<SubGridPropertyRow> BuildSubGridPropertyRows(SubGridDataProjection snapshot)
        {
            var detailCountDict = snapshot.DetailCounts.ToDictionary(x => x.PropertyId, x => x.Count);
            var docDict = snapshot.Documents
                .Where(x => !string.IsNullOrWhiteSpace(x.DocumentGuid))
                .GroupBy(x => x.PropertyId)
                .ToDictionary(g => g.Key, g => g.First().DocumentGuid);
            var planDocDict = snapshot.PlanDocuments
                .Where(x => !string.IsNullOrWhiteSpace(x.DocumentGuid))
                .GroupBy(x => x.PropertyId)
                .ToDictionary(g => g.Key, g => g.First().DocumentGuid);
            var propertyMapDict = snapshot.PropertyMaps
                .Where(x => x.PropertyIdOld.HasValue)
                .GroupBy(x => x.PropertyIdNew)
                .ToDictionary(g => g.Key, g => g.First().PropertyIdOld!.Value);
            var newDetailsDict = snapshot.NewDetails
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First());
            var newRvDict = snapshot.NewRvValues
                .GroupBy(x => x.PropertyId)
                .ToDictionary(g => g.Key, g => g.First().Amount.ToString("0.00"));
            var newCTaxDict = SumTax(snapshot.NewCurrentTaxes);
            var newRTaxDict = SumTax(snapshot.NewPendingTaxes);
            var oldDetailsDict = snapshot.OldDetails
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First());
            var oldCTaxDict = SumTax(snapshot.OldCurrentTaxes);
            var oldRTaxDict = SumTax(snapshot.OldPendingTaxes);
            var applyTaxesPropertyIdSet = snapshot.ApplyTaxesPropertyIds.ToHashSet();
            var assessmentDetailsDict = snapshot.AssessmentDetails
                .GroupBy(x => x.PropertyId)
                .ToDictionary(g => g.Key, g => g.First());

            return snapshot.Properties.Select(p =>
            {
                var propertyNo = (string.IsNullOrEmpty(p.WardNo) ? "" : p.WardNo) + "-" +
                                 (string.IsNullOrEmpty(p.PropertyNo) ? "" : p.PropertyNo) + "-" +
                                 (string.IsNullOrEmpty(p.PartitionNo) ? "" : p.PartitionNo);

                detailCountDict.TryGetValue(p.Id, out var detailCount);
                docDict.TryGetValue(p.Id, out var docGuid);
                planDocDict.TryGetValue(p.Id, out var planDocGuid);

                var newArea = "N/A";
                var newUse = "N/A";
                var hasNewDetails = newDetailsDict.TryGetValue(p.Id, out var newDetail);
                if (hasNewDetails)
                {
                    newArea = newDetail!.Area > 0 ? newDetail.Area.ToString("0.00") : "N/A";
                    newUse = !string.IsNullOrEmpty(newDetail.Use) ? newDetail.Use : "N/A";
                }

                newRvDict.TryGetValue(p.Id, out var newRv);
                newCTaxDict.TryGetValue(p.Id, out var newCTax);
                newRTaxDict.TryGetValue(p.Id, out var newRTax);
                var newTotalTax = SumTaxText(newCTax, newRTax);

                var oldArea = "N/A";
                var oldUse = "N/A";
                var oldRV = "N/A";
                var oldCTax = "N/A";
                var oldRTax = "N/A";
                var oldTotalTax = "N/A";

                if (propertyMapDict.TryGetValue(p.Id, out var oldPropertyId))
                {
                    if (oldDetailsDict.TryGetValue(oldPropertyId, out var oldDetail))
                    {
                        oldArea = oldDetail.Area > 0 ? oldDetail.Area.ToString("0.00") : "N/A";
                        oldUse = !string.IsNullOrEmpty(oldDetail.Use) ? oldDetail.Use : "N/A";
                        oldRV = oldDetail.OldRV > 0 ? oldDetail.OldRV.ToString("0.00") : "N/A";
                    }

                    oldCTaxDict.TryGetValue(oldPropertyId, out var ctax);
                    oldRTaxDict.TryGetValue(oldPropertyId, out var rtax);
                    oldCTax = ctax ?? "N/A";
                    oldRTax = rtax ?? "N/A";
                    oldTotalTax = SumTaxText(oldCTax, oldRTax);
                }

                var oldTotalTaxValue = decimal.TryParse(oldTotalTax, out var parsedOldTotalTax) ? parsedOldTotalTax : 0;
                var newTotalTaxValue = decimal.TryParse(newTotalTax, out var parsedNewTotalTax) ? parsedNewTotalTax : 0;
                assessmentDetailsDict.TryGetValue(p.Id, out var assessmentDetail);

                var property = new SubGridPropertyDetailsDto
                {
                    PropertyId = p.Id,
                    WardId = p.WardId,
                    WardNo = p.WardNo,
                    PropertyNo = propertyNo,
                    Category = p.CategoryName,
                    PropertyDescription = p.TypeDescription,
                    PropertyType = p.TypeName,
                    OwnerName = p.OwnerName,
                    OccupierName = p.OccupierName,
                    MobileNo = p.MobileNo,
                    Address = p.Address,
                    FlatOrShopName = p.FlatOrShopName,
                    WingName = p.WingName,
                    AssessmentStatus = p.AssessmentStatusName,
                    FloorCount = detailCount,
                    DocumentGuid = docGuid,
                    PlanDocumentGuid = planDocGuid,
                    AdditionalRevenue = newTotalTaxValue - oldTotalTaxValue,
                    PropertyDetailsComparison = new PropertyDetailsComparisonDto
                    {
                        NewRecord = new PropertyDetailsValueDto
                        {
                            Area = newArea,
                            Use = newUse,
                            RV = newRv ?? "N/A",
                            CTax = newCTax ?? "N/A",
                            RTax = newRTax ?? "N/A",
                            TotalTax = newTotalTax
                        },
                        OldRecord = new PropertyDetailsValueDto
                        {
                            Area = oldArea,
                            Use = oldUse,
                            RV = oldRV,
                            CTax = oldCTax,
                            RTax = oldRTax,
                            TotalTax = oldTotalTax
                        }
                    }
                };

                var qcChecklist = new AssessmentQcChecklistDto
                {
                    SiteQc = true,
                    ApplyTaxes = applyTaxesPropertyIdSet.Contains(p.Id),
                    OfficeQc = !string.IsNullOrWhiteSpace(p.AssessmentStatusName),
                    DataUpdated = hasNewDetails,
                    AddTaxes = newCTaxDict.ContainsKey(p.Id),
                    OcCcBill = assessmentDetail?.PartOCDate.HasValue == true
                               || assessmentDetail?.ApplyTaxesFrom.HasValue == true
                };

                return new SubGridPropertyRow(property, qcChecklist);
            }).ToList();
        }

        private sealed record SubGridPropertyRow(
            SubGridPropertyDetailsDto Property,
            AssessmentQcChecklistDto QcChecklist);

        private static Dictionary<int, string> SumTax(IEnumerable<SubGridTaxValueProjection> values)
            => values
                .GroupBy(x => x.PropertyId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount).ToString("0.00"));

        private static string SumTaxText(string? currentTax, string? pendingTax)
        {
            if (decimal.TryParse(currentTax ?? "0", out var currentTaxValue) &&
                decimal.TryParse(pendingTax ?? "0", out var pendingTaxValue))
            {
                return (currentTaxValue + pendingTaxValue).ToString("0.00");
            }

            return "N/A";
        }

        #endregion
    }
}

