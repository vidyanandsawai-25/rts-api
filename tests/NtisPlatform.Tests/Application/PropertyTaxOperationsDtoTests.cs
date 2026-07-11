using NtisPlatform.Application.DTOs.PropertyTaxOperations;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class PropertyTaxOperationsDtoTests
{
    [Fact]
    public void OperationScopeDto_Properties_WorkCorrectly()
    {
        var dto = new OperationScopeDto
        {
            ZoneIds = new List<int> { 1 },
            WardIds = new List<int> { 2 },
            PropertyTypeIds = new List<int> { 3 },
            AssessmentStatusIds = new List<int> { 4 },
            Building = new List<string> { "Main" },
            FromPropertyNo = "1",
            ToPropertyNo = "10",
            PropertyIds = new List<int> { 5 },
            SearchText = "Search",
            UpicIds = new List<string> { "UPIC" },
            MobileNumbers = new List<string> { "12345" }
        };

        Assert.Contains(1, dto.ZoneIds);
        Assert.Contains(2, dto.WardIds);
        Assert.Contains(3, dto.PropertyTypeIds);
        Assert.Contains(4, dto.AssessmentStatusIds);
        Assert.Contains("Main", dto.Building);
        Assert.Equal("1", dto.FromPropertyNo);
        Assert.Equal("10", dto.ToPropertyNo);
        Assert.Contains(5, dto.PropertyIds);
        Assert.Equal("Search", dto.SearchText);
        Assert.Contains("UPIC", dto.UpicIds);
        Assert.Contains("12345", dto.MobileNumbers);
    }

    [Fact]
    public void OperationsInitDto_Properties_WorkCorrectly()
    {
        var dto = new OperationsInitDto
        {
            FinanceYears = new List<FinanceYearOptionDto>
            {
                new() { Value = "2025", Label = "2025-26" }
            },
            Permissions = new OperationPermissionsDto { AddTax = true },
            Summary = new OperationsSummaryDto
            {
                TotalProperties = 100,
                EligibleRecords = 80,
                SkippedRecords = 20,
                RunningJobs = 1
            }
        };

        Assert.NotNull(dto.FinanceYears);
        Assert.Single(dto.FinanceYears);
        Assert.Equal("2025", dto.FinanceYears[0].Value);
        Assert.Equal("2025-26", dto.FinanceYears[0].Label);
        Assert.True(dto.Permissions.AddTax);
        Assert.Equal(100, dto.Summary.TotalProperties);
        Assert.Equal(80, dto.Summary.EligibleRecords);
        Assert.Equal(20, dto.Summary.SkippedRecords);
        Assert.Equal(1, dto.Summary.RunningJobs);
    }

    [Fact]
    public void EligibleCountRequestDto_Properties_WorkCorrectly()
    {
        var dto = new EligibleCountRequestDto
        {
            FinanceYearId = 1,
            ScopeType = "Scope",
            Scope = new OperationScopeDto()
        };

        Assert.Equal(1, dto.FinanceYearId);
        Assert.Equal("Scope", dto.ScopeType);
        Assert.NotNull(dto.Scope);
    }

    [Fact]
    public void EligibleCountResponseDto_Properties_WorkCorrectly()
    {
        var dto = new EligibleCountResponseDto
        {
            Total = 10,
            Eligible = 8,
            Skipped = 2
        };

        Assert.Equal(10, dto.Total);
        Assert.Equal(8, dto.Eligible);
        Assert.Equal(2, dto.Skipped);
    }

    [Fact]
    public void OperationPreviewRequestDto_Properties_WorkCorrectly()
    {
        var dto = new OperationPreviewRequestDto
        {
            FinanceYearId = 1,
            ScopeType = "Scope",
            Scope = new OperationScopeDto(),
            PageNumber = 1,
            PageSize = 10
        };

        Assert.Equal(1, dto.FinanceYearId);
        Assert.Equal("Scope", dto.ScopeType);
        Assert.NotNull(dto.Scope);
        Assert.Equal(1, dto.PageNumber);
        Assert.Equal(10, dto.PageSize);
    }

    [Fact]
    public void OperationPreviewResponseDto_Properties_WorkCorrectly()
    {
        var dto = new OperationPreviewResponseDto
        {
            TotalSelected = 100,
            Eligible = 90,
            Skipped = 10,
            Records = new List<JobPropertyPreviewDto>()
        };

        Assert.Equal(100, dto.TotalSelected);
        Assert.Equal(90, dto.Eligible);
        Assert.Equal(10, dto.Skipped);
        Assert.NotNull(dto.Records);
    }

    [Fact]
    public void ExecuteOperationRequestDto_Properties_WorkCorrectly()
    {
        var dto = new ExecuteOperationRequestDto
        {
            Operation = "Op",
            FinanceYearId = 1,
            ScopeType = "Scope",
            Scope = new OperationScopeDto(),
            Options = new OperationOptionsDto()
        };

        Assert.Equal("Op", dto.Operation);
        Assert.Equal(1, dto.FinanceYearId);
        Assert.Equal("Scope", dto.ScopeType);
        Assert.NotNull(dto.Scope);
        Assert.NotNull(dto.Options);
    }

    [Fact]
    public void ExecuteOperationResponseDto_Properties_WorkCorrectly()
    {
        var dto = new ExecuteOperationResponseDto
        {
            JobId = "JOB1",
            Status = "Pending",
            Summary = new JobSummaryDto { Total = 100 }
        };

        Assert.Equal("JOB1", dto.JobId);
        Assert.Equal("Pending", dto.Status);
        Assert.NotNull(dto.Summary);
        Assert.Equal(100, dto.Summary.Total);
    }

    [Fact]
    public void JobStatusDto_Properties_WorkCorrectly()
    {
        var dto = new JobStatusDto
        {
            JobId = "JOB1",
            Status = "InProgress",
            Total = 100,
            Processed = 50,
            Success = 45,
            Failed = 5,
            Pending = 50,
            Percentage = 50
        };

        Assert.Equal("JOB1", dto.JobId);
        Assert.Equal("InProgress", dto.Status);
        Assert.Equal(100, dto.Total);
        Assert.Equal(50, dto.Processed);
        Assert.Equal(45, dto.Success);
        Assert.Equal(5, dto.Failed);
        Assert.Equal(50, dto.Pending);
        Assert.Equal(50, dto.Percentage);
    }
}
