using System;
using NtisPlatform.Core.Entities.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for LeaseRentBillTransactionDetailEntity - a month-wise payment detail line of a
/// LeaseRentBillTransactionEntity. TotalDemandAmount is a SQL computed (PERSISTED) column with a
/// private setter - it's read back by EF via reflection/backing-field materialization, never
/// written by application code, so these tests set it the same way EF would (via reflection) to
/// confirm the property is genuinely readable/settable at that level rather than compile-only.
/// </summary>
public class LeaseRentBillTransactionDetailEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new LeaseRentBillTransactionDetailEntity
        {
            Id = 1,
            LeaseRentBillTransactionId = 10,
            MonthWiseDemandId = 20,
            AssetId = 30,
            LeaseRegistrationId = 40,
            FinanceYear = 2026,
            DemandYear = 2026,
            QuarterNo = 1,
            DemandMonth = 4,
            MonthlyRentAmount = 15000m,
            PenaltyAmount = 100m,
            GSTAmount = 270m,
            PreviousPaidAmount = 0m,
            CurrentPaidAmount = 15370m,
            BalanceAmount = 0m,
            PaymentStatus = "Paid"
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(10, entity.LeaseRentBillTransactionId);
        Assert.Equal(20, entity.MonthWiseDemandId);
        Assert.Equal(30, entity.AssetId);
        Assert.Equal(40, entity.LeaseRegistrationId);
        Assert.Equal(2026, entity.FinanceYear);
        Assert.Equal(2026, entity.DemandYear);
        Assert.Equal((byte)1, entity.QuarterNo);
        Assert.Equal((byte)4, entity.DemandMonth);
        Assert.Equal(15000m, entity.MonthlyRentAmount);
        Assert.Equal(100m, entity.PenaltyAmount);
        Assert.Equal(270m, entity.GSTAmount);
        Assert.Equal(0m, entity.PreviousPaidAmount);
        Assert.Equal(15370m, entity.CurrentPaidAmount);
        Assert.Equal(0m, entity.BalanceAmount);
        Assert.Equal("Paid", entity.PaymentStatus);
    }

    [Fact]
    public void Defaults_PaymentStatusIsPaid_TotalDemandAmountIsZero()
    {
        var entity = new LeaseRentBillTransactionDetailEntity();

        Assert.Equal("Paid", entity.PaymentStatus);
        Assert.Equal(0m, entity.TotalDemandAmount);
    }

    [Fact]
    public void Defaults_NavigationProperties_AreNull()
    {
        var entity = new LeaseRentBillTransactionDetailEntity();

        Assert.Null(entity.Transaction);
        Assert.Null(entity.MonthWiseDemand);
    }

    [Fact]
    public void TotalDemandAmount_HasPrivateSetter_SettableViaReflectionLikeEFMaterialization()
    {
        var entity = new LeaseRentBillTransactionDetailEntity();
        var property = typeof(LeaseRentBillTransactionDetailEntity).GetProperty(nameof(LeaseRentBillTransactionDetailEntity.TotalDemandAmount));

        Assert.NotNull(property);
        Assert.True(property!.CanWrite);
        property.SetValue(entity, 15370m);

        Assert.Equal(15370m, entity.TotalDemandAmount);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new LeaseRentBillTransactionDetailEntity();

        Assert.True(entity.IsActive);
    }
}
