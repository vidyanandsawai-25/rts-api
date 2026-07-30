using AutoMapper;
using NtisPlatform.Application.DTOs.AutomationDashboard;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Interfaces.IAutomationDashboard;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Services
{
    /// <summary>
    /// Service for Automation Dashboard operations.
    /// Orchestrates calls to dedicated stage repositories.
    /// </summary>
    public class AutomationDashboardService : IAutomationDashboardService
    {
        private readonly IAutomationDashboardRepository _dashboardRepository;
        private readonly IDataEntryStageRepository _dataEntryRepository;
        private readonly IGeoSequencingStageService _geoSequencingStageService;
        private readonly IInternalSurveyStageService _internalSurveyStageService;
        private readonly IDataEntryStageService _dataEntryStageService;
        private readonly IAssessmentStageService _assessmentStageService;
        private readonly IMapper _mapper;

        public AutomationDashboardService(
            IAutomationDashboardRepository dashboardRepository,
            IDataEntryStageRepository dataEntryRepository,
            IGeoSequencingStageService geoSequencingStageService,
            IInternalSurveyStageService internalSurveyStageService,
            IDataEntryStageService dataEntryStageService,
            IAssessmentStageService assessmentStageService,
            IMapper mapper)
        {
            _dashboardRepository = dashboardRepository;
            _dataEntryRepository = dataEntryRepository;
            _geoSequencingStageService = geoSequencingStageService;
            _internalSurveyStageService = internalSurveyStageService;
            _dataEntryStageService = dataEntryStageService;
            _assessmentStageService = assessmentStageService;
            _mapper = mapper;
        }

        public Task<MainCardsResponseDto> GetMainCardsAsync()
            => _dashboardRepository.GetMainCardsAsync();

        public Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync()
            => _dashboardRepository.GetWorkflowCardsAsync();

        public Task<GeoSequencingGridResponseDto> GetGeoSequencingGridDataAsync(
            PropertySearchRequestDto? searchRequest = null, CancellationToken cancellationToken = default)
            => _geoSequencingStageService.GetGeoSequencingGridDataAsync(searchRequest, cancellationToken);

        public Task<GeoSequencingWardWiseSummaryResponseDto> GetGeoSequencingWardWiseSummaryAsync(
            int zoneId, int workflowStageId, int? pageNumber, int? pageSize, CancellationToken cancellationToken = default)
            => _geoSequencingStageService.GetGeoSequencingWardWiseSummaryAsync(zoneId, workflowStageId, pageNumber, pageSize, cancellationToken);

        public Task<InternalSurveyGridResponseDto> GetInternalSurveyGridDataAsync(
            PropertySearchRequestDto? searchRequest = null, CancellationToken cancellationToken = default)
            => _internalSurveyStageService.GetInternalSurveyGridDataAsync(searchRequest, cancellationToken);

        public Task<InternalSurveyWardWiseSummaryResponseDto> GetInternalSurveyWardWiseSummaryAsync(
            int zoneId, int workflowStageId, int? pageNumber, int? pageSize, CancellationToken cancellationToken = default)
            => _internalSurveyStageService.GetInternalSurveyWardWiseSummaryAsync(zoneId, workflowStageId, pageNumber, pageSize, cancellationToken);

        public Task<DataEntryGridResponseDto> GetDataEntryGridDataAsync(
            PropertySearchRequestDto? searchRequest = null, CancellationToken cancellationToken = default)
            => _dataEntryStageService.GetDataEntryGridDataAsync(searchRequest, cancellationToken);

        public Task<DataEntryWardWiseSummaryResponseDto> GetDataEntryWardWiseSummaryAsync(
            int zoneId, int workflowStageId, int? pageNumber, int? pageSize, CancellationToken cancellationToken = default)
            => _dataEntryRepository.GetDataEntryWardWiseSummaryAsync(zoneId, workflowStageId, pageNumber, pageSize, cancellationToken);

        public Task<AssessmentGridResponseDto> GetAssessmentGridDataAsync(
            PropertySearchRequestDto? searchRequest, string type, CancellationToken cancellationToken = default)
            => _assessmentStageService.GetAssessmentGridDataAsync(searchRequest, type, cancellationToken);

        public Task<SendToApproveResponseDto> SendToApproveAsync(
            SendToApproveRequestDto request,
            CancellationToken cancellationToken = default)
            => _assessmentStageService.SendToApproveAsync(request, cancellationToken);

        public Task<List<TrackStageStatusDto>> TrackStageStatusAsync(
            int propertyId,
            CancellationToken cancellationToken = default)
            => _dashboardRepository.TrackStageStatusAsync(propertyId, cancellationToken);

        public Task<SubGridPDDataDto> GetSubGridDataAsync(SubGridQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            var query = new SubGridFilterRequestDto
            {
                ZoneId = queryParameters.ZoneId,
                WorkflowStageId = queryParameters.WorkflowStageId,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize,
                WardId = queryParameters.WardId,
                PropertyTypeCategoryId = queryParameters.PropertyTypeCategoryId,
                PropertyTypeId = queryParameters.PropertyTypeId,
                AssessmentTypeId = queryParameters.AssessmentTypeId
            };

            return GetSubGridResponseAsync(() => _dashboardRepository.GetSubGridDataAsync(query, cancellationToken));
        }

        public Task<SubGridPDDataDto> GetWardSubGridDataAsync(
            WardSubGridQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            if (!queryParameters.WardId.HasValue || queryParameters.WardId.Value <= 0)
                throw new ArgumentException("WardId parameter is required");

            var query = new SubGridFilterRequestDto
            {
                WardId = queryParameters.WardId,
                WorkflowStageId = queryParameters.WorkflowStageId,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize,
                PropertyTypeCategoryId = queryParameters.PropertyTypeCategoryId,
                PropertyTypeId = queryParameters.PropertyTypeId,
                AssessmentTypeId = queryParameters.AssessmentTypeId
            };

            return GetSubGridResponseAsync(() => _dashboardRepository.GetSubGridDataAsync(query, cancellationToken));
        }

        public Task<PendingAssessmentSubGridPDDataDto> GetPendingAssessmentPropsAsync(
            int? pageNumber,
            int? pageSize,
            CancellationToken cancellationToken = default)
            => GetPendingAssessmentSubGridResponseAsync(() => _dashboardRepository.GetPendingAssessmentPropsAsync(
                pageNumber,
                pageSize,
                cancellationToken));

        private async Task<SubGridPDDataDto> GetSubGridResponseAsync(
            Func<Task<SubGridDataProjection>> fetchSnapshot)
            => BuildSubGridResponse(await fetchSnapshot());

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
    }
}
