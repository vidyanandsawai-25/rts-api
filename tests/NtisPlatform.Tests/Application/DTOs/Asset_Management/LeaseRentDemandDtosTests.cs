using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.LeaseRentDemand;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in LeaseRentDemand/LeaseRentDemandDtos.cs: MonthWiseDemandDto (read model),
/// GenerateDemandDto, GenerateDemandResultDto and DemandSummaryDto.
///
/// NOTE: this file's MonthWiseDemandDto lives in the
/// NtisPlatform.Application.DTOs.Asset_Management.LeaseRentDemand namespace and is a DIFFERENT type
/// from the identically-named MonthWiseDemandDto in the
/// NtisPlatform.Application.DTOs.Asset_Management.MonthWiseDemand namespace (see
/// MonthWiseDemandDtoTests.cs for that one). Only the LeaseRentDemand namespace is imported here to
/// avoid an ambiguous-reference compile error.
/// </summary>
public class LeaseRentDemandDtosTests
{
    #region MonthWiseDemandDto (LeaseRentDemand read model)

    [Fact]
    public void MonthWiseDemandDto_PropertiesGetAndSetCorrectly()
    {
        var lastPaymentDate = DateTime.Now.AddDays(-5);
        var dueDate = DateTime.Now.AddDays(10);
        var dto = new MonthWiseDemandDto
        {
            Id = 1,
            IsActive = true,
            AssetId = 10,
            LeaseRegistrationId = 20,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            PenaltyRuleMasterId = 3,
            PenaltyAmount = 10m,
            GSTMasterId = 4,
            GSTAmount = 90m,
            TotalDemandAmount = 600m,
            PaidAmount = 300m,
            PendingAmount = 300m,
            DemandStatus = "Partial",
            LastPaymentDate = lastPaymentDate,
            DueDate = dueDate
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(10, dto.AssetId);
        Assert.Equal(20, dto.LeaseRegistrationId);
        Assert.Equal(2025, dto.FinanceYear);
        Assert.Equal(2025, dto.DemandYear);
        Assert.Equal((byte)1, dto.QuarterNo);
        Assert.Equal((byte)4, dto.DemandMonth);
        Assert.Equal(500m, dto.MonthlyRentAmount);
        Assert.Equal(3, dto.PenaltyRuleMasterId);
        Assert.Equal(10m, dto.PenaltyAmount);
        Assert.Equal(4, dto.GSTMasterId);
        Assert.Equal(90m, dto.GSTAmount);
        Assert.Equal(600m, dto.TotalDemandAmount);
        Assert.Equal(300m, dto.PaidAmount);
        Assert.Equal(300m, dto.PendingAmount);
        Assert.Equal("Partial", dto.DemandStatus);
        Assert.Equal(lastPaymentDate, dto.LastPaymentDate);
        Assert.Equal(dueDate, dto.DueDate);
    }

    [Fact]
    public void MonthWiseDemandDto_Defaults_NullableFieldsAreNull_DemandStatusIsEmptyString()
    {
        // Unlike the MonthWiseDemand-namespace MonthWiseDemandDto (which defaults DemandStatus to
        // "Pending"), this read model defaults DemandStatus to string.Empty - the two same-named
        // types are not shape-identical despite the coincidental name collision.
        var dto = new MonthWiseDemandDto();

        Assert.Equal(string.Empty, dto.DemandStatus);
        Assert.Null(dto.PenaltyRuleMasterId);
        Assert.Null(dto.GSTMasterId);
        Assert.Null(dto.LastPaymentDate);
        Assert.Null(dto.DueDate);
    }

    #endregion

    #region GenerateDemandDto

    [Fact]
    public void GenerateDemandDto_WithValidFinanceYear_IsValid()
    {
        var dto = new GenerateDemandDto { FinanceYear = 2025 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void GenerateDemandDto_WithFinanceYearBelowMinimum_IsInvalid()
    {
        var dto = new GenerateDemandDto { FinanceYear = 1999 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(GenerateDemandDto.FinanceYear))
            && r.ErrorMessage == "AMS_GenerateDemand_FinanceYear_InvalidRange");
    }

    [Fact]
    public void GenerateDemandDto_WithFinanceYearAboveMaximum_IsInvalid()
    {
        var dto = new GenerateDemandDto { FinanceYear = 2101 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(GenerateDemandDto.FinanceYear))
            && r.ErrorMessage == "AMS_GenerateDemand_FinanceYear_InvalidRange");
    }

    [Fact]
    public void GenerateDemandDto_WithDefaultFinanceYear_IsInvalid()
    {
        // FinanceYear has only [Range(2000, 2100)] - no [Required] at all - so unlike most other Id/Year
        // fields in this PR, the default value (0) is fully guarded here with no dead attribute involved.
        var dto = new GenerateDemandDto();

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(GenerateDemandDto.FinanceYear))
            && r.ErrorMessage == "AMS_GenerateDemand_FinanceYear_InvalidRange");
    }

    #endregion

    #region GenerateDemandResultDto

    [Fact]
    public void GenerateDemandResultDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new GenerateDemandResultDto
        {
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            Created = 5,
            Updated = 3,
            SkippedOutOfWindow = 1,
            TotalRows = 9
        };

        Assert.Equal(1, dto.LeaseRegistrationId);
        Assert.Equal(2025, dto.FinanceYear);
        Assert.Equal(5, dto.Created);
        Assert.Equal(3, dto.Updated);
        Assert.Equal(1, dto.SkippedOutOfWindow);
        Assert.Equal(9, dto.TotalRows);
    }

    #endregion

    #region DemandSummaryDto

    [Fact]
    public void DemandSummaryDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new DemandSummaryDto
        {
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            CurrentFinanceYear = 2025,
            TotalRent = 1000m,
            TotalPenalty = 50m,
            TotalGst = 90m,
            TotalDemand = 1140m,
            TotalPaid = 600m,
            TotalPending = 540m,
            CurrentDemand = 570m,
            CurrentPaid = 300m,
            CurrentPending = 270m,
            CurrentPenalty = 25m,
            CurrentGst = 45m,
            PendingDemand = 570m,
            PendingPaid = 300m,
            PendingOutstanding = 270m,
            PendingPenalty = 25m,
            PendingGst = 45m,
            DemandCount = 12,
            PaidCount = 6,
            PartialCount = 2,
            PendingCount = 4
        };

        Assert.Equal(1, dto.LeaseRegistrationId);
        Assert.Equal(2025, dto.FinanceYear);
        Assert.Equal(2025, dto.CurrentFinanceYear);
        Assert.Equal(1000m, dto.TotalRent);
        Assert.Equal(50m, dto.TotalPenalty);
        Assert.Equal(90m, dto.TotalGst);
        Assert.Equal(1140m, dto.TotalDemand);
        Assert.Equal(600m, dto.TotalPaid);
        Assert.Equal(540m, dto.TotalPending);
        Assert.Equal(570m, dto.CurrentDemand);
        Assert.Equal(300m, dto.CurrentPaid);
        Assert.Equal(270m, dto.CurrentPending);
        Assert.Equal(25m, dto.CurrentPenalty);
        Assert.Equal(45m, dto.CurrentGst);
        Assert.Equal(570m, dto.PendingDemand);
        Assert.Equal(300m, dto.PendingPaid);
        Assert.Equal(270m, dto.PendingOutstanding);
        Assert.Equal(25m, dto.PendingPenalty);
        Assert.Equal(45m, dto.PendingGst);
        Assert.Equal(12, dto.DemandCount);
        Assert.Equal(6, dto.PaidCount);
        Assert.Equal(2, dto.PartialCount);
        Assert.Equal(4, dto.PendingCount);
    }

    [Fact]
    public void DemandSummaryDto_Defaults_FinanceYearIsNull()
    {
        var dto = new DemandSummaryDto();

        Assert.Null(dto.FinanceYear);
        Assert.Equal(0, dto.CurrentFinanceYear);
        Assert.Equal(0m, dto.TotalDemand);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
