using System;
using NtisPlatform.Core.Entities.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for LeaseRentRegistrationHistoryEntity - an append-only audit log entry, each row a
/// full point-in-time snapshot (Snap_-prefixed properties) of an AssetLeaseRentDetailsEntity
/// captured after a change. By design this entity has no soft-delete fields (append-only, never
/// updated or removed).
/// </summary>
public class LeaseRentRegistrationHistoryEntityTests
{
    [Fact]
    public void Properties_RoundTrip_IdentityAndTenantSnapshot()
    {
        var performedDate = DateTime.Now;
        var entity = new LeaseRentRegistrationHistoryEntity
        {
            Id = 1,
            LeaseRentRegistrationId = 10,
            AssetId = 20,
            Remarks = "Lease renewed",
            Snap_GrievanceNo = "GR-1",
            Snap_ShopNo = "S-1",
            Snap_Floor = "Ground",
            Snap_ShopName = "Corner Shop",
            Snap_TenantName = "Jane Doe",
            Snap_TenantMobile = "9999999999",
            Snap_TenantEmail = "jane@example.com",
            Snap_TenantType = "Individual",
            Snap_TenantAadhaarNo = "1234-5678-9012",
            Snap_TenantPanCardNo = "ABCDE1234F",
            Snap_TenantAddress = "123 Main St",
            Snap_GSTNo = "GST-1",
            Snap_PreviousTenantName = "John Smith",
            Snap_PreviousTenantMobile = "8888888888",
            Snap_TotalAreaSqFt = 500m,
            PerformedBy = 100,
            PerformedDate = performedDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.LeaseRentRegistrationId);
        Assert.Equal(20, entity.AssetId);
        Assert.Equal("Lease renewed", entity.Remarks);
        Assert.Equal("GR-1", entity.Snap_GrievanceNo);
        Assert.Equal("S-1", entity.Snap_ShopNo);
        Assert.Equal("Ground", entity.Snap_Floor);
        Assert.Equal("Corner Shop", entity.Snap_ShopName);
        Assert.Equal("Jane Doe", entity.Snap_TenantName);
        Assert.Equal("9999999999", entity.Snap_TenantMobile);
        Assert.Equal("jane@example.com", entity.Snap_TenantEmail);
        Assert.Equal("Individual", entity.Snap_TenantType);
        Assert.Equal("1234-5678-9012", entity.Snap_TenantAadhaarNo);
        Assert.Equal("ABCDE1234F", entity.Snap_TenantPanCardNo);
        Assert.Equal("123 Main St", entity.Snap_TenantAddress);
        Assert.Equal("GST-1", entity.Snap_GSTNo);
        Assert.Equal("John Smith", entity.Snap_PreviousTenantName);
        Assert.Equal("8888888888", entity.Snap_PreviousTenantMobile);
        Assert.Equal(500m, entity.Snap_TotalAreaSqFt);
        Assert.Equal(100, entity.PerformedBy);
        Assert.Equal(performedDate, entity.PerformedDate);
    }

    [Fact]
    public void Properties_RoundTrip_LeaseAndFinancialSnapshot()
    {
        var leaseStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var leaseEnd = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = new LeaseRentRegistrationHistoryEntity
        {
            Snap_ApplicationType = "New",
            Snap_LeaseType = "Rent",
            Snap_LeaseRentType = "Commercial",
            Snap_OldLeaseStartDate = leaseStart.AddYears(-1),
            Snap_OldLeaseEndDate = leaseEnd.AddYears(-1),
            Snap_LeaseStartDate = leaseStart,
            Snap_LeaseEndDate = leaseEnd,
            Snap_TerminationDate = null,
            Snap_Duration = 12,
            Snap_PreviousMonthlyRent = 10000m,
            Snap_MonthlyRent = 15000m,
            Snap_RentAmount = 15000m,
            Snap_SecurityDeposit = 30000m,
            Snap_DepositType = "Refundable",
            Snap_PaymentFrequency = "Monthly",
            Snap_AgreementId = "AGR-1",
            Snap_IncrementFrequency = "Yearly",
            Snap_IncrementType = "Percentage",
            Snap_IncrementValue = 5.0,
            Snap_IncrementMethod = "Auto"
        };

        Assert.Equal("New", entity.Snap_ApplicationType);
        Assert.Equal("Rent", entity.Snap_LeaseType);
        Assert.Equal("Commercial", entity.Snap_LeaseRentType);
        Assert.Equal(leaseStart, entity.Snap_LeaseStartDate);
        Assert.Equal(leaseEnd, entity.Snap_LeaseEndDate);
        Assert.Null(entity.Snap_TerminationDate);
        Assert.Equal(12, entity.Snap_Duration);
        Assert.Equal(10000m, entity.Snap_PreviousMonthlyRent);
        Assert.Equal(15000m, entity.Snap_MonthlyRent);
        Assert.Equal(15000m, entity.Snap_RentAmount);
        Assert.Equal(30000m, entity.Snap_SecurityDeposit);
        Assert.Equal("Refundable", entity.Snap_DepositType);
        Assert.Equal("Monthly", entity.Snap_PaymentFrequency);
        Assert.Equal("AGR-1", entity.Snap_AgreementId);
        Assert.Equal("Yearly", entity.Snap_IncrementFrequency);
        Assert.Equal("Percentage", entity.Snap_IncrementType);
        Assert.Equal(5.0, entity.Snap_IncrementValue);
        Assert.Equal("Auto", entity.Snap_IncrementMethod);
    }

    [Fact]
    public void Properties_RoundTrip_WorkflowSnapshot()
    {
        var entity = new LeaseRentRegistrationHistoryEntity
        {
            Snap_CorrectionField = "TenantMobile",
            Snap_CorrectedValue = "9999999999",
            Snap_Reason = "Typo fix",
            Snap_WorkflowStatus = "Approved",
            Snap_RejectionReason = null,
            Snap_IsRejection = false,
            Snap_IsVerified = true,
            Snap_IsApproved = true,
            Snap_RentStatus = "Active",
            Snap_PaymentStatus = "Paid",
            Snap_IsActive = true
        };

        Assert.Equal("TenantMobile", entity.Snap_CorrectionField);
        Assert.Equal("9999999999", entity.Snap_CorrectedValue);
        Assert.Equal("Typo fix", entity.Snap_Reason);
        Assert.Equal("Approved", entity.Snap_WorkflowStatus);
        Assert.Null(entity.Snap_RejectionReason);
        Assert.False(entity.Snap_IsRejection);
        Assert.True(entity.Snap_IsVerified);
        Assert.True(entity.Snap_IsApproved);
        Assert.Equal("Active", entity.Snap_RentStatus);
        Assert.Equal("Paid", entity.Snap_PaymentStatus);
        Assert.True(entity.Snap_IsActive);
    }

    [Fact]
    public void Defaults_NonNullableSnapshotStrings_AreEmpty_NullableOnesAreNull()
    {
        var entity = new LeaseRentRegistrationHistoryEntity();

        Assert.Equal(string.Empty, entity.Snap_TenantName);
        Assert.Equal(string.Empty, entity.Snap_TenantMobile);
        Assert.Equal(string.Empty, entity.Snap_TenantType);
        Assert.Equal(string.Empty, entity.Snap_LeaseType);
        Assert.Equal(string.Empty, entity.Snap_PaymentFrequency);
        Assert.Equal(string.Empty, entity.Snap_WorkflowStatus);
        Assert.Equal(string.Empty, entity.Snap_RentStatus);
        Assert.Null(entity.Snap_ShopNo);
        Assert.Null(entity.Snap_TenantEmail);
        Assert.Null(entity.Remarks);
    }

    [Fact]
    public void Defaults_NullableBoolSnapshotFlags_AreNull()
    {
        // Unlike the live AssetLeaseRentDetailsEntity's IsRejection/IsVerified/IsApproved (plain
        // non-nullable bool, default false), the snapshot equivalents here are nullable bool? -
        // a null snapshot value means "not captured for this history row", distinct from false.
        var entity = new LeaseRentRegistrationHistoryEntity();

        Assert.Null(entity.Snap_IsRejection);
        Assert.Null(entity.Snap_IsVerified);
        Assert.Null(entity.Snap_IsApproved);
        Assert.Null(entity.Snap_IncrementStatus);
    }

    [Fact]
    public void Defaults_NavigationProperties_AreNull()
    {
        var entity = new LeaseRentRegistrationHistoryEntity();

        Assert.Null(entity.LeaseRentRegistration);
        Assert.Null(entity.Asset);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new LeaseRentRegistrationHistoryEntity();

        Assert.True(entity.IsActive);
    }
}
