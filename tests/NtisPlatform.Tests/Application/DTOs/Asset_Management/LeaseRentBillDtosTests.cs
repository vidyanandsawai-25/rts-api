using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.LeaseRentBill;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in LeaseRentBillDtos.cs: BillPaymentAllocationDto, CreateBillPaymentDto
/// (including its IValidatableObject.Validate mode-aware custom rule), BillReceiptLineDto,
/// BillReceiptDto and BillTransactionDto.
/// </summary>
public class LeaseRentBillDtosTests
{
    #region BillPaymentAllocationDto

    [Fact]
    public void BillPaymentAllocationDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new BillPaymentAllocationDto
        {
            MonthWiseDemandId = 5,
            PayAmount = 1250.50m
        };

        Assert.Equal(5, dto.MonthWiseDemandId);
        Assert.Equal(1250.50m, dto.PayAmount);
    }

    #endregion

    #region CreateBillPaymentDto

    [Fact]
    public void Create_WithValidFullPayment_IsValid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithValidPartialPayment_IsValid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Partial",
            PaymentMode = "UPI",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            CustomAmount = 500m
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithValidMonthwisePayment_IsValid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Monthwise",
            PaymentMode = "NEFT",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            Allocations = new List<BillPaymentAllocationDto>
            {
                new() { MonthWiseDemandId = 1, PayAmount = 100m }
            }
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithEmptyPaymentType_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = string.Empty,
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PaymentType))
            && r.ErrorMessage == "AMS_BillPayment_PaymentType_Required");
    }

    [Fact]
    public void Create_WithInvalidPaymentType_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Half",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PaymentType))
            && r.ErrorMessage == "AMS_BillPayment_PaymentType_Invalid");
    }

    [Fact]
    public void Create_WithMissingPaymentMode_IsInvalid()
    {
        // PaymentMode defaults to string.Empty, so omitting it already triggers [Required].
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PaymentMode))
            && r.ErrorMessage == "AMS_BillPayment_PaymentMode_Required");
    }

    [Fact]
    public void Create_WithInvalidPaymentMode_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Bitcoin",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PaymentMode))
            && r.ErrorMessage == "AMS_BillPayment_PaymentMode_Invalid");
    }

    [Fact]
    public void Create_WithDefaultPaymentDate_PassesValidation_DespiteRequiredAttribute()
    {
        // PaymentDate is a non-nullable DateTime, so [Required] can never fail for it (a struct is
        // never "missing") - there is no paired [Range]/custom check either, so DateTime.MinValue
        // passes DataAnnotations validation despite the field being documented as required.
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(default, dto.PaymentDate);
    }

    [Fact]
    public void Create_WithMissingPayerMobile_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerEmail = "tenant@example.com"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PayerMobile))
            && r.ErrorMessage == "AMS_BillPayment_PayerMobile_Required");
    }

    [Fact]
    public void Create_WithInvalidPayerMobile_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "12345",
            PayerEmail = "tenant@example.com"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PayerMobile))
            && r.ErrorMessage == "AMS_BillPayment_PayerMobile_Invalid");
    }

    [Fact]
    public void Create_WithMissingPayerEmail_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PayerEmail))
            && r.ErrorMessage == "AMS_BillPayment_PayerEmail_Required");
    }

    [Fact]
    public void Create_WithInvalidPayerEmailFormat_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "not-an-email"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PayerEmail))
            && r.ErrorMessage == "AMS_BillPayment_PayerEmail_Invalid");
    }

    [Fact]
    public void Create_WithPayerEmailExceeding200Characters_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = new string('a', 195) + "@test.com"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.PayerEmail))
            && r.ErrorMessage == "AMS_BillPayment_PayerEmail_MaxLengthExceeded_200");
    }

    [Fact]
    public void Create_WithBankNameExceeding100Characters_IsInvalid()
    {
        // Representative of the StringLength(100) pattern shared by BranchName, ChequeOrTransactionNo,
        // OnlineTransactionId and PaymentGatewayName - each has its own resource key but identical shape.
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            BankName = new string('B', 101)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.BankName))
            && r.ErrorMessage == "AMS_BillPayment_BankName_MaxLengthExceeded_100");
    }

    [Fact]
    public void Create_WithRemarkExceeding500Characters_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            Remark = new string('R', 501)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.Remark))
            && r.ErrorMessage == "AMS_BillPayment_Remark_MaxLengthExceeded_500");
    }

    [Fact]
    public void Create_WithNegativeDiscountAmount_IsInvalid()
    {
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            DiscountAmount = -1m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.DiscountAmount))
            && r.ErrorMessage == "AMS_BillPayment_DiscountAmount_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeCustomAmount_IsInvalid()
    {
        // The [Range(0, double.MaxValue)] attribute fires regardless of PaymentType - unlike the
        // custom Validate() rule below, which only cares about CustomAmount when PaymentType is Partial.
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            CustomAmount = -1m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.CustomAmount))
            && r.ErrorMessage == "AMS_BillPayment_CustomAmount_InvalidRange");
    }

    [Fact]
    public void Create_WithMonthwiseAndNoAllocations_IsInvalid()
    {
        // Custom Validate(): Monthwise with a null/empty Allocations list yields Allocations_Required
        // and stops (yield break) before checking individual allocation ids.
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Monthwise",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            Allocations = new List<BillPaymentAllocationDto>()
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.Allocations))
            && r.ErrorMessage == "AMS_BillPayment_Allocations_Required");
    }

    [Fact]
    public void Create_WithMonthwiseAndInvalidAllocationDemandId_IsInvalid()
    {
        // Custom Validate(): each allocation with MonthWiseDemandId < 1 yields an indexed member name
        // "Allocations[i].MonthWiseDemandId" - not just "Allocations".
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Monthwise",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            Allocations = new List<BillPaymentAllocationDto>
            {
                new() { MonthWiseDemandId = 0, PayAmount = 10m }
            }
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains("Allocations[0].MonthWiseDemandId")
            && r.ErrorMessage == "AMS_BillPayment_MonthWiseDemandId_InvalidRange");
    }

    [Fact]
    public void Create_WithPartialAndZeroCustomAmount_IsInvalid()
    {
        // CustomAmount = 0 passes the [Range(0, double.MaxValue)] attribute (0 is in range), so only
        // the custom Validate() rule (PaymentType == "Partial" && CustomAmount <= 0) catches this.
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Partial",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com",
            CustomAmount = 0m
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBillPaymentDto.CustomAmount))
            && r.ErrorMessage == "AMS_BillPayment_CustomAmount_Required");
    }

    [Fact]
    public void Create_WithFullPaymentTypeAndEmptyAllocations_IsValid()
    {
        // Allocations/CustomAmount are ignored for Full - an empty (default) Allocations list must
        // not trip the Monthwise-only custom rule.
        var dto = new CreateBillPaymentDto
        {
            PaymentType = "Full",
            PaymentMode = "Cash",
            PaymentDate = DateTime.Now,
            PayerMobile = "9876543210",
            PayerEmail = "tenant@example.com"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Empty(dto.Allocations);
    }

    [Fact]
    public void Create_Defaults_OptionalFieldsAreNull_AllocationsIsEmptyList()
    {
        var dto = new CreateBillPaymentDto();

        Assert.Equal("Full", dto.PaymentType);
        Assert.Equal(string.Empty, dto.PaymentMode);
        Assert.Equal(string.Empty, dto.PayerMobile);
        Assert.Equal(string.Empty, dto.PayerEmail);
        Assert.Null(dto.BankName);
        Assert.Null(dto.BranchName);
        Assert.Null(dto.ChequeOrTransactionNo);
        Assert.Null(dto.ChequeDate);
        Assert.Null(dto.OnlineTransactionId);
        Assert.Null(dto.PaymentGatewayName);
        Assert.Null(dto.Remark);
        Assert.Equal(0m, dto.DiscountAmount);
        Assert.Equal(0m, dto.AdjustmentAmount);
        Assert.Equal(0m, dto.CustomAmount);
        Assert.NotNull(dto.Allocations);
        Assert.Empty(dto.Allocations);
    }

    #endregion

    #region BillReceiptLineDto

    [Fact]
    public void BillReceiptLineDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new BillReceiptLineDto
        {
            MonthWiseDemandId = 1,
            DemandYear = 2025,
            DemandMonth = 4,
            CurrentPaidAmount = 1000m,
            BalanceAmount = 0m,
            PaymentStatus = "Paid"
        };

        Assert.Equal(1, dto.MonthWiseDemandId);
        Assert.Equal(2025, dto.DemandYear);
        Assert.Equal((byte)4, dto.DemandMonth);
        Assert.Equal(1000m, dto.CurrentPaidAmount);
        Assert.Equal(0m, dto.BalanceAmount);
        Assert.Equal("Paid", dto.PaymentStatus);
    }

    [Fact]
    public void BillReceiptLineDto_Defaults_PaymentStatusIsEmptyString()
    {
        var dto = new BillReceiptLineDto();

        Assert.Equal(string.Empty, dto.PaymentStatus);
    }

    #endregion

    #region BillReceiptDto

    [Fact]
    public void BillReceiptDto_PropertiesGetAndSetCorrectly()
    {
        var paymentDate = DateTime.Now;
        var lines = new List<BillReceiptLineDto> { new() { MonthWiseDemandId = 1 } };
        var dto = new BillReceiptDto
        {
            TransactionId = 1,
            TransactionNo = "TXN-001",
            ReceiptNo = "RCPT-001",
            PaymentType = "Full",
            PaymentDate = paymentDate,
            TotalDemandAmount = 1000m,
            DiscountAmount = 50m,
            AdjustmentAmount = 10m,
            NetPayableAmount = 940m,
            PaidAmount = 940m,
            TenantName = "John Doe",
            Lines = lines
        };

        Assert.Equal(1, dto.TransactionId);
        Assert.Equal("TXN-001", dto.TransactionNo);
        Assert.Equal("RCPT-001", dto.ReceiptNo);
        Assert.Equal("Full", dto.PaymentType);
        Assert.Equal(paymentDate, dto.PaymentDate);
        Assert.Equal(1000m, dto.TotalDemandAmount);
        Assert.Equal(50m, dto.DiscountAmount);
        Assert.Equal(10m, dto.AdjustmentAmount);
        Assert.Equal(940m, dto.NetPayableAmount);
        Assert.Equal(940m, dto.PaidAmount);
        Assert.Equal("John Doe", dto.TenantName);
        Assert.Same(lines, dto.Lines);
    }

    [Fact]
    public void BillReceiptDto_Defaults_LinesIsEmptyList_OptionalFieldsAreNull()
    {
        var dto = new BillReceiptDto();

        Assert.NotNull(dto.Lines);
        Assert.Empty(dto.Lines);
        Assert.Null(dto.ReceiptNo);
        Assert.Null(dto.TenantName);
        Assert.Equal(string.Empty, dto.TransactionNo);
        Assert.Equal(string.Empty, dto.PaymentType);
    }

    #endregion

    #region BillTransactionDto

    [Fact]
    public void BillTransactionDto_PropertiesGetAndSetCorrectly()
    {
        var paymentDate = DateTime.Now;
        var dto = new BillTransactionDto
        {
            Id = 1,
            TransactionNo = "TXN-001",
            ReceiptNo = "RCPT-001",
            FinanceYear = 2025,
            TotalDemandAmount = 1000m,
            DiscountAmount = 50m,
            AdjustmentAmount = 10m,
            NetPayableAmount = 940m,
            PaidAmount = 940m,
            PaymentMode = "Cash",
            PaymentDate = paymentDate,
            PaymentStatus = "Success"
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("TXN-001", dto.TransactionNo);
        Assert.Equal("RCPT-001", dto.ReceiptNo);
        Assert.Equal(2025, dto.FinanceYear);
        Assert.Equal(1000m, dto.TotalDemandAmount);
        Assert.Equal(50m, dto.DiscountAmount);
        Assert.Equal(10m, dto.AdjustmentAmount);
        Assert.Equal(940m, dto.NetPayableAmount);
        Assert.Equal(940m, dto.PaidAmount);
        Assert.Equal("Cash", dto.PaymentMode);
        Assert.Equal(paymentDate, dto.PaymentDate);
        Assert.Equal("Success", dto.PaymentStatus);
    }

    [Fact]
    public void BillTransactionDto_Defaults_StringsAreEmpty_ReceiptNoIsNull()
    {
        var dto = new BillTransactionDto();

        Assert.Equal(string.Empty, dto.TransactionNo);
        Assert.Null(dto.ReceiptNo);
        Assert.Equal(string.Empty, dto.PaymentMode);
        Assert.Equal(string.Empty, dto.PaymentStatus);
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
