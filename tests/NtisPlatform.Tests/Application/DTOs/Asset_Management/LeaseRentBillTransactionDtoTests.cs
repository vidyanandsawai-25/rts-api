using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.LeaseRentBillTransaction;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for LeaseRentBillTransactionDto / CreateLeaseRentBillTransactionDto /
/// UpdateLeaseRentBillTransactionDto - the payment header for lease-rent collection.
/// </summary>
public class LeaseRentBillTransactionDtoTests
{
    #region LeaseRentBillTransactionDto (read)

    [Fact]
    public void LeaseRentBillTransactionDto_PropertiesGetAndSetCorrectly()
    {
        var paymentDate = DateTime.Now;
        var cancelledDate = DateTime.Now.AddDays(-1);
        var chequeDate = DateTime.Now.AddDays(-2);
        var createdDate = DateTime.Now.AddDays(-10);
        var dto = new LeaseRentBillTransactionDto
        {
            Id = 1,
            IsActive = true,
            CreatedDate = createdDate,
            UpdatedDate = createdDate,
            TransactionNo = "TXN-001",
            ReceiptNo = "RCPT-001",
            AssetId = 10,
            LeaseRegistrationId = 20,
            FinanceYear = 2025,
            TotalMonthlyRentAmount = 500m,
            TotalPenaltyAmount = 50m,
            TotalGSTAmount = 90m,
            TotalDemandAmount = 640m,
            DiscountAmount = 20m,
            AdjustmentAmount = 10m,
            NetPayableAmount = 610m,
            PaidAmount = 610m,
            PaymentMode = "Cash",
            PaymentDate = paymentDate,
            BankName = "SBI",
            BranchName = "Main Branch",
            ChequeOrTransactionNo = "CHQ-1",
            ChequeDate = chequeDate,
            OnlineTransactionId = "TXN123",
            PaymentGatewayName = "Razorpay",
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            PaymentStatus = "Success",
            CancelledBy = 5,
            CancelledDate = cancelledDate,
            CancellationReason = "Duplicate",
            Remark = "Remark",
            AssetName = "Shop 1",
            AssetNo = "AST-001"
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(createdDate, dto.CreatedDate);
        Assert.Equal(createdDate, dto.UpdatedDate);
        Assert.Equal("TXN-001", dto.TransactionNo);
        Assert.Equal("RCPT-001", dto.ReceiptNo);
        Assert.Equal(10, dto.AssetId);
        Assert.Equal(20, dto.LeaseRegistrationId);
        Assert.Equal(2025, dto.FinanceYear);
        Assert.Equal(500m, dto.TotalMonthlyRentAmount);
        Assert.Equal(50m, dto.TotalPenaltyAmount);
        Assert.Equal(90m, dto.TotalGSTAmount);
        Assert.Equal(640m, dto.TotalDemandAmount);
        Assert.Equal(20m, dto.DiscountAmount);
        Assert.Equal(10m, dto.AdjustmentAmount);
        Assert.Equal(610m, dto.NetPayableAmount);
        Assert.Equal(610m, dto.PaidAmount);
        Assert.Equal("Cash", dto.PaymentMode);
        Assert.Equal(paymentDate, dto.PaymentDate);
        Assert.Equal("SBI", dto.BankName);
        Assert.Equal("Main Branch", dto.BranchName);
        Assert.Equal("CHQ-1", dto.ChequeOrTransactionNo);
        Assert.Equal(chequeDate, dto.ChequeDate);
        Assert.Equal("TXN123", dto.OnlineTransactionId);
        Assert.Equal("Razorpay", dto.PaymentGatewayName);
        Assert.Equal("9876543210", dto.PayerMobile);
        Assert.Equal("tenant@example.com", dto.PayerEmail);
        Assert.Equal("Success", dto.PaymentStatus);
        Assert.Equal(5, dto.CancelledBy);
        Assert.Equal(cancelledDate, dto.CancelledDate);
        Assert.Equal("Duplicate", dto.CancellationReason);
        Assert.Equal("Remark", dto.Remark);
        Assert.Equal("Shop 1", dto.AssetName);
        Assert.Equal("AST-001", dto.AssetNo);
    }

    [Fact]
    public void LeaseRentBillTransactionDto_Defaults_OptionalFieldsAreNull_PaymentStatusIsSuccess()
    {
        var dto = new LeaseRentBillTransactionDto();

        Assert.Equal(string.Empty, dto.TransactionNo);
        Assert.Null(dto.ReceiptNo);
        Assert.Equal(string.Empty, dto.PaymentMode);
        Assert.Equal("Success", dto.PaymentStatus);
        Assert.Null(dto.BankName);
        Assert.Null(dto.BranchName);
        Assert.Null(dto.ChequeOrTransactionNo);
        Assert.Null(dto.ChequeDate);
        Assert.Null(dto.OnlineTransactionId);
        Assert.Null(dto.PaymentGatewayName);
        Assert.Null(dto.PayerMobile);
        Assert.Null(dto.PayerEmail);
        Assert.Null(dto.CancelledBy);
        Assert.Null(dto.CancelledDate);
        Assert.Null(dto.CancellationReason);
        Assert.Null(dto.Remark);
        Assert.Null(dto.AssetName);
        Assert.Null(dto.AssetNo);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    #endregion

    #region CreateLeaseRentBillTransactionDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithEmptyTransactionNo_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = string.Empty,
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.TransactionNo))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_TransactionNo_Required");
    }

    [Fact]
    public void Create_WithTransactionNoExceeding50Characters_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = new string('T', 51),
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.TransactionNo))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_TransactionNo_MaxLengthExceeded_50");
    }

    [Fact]
    public void Create_WithZeroAssetId_IsInvalid()
    {
        // AssetId is a non-nullable int, so [Required] can never fire; the default 0 is actually
        // caught by [Range(1, int.MaxValue)]. LeaseRegistrationId/FinanceYear share the identical
        // Required+Range shape (own resource keys) and are not independently re-tested here.
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 0,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.AssetId))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_AssetId_InvalidRange");
    }

    [Fact]
    public void Create_WithFinanceYearOutOfRange_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 1999,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.FinanceYear))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_FinanceYear_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeTotalMonthlyRentAmount_IsInvalid()
    {
        // Representative of the [Range(0, double.MaxValue)] pattern also used by TotalPenaltyAmount,
        // TotalGSTAmount and DiscountAmount (each with its own resource key).
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            TotalMonthlyRentAmount = -1m,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.TotalMonthlyRentAmount))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_TotalMonthlyRentAmount_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroPaidAmount_IsInvalid()
    {
        // PaidAmount is [Required] (dead on this non-nullable decimal) but [Range(0.01, double.MaxValue)]
        // does real work here - unlike the Amount fields above, 0 is actually below this minimum.
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 0m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PaidAmount))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PaidAmount_InvalidRange");
    }

    [Fact]
    public void Create_WithPaidAmountAtMinimumBoundary_IsValid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 0.01m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithInvalidPaymentMode_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Bitcoin",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PaymentMode))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PaymentMode_Invalid");
    }

    [Fact]
    public void Create_WithEmptyPaymentMode_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = string.Empty,
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PaymentMode))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PaymentMode_Required");
    }

    [Fact]
    public void Create_WithDefaultPaymentDate_PassesValidation_DespiteRequiredAttribute()
    {
        // PaymentDate is a non-nullable DateTime with only [Required] (no [Range]/custom check), so
        // DateTime.MinValue passes DataAnnotations validation despite being documented as required.
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentStatus = "Success"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(default, dto.PaymentDate);
    }

    [Fact]
    public void Create_WithInvalidPayerMobile_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success",
            PayerMobile = "abc123"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PayerMobile))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PayerMobile_Invalid");
    }

    [Fact]
    public void Create_WithPayerMobileExceeding20Characters_IsInvalid()
    {
        // All-digit string so it passes the regex but fails [StringLength(20)].
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success",
            PayerMobile = new string('9', 21)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PayerMobile))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PayerMobile_MaxLengthExceeded_20");
    }

    [Fact]
    public void Create_WithInvalidPayerEmailFormat_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success",
            PayerEmail = "not-an-email"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PayerEmail))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PayerEmail_Invalid");
    }

    [Fact]
    public void Create_WithPayerEmailExceeding100Characters_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Success",
            PayerEmail = new string('a', 95) + "@test.com"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PayerEmail))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PayerEmail_MaxLengthExceeded_100");
    }

    [Fact]
    public void Create_WithInvalidPaymentStatus_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = "Unknown"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PaymentStatus))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PaymentStatus_Invalid");
    }

    [Fact]
    public void Create_WithEmptyPaymentStatus_IsInvalid()
    {
        var dto = new CreateLeaseRentBillTransactionDto
        {
            TransactionNo = "TXN-001",
            AssetId = 1,
            LeaseRegistrationId = 1,
            FinanceYear = 2025,
            PaidAmount = 100m,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PaymentStatus = string.Empty
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateLeaseRentBillTransactionDto.PaymentStatus))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PaymentStatus_Required");
    }

    [Fact]
    public void Create_Defaults_OptionalFieldsAreNull()
    {
        var dto = new CreateLeaseRentBillTransactionDto();

        Assert.Null(dto.ReceiptNo);
        Assert.Null(dto.BankName);
        Assert.Null(dto.BranchName);
        Assert.Null(dto.ChequeOrTransactionNo);
        Assert.Null(dto.ChequeDate);
        Assert.Null(dto.OnlineTransactionId);
        Assert.Null(dto.PaymentGatewayName);
        Assert.Null(dto.PayerMobile);
        Assert.Null(dto.PayerEmail);
        Assert.Null(dto.Remark);
        Assert.Equal("Success", dto.PaymentStatus);
        Assert.Equal(0m, dto.AdjustmentAmount);
    }

    #endregion

    #region UpdateLeaseRentBillTransactionDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateLeaseRentBillTransactionDto { PaymentStatus = "Success" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithNegativeDiscountAmount_IsInvalid()
    {
        var dto = new UpdateLeaseRentBillTransactionDto { PaymentStatus = "Success", DiscountAmount = -1m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateLeaseRentBillTransactionDto.DiscountAmount))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_DiscountAmount_InvalidRange");
    }

    [Fact]
    public void Update_WithInvalidPaymentStatus_IsInvalid()
    {
        var dto = new UpdateLeaseRentBillTransactionDto { PaymentStatus = "Unknown" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateLeaseRentBillTransactionDto.PaymentStatus))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PaymentStatus_Invalid");
    }

    [Fact]
    public void Update_WithEmptyPaymentStatus_IsInvalid()
    {
        var dto = new UpdateLeaseRentBillTransactionDto { PaymentStatus = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateLeaseRentBillTransactionDto.PaymentStatus))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_PaymentStatus_Required");
    }

    [Fact]
    public void Update_WithRemarkExceeding500Characters_IsInvalid()
    {
        // Representative of the StringLength(500) pattern shared with CancellationReason (own key).
        var dto = new UpdateLeaseRentBillTransactionDto { PaymentStatus = "Success", Remark = new string('R', 501) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateLeaseRentBillTransactionDto.Remark))
            && r.ErrorMessage == "AMS_LeaseRentBillTransaction_Remark_MaxLengthExceeded_500");
    }

    [Fact]
    public void Update_Defaults_OptionalFieldsAreNull_PaymentStatusIsSuccess()
    {
        var dto = new UpdateLeaseRentBillTransactionDto();

        Assert.Null(dto.ReceiptNo);
        Assert.Null(dto.CancellationReason);
        Assert.Null(dto.Remark);
        Assert.Equal("Success", dto.PaymentStatus);
        Assert.Equal(0m, dto.DiscountAmount);
        Assert.Equal(0m, dto.AdjustmentAmount);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
