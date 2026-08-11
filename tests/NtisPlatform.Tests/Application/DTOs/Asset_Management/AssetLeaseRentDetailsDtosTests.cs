using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for the DTOs in AssetLeaseRentDetailsDtos.cs - shop/tenant lease-and-rent registration,
/// its verify/approve/reject workflow payloads, paged listing (with stats), previous-tenant
/// history snapshots, and document upload DTOs.
/// </summary>
public class AssetLeaseRentDetailsDtosTests
{
    #region AssetLeaseRentDetailsNamesDto

    [Fact]
    public void AssetLeaseRentDetailsNamesDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new AssetLeaseRentDetailsNamesDto
        {
            AssetNo = "AST-001",
            AssetName = "Building A",
            AssetCategoryName = "Commercial",
            ApplicationTypeName = "Shop",
            FloorDescription = "Ground Floor"
        };

        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Building A", dto.AssetName);
        Assert.Equal("Commercial", dto.AssetCategoryName);
        Assert.Equal("Shop", dto.ApplicationTypeName);
        Assert.Equal("Ground Floor", dto.FloorDescription);
    }

    [Fact]
    public void AssetLeaseRentDetailsNamesDto_Defaults_AllPropertiesAreNull()
    {
        var dto = new AssetLeaseRentDetailsNamesDto();

        Assert.Null(dto.AssetNo);
        Assert.Null(dto.AssetName);
        Assert.Null(dto.AssetCategoryName);
        Assert.Null(dto.ApplicationTypeName);
        Assert.Null(dto.FloorDescription);
    }

    #endregion

    #region AssetLeaseRentDetailsDto (read)

    [Fact]
    public void AssetLeaseRentDetailsDto_PropertiesGetAndSetCorrectly()
    {
        var createdDate = DateTime.UtcNow.AddDays(-30);
        var updatedDate = DateTime.UtcNow;
        var leaseStart = DateTime.UtcNow.AddDays(-10);
        var leaseEnd = DateTime.UtcNow.AddYears(1);
        var rejectionDate = DateTime.UtcNow.AddDays(-5);
        var verifiedDate = DateTime.UtcNow.AddDays(-3);
        var approvedDate = DateTime.UtcNow.AddDays(-1);
        var names = new AssetLeaseRentDetailsNamesDto { AssetNo = "AST-001" };

        var dto = new AssetLeaseRentDetailsDto
        {
            Id = 1,
            IsActive = true,
            CreatedDate = createdDate,
            UpdatedDate = updatedDate,
            ParentAssetId = 2,
            AssetId = 3,
            FloorDetailsId = 4,
            ShopNo = "S-101",
            ShopName = "Corner Shop",
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantEmail = "john@example.com",
            TenantType = "Individual",
            TenantAadhaarNo = "123456789012",
            TenantPanCardNo = "ABCDE1234F",
            TenantAddress = "123 Main St",
            GSTNo = "22AAAAA0000A1Z5",
            TotalAreaSqFt = 500m,
            ApplicationTypeId = 5,
            LeaseType = "Rent",
            LeaseStartDate = leaseStart,
            LeaseEndDate = leaseEnd,
            Duration = 12,
            MonthlyRent = 15000m,
            RentAmount = 15000m,
            SecurityDeposit = 30000m,
            DepositType = "Refundable",
            PaymentFrequency = "Monthly",
            AgreementId = "AGR-001",
            IncrementFrequency = "Yearly",
            IncrementType = "Percentage",
            IncrementValue = 5.5,
            IncrementMethod = "Automatic",
            Reason = "Renewal",
            WorkflowStatus = "Approved",
            RejectionReason = "N/A",
            IsRejection = false,
            RejectionBy = 6,
            RejectionDate = rejectionDate,
            IsVerified = true,
            VerifiedBy = 7,
            VerifiedDate = verifiedDate,
            IsApproved = true,
            ApprovedBy = 8,
            ApprovedDate = approvedDate,
            LeaseDurationDisplay = "1 Year",
            RentAmountDisplay = "15,000",
            Names = names,
            AssetNo = "AST-001",
            AssetName = "Building A",
            AssetCategoryName = "Commercial"
        };

        Assert.Equal(1, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(createdDate, dto.CreatedDate);
        Assert.Equal(updatedDate, dto.UpdatedDate);
        Assert.Equal(2, dto.ParentAssetId);
        Assert.Equal(3, dto.AssetId);
        Assert.Equal(4, dto.FloorDetailsId);
        Assert.Equal("S-101", dto.ShopNo);
        Assert.Equal("Corner Shop", dto.ShopName);
        Assert.Equal("John Doe", dto.TenantName);
        Assert.Equal("9999999999", dto.TenantMobile);
        Assert.Equal("john@example.com", dto.TenantEmail);
        Assert.Equal("Individual", dto.TenantType);
        Assert.Equal("123456789012", dto.TenantAadhaarNo);
        Assert.Equal("ABCDE1234F", dto.TenantPanCardNo);
        Assert.Equal("123 Main St", dto.TenantAddress);
        Assert.Equal("22AAAAA0000A1Z5", dto.GSTNo);
        Assert.Equal(500m, dto.TotalAreaSqFt);
        Assert.Equal(5, dto.ApplicationTypeId);
        Assert.Equal("Rent", dto.LeaseType);
        Assert.Equal(leaseStart, dto.LeaseStartDate);
        Assert.Equal(leaseEnd, dto.LeaseEndDate);
        Assert.Equal(12, dto.Duration);
        Assert.Equal(15000m, dto.MonthlyRent);
        Assert.Equal(15000m, dto.RentAmount);
        Assert.Equal(30000m, dto.SecurityDeposit);
        Assert.Equal("Refundable", dto.DepositType);
        Assert.Equal("Monthly", dto.PaymentFrequency);
        Assert.Equal("AGR-001", dto.AgreementId);
        Assert.Equal("Yearly", dto.IncrementFrequency);
        Assert.Equal("Percentage", dto.IncrementType);
        Assert.Equal(5.5, dto.IncrementValue);
        Assert.Equal("Automatic", dto.IncrementMethod);
        Assert.Equal("Renewal", dto.Reason);
        Assert.Equal("Approved", dto.WorkflowStatus);
        Assert.Equal("N/A", dto.RejectionReason);
        Assert.False(dto.IsRejection);
        Assert.Equal(6, dto.RejectionBy);
        Assert.Equal(rejectionDate, dto.RejectionDate);
        Assert.True(dto.IsVerified);
        Assert.Equal(7, dto.VerifiedBy);
        Assert.Equal(verifiedDate, dto.VerifiedDate);
        Assert.True(dto.IsApproved);
        Assert.Equal(8, dto.ApprovedBy);
        Assert.Equal(approvedDate, dto.ApprovedDate);
        Assert.Equal("1 Year", dto.LeaseDurationDisplay);
        Assert.Equal("15,000", dto.RentAmountDisplay);
        Assert.Same(names, dto.Names);
        Assert.Equal("AST-001", dto.AssetNo);
        Assert.Equal("Building A", dto.AssetName);
        Assert.Equal("Commercial", dto.AssetCategoryName);
    }

    [Fact]
    public void AssetLeaseRentDetailsDto_Defaults_StringDefaultsAndNamesAreInitialized()
    {
        var dto = new AssetLeaseRentDetailsDto();

        Assert.Null(dto.ParentAssetId);
        Assert.Equal(0, dto.AssetId);
        Assert.Null(dto.FloorDetailsId);
        Assert.Null(dto.ShopNo);
        Assert.Null(dto.ShopName);
        Assert.Equal(string.Empty, dto.TenantName);
        Assert.Equal(string.Empty, dto.TenantMobile);
        Assert.Null(dto.TenantEmail);
        Assert.Equal("Individual", dto.TenantType);
        Assert.Null(dto.TotalAreaSqFt);
        Assert.Equal("Rent", dto.LeaseType);
        Assert.Equal(default, dto.LeaseStartDate);
        Assert.Null(dto.LeaseEndDate);
        Assert.Equal(0m, dto.MonthlyRent);
        Assert.Null(dto.RentAmount);
        Assert.Equal(0m, dto.SecurityDeposit);
        Assert.Equal("Monthly", dto.PaymentFrequency);
        Assert.Equal(string.Empty, dto.WorkflowStatus);
        Assert.False(dto.IsRejection);
        Assert.False(dto.IsVerified);
        Assert.False(dto.IsApproved);
        Assert.NotNull(dto.Names);
        Assert.Null(dto.Names.AssetNo);
        Assert.Null(dto.AssetNo);
        Assert.Null(dto.AssetName);
        Assert.Null(dto.AssetCategoryName);
    }

    #endregion

    #region CreateAssetLeaseRentDetailsDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroAssetId_PassesValidation_DespiteRequiredAttribute()
    {
        // AssetId is a non-nullable int carrying only [Required] - unlike e.g. AssetDetailsDto's
        // AssetId/OrganizationId, there's no paired [Range(1, ...)] here. A struct can never be
        // "missing" for [Required], so AssetId = 0 (the CLR default when omitted) produces zero
        // DataAnnotations errors even though the field is documented as mandatory. This is a real
        // validation gap, not just an academic footnote - see final summary.
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 0,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(0, dto.AssetId);
    }

    [Fact]
    public void Create_WithDefaultLeaseStartDate_PassesValidation_DespiteRequiredAttribute()
    {
        // LeaseStartDate is a non-nullable DateTime with only [Required] - same dead-attribute
        // gotcha as AssetId above. Omitting it leaves DateTime.MinValue, which passes validation.
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(default, dto.LeaseStartDate);
    }

    [Fact]
    public void Create_WithEmptyTenantName_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = string.Empty,
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.TenantName))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantName_Required");
    }

    [Fact]
    public void Create_WithTenantNameExceeding500Characters_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = new string('T', 501),
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.TenantName))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantName_MaxLengthExceeded_500");
    }

    [Fact]
    public void Create_WithEmptyTenantMobile_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = string.Empty,
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.TenantMobile))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantMobile_Required");
    }

    [Fact]
    public void Create_WithTenantMobileExceeding20Characters_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = new string('9', 21),
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.TenantMobile))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantMobile_MaxLengthExceeded_20");
    }

    [Fact]
    public void Create_WithInvalidTenantEmail_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantEmail = "not-an-email",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.TenantEmail))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantEmail_Invalid");
    }

    [Fact]
    public void Create_WithValidTenantEmail_IsValid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantEmail = "john@example.com",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithEmptyTenantType_IsInvalid()
    {
        // TenantType defaults to the non-empty "Individual", so omitting it from the initializer
        // would never exercise [Required] - it must be explicitly cleared to prove the attribute
        // fires (see AssetConditionMasterDtoValidationTests for the same pattern).
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = string.Empty,
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.TenantType))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantType_Required");
    }

    [Fact]
    public void Create_WithShopNoExceeding50Characters_IsInvalid()
    {
        // Representative of the "optional string, [StringLength] only, no [Required]" shape
        // shared by ShopName, TenantAadhaarNo, TenantPanCardNo, TenantAddress, GSTNo, DepositType,
        // AgreementId, IncrementFrequency, IncrementType, IncrementMethod and Reason.
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly",
            ShopNo = new string('S', 51)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.ShopNo))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_ShopNo_MaxLengthExceeded_50");
    }

    [Fact]
    public void Create_WithReasonExceeding1000Characters_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly",
            Reason = new string('R', 1001)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.Reason))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_Reason_MaxLengthExceeded_1000");
    }

    [Fact]
    public void Create_WithEmptyLeaseType_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = string.Empty,
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.LeaseType))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_LeaseType_Required");
    }

    [Fact]
    public void Create_WithZeroMonthlyRent_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 0m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.MonthlyRent))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_MonthlyRent_InvalidRange");
    }

    [Fact]
    public void Create_WithNegativeMonthlyRent_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = -100m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.MonthlyRent))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_MonthlyRent_InvalidRange");
    }

    [Fact]
    public void Create_WithZeroSecurityDeposit_IsValid()
    {
        // SecurityDeposit's Range minimum is 0 (inclusive) - a no-deposit lease is legitimate.
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 0m,
            PaymentFrequency = "Monthly"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithNegativeSecurityDeposit_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = -1m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.SecurityDeposit))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_SecurityDeposit_InvalidRange");
    }

    [Fact]
    public void Create_WithEmptyPaymentFrequency_IsInvalid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = string.Empty
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.PaymentFrequency))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_PaymentFrequency_Required");
    }

    [Fact]
    public void Create_WithWorkflowStatusExceeding30Characters_IsInvalid()
    {
        // Unlike TenantType/LeaseType/PaymentFrequency, WorkflowStatus has no [Required] at all -
        // only [StringLength(30)]. An empty WorkflowStatus is therefore valid at the DTO level.
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly",
            WorkflowStatus = new string('W', 31)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.WorkflowStatus))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_WorkflowStatus_MaxLengthExceeded_30");
    }

    [Fact]
    public void Create_WithEmptyWorkflowStatus_IsValid()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly",
            WorkflowStatus = string.Empty
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_Defaults_OptionalFieldsAreNull()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m
        };

        Assert.Null(dto.ParentAssetId);
        Assert.Null(dto.FloorDetailsId);
        Assert.Null(dto.ShopNo);
        Assert.Null(dto.ShopName);
        Assert.Null(dto.TenantEmail);
        Assert.Null(dto.TenantAadhaarNo);
        Assert.Null(dto.TenantPanCardNo);
        Assert.Null(dto.TenantAddress);
        Assert.Null(dto.GSTNo);
        Assert.Null(dto.TotalAreaSqFt);
        Assert.Null(dto.ApplicationTypeId);
        Assert.Null(dto.LeaseEndDate);
        Assert.Null(dto.Duration);
        Assert.Null(dto.RentAmount);
        Assert.Null(dto.DepositType);
        Assert.Null(dto.AgreementId);
        Assert.Null(dto.IncrementFrequency);
        Assert.Null(dto.IncrementType);
        Assert.Null(dto.IncrementValue);
        Assert.Null(dto.IncrementMethod);
        Assert.Null(dto.Reason);
        Assert.Equal("Individual", dto.TenantType);
        Assert.Equal("Rent", dto.LeaseType);
        Assert.Equal("Monthly", dto.PaymentFrequency);
        Assert.Equal("Pending", dto.WorkflowStatus);
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = string.Empty,
            TenantMobile = string.Empty,
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 0m,
            SecurityDeposit = -1m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 4);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.TenantName)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.TenantMobile)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.MonthlyRent)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetLeaseRentDetailsDto.SecurityDeposit)));
    }

    #endregion

    #region UpdateAssetLeaseRentDetailsDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly",
            IsActive = true
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithZeroAssetId_IsValid_AssetIdHasNoValidationAttributesAtAll()
    {
        // Notable asymmetry: CreateAssetLeaseRentDetailsDto.AssetId carries a (dead) [Required],
        // but UpdateAssetLeaseRentDetailsDto.AssetId carries no attribute whatsoever. Both allow
        // AssetId = 0 through DataAnnotations, but the Update side doesn't even document the
        // intent via an attribute. See final summary.
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 0,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithDefaultLeaseStartDate_PassesValidation_DespiteRequiredAttribute()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal(default, dto.LeaseStartDate);
    }

    [Fact]
    public void Update_WithEmptyTenantName_IsInvalid()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = string.Empty,
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetLeaseRentDetailsDto.TenantName))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantName_Required");
    }

    [Fact]
    public void Update_WithTenantNameExceeding500Characters_IsInvalid()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = new string('T', 501),
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetLeaseRentDetailsDto.TenantName))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantName_MaxLengthExceeded_500");
    }

    [Fact]
    public void Update_WithInvalidTenantEmail_IsInvalid()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantEmail = "not-an-email",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetLeaseRentDetailsDto.TenantEmail))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_TenantEmail_Invalid");
    }

    [Fact]
    public void Update_WithZeroMonthlyRent_IsInvalid()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 0m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetLeaseRentDetailsDto.MonthlyRent))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_MonthlyRent_InvalidRange");
    }

    [Fact]
    public void Update_WithNegativeSecurityDeposit_IsInvalid()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = -1m,
            PaymentFrequency = "Monthly"
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetLeaseRentDetailsDto.SecurityDeposit))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetails_SecurityDeposit_InvalidRange");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            TenantType = "Individual",
            LeaseType = "Rent",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m,
            SecurityDeposit = 10000m,
            PaymentFrequency = "Monthly",
            IsActive = false
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_Defaults_OptionalFieldsAreNull()
    {
        var dto = new UpdateAssetLeaseRentDetailsDto
        {
            AssetId = 1,
            TenantName = "John Doe",
            TenantMobile = "9999999999",
            LeaseStartDate = DateTime.UtcNow,
            MonthlyRent = 5000m
        };

        Assert.Null(dto.ParentAssetId);
        Assert.Null(dto.FloorDetailsId);
        Assert.Null(dto.ShopNo);
        Assert.Null(dto.TotalAreaSqFt);
        Assert.Null(dto.LeaseEndDate);
        Assert.Null(dto.Duration);
        Assert.Null(dto.RentAmount);
        Assert.Null(dto.IncrementValue);
        Assert.Equal("Individual", dto.TenantType);
        Assert.Equal("Rent", dto.LeaseType);
        Assert.Equal("Monthly", dto.PaymentFrequency);
        Assert.Equal("Pending", dto.WorkflowStatus);
        Assert.Null(dto.UpdatedBy);
    }

    #endregion

    #region LeaseWorkflowActionDto

    [Fact]
    public void LeaseWorkflowActionDto_WithValidRemarks_IsValid()
    {
        var dto = new LeaseWorkflowActionDto { Remarks = "Looks good, verified." };

        Assert.Empty(ValidateModel(dto));
        Assert.Equal("Looks good, verified.", dto.Remarks);
    }

    [Fact]
    public void LeaseWorkflowActionDto_WithNullRemarks_IsValid()
    {
        // Remarks has no [Required] - a verify/approve/revert action doesn't need a comment.
        var dto = new LeaseWorkflowActionDto();

        Assert.Empty(ValidateModel(dto));
        Assert.Null(dto.Remarks);
    }

    [Fact]
    public void LeaseWorkflowActionDto_WithRemarksExceeding500Characters_IsInvalid()
    {
        var dto = new LeaseWorkflowActionDto { Remarks = new string('R', 501) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(LeaseWorkflowActionDto.Remarks))
            && r.ErrorMessage == "AMS_LeaseWorkflowAction_Remarks_MaxLengthExceeded_500");
    }

    #endregion

    #region LeaseRejectDto

    [Fact]
    public void LeaseRejectDto_WithValidReason_IsValid()
    {
        var dto = new LeaseRejectDto { Reason = "Documents incomplete" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void LeaseRejectDto_WithMissingReason_IsInvalid()
    {
        // Reason defaults to string.Empty, so leaving it unset already exercises [Required]
        // (no need to explicitly assign empty string, unlike TenantType/LeaseType above).
        var dto = new LeaseRejectDto();

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(LeaseRejectDto.Reason))
            && r.ErrorMessage == "AMS_LeaseReject_Reason_Required");
    }

    [Fact]
    public void LeaseRejectDto_WithReasonExceeding500Characters_IsInvalid()
    {
        var dto = new LeaseRejectDto { Reason = new string('R', 501) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(LeaseRejectDto.Reason))
            && r.ErrorMessage == "AMS_LeaseReject_Reason_MaxLengthExceeded_500");
    }

    #endregion

    #region LeaseStatsDto

    [Fact]
    public void LeaseStatsDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new LeaseStatsDto
        {
            TotalApproved = 10,
            TotalVerified = 8,
            VerificationPending = 2,
            ApprovalPending = 3,
            TotalRejected = 1
        };

        Assert.Equal(10, dto.TotalApproved);
        Assert.Equal(8, dto.TotalVerified);
        Assert.Equal(2, dto.VerificationPending);
        Assert.Equal(3, dto.ApprovalPending);
        Assert.Equal(1, dto.TotalRejected);
    }

    [Fact]
    public void LeaseStatsDto_Defaults_AllCountsAreZero()
    {
        var dto = new LeaseStatsDto();

        Assert.Equal(0, dto.TotalApproved);
        Assert.Equal(0, dto.TotalVerified);
        Assert.Equal(0, dto.VerificationPending);
        Assert.Equal(0, dto.ApprovalPending);
        Assert.Equal(0, dto.TotalRejected);
    }

    #endregion

    #region AssetLeaseRentDetailsPagedResult

    [Fact]
    public void AssetLeaseRentDetailsPagedResult_DefaultConstructor_StatsInitialized_ItemsEmpty()
    {
        var pagedResult = new AssetLeaseRentDetailsPagedResult();

        Assert.NotNull(pagedResult.Stats);
        Assert.Equal(0, pagedResult.Stats.TotalApproved);
        Assert.NotNull(pagedResult.Items);
        Assert.Empty(pagedResult.Items);
    }

    [Fact]
    public void AssetLeaseRentDetailsPagedResult_ParameterizedConstructor_SetsAllProperties()
    {
        var items = new List<AssetLeaseRentDetailsDto> { new() { AssetId = 1 }, new() { AssetId = 2 } };
        var stats = new LeaseStatsDto { TotalApproved = 5, TotalRejected = 1 };

        var pagedResult = new AssetLeaseRentDetailsPagedResult(items, totalCount: 25, pageNumber: 2, pageSize: 10, stats);

        Assert.Same(items, pagedResult.Items);
        Assert.Equal(25, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.PageNumber);
        Assert.Equal(10, pagedResult.PageSize);
        Assert.Same(stats, pagedResult.Stats);
        Assert.Equal(3, pagedResult.TotalPages);
        Assert.True(pagedResult.HasNext);
        Assert.True(pagedResult.HasPrevious);
    }

    [Fact]
    public void AssetLeaseRentDetailsPagedResult_IsAssignableToPagedResultOfDto()
    {
        var pagedResult = new AssetLeaseRentDetailsPagedResult();

        Assert.IsAssignableFrom<PagedResult<AssetLeaseRentDetailsDto>>(pagedResult);
    }

    #endregion

    #region AssetLeaseRentPreviousTenantHistoryDto

    [Fact]
    public void AssetLeaseRentPreviousTenantHistoryDto_PropertiesGetAndSetCorrectly()
    {
        var performedDate = DateTime.UtcNow.AddDays(-2);
        var oldLeaseStart = DateTime.UtcNow.AddYears(-2);
        var oldLeaseEnd = DateTime.UtcNow.AddYears(-1);
        var leaseStart = DateTime.UtcNow.AddDays(-30);
        var leaseEnd = DateTime.UtcNow.AddYears(1);
        var terminationDate = DateTime.UtcNow.AddDays(-1);

        var dto = new AssetLeaseRentPreviousTenantHistoryDto
        {
            Id = 1,
            ActionType = "Terminate",
            ActionLabel = "Lease Terminated",
            PerformedDate = performedDate,
            FromStatus = "Approved",
            ToStatus = "Terminated",
            Remarks = "Tenant vacated",
            TenantName = "Jane Doe",
            TenantMobile = "8888888888",
            TenantEmail = "jane@example.com",
            TenantType = "Individual",
            TenantAadhaarNo = "111122223333",
            TenantPanCardNo = "XYZAB1234C",
            TenantAddress = "456 Side St",
            PreviousTenantName = "John Doe",
            PreviousTenantMobile = "9999999999",
            LeaseType = "Rent",
            ShopNo = "S-101",
            Floor = "Ground",
            ShopName = "Corner Shop",
            OldLeaseStartDate = oldLeaseStart,
            OldLeaseEndDate = oldLeaseEnd,
            LeaseStartDate = leaseStart,
            LeaseEndDate = leaseEnd,
            TerminationDate = terminationDate,
            PreviousMonthlyRent = 12000m,
            MonthlyRent = 15000m,
            SecurityDeposit = 30000m,
            PaymentFrequency = "Monthly",
            WorkflowStatus = "Terminated",
            RentStatus = "Closed"
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("Terminate", dto.ActionType);
        Assert.Equal("Lease Terminated", dto.ActionLabel);
        Assert.Equal(performedDate, dto.PerformedDate);
        Assert.Equal("Approved", dto.FromStatus);
        Assert.Equal("Terminated", dto.ToStatus);
        Assert.Equal("Tenant vacated", dto.Remarks);
        Assert.Equal("Jane Doe", dto.TenantName);
        Assert.Equal("8888888888", dto.TenantMobile);
        Assert.Equal("jane@example.com", dto.TenantEmail);
        Assert.Equal("Individual", dto.TenantType);
        Assert.Equal("111122223333", dto.TenantAadhaarNo);
        Assert.Equal("XYZAB1234C", dto.TenantPanCardNo);
        Assert.Equal("456 Side St", dto.TenantAddress);
        Assert.Equal("John Doe", dto.PreviousTenantName);
        Assert.Equal("9999999999", dto.PreviousTenantMobile);
        Assert.Equal("Rent", dto.LeaseType);
        Assert.Equal("S-101", dto.ShopNo);
        Assert.Equal("Ground", dto.Floor);
        Assert.Equal("Corner Shop", dto.ShopName);
        Assert.Equal(oldLeaseStart, dto.OldLeaseStartDate);
        Assert.Equal(oldLeaseEnd, dto.OldLeaseEndDate);
        Assert.Equal(leaseStart, dto.LeaseStartDate);
        Assert.Equal(leaseEnd, dto.LeaseEndDate);
        Assert.Equal(terminationDate, dto.TerminationDate);
        Assert.Equal(12000m, dto.PreviousMonthlyRent);
        Assert.Equal(15000m, dto.MonthlyRent);
        Assert.Equal(30000m, dto.SecurityDeposit);
        Assert.Equal("Monthly", dto.PaymentFrequency);
        Assert.Equal("Terminated", dto.WorkflowStatus);
        Assert.Equal("Closed", dto.RentStatus);
    }

    [Fact]
    public void AssetLeaseRentPreviousTenantHistoryDto_Defaults_NullableFieldsAreNull_NonNullableStringsAreEmpty()
    {
        var dto = new AssetLeaseRentPreviousTenantHistoryDto();

        Assert.Equal(string.Empty, dto.ActionType);
        Assert.Equal(string.Empty, dto.ActionLabel);
        Assert.Equal(string.Empty, dto.TenantName);
        Assert.Equal(string.Empty, dto.TenantMobile);
        Assert.Equal(string.Empty, dto.LeaseType);
        Assert.Equal(string.Empty, dto.PaymentFrequency);
        Assert.Equal(string.Empty, dto.WorkflowStatus);
        Assert.Equal(string.Empty, dto.RentStatus);
        Assert.Null(dto.FromStatus);
        Assert.Null(dto.ToStatus);
        Assert.Null(dto.Remarks);
        Assert.Null(dto.TenantEmail);
        Assert.Null(dto.PreviousTenantName);
        Assert.Null(dto.PreviousTenantMobile);
        Assert.Null(dto.OldLeaseStartDate);
        Assert.Null(dto.OldLeaseEndDate);
        Assert.Null(dto.LeaseStartDate);
        Assert.Null(dto.LeaseEndDate);
        Assert.Null(dto.TerminationDate);
        Assert.Null(dto.PreviousMonthlyRent);
        Assert.Equal(0m, dto.MonthlyRent);
        Assert.Equal(0m, dto.SecurityDeposit);
    }

    #endregion

    #region AssetLeaseRentDetailsDocumentUploadFormDto

    [Fact]
    public void UploadForm_WithValidData_IsValid()
    {
        var dto = new AssetLeaseRentDetailsDocumentUploadFormDto
        {
            File = MockFormFile(),
            AssetLeaseRentDetailsId = 1,
            ModuleId = 1
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void UploadForm_WithNullFile_IsInvalid()
    {
        // Unlike int/DateTime, IFormFile is a reference type - [Required] genuinely works here
        // because the field really can be "missing" (null) at runtime, in contrast to the
        // dead-Required gotchas on AssetId/LeaseStartDate above.
        var dto = new AssetLeaseRentDetailsDocumentUploadFormDto
        {
            AssetLeaseRentDetailsId = 1,
            ModuleId = 1
        };

        var results = ValidateModel(dto);

        Assert.Null(dto.File);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssetLeaseRentDetailsDocumentUploadFormDto.File))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetailsDocumentUploadForm_File_Required");
    }

    [Fact]
    public void UploadForm_WithZeroAssetLeaseRentDetailsId_IsInvalid()
    {
        // AssetLeaseRentDetailsId is non-nullable int with [Required] + [Range(1, int.MaxValue)] -
        // here the Range attribute (not Required) is what actually catches the omitted/zero case.
        var dto = new AssetLeaseRentDetailsDocumentUploadFormDto
        {
            File = MockFormFile(),
            AssetLeaseRentDetailsId = 0,
            ModuleId = 1
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssetLeaseRentDetailsDocumentUploadFormDto.AssetLeaseRentDetailsId))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetailsDocumentUploadForm_AssetLeaseRentDetailsId_InvalidRange");
    }

    [Fact]
    public void UploadForm_WithZeroModuleId_IsInvalid()
    {
        var dto = new AssetLeaseRentDetailsDocumentUploadFormDto
        {
            File = MockFormFile(),
            AssetLeaseRentDetailsId = 1,
            ModuleId = 0
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssetLeaseRentDetailsDocumentUploadFormDto.ModuleId))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetailsDocumentUploadForm_ModuleId_InvalidRange");
    }

    [Fact]
    public void UploadForm_WithDocumentTitleExceeding200Characters_IsInvalid()
    {
        var dto = new AssetLeaseRentDetailsDocumentUploadFormDto
        {
            File = MockFormFile(),
            AssetLeaseRentDetailsId = 1,
            ModuleId = 1,
            DocumentTitle = new string('D', 201)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssetLeaseRentDetailsDocumentUploadFormDto.DocumentTitle))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetailsDocumentUploadForm_DocumentTitle_MaxLengthExceeded_200");
    }

    [Fact]
    public void UploadForm_WithRemarksExceeding500Characters_IsInvalid()
    {
        var dto = new AssetLeaseRentDetailsDocumentUploadFormDto
        {
            File = MockFormFile(),
            AssetLeaseRentDetailsId = 1,
            ModuleId = 1,
            Remarks = new string('R', 501)
        };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AssetLeaseRentDetailsDocumentUploadFormDto.Remarks))
            && r.ErrorMessage == "AMS_AssetLeaseRentDetailsDocumentUploadForm_Remarks_MaxLengthExceeded_500");
    }

    [Fact]
    public void UploadForm_Defaults_OptionalFieldsAreNull()
    {
        var dto = new AssetLeaseRentDetailsDocumentUploadFormDto
        {
            File = MockFormFile(),
            AssetLeaseRentDetailsId = 1,
            ModuleId = 1
        };

        Assert.Null(dto.FloorDetailId);
        Assert.Null(dto.DocumentTitle);
        Assert.Null(dto.DocumentType);
        Assert.Null(dto.DocumentDate);
        Assert.Null(dto.DocumentNumber);
        Assert.Null(dto.Remarks);
        Assert.False(dto.IsPrimaryDocument);
        Assert.Null(dto.BindingPurpose);
        Assert.Null(dto.UploadedByUserId);
    }

    private static IFormFile MockFormFile()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("document.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);
        return mockFile.Object;
    }

    #endregion

    #region AssetLeaseRentDetailsDocumentUploadResponseDto

    [Fact]
    public void UploadResponseDto_PropertiesGetAndSetCorrectly()
    {
        var guid = Guid.NewGuid();
        var dto = new AssetLeaseRentDetailsDocumentUploadResponseDto
        {
            DocumentGuid = guid,
            DocumentId = 1,
            DocumentBindingId = 2,
            FileName = "lease-agreement.pdf",
            FileSizeBytes = 4096,
            StoragePath = "/storage/lease-agreement.pdf"
        };

        Assert.Equal(guid, dto.DocumentGuid);
        Assert.Equal(1, dto.DocumentId);
        Assert.Equal(2, dto.DocumentBindingId);
        Assert.Equal("lease-agreement.pdf", dto.FileName);
        Assert.Equal(4096, dto.FileSizeBytes);
        Assert.Equal("/storage/lease-agreement.pdf", dto.StoragePath);
    }

    [Fact]
    public void UploadResponseDto_Defaults_StringsAreEmpty()
    {
        var dto = new AssetLeaseRentDetailsDocumentUploadResponseDto();

        Assert.Equal(string.Empty, dto.FileName);
        Assert.Equal(string.Empty, dto.StoragePath);
        Assert.Equal(Guid.Empty, dto.DocumentGuid);
    }

    #endregion

    #region AssetLeaseRentDetailsDocumentDto

    [Fact]
    public void DocumentDto_PropertiesGetAndSetCorrectly()
    {
        var guid = Guid.NewGuid();
        var documentDate = DateTime.UtcNow.AddDays(-10);
        var createdDate = DateTime.UtcNow;

        var dto = new AssetLeaseRentDetailsDocumentDto
        {
            AssetDocumentId = 1,
            DocumentId = 2,
            DocumentGuid = guid,
            DocumentBindingId = 3,
            FileName = "stored-name.pdf",
            OriginalFileName = "lease-agreement.pdf",
            FileExtension = ".pdf",
            MimeType = "application/pdf",
            FileSizeBytes = 2048,
            DocumentType = "Agreement",
            DocumentTitle = "Lease Agreement",
            DocumentDate = documentDate,
            DocumentNumber = "DOC-001",
            Remarks = "Signed copy",
            IsPrimaryDocument = true,
            BindingPurpose = "Lease",
            UploadedBy = 4,
            CreatedDate = createdDate,
            DownloadCount = 5
        };

        Assert.Equal(1, dto.AssetDocumentId);
        Assert.Equal(2, dto.DocumentId);
        Assert.Equal(guid, dto.DocumentGuid);
        Assert.Equal(3, dto.DocumentBindingId);
        Assert.Equal("stored-name.pdf", dto.FileName);
        Assert.Equal("lease-agreement.pdf", dto.OriginalFileName);
        Assert.Equal(".pdf", dto.FileExtension);
        Assert.Equal("application/pdf", dto.MimeType);
        Assert.Equal(2048, dto.FileSizeBytes);
        Assert.Equal("Agreement", dto.DocumentType);
        Assert.Equal("Lease Agreement", dto.DocumentTitle);
        Assert.Equal(documentDate, dto.DocumentDate);
        Assert.Equal("DOC-001", dto.DocumentNumber);
        Assert.Equal("Signed copy", dto.Remarks);
        Assert.True(dto.IsPrimaryDocument);
        Assert.Equal("Lease", dto.BindingPurpose);
        Assert.Equal(4, dto.UploadedBy);
        Assert.Equal(createdDate, dto.CreatedDate);
        Assert.Equal(5, dto.DownloadCount);
    }

    [Fact]
    public void DocumentDto_Defaults_NullableFieldsAreNull_NonNullableStringsAreEmpty()
    {
        var dto = new AssetLeaseRentDetailsDocumentDto();

        Assert.Equal(string.Empty, dto.FileName);
        Assert.Equal(string.Empty, dto.OriginalFileName);
        Assert.Equal(string.Empty, dto.FileExtension);
        Assert.Equal(string.Empty, dto.MimeType);
        Assert.Null(dto.DocumentType);
        Assert.Null(dto.DocumentTitle);
        Assert.Null(dto.DocumentDate);
        Assert.Null(dto.DocumentNumber);
        Assert.Null(dto.Remarks);
        Assert.False(dto.IsPrimaryDocument);
        Assert.Null(dto.BindingPurpose);
        Assert.Equal(0, dto.UploadedBy);
        Assert.Equal(0, dto.DownloadCount);
    }

    #endregion

    private static IList<System.ComponentModel.DataAnnotations.ValidationResult> ValidateModel(object model)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
