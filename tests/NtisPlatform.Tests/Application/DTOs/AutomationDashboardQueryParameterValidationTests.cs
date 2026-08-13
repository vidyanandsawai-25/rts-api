using System.ComponentModel.DataAnnotations;
using NtisPlatform.Core.Models.AutomationDashboard;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class AutomationDashboardQueryParameterValidationTests
{
    [Fact]
    public void DashboardGridQueryParameters_InvalidWorkflowStageId_FailsValidation()
    {
        var query = new DashboardGridQueryParameters { WorkflowStageId = 0 };

        var results = Validate(query);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DashboardGridQueryParameters.WorkflowStageId)));
    }

    [Fact]
    public void DashboardGridQueryParameters_MissingWorkflowStageId_FailsValidation()
    {
        var query = new DashboardGridQueryParameters();

        var results = Validate(query);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(DashboardGridQueryParameters.WorkflowStageId)));
    }

    [Fact]
    public void AssessmentGridQueryParameters_InvalidIdsAndLongSearchText_FailValidation()
    {
        var query = new AssessmentGridQueryParameters
        {
            WorkflowStageId = 0,
            PropertyTypeId = 0,
            PropertyTypeCategoryId = 0,
            TypeOfUseId = 0,
            CategoryId = 0,
            PropertyNo = new string('P', 101),
            OwnerName = new string('O', 251),
            Type = new string('T', 51)
        };

        var results = Validate(query);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.WorkflowStageId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.PropertyTypeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.PropertyTypeCategoryId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.TypeOfUseId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.CategoryId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.PropertyNo)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.OwnerName)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.Type)));
    }

    [Fact]
    public void AssessmentGridQueryParameters_MissingWorkflowStageIdAndType_FailValidation()
    {
        var query = new AssessmentGridQueryParameters();

        var results = Validate(query);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.WorkflowStageId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssessmentGridQueryParameters.Type)));
    }

    [Fact]
    public void WardWiseSummaryQueryParameters_InvalidZoneAndWorkflowStage_FailValidation()
    {
        var query = new WardWiseSummaryQueryParameters
        {
            ZoneId = 0,
            WorkflowStageId = 0,
            PropertyTypeId = 0,
            PropertyTypeCategoryId = 0
        };

        var results = Validate(query);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WardWiseSummaryQueryParameters.ZoneId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WardWiseSummaryQueryParameters.WorkflowStageId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WardWiseSummaryQueryParameters.PropertyTypeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WardWiseSummaryQueryParameters.PropertyTypeCategoryId)));
    }

    [Fact]
    public void SubGridQueryParameters_InvalidFilters_FailValidation()
    {
        var query = new SubGridQueryParameters
        {
            ZoneId = 0,
            WorkflowStageId = 0,
            WardId = 0,
            PropertyTypeCategoryId = 0,
            PropertyTypeId = 0,
            AssessmentTypeId = 0,
            PropertyNo = new string('P', 101),
            OwnerName = new string('O', 251)
        };

        var results = Validate(query);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubGridQueryParameters.ZoneId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubGridQueryParameters.WorkflowStageId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubGridQueryParameters.WardId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubGridQueryParameters.PropertyTypeCategoryId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubGridQueryParameters.PropertyTypeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubGridQueryParameters.AssessmentTypeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubGridQueryParameters.PropertyNo)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubGridQueryParameters.OwnerName)));
    }

    [Fact]
    public void WardSubGridQueryParameters_MissingWardId_FailsValidation()
    {
        var query = new WardSubGridQueryParameters { WorkflowStageId = 1 };

        var results = Validate(query);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WardSubGridQueryParameters.WardId)));
    }

    [Fact]
    public void PendingAssessmentQueryParameters_InvalidFilters_FailValidation()
    {
        var query = new PendingAssessmentQueryParameters
        {
            SearchTerm = new string('S', 251),
            SurveyTypeId = 0,
            ZoneId = 0,
            ZoneNo = new string('Z', 51),
            WardId = 0,
            WardNo = new string('W', 51),
            PropertyTypeId = 0
        };

        var results = Validate(query);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PendingAssessmentQueryParameters.SearchTerm)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PendingAssessmentQueryParameters.SurveyTypeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PendingAssessmentQueryParameters.ZoneId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PendingAssessmentQueryParameters.ZoneNo)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PendingAssessmentQueryParameters.WardId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PendingAssessmentQueryParameters.WardNo)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(PendingAssessmentQueryParameters.PropertyTypeId)));
    }

    [Fact]
    public void SendToApproveRequestDto_InvalidPropertyIdsAndUserId_FailValidation()
    {
        var request = new SendToApproveRequestDto
        {
            PropertyIds = new List<int> { 10, 0 },
            UserId = 0
        };

        var results = Validate(request);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SendToApproveRequestDto.PropertyIds)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SendToApproveRequestDto.UserId)));
    }

    [Fact]
    public void SendToApproveRequestDto_ValidRequest_PassesValidation()
    {
        var request = new SendToApproveRequestDto
        {
            PropertyIds = new List<int> { 10, 11 },
            UserId = 99
        };

        var results = Validate(request);

        Assert.Empty(results);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }
}

