using System;
using System.Collections.Generic;
using System.Linq;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for AssetLeaseRentDetailsEntity - shop/tenant lease-and-rent registration, including its
/// verify/approve/reject workflow fields and the append-only History collection.
/// </summary>
public class AssetLeaseRentDetailsEntityTests
{
    [Fact]
    public void Properties_RoundTrip_CoreLeaseFields()
    {
        var leaseStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var leaseEnd = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = new AssetLeaseRentDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            FloorDetailsId = 2,
            ShopNo = "S-1",
            ShopName = "Corner Shop",
            TenantName = "Jane Doe",
            TenantMobile = "9999999999",
            TenantEmail = "jane@example.com",
            TenantType = "Company",
            TenantAadhaarNo = "1234-5678-9012",
            TenantPanCardNo = "ABCDE1234F",
            TenantAddress = "123 Main St",
            GSTNo = "GST123",
            TotalAreaSqFt = 500m,
            ApplicationTypeId = 3,
            LeaseType = "Lease",
            LeaseStartDate = leaseStart,
            LeaseEndDate = leaseEnd,
            Duration = 12,
            RentAmount = 15000m,
            SecurityDeposit = 30000m,
            DepositType = "Refundable",
            PaymentFrequency = "Quarterly",
            AgreementId = "AGR-1",
            IsIncrement = true,
            IncrementFrequency = "Yearly",
            IncrementType = "Percentage",
            IncrementValue = 5.0,
            IncrementMethod = "Auto",
            Reason = "New lease"
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.AssetId);
        Assert.Equal(2, entity.FloorDetailsId);
        Assert.Equal("S-1", entity.ShopNo);
        Assert.Equal("Corner Shop", entity.ShopName);
        Assert.Equal("Jane Doe", entity.TenantName);
        Assert.Equal("9999999999", entity.TenantMobile);
        Assert.Equal("jane@example.com", entity.TenantEmail);
        Assert.Equal("Company", entity.TenantType);
        Assert.Equal("1234-5678-9012", entity.TenantAadhaarNo);
        Assert.Equal("ABCDE1234F", entity.TenantPanCardNo);
        Assert.Equal("123 Main St", entity.TenantAddress);
        Assert.Equal("GST123", entity.GSTNo);
        Assert.Equal(500m, entity.TotalAreaSqFt);
        Assert.Equal(3, entity.ApplicationTypeId);
        Assert.Equal("Lease", entity.LeaseType);
        Assert.Equal(leaseStart, entity.LeaseStartDate);
        Assert.Equal(leaseEnd, entity.LeaseEndDate);
        Assert.Equal(12, entity.Duration);
        Assert.Equal(15000m, entity.RentAmount);
        Assert.Equal(30000m, entity.SecurityDeposit);
        Assert.Equal("Refundable", entity.DepositType);
        Assert.Equal("Quarterly", entity.PaymentFrequency);
        Assert.Equal("AGR-1", entity.AgreementId);
        Assert.True(entity.IsIncrement);
        Assert.Equal("Yearly", entity.IncrementFrequency);
        Assert.Equal("Percentage", entity.IncrementType);
        Assert.Equal(5.0, entity.IncrementValue);
        Assert.Equal("Auto", entity.IncrementMethod);
        Assert.Equal("New lease", entity.Reason);
    }

    [Fact]
    public void Properties_RoundTrip_WorkflowFields()
    {
        var rejectionDate = DateTime.UtcNow.AddDays(-3);
        var verifiedDate = DateTime.UtcNow.AddDays(-2);
        var approvedDate = DateTime.UtcNow.AddDays(-1);
        var entity = new AssetLeaseRentDetailsEntity
        {
            WorkflowStatus = "Approved",
            RejectionReason = "Incomplete docs",
            IsRejection = true,
            RejectionBy = 10,
            RejectionDate = rejectionDate,
            IsVerified = true,
            VerifiedBy = 20,
            VerifiedDate = verifiedDate,
            IsApproved = true,
            ApprovedBy = 30,
            ApprovedDate = approvedDate
        };

        Assert.Equal("Approved", entity.WorkflowStatus);
        Assert.Equal("Incomplete docs", entity.RejectionReason);
        Assert.True(entity.IsRejection);
        Assert.Equal(10, entity.RejectionBy);
        Assert.Equal(rejectionDate, entity.RejectionDate);
        Assert.True(entity.IsVerified);
        Assert.Equal(20, entity.VerifiedBy);
        Assert.Equal(verifiedDate, entity.VerifiedDate);
        Assert.True(entity.IsApproved);
        Assert.Equal(30, entity.ApprovedBy);
        Assert.Equal(approvedDate, entity.ApprovedDate);
    }

    [Fact]
    public void Defaults_StringFieldsHaveDocumentedNonEmptyDefaults()
    {
        var entity = new AssetLeaseRentDetailsEntity();

        Assert.Equal(string.Empty, entity.TenantName);
        Assert.Equal(string.Empty, entity.TenantMobile);
        Assert.Equal("Individual", entity.TenantType);
        Assert.Equal("Rent", entity.LeaseType);
        Assert.Equal("Monthly", entity.PaymentFrequency);
        Assert.Equal("Pending", entity.WorkflowStatus);
    }

    [Fact]
    public void Defaults_WorkflowBooleans_AreFalse()
    {
        var entity = new AssetLeaseRentDetailsEntity();

        Assert.False(entity.IsRejection);
        Assert.False(entity.IsVerified);
        Assert.False(entity.IsApproved);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Defaults_NavigationProperties_AssetAndApplicationTypeAreNull_HistoryIsEmptyNotNull()
    {
        var entity = new AssetLeaseRentDetailsEntity();

        Assert.Null(entity.Asset);
        Assert.Null(entity.ApplicationType);
        Assert.NotNull(entity.History);
        Assert.Empty(entity.History);
    }

    [Fact]
    public void History_CanBeAddedTo()
    {
        var entity = new AssetLeaseRentDetailsEntity();
        var historyEntry = new LeaseRentRegistrationHistoryEntity { Id = 1 };

        entity.History.Add(historyEntry);

        Assert.Single(entity.History);
        Assert.Same(historyEntry, entity.History.First());
    }

    [Fact]
    public void History_CanBeReplacedWithAnotherCollection()
    {
        var entity = new AssetLeaseRentDetailsEntity
        {
            History = new List<LeaseRentRegistrationHistoryEntity>
            {
                new() { Id = 1 },
                new() { Id = 2 }
            }
        };

        Assert.Equal(2, entity.History.Count);
    }

    [Fact]
    public void ImplementsIHardDeletable()
    {
        Assert.True(typeof(IHardDeletable).IsAssignableFrom(typeof(AssetLeaseRentDetailsEntity)));
    }

    [Fact]
    public void ExplicitIHardDeletable_GetAndSetWork()
    {
        IHardDeletable entity = new AssetLeaseRentDetailsEntity();
        var now = DateTime.UtcNow;

        entity.MarkedForDeletion = true;
        entity.MarkedForDeletionDate = now;

        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void InheritsBaseEntity_AuditColumnsAreAvailable()
    {
        var entity = new AssetLeaseRentDetailsEntity
        {
            CreatedBy = 100,
            UpdatedBy = 200
        };

        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.True(entity.IsActive);
    }
}
