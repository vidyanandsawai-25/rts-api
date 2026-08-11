using System;
using NtisPlatform.Core.Entities.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for MonthWiseDemandEntity - one month's rent demand for a lease. TotalDemandAmount and
/// PendingAmount are SQL computed (PERSISTED) columns with private setters - see the
/// TotalDemandAmount test note in LeaseRentBillTransactionDetailEntityTests.
/// </summary>
public class MonthWiseDemandEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var lastPaymentDate = DateTime.UtcNow.AddDays(-5);
        var dueDate = DateTime.UtcNow.AddDays(5);
        var entity = new MonthWiseDemandEntity
        {
            Id = 1,
            AssetId = 10,
            LeaseRegistrationId = 20,
            FinanceYear = 2026,
            DemandYear = 2026,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 15000m,
            PenaltyRuleMasterId = 2,
            PenaltyAmount = 100m,
            GSTMasterId = 3,
            GSTAmount = 270m,
            PaidAmount = 5000m,
            DemandStatus = "Partial",
            LastPaymentDate = lastPaymentDate,
            DueDate = dueDate
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.AssetId);
        Assert.Equal(20, entity.LeaseRegistrationId);
        Assert.Equal(2026, entity.FinanceYear);
        Assert.Equal(2026, entity.DemandYear);
        Assert.Equal((byte)1, entity.QuarterNo);
        Assert.Equal((byte)4, entity.DemandMonth);
        Assert.Equal(15000m, entity.MonthlyRentAmount);
        Assert.Equal(2, entity.PenaltyRuleMasterId);
        Assert.Equal(100m, entity.PenaltyAmount);
        Assert.Equal(3, entity.GSTMasterId);
        Assert.Equal(270m, entity.GSTAmount);
        Assert.Equal(5000m, entity.PaidAmount);
        Assert.Equal("Partial", entity.DemandStatus);
        Assert.Equal(lastPaymentDate, entity.LastPaymentDate);
        Assert.Equal(dueDate, entity.DueDate);
    }

    [Fact]
    public void Defaults_DemandStatusIsPending_OptionalFKsAreNull()
    {
        var entity = new MonthWiseDemandEntity();

        Assert.Equal("Pending", entity.DemandStatus);
        Assert.Null(entity.PenaltyRuleMasterId);
        Assert.Null(entity.GSTMasterId);
        Assert.Null(entity.LastPaymentDate);
        Assert.Null(entity.DueDate);
    }

    [Fact]
    public void Defaults_ComputedAmountsAreZero()
    {
        var entity = new MonthWiseDemandEntity();

        Assert.Equal(0m, entity.TotalDemandAmount);
        Assert.Equal(0m, entity.PendingAmount);
    }

    [Fact]
    public void Defaults_NavigationProperties_AreNull()
    {
        var entity = new MonthWiseDemandEntity();

        Assert.Null(entity.GSTMaster);
        Assert.Null(entity.PenaltyRuleMaster);
    }

    [Fact]
    public void TotalDemandAmountAndPendingAmount_HavePrivateSetters_SettableViaReflection()
    {
        var entity = new MonthWiseDemandEntity();
        var totalProperty = typeof(MonthWiseDemandEntity).GetProperty(nameof(MonthWiseDemandEntity.TotalDemandAmount));
        var pendingProperty = typeof(MonthWiseDemandEntity).GetProperty(nameof(MonthWiseDemandEntity.PendingAmount));

        Assert.NotNull(totalProperty);
        Assert.NotNull(pendingProperty);
        totalProperty!.SetValue(entity, 15370m);
        pendingProperty!.SetValue(entity, 10370m);

        Assert.Equal(15370m, entity.TotalDemandAmount);
        Assert.Equal(10370m, entity.PendingAmount);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new MonthWiseDemandEntity();

        Assert.True(entity.IsActive);
    }
}
