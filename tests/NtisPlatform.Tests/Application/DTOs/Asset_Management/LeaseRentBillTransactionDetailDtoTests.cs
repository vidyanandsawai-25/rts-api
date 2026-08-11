using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.LeaseRentBillTransactionDetail;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for LeaseRentBillTransactionDetailDto / CreateLeaseRentBillTransactionDetailDto /
/// UpdateLeaseRentBillTransactionDetailDto - the month-wise payment detail line under a bill
/// transaction header.
/// </summary>
public class LeaseRentBillTransactionDetailDtoTests
{
    #region LeaseRentBillTransactionDetailDto (read)

    [Fact]
    public void LeaseRentBillTransactionDetailDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new LeaseRentBillTransactionDetailDto
        {
            Id = 1,
            IsActive = true,
            LeaseRentBillTransactionId = 2,
            MonthWiseDemandId = 3,
            AssetId = 4,
            LeaseRegistrationId = 5,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            PenaltyAmount = 10m,
            GSTAmount = 90m,
            TotalDemandAmount = 600m,
            PreviousPaidAmount = 0m,
            CurrentPaidAmount = 600m,
            BalanceAmount = 0m,
            PaymentStatus = "Paid",
            AssetName = "Shop 1",
            AssetNo = "AST-001"
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(2, dto.LeaseRentBillTransactionId);
        Assert.Equal(3, dto.MonthWiseDemandId);
        Assert.Equal(4, dto.AssetId);
        Assert.Equal(5, dto.LeaseRegistrationId);
        Assert.Equal(2025, dto.FinanceYear);
        Assert.Equal(2025, dto.DemandYear);
        Assert.Equal((byte)1, dto.QuarterNo);
        Assert.Equal((byte)4, dto.DemandMonth);
        Assert.Equal(500m, dto.MonthlyRentAmount);
        Assert.Equal(10m, dto.PenaltyAmount);
        Assert.Equal(90m, dto.GSTAmount);
        Assert.Equal(600m, dto.TotalDemandAmount);
        Assert.Equal(0m, dto.PreviousPaidAmount);
        Assert.Equal(600m, dto.CurrentPaidAmount);
        Assert.Equal(0m, dto.BalanceAmount);
        Assert.Equal("Paid", dto.PaymentStatus);
        Assert.Equal("Shop 1", dto.AssetName);
        Assert.Equal("AST-001", dto.AssetNo);
    }

    [Fact]
    public void LeaseRentBillTransactionDetailDto_Defaults_PaymentStatusIsPaid_NavigationFieldsAreNull()
    {
        var dto = new LeaseRentBillTransactionDetailDto();

        Assert.Equal("Paid", dto.PaymentStatus);
        Assert.Null(dto.AssetName);
        Assert.Null(dto.AssetNo);
    }

    #endregion

    #region CreateLeaseRentBillTransactionDetailDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 1,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            CurrentPaidAmount = 500m,
            PaymentStatus = "Paid"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroLeaseRentBillTransactionId_IsInvalid()
    {
        // Representative of the Required+Range(1, int.MaxValue) pattern shared by MonthWiseDemandId,
        // AssetId and LeaseRegistrationId (each has its own resource key) - [Required] never fires on
        // a non-nullable int, so [Range] is what actually rejects the default 0.
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 0,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            CurrentPaidAmount = 500m,
            PaymentStatus = "Paid"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDetailDto.LeaseRentBillTransactionId))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_LeaseRentBillTransactionId_InvalidRange");
    }

    [Fact]
    public void Create_WithFinanceYearOutOfRange_IsInvalid()
    {
        // DemandYear shares the identical Required+Range(2000, 2100) shape (own key) and is not
        // independently re-tested here.
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 1,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 1999,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            CurrentPaidAmount = 500m,
            PaymentStatus = "Paid"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDetailDto.FinanceYear))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_FinanceYear_InvalidRange");
    }

    [Fact]
    public void Create_WithQuarterNoOutOfRange_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 1,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 0,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            CurrentPaidAmount = 500m,
            PaymentStatus = "Paid"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDetailDto.QuarterNo))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_QuarterNo_InvalidRange");
    }

    [Fact]
    public void Create_WithDemandMonthOutOfRange_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 1,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 13,
            MonthlyRentAmount = 500m,
            CurrentPaidAmount = 500m,
            PaymentStatus = "Paid"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDetailDto.DemandMonth))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_DemandMonth_InvalidRange");
    }

    [Fact]
    public void Create_WithDefaultMonthlyRentAmountAndCurrentPaidAmount_PassesValidation_DespiteRequiredAttributes()
    {
        // Both are [Required] non-nullable decimals paired with [Range(0, double.MaxValue)] - unlike
        // PaidAmount elsewhere in this PR (Range starts at 0.01), 0 is *inside* this range, so leaving
        // either field unset (0m) passes DataAnnotations validation entirely: [Required] never fires on
        // a value type, and [Range(0, ...)] does not reject the default either. There is no functional
        // guard against an omitted amount here.
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 1,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            PaymentStatus = "Paid"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(0m, dto.MonthlyRentAmount);
        Assert.Equal(0m, dto.CurrentPaidAmount);
    }

    [Fact]
    public void Create_WithNegativePenaltyAmount_IsInvalid()
    {
        // Representative of the [Range(0, double.MaxValue)] pattern also used by GSTAmount,
        // PreviousPaidAmount and BalanceAmount (each with its own resource key, all optional).
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 1,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            CurrentPaidAmount = 500m,
            PenaltyAmount = -1m,
            PaymentStatus = "Paid"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDetailDto.PenaltyAmount))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_PenaltyAmount_InvalidRange");
    }

    [Fact]
    public void Create_WithInvalidPaymentStatus_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 1,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            CurrentPaidAmount = 500m,
            PaymentStatus = "Refunded"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDetailDto.PaymentStatus))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_PaymentStatus_Invalid");
    }

    [Fact]
    public void Create_WithEmptyPaymentStatus_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDetailDto
        {
            LeaseRentBillTransactionId = 1,
            MonthWiseDemandId = 1,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            DemandYear = 2025,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 500m,
            CurrentPaidAmount = 500m,
            PaymentStatus = string.Empty
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDetailDto.PaymentStatus))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_PaymentStatus_Required");
    }

    [Fact]
    public void Create_Defaults_PaymentStatusIsPaid()
    {
        var dto = new CreateLeaseRentBillTransactionDetailDto();

        Assert.Equal("Paid", dto.PaymentStatus);
        Assert.Equal(0m, dto.PenaltyAmount);
        Assert.Equal(0m, dto.GSTAmount);
        Assert.Equal(0m, dto.PreviousPaidAmount);
        Assert.Equal(0m, dto.BalanceAmount);
    }

    #endregion

    #region UpdateLeaseRentBillTransactionDetailDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateLeaseRentBillTransactionDetailDto { CurrentPaidAmount = 100m, PaymentStatus = "Paid" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithDefaultCurrentPaidAmount_PassesValidation_DespiteRequiredAttribute()
    {
        // Same [Required] decimal + [Range(0, double.MaxValue)] pattern as the Create DTO - 0
        // satisfies the range, so an omitted amount is not actually caught.
        var dto = new UpdateLeaseRentBillTransactionDetailDto { PaymentStatus = "Paid" };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(0m, dto.CurrentPaidAmount);
    }

    [Fact]
    public void Update_WithNegativePreviousPaidAmount_IsInvalid()
    {
        var dto = new UpdateLeaseRentBillTransactionDetailDto
        {
            PreviousPaidAmount = -1m,
            CurrentPaidAmount = 100m,
            PaymentStatus = "Paid"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateLeaseRentBillTransactionDetailDto.PreviousPaidAmount))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_PreviousPaidAmount_InvalidRange");
    }

    [Fact]
    public void Update_WithInvalidPaymentStatus_IsInvalid()
    {
        var dto = new UpdateLeaseRentBillTransactionDetailDto { CurrentPaidAmount = 100m, PaymentStatus = "Refunded" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateLeaseRentBillTransactionDetailDto.PaymentStatus))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_PaymentStatus_Invalid");
    }

    [Fact]
    public void Update_WithEmptyPaymentStatus_IsInvalid()
    {
        var dto = new UpdateLeaseRentBillTransactionDetailDto { CurrentPaidAmount = 100m, PaymentStatus = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateLeaseRentBillTransactionDetailDto.PaymentStatus))
            && r.ErrorMessage == "AMS_LeaseRentBillTransactionDetail_PaymentStatus_Required");
    }

    [Fact]
    public void Update_Defaults_PaymentStatusIsPaid()
    {
        var dto = new UpdateLeaseRentBillTransactionDetailDto();

        Assert.Equal("Paid", dto.PaymentStatus);
        Assert.Equal(0m, dto.PreviousPaidAmount);
        Assert.Equal(0m, dto.BalanceAmount);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
