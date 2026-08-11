using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.MonthWiseDemand;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for MonthWiseDemandDto / CreateMonthWiseDemandDto / UpdateMonthWiseDemandDto - the
/// MonthWiseDemandEntity DTOs (one month's rent demand for a lease).
///
/// NOTE: this file's MonthWiseDemandDto lives in the
/// NtisPlatform.Application.DTOs.Asset_Management.MonthWiseDemand namespace and is a DIFFERENT type
/// from the identically-named MonthWiseDemandDto in the
/// NtisPlatform.Application.DTOs.Asset_Management.LeaseRentDemand namespace (see
/// LeaseRentDemandDtosTests.cs for that one). Only the MonthWiseDemand namespace is imported here to
/// avoid an ambiguous-reference compile error.
/// </summary>
public class MonthWiseDemandDtoTests
{
    #region MonthWiseDemandDto (read)

    [Fact]
    public void MonthWiseDemandDto_PropertiesGetAndSetCorrectly()
    {
        var lastPaymentDate = DateTime.UtcNow.AddDays(-5);
        var dueDate = DateTime.UtcNow.AddDays(10);
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
            DueDate = dueDate,
            AssetName = "Shop 1",
            AssetNo = "AST-001",
            PenaltyRuleName = "Standard Penalty",
            GSTName = "GST 18%"
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
        Assert.Equal("Shop 1", dto.AssetName);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Standard Penalty", dto.PenaltyRuleName);
        Assert.Equal("GST 18%", dto.GSTName);
    }

    [Fact]
    public void MonthWiseDemandDto_Defaults_DemandStatusIsPending_OptionalFieldsAreNull()
    {
        // Unlike the LeaseRentDemand-namespace MonthWiseDemandDto (which defaults DemandStatus to
        // string.Empty), this read model defaults DemandStatus to "Pending".
        var dto = new MonthWiseDemandDto();

        Assert.Equal("Pending", dto.DemandStatus);
        Assert.Null(dto.PenaltyRuleMasterId);
        Assert.Null(dto.GSTMasterId);
        Assert.Null(dto.LastPaymentDate);
        Assert.Null(dto.DueDate);
        Assert.Null(dto.AssetName);
        Assert.Null(dto.AssetNo);
        Assert.Null(dto.PenaltyRuleName);
        Assert.Null(dto.GSTName);
    }

    #endregion

    #region CreateMonthWiseDemandDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            DemandStatus = "Pending"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroAssetId_IsInvalid()
    {
        // Representative of the Required+Range(1, int.MaxValue) pattern shared by LeaseRegistrationId
        // (own key) - [Required] never fires on a non-nullable int, so [Range] catches the default 0.
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 0,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.AssetId))
            && r.ErrorMessage == "AMS_MonthWiseDemand_AssetId_InvalidRange");
    }

    [Fact]
    public void Create_WithFinanceYearOutOfRange_IsInvalid()
    {
        // DemandYear shares the identical Required+Range(2000, 2100) shape (own key) and is not
        // independently re-tested here.
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 1999,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.FinanceYear))
            && r.ErrorMessage == "AMS_MonthWiseDemand_FinanceYear_InvalidRange");
    }

    [Fact]
    public void Create_WithQuarterNoOutOfRange_IsInvalid()
    {
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 5,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.QuarterNo))
            && r.ErrorMessage == "AMS_MonthWiseDemand_QuarterNo_InvalidRange");
    }

    [Fact]
    public void Create_WithDemandMonthOutOfRange_IsInvalid()
    {
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 0,
            MonthlyRentAmount = 500m,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.DemandMonth))
            && r.ErrorMessage == "AMS_MonthWiseDemand_DemandMonth_InvalidRange");
    }

    [Fact]
    public void Create_WithDefaultMonthlyRentAmount_PassesValidation_DespiteRequiredAttribute()
    {
        // MonthlyRentAmount is [Required] on a non-nullable decimal, paired with
        // [Range(0, double.MaxValue)] - 0 is inside that range, so leaving it unset (0m) passes
        // DataAnnotations validation entirely despite being documented as required.
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            DemandStatus = "Pending"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(0m, dto.MonthlyRentAmount);
    }

    [Fact]
    public void Create_WithZeroPenaltyRuleMasterId_IsInvalid()
    {
        // PenaltyRuleMasterId is a nullable int with only [Range(1, int.MaxValue)] - no [Required] -
        // so null (unset) is valid, but an explicit 0 is rejected.
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            PenaltyRuleMasterId = 0,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.PenaltyRuleMasterId))
            && r.ErrorMessage == "AMS_MonthWiseDemand_PenaltyRuleMasterId_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroGSTMasterId_IsInvalid()
    {
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            GSTMasterId = 0,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.GSTMasterId))
            && r.ErrorMessage == "AMS_MonthWiseDemand_GSTMasterId_InvalidRange");
    }

    [Fact]
    public void Create_WithNullPenaltyRuleMasterIdAndGSTMasterId_IsValid()
    {
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            DemandStatus = "Pending"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Null(dto.PenaltyRuleMasterId);
        Assert.Null(dto.GSTMasterId);
    }

    [Fact]
    public void Create_WithNegativePenaltyAmount_IsInvalid()
    {
        // Representative of the [Range(0, double.MaxValue)] pattern also used by GSTAmount and
        // PaidAmount (each with its own resource key, all optional with no [Required]).
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            PenaltyAmount = -1m,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.PenaltyAmount))
            && r.ErrorMessage == "AMS_MonthWiseDemand_PenaltyAmount_InvalidRange");
    }

    [Fact]
    public void Create_WithInvalidDemandStatus_IsInvalid()
    {
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            DemandStatus = "Overdue"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.DemandStatus))
            && r.ErrorMessage == "AMS_MonthWiseDemand_DemandStatus_Invalid");
    }

    [Fact]
    public void Create_WithEmptyDemandStatus_IsInvalid()
    {
        var dto = new CreateMonthWiseDemandDto
        {
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            DemandStatus = string.Empty
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateMonthWiseDemandDto.DemandStatus))
            && r.ErrorMessage == "AMS_MonthWiseDemand_DemandStatus_Required");
    }

    [Fact]
    public void Create_Defaults_DemandStatusIsPending_OptionalFieldsAreNull()
    {
        var dto = new CreateMonthWiseDemandDto();

        Assert.Equal("Pending", dto.DemandStatus);
        Assert.Null(dto.PenaltyRuleMasterId);
        Assert.Null(dto.GSTMasterId);
        Assert.Null(dto.LastPaymentDate);
        Assert.Null(dto.DueDate);
        Assert.Equal(0m, dto.PenaltyAmount);
        Assert.Equal(0m, dto.GSTAmount);
        Assert.Equal(0m, dto.PaidAmount);
    }

    #endregion

    #region UpdateMonthWiseDemandDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateMonthWiseDemandDto { MonthlyRentAmount = 500m, DemandStatus = "Pending" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithDefaultMonthlyRentAmount_PassesValidation_DespiteRequiredAttribute()
    {
        // Same [Required] decimal + [Range(0, double.MaxValue)] pattern as the Create DTO - 0
        // satisfies the range, so an omitted amount is not actually caught.
        var dto = new UpdateMonthWiseDemandDto { DemandStatus = "Pending" };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(0m, dto.MonthlyRentAmount);
    }

    [Fact]
    public void Update_WithZeroPenaltyRuleMasterId_IsInvalid()
    {
        var dto = new UpdateMonthWiseDemandDto
        {
            MonthlyRentAmount = 500m,
            PenaltyRuleMasterId = 0,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateMonthWiseDemandDto.PenaltyRuleMasterId))
            && r.ErrorMessage == "AMS_MonthWiseDemand_PenaltyRuleMasterId_InvalidRange");
    }

    [Fact]
    public void Update_WithNegativePenaltyAmount_IsInvalid()
    {
        var dto = new UpdateMonthWiseDemandDto
        {
            MonthlyRentAmount = 500m,
            PenaltyAmount = -1m,
            DemandStatus = "Pending"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateMonthWiseDemandDto.PenaltyAmount))
            && r.ErrorMessage == "AMS_MonthWiseDemand_PenaltyAmount_InvalidRange");
    }

    [Fact]
    public void Update_WithInvalidDemandStatus_IsInvalid()
    {
        var dto = new UpdateMonthWiseDemandDto { MonthlyRentAmount = 500m, DemandStatus = "Overdue" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateMonthWiseDemandDto.DemandStatus))
            && r.ErrorMessage == "AMS_MonthWiseDemand_DemandStatus_Invalid");
    }

    [Fact]
    public void Update_WithEmptyDemandStatus_IsInvalid()
    {
        var dto = new UpdateMonthWiseDemandDto { MonthlyRentAmount = 500m, DemandStatus = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateMonthWiseDemandDto.DemandStatus))
            && r.ErrorMessage == "AMS_MonthWiseDemand_DemandStatus_Required");
    }

    [Fact]
    public void Update_Defaults_DemandStatusIsPending_OptionalFieldsAreNull()
    {
        var dto = new UpdateMonthWiseDemandDto();

        Assert.Equal("Pending", dto.DemandStatus);
        Assert.Null(dto.PenaltyRuleMasterId);
        Assert.Null(dto.GSTMasterId);
        Assert.Null(dto.LastPaymentDate);
        Assert.Null(dto.DueDate);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
