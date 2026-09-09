using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure;

public class ApartmentTaxDetailsRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_NoMatchingProperty_ReturnsNull()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 1, WardId = 96, PropertyNo = "20", IsActive = true });
        await context.SaveChangesAsync();

        var repository = new ApartmentTaxDetailsRepository(context);
        var result = await repository.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "999", TaxType = ApartmentTaxType.RV });

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_RvTaxType_SumsPolicyTaxDetailsAcrossUnits()
    {
        using var context = CreateContext();
        context.PropertyMast.AddRange(
            new PropertyEntity { Id = 1, WardId = 96, PropertyNo = "20", IsActive = true },
            new PropertyEntity { Id = 2, WardId = 96, PropertyNo = "20", IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = 1, TaxCode = "GT", TaxName = "General Tax", TaxCategoryId = 1, CalculationModeId = 1 });
        context.PolicyCodeMaster.Add(new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL" });
        context.PolicyTaxDetails.AddRange(
            new PolicyTaxDetailsEntity { Id = 1, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, IsCurrent = true },
            new PolicyTaxDetailsEntity { Id = 2, PropertyId = 2, TaxId = 1, PolicyCodeId = 1, TaxAmount = 150m, IsActive = true, MarkedForDeletion = false, IsCurrent = true });
        await context.SaveChangesAsync();

        var repository = new ApartmentTaxDetailsRepository(context);
        var result = await repository.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "20", TaxType = ApartmentTaxType.RV });

        Assert.NotNull(result);
        Assert.Equal(2, result!.PropertyCount);
        var rv = Assert.Single(result.CurrentTaxes);
        Assert.Equal("RV", rv.TaxType);
        var head = Assert.Single(rv.TaxHeads);
        Assert.Equal("General Tax", head.TaxName);
        Assert.Equal("NETTAX", head.PolicyCode);
        Assert.Equal(250m, head.TaxAmount);
        Assert.Empty(result.Arrears);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_RvTaxType_ExcludesInactiveAndDeletedRows()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 1, WardId = 96, PropertyNo = "20", IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = 1, TaxCode = "GT", TaxName = "General Tax", TaxCategoryId = 1, CalculationModeId = 1 });
        context.PolicyCodeMaster.Add(new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL" });
        context.PolicyTaxDetails.AddRange(
            new PolicyTaxDetailsEntity { Id = 1, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, IsCurrent = true },
            new PolicyTaxDetailsEntity { Id = 2, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 500m, IsActive = false, MarkedForDeletion = false, IsCurrent = true },
            new PolicyTaxDetailsEntity { Id = 3, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 900m, IsActive = true, MarkedForDeletion = true, IsCurrent = true });
        await context.SaveChangesAsync();

        var repository = new ApartmentTaxDetailsRepository(context);
        var result = await repository.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "20", TaxType = ApartmentTaxType.RV });

        var head = Assert.Single(Assert.Single(result!.CurrentTaxes).TaxHeads);
        Assert.Equal(100m, head.TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_CvTaxType_ReadsPolicyTaxDetailsCvOnly()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 1, WardId = 96, PropertyNo = "20", IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = 1, TaxCode = "GT", TaxName = "General Tax", TaxCategoryId = 1, CalculationModeId = 1 });
        context.PolicyCodeMaster.Add(new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL" });
        context.PolicyTaxDetails.Add(
            new PolicyTaxDetailsEntity { Id = 1, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, IsCurrent = true });
        context.PolicyTaxDetailsCV.Add(
            new PolicyTaxDetailsCVEntity { Id = 1, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false, IsCurrent = true });
        await context.SaveChangesAsync();

        var repository = new ApartmentTaxDetailsRepository(context);
        var result = await repository.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "20", TaxType = ApartmentTaxType.CV });

        var cv = Assert.Single(result!.CurrentTaxes);
        Assert.Equal("CV", cv.TaxType);
        var head = Assert.Single(cv.TaxHeads);
        Assert.Equal(300m, head.TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_DualTaxType_ReturnsBothRvAndCv()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 1, WardId = 96, PropertyNo = "20", IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = 1, TaxCode = "GT", TaxName = "General Tax", TaxCategoryId = 1, CalculationModeId = 1 });
        context.PolicyCodeMaster.Add(new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL" });
        context.PolicyTaxDetails.Add(
            new PolicyTaxDetailsEntity { Id = 1, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, IsCurrent = true });
        context.PolicyTaxDetailsCV.Add(
            new PolicyTaxDetailsCVEntity { Id = 1, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 300m, IsActive = true, MarkedForDeletion = false, IsCurrent = true });
        await context.SaveChangesAsync();

        var repository = new ApartmentTaxDetailsRepository(context);
        var result = await repository.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "20", TaxType = ApartmentTaxType.Dual });

        Assert.Equal(2, result!.CurrentTaxes.Count);
        Assert.Equal(100m, result.CurrentTaxes.Single(c => c.TaxType == "RV").TaxHeads.Single().TaxAmount);
        Assert.Equal(300m, result.CurrentTaxes.Single(c => c.TaxType == "CV").TaxHeads.Single().TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_WingMasterIdFilter_NarrowsToSingleWingAndResolvesContext()
    {
        using var context = CreateContext();
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 100, SocietyName = "Test Society" });
        context.WingEntity.AddRange(
            new WingEntity { Id = 1, WingNo = "A" },
            new WingEntity { Id = 2, WingNo = "B" });
        context.WingDetailsMast.AddRange(
            new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 100, WingMasterId = 1, WingName = "A Wing" },
            new WingDetailsMastEntity { Id = 20, SocietyDetailsMastId = 100, WingMasterId = 2, WingName = "B Wing" });
        context.PropertyMast.AddRange(
            new PropertyEntity { Id = 1, WardId = 96, PropertyNo = "20", WingDetailId = 10, IsActive = true },
            new PropertyEntity { Id = 2, WardId = 96, PropertyNo = "20", WingDetailId = 20, IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = 1, TaxCode = "GT", TaxName = "General Tax", TaxCategoryId = 1, CalculationModeId = 1 });
        context.PolicyCodeMaster.Add(new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL" });
        context.PolicyTaxDetails.AddRange(
            new PolicyTaxDetailsEntity { Id = 1, PropertyId = 1, TaxId = 1, PolicyCodeId = 1, TaxAmount = 100m, IsActive = true, MarkedForDeletion = false, IsCurrent = true },
            new PolicyTaxDetailsEntity { Id = 2, PropertyId = 2, TaxId = 1, PolicyCodeId = 1, TaxAmount = 200m, IsActive = true, MarkedForDeletion = false, IsCurrent = true });
        await context.SaveChangesAsync();

        var repository = new ApartmentTaxDetailsRepository(context);

        var wingA = await repository.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "20", WingMasterId = 1, TaxType = ApartmentTaxType.RV });

        Assert.Equal(1, wingA!.PropertyCount);
        Assert.Equal(1, wingA.WingMasterId);
        Assert.Equal("A Wing", wingA.WingName);
        Assert.Equal("A", wingA.WingNo);
        Assert.Equal("Test Society", wingA.SocietyName);
        Assert.Equal(100m, wingA.CurrentTaxes.Single().TaxHeads.Single().TaxAmount);

        var wholeComplex = await repository.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "20", TaxType = ApartmentTaxType.RV });

        Assert.Equal(2, wholeComplex!.PropertyCount);
        Assert.Null(wholeComplex.WingMasterId);
        Assert.Null(wholeComplex.WingName);
        Assert.Null(wholeComplex.WingNo);
        Assert.Equal("Test Society", wholeComplex.SocietyName);
        Assert.Equal(300m, wholeComplex.CurrentTaxes.Single().TaxHeads.Single().TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_Arrears_OnlyClosedYearRetroDemandActiveRows()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(new PropertyEntity { Id = 1, WardId = 96, PropertyNo = "20", IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = 1, TaxCode = "GT", TaxName = "General Tax", TaxCategoryId = 1, CalculationModeId = 1 });
        context.YearMaster.AddRange(
            new YearMasterEntity { Id = 1, YearCode = "2026-27", IsActive = true },
            new YearMasterEntity { Id = 2, YearCode = "2025-26", IsActive = false });
        context.PolicyCodeMaster.AddRange(
            new PolicyCodeMasterEntity { Id = 1, PolicyCode = "RETRO", PolicyName = "Retro Demand", PolicyType = "NORMAL", IsRetroDemand = true },
            new PolicyCodeMasterEntity { Id = 2, PolicyCode = "NETTAX", PolicyName = "Net Tax", PolicyType = "NORMAL", IsRetroDemand = false });
        context.TransMast.AddRange(
            new TransMastEntity { Id = 1, PropertyId = 1, TaxId = 1, FinanceYearId = 2, PolicyCodeId = 1, TaxAmount = 500m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false },
            new TransMastEntity { Id = 2, PropertyId = 1, TaxId = 1, FinanceYearId = 1, PolicyCodeId = 1, TaxAmount = 999m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false },
            new TransMastEntity { Id = 3, PropertyId = 1, TaxId = 1, FinanceYearId = 2, PolicyCodeId = 2, TaxAmount = 888m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false },
            new TransMastEntity { Id = 4, PropertyId = 1, TaxId = 1, FinanceYearId = 2, PolicyCodeId = 1, TaxAmount = 777m, CalculationType = "RV", IsActive = false, MarkedForDeletion = false },
            new TransMastEntity { Id = 5, PropertyId = 1, TaxId = 1, FinanceYearId = 2, PolicyCodeId = 1, TaxAmount = 666m, CalculationType = "RV", IsActive = true, MarkedForDeletion = true });
        await context.SaveChangesAsync();

        var repository = new ApartmentTaxDetailsRepository(context);
        var result = await repository.GetTaxDetailsAsync(
            new ApartmentTaxDetailsQueryParameters { WardId = 96, PropertyNo = "20", TaxType = ApartmentTaxType.RV });

        var arrears = Assert.Single(result!.Arrears);
        Assert.Equal("General Tax", arrears.TaxName);
        Assert.Equal(500m, arrears.TaxAmount);
    }
}
