using System;
using System.Collections.Generic;
using System.Linq;
using NtisPlatform.Core.Entities.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Asset_Management;

/// <summary>
/// Tests for LeaseRentBillTransactionEntity - the payment header for a lease-rent collection.
/// TotalDemandAmount and NetPayableAmount are SQL computed (PERSISTED) columns with private
/// setters - see the TotalDemandAmount test note in LeaseRentBillTransactionDetailEntityTests.
/// </summary>
public class LeaseRentBillTransactionEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var paymentDate = DateTime.Now;
        var chequeDate = DateTime.Now.AddDays(-1);
        var cancelledDate = DateTime.Now.AddDays(1);
        var entity = new LeaseRentBillTransactionEntity
        {
            Id = 1,
            TransactionNo = "TXN-001",
            ReceiptNo = "RCPT-001",
            AssetId = 10,
            LeaseRegistrationId = 20,
            FinanceYear = 2026,
            TotalMonthlyRentAmount = 15000m,
            TotalPenaltyAmount = 100m,
            TotalGSTAmount = 270m,
            DiscountAmount = 50m,
            AdjustmentAmount = 10m,
            PaidAmount = 15330m,
            PaymentMode = "UPI",
            PaymentDate = paymentDate,
            BankName = "SBI",
            BranchName = "Main Branch",
            ChequeOrTransactionNo = "TXN123",
            ChequeDate = chequeDate,
            OnlineTransactionId = "ONL-1",
            PaymentGatewayName = "Razorpay",
            PayerMobile = "9999999999",
            PayerEmail = "payer@example.com",
            PaymentStatus = "Success",
            CancelledBy = 5,
            CancelledDate = cancelledDate,
            CancellationReason = "Duplicate",
            Remark = "First payment"
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("TXN-001", entity.TransactionNo);
        Assert.Equal("RCPT-001", entity.ReceiptNo);
        Assert.Equal(10, entity.AssetId);
        Assert.Equal(20, entity.LeaseRegistrationId);
        Assert.Equal(2026, entity.FinanceYear);
        Assert.Equal(15000m, entity.TotalMonthlyRentAmount);
        Assert.Equal(100m, entity.TotalPenaltyAmount);
        Assert.Equal(270m, entity.TotalGSTAmount);
        Assert.Equal(50m, entity.DiscountAmount);
        Assert.Equal(10m, entity.AdjustmentAmount);
        Assert.Equal(15330m, entity.PaidAmount);
        Assert.Equal("UPI", entity.PaymentMode);
        Assert.Equal(paymentDate, entity.PaymentDate);
        Assert.Equal("SBI", entity.BankName);
        Assert.Equal("Main Branch", entity.BranchName);
        Assert.Equal("TXN123", entity.ChequeOrTransactionNo);
        Assert.Equal(chequeDate, entity.ChequeDate);
        Assert.Equal("ONL-1", entity.OnlineTransactionId);
        Assert.Equal("Razorpay", entity.PaymentGatewayName);
        Assert.Equal("9999999999", entity.PayerMobile);
        Assert.Equal("payer@example.com", entity.PayerEmail);
        Assert.Equal("Success", entity.PaymentStatus);
        Assert.Equal(5, entity.CancelledBy);
        Assert.Equal(cancelledDate, entity.CancelledDate);
        Assert.Equal("Duplicate", entity.CancellationReason);
        Assert.Equal("First payment", entity.Remark);
    }

    [Fact]
    public void Defaults_StringFieldsHaveDocumentedDefaults()
    {
        var entity = new LeaseRentBillTransactionEntity();

        Assert.Equal(string.Empty, entity.TransactionNo);
        Assert.Equal(string.Empty, entity.PaymentMode);
        Assert.Equal("Success", entity.PaymentStatus);
        Assert.Null(entity.ReceiptNo);
        Assert.Null(entity.CancelledBy);
    }

    [Fact]
    public void Defaults_ComputedAmountsAreZero()
    {
        var entity = new LeaseRentBillTransactionEntity();

        Assert.Equal(0m, entity.TotalDemandAmount);
        Assert.Equal(0m, entity.NetPayableAmount);
    }

    [Fact]
    public void Defaults_DetailsCollection_IsEmptyNotNull()
    {
        var entity = new LeaseRentBillTransactionEntity();

        Assert.NotNull(entity.Details);
        Assert.Empty(entity.Details);
    }

    [Fact]
    public void Details_CanBeAddedTo()
    {
        var entity = new LeaseRentBillTransactionEntity();
        var detail = new LeaseRentBillTransactionDetailEntity { Id = 1 };

        entity.Details.Add(detail);

        Assert.Single(entity.Details);
        Assert.Same(detail, entity.Details.First());
    }

    [Fact]
    public void Details_CanBeReplacedWithAnotherCollection()
    {
        var entity = new LeaseRentBillTransactionEntity
        {
            Details = new List<LeaseRentBillTransactionDetailEntity>
            {
                new() { Id = 1 },
                new() { Id = 2 }
            }
        };

        Assert.Equal(2, entity.Details.Count);
    }

    [Fact]
    public void TotalDemandAmountAndNetPayableAmount_HavePrivateSetters_SettableViaReflection()
    {
        var entity = new LeaseRentBillTransactionEntity();
        var totalProperty = typeof(LeaseRentBillTransactionEntity).GetProperty(nameof(LeaseRentBillTransactionEntity.TotalDemandAmount));
        var netProperty = typeof(LeaseRentBillTransactionEntity).GetProperty(nameof(LeaseRentBillTransactionEntity.NetPayableAmount));

        Assert.NotNull(totalProperty);
        Assert.NotNull(netProperty);
        totalProperty!.SetValue(entity, 15370m);
        netProperty!.SetValue(entity, 15310m);

        Assert.Equal(15370m, entity.TotalDemandAmount);
        Assert.Equal(15310m, entity.NetPayableAmount);
    }

    [Fact]
    public void InheritsBaseEntity_IsActiveDefaultsToTrue()
    {
        var entity = new LeaseRentBillTransactionEntity();

        Assert.True(entity.IsActive);
    }
}
