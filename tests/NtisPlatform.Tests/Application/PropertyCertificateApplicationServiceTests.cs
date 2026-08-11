using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Covers the Building Permission "Save" flow added for floor-wise/property-wise certificates:
/// scope validation and tax-trigger gating on IsTaxable (not IsProtected -- that flag only
/// governs whether the certificate TYPE master row can be deleted). Document upload is handled
/// entirely by the Global Document endpoint (POST /api/documents/upload), not by this service.
/// </summary>
public class PropertyCertificateApplicationServiceTests
{
    private static PropertyCertificateApplicationService BuildService(
        Mock<IPropertyCertificateService> certService,
        Mock<IRepository<PropertyCertificateTypeMasterEntity, int>> typeRepo,
        Mock<IRepository<PropertyDetailsEntity, int>>? detailsRepo = null,
        Mock<IDocumentApplicationService>? documentService = null,
        Mock<IUnitOfWork>? unitOfWork = null,
        Mock<IModuleLookupService>? moduleLookupService = null,
        Mock<IPublisher>? publisher = null,
        Mock<ICertificateTaxGuidelineReaderService>? guidelineReader = null)
    {
        return new PropertyCertificateApplicationService(
            certService.Object,
            (documentService ?? new Mock<IDocumentApplicationService>()).Object,
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object,
            (moduleLookupService ?? new Mock<IModuleLookupService>()).Object,
            typeRepo.Object,
            (detailsRepo ?? new Mock<IRepository<PropertyDetailsEntity, int>>()).Object,
            (publisher ?? new Mock<IPublisher>()).Object,
            (guidelineReader ?? DefaultGuidelineReaderMock()).Object,
            NullLogger<PropertyCertificateApplicationService>.Instance);
    }

    private static Mock<ICertificateTaxGuidelineReaderService> DefaultGuidelineReaderMock()
    {
        var mock = new Mock<ICertificateTaxGuidelineReaderService>();
        mock.Setup(s => s.GetActiveSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateTaxGuidelineSettings(
                EnableCertificateBasedTax: true,
                ApplyOnlyTaxableCertTypes: true,
                DatePriority1: "CC", DatePriority2: "OC", DatePriority3: "ELECTRIC_BILL", DatePriority4: "RETROSPECTIVE",
                CertificateRequireNoAndDate: false,
                MissingCertificateNoAction: "IGNORE_FOR_TAX",
                MissingCertificateDateAction: "IGNORE_FOR_TAX",
                IgnoreCcToOcWithinValue: 6, IgnoreCcToOcWithinType: "MONTHS",
                CcOcGapComparison: "LESS_THAN_OR_EQUAL",
                CcOcGapWithinAction: "APPLY_OC_ONLY",
                CcOcGapExceededAction: "APPLY_CC_THEN_OC",
                InvalidCcOcDateOrderAction: "USE_PRIORITY_AND_LOG",
                CcOnlyAction: "APPLY_FROM_CC_DATE",
                OcOnlyAction: "APPLY_FROM_OC_DATE",
                FinancialYearStartMonth: 4, FinancialYearStartDay: 1,
                CCPeriodMultiplier: 1.0m, OCPeriodMultiplier: 1.0m,
                ElectricBillDateRule: "FROM_FY_START", ElectricBillAddMonths: 0, ElectricBillMultiplier: 1.0m,
                ElectricBillMinimumFinancialYear: 2016, EnableRetrospectiveTax: true,
                NoDateRule: "DEFAULT_RETROSPECTIVE", LookbackYears: 6, DefaultRetrospectiveMultiplier: 1.0m,
                EnableCurrentYearProration: true, ProrationMethod: "DAILY", CurrentYearProrationStartRule: "EXACT_DATE",
                TaxPersistenceMode: "PROPERTY_AGGREGATED",
                SaveInPolicyTaxDetails: true, SaveInTransMast: true, DoNotUpdateNettax: true,
                RecalculateOnSave: true, RecalculateOnDelete: true, GuidelineChangeApplyMode: "NEXT_CALCULATION",
                CcPartialPolicyCode: "PARTIAL_CC", CcFullPolicyCode: "CC",
                OcPartialPolicyCode: "PARTIAL_OC", OcFullPolicyCode: "OC",
                ElectricBillPartialPolicyCode: "PARTIAL_ELECTRIC_BILL", ElectricBillFullPolicyCode: "ELECTRIC_BILL",
                CertificateTaxScopeMode: "FLOOR_WISE", AllowFloorWiseCertificateMetadata: true, EnableCcToOcSplit: true,
                ElectricBillCertificateCodes: "ELECTRIC_BILL", RetrospectiveCurrentYearCount: 1,
                RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT", FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY",
                TaxationRateMode: "CURRENT_YEAR_FOR_ALL", TaxPercentageMode: "CURRENT_YEAR_FOR_ALL", FixedTaxPercentage: 0m));
        return mock;
    }

    [Fact]
    public async Task SaveCertificateAsync_FloorScope_WithoutPropertyDetailsId_ThrowsArgumentException()
    {
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var certService = new Mock<IPropertyCertificateService>();
        var service = BuildService(certService, typeRepo);

        var request = new SaveCertificateRequestDto
        {
            PropertyId = 550722,
            PropertyDetailsId = null,
            CertificateScope = CertificateScope.Floor, // invalid: Floor scope requires PropertyDetailsId
            CertificateTypeId = 2,
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveCertificateAsync(request, userId: 1));
    }

    [Fact]
    public async Task SaveCertificateAsync_PropertyScope_WithPropertyDetailsId_ThrowsArgumentException()
    {
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var certService = new Mock<IPropertyCertificateService>();
        var service = BuildService(certService, typeRepo);

        var request = new SaveCertificateRequestDto
        {
            PropertyId = 550722,
            PropertyDetailsId = 1702274, // invalid: Property scope must not carry a floor id
            CertificateScope = CertificateScope.Property,
            CertificateTypeId = 2,
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveCertificateAsync(request, userId: 1));
    }

    [Fact]
    public async Task SaveCertificateAsync_TaxableType_SavesMetadata_AndReportsTaxTriggered()
    {
        // The RV-refresh-then-Occupation-Tax pipeline is triggered only for taxable certificate
        // types (IsTaxable=1) -- the actual publish happens inside
        // IPropertyCertificateService.CreateAsync/UpdateAsync/ToggleEnabledAsync (see
        // PropertyCertificateServiceCriticalFixTests for that), which is mocked here, so this
        // test only verifies SaveCertificateAsync itself never publishes directly and reports
        // TaxRecalculationTriggered matching the certificate type's IsTaxable flag.
        const int propertyId = 550722;
        const int certificateTypeId = 4;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Occupancy Certificate",
            CertificateTypeCode = "OC",
            IsRequired = false,
            IsProtected = false,
            IsTaxable = true,
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.CreateAsync(
                propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(555);
        certService.Setup(s => s.ToggleEnabledAsync(555, true, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var publisher = new Mock<IPublisher>();
        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork, publisher: publisher);

        var request = new SaveCertificateRequestDto
        {
            PropertyId = propertyId,
            PropertyDetailsId = null,
            CertificateScope = CertificateScope.Property,
            CertificateTypeId = certificateTypeId,
            CertificateNo = "OC-001",
            CertificateIssueDate = DateTime.Now.AddDays(-5)
        };

        var result = await service.SaveCertificateAsync(request, userId: 1);

        Assert.Equal(555, result.PropertyCertificateId);
        Assert.True(result.TaxRecalculationTriggered);
        // SaveCertificateAsync itself never publishes directly (that responsibility lives in
        // IPropertyCertificateService's real implementation, mocked away here).
        publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveCertificateAsync_NonTaxableType_SavesMetadata_AndReportsTaxNotTriggered()
    {
        // A non-taxable certificate type (e.g. "Index 2") must report TaxRecalculationTriggered
        // as false -- the response must reflect whether recalculation actually ran, not just
        // whether the save succeeded.
        const int propertyId = 550722;
        const int certificateTypeId = 4;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Index 2",
            CertificateTypeCode = "INDEX_2",
            IsRequired = false,
            IsProtected = false,
            IsTaxable = false,
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.CreateAsync(
                propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(556);
        certService.Setup(s => s.ToggleEnabledAsync(556, true, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork);

        var request = new SaveCertificateRequestDto
        {
            PropertyId = propertyId,
            PropertyDetailsId = null,
            CertificateScope = CertificateScope.Property,
            CertificateTypeId = certificateTypeId,
            CertificateNo = "INDEX2-001",
            CertificateIssueDate = DateTime.Now.AddDays(-5)
        };

        var result = await service.SaveCertificateAsync(request, userId: 1);

        Assert.Equal(556, result.PropertyCertificateId);
        Assert.False(result.TaxRecalculationTriggered);
    }

    [Fact]
    public async Task SaveCertificateAsync_RequiredType_NoDocumentYet_StillSavesMetadata()
    {
        // Document upload is fully decoupled (goes through POST /api/documents/upload separately),
        // so IsRequired no longer blocks saving certificate metadata — it's just descriptive/UI-facing.
        const int propertyId = 550722;
        const int certificateTypeId = 7;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Possession Certificate",
            CertificateTypeCode = "POSSESSION",
            IsRequired = true,
            IsProtected = false,
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.CreateAsync(
                propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(777);
        certService.Setup(s => s.ToggleEnabledAsync(777, true, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork);

        var request = new SaveCertificateRequestDto
        {
            PropertyId = propertyId,
            PropertyDetailsId = null,
            CertificateScope = CertificateScope.Property,
            CertificateTypeId = certificateTypeId,
            CertificateNo = "POSS-001",
            CertificateIssueDate = DateTime.Now.AddDays(-10)
        };

        var result = await service.SaveCertificateAsync(request, userId: 1);

        Assert.Equal(777, result.PropertyCertificateId);
        Assert.Null(result.DocumentBindingId);
    }

    [Fact]
    public async Task SaveCertificateAsync_NewCertificate_DoesNotRedundantlyToggleEnabled()
    {
        // PropertyCertificateEntity.Create already sets IsActive = true. Calling
        // ToggleEnabledAsync right after would be a same-state no-op that still unconditionally
        // publishes a second PropertyCertificateChangedEvent, running the whole RV + Occupation
        // Tax recalculation pipeline twice for one certificate creation -- which collided with
        // itself once the UnitOfWork nested-transaction fix let both actually execute.
        const int propertyId = 549441;
        const int certificateTypeId = 9;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Commencement Certificate",
            CertificateTypeCode = "CC",
            IsRequired = false,
            IsProtected = false,
            IsTaxable = true,
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.CreateAsync(
                propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .ReturnsAsync(901);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork);

        var request = new SaveCertificateRequestDto
        {
            PropertyId = propertyId,
            PropertyDetailsId = null,
            CertificateScope = CertificateScope.Property,
            CertificateTypeId = certificateTypeId,
            CertificateNo = "525565322442",
            CertificateIssueDate = DateTime.Now.AddDays(-3)
        };

        var result = await service.SaveCertificateAsync(request, userId: 1);

        Assert.Equal(901, result.PropertyCertificateId);
        certService.Verify(s => s.CreateAsync(
            propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>(), It.IsAny<int?>()), Times.Once);
        certService.Verify(s => s.ToggleEnabledAsync(
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveAllAsync_NewCertificate_DoesNotRedundantlyToggleEnabled()
    {
        const int propertyId = 549441;
        const int certificateTypeId = 9;

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity>());

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.CreateAsync(
                propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), true))
            .ReturnsAsync(902);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork);

        var bulkDto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = propertyId,
            Certificates = new List<PropertyCertificateItemDto>
            {
                new()
                {
                    CertificateTypeId = certificateTypeId,
                    IsEnabled = true,
                    CertificateNumber = "525565322442",
                    CertificateDate = DateTime.Now.AddDays(-3)
                }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, userId: 1);

        Assert.Equal(1, response.EnabledCount);
        Assert.Empty(response.Errors);
        // suppressRecalculation: true -- BulkSaveAllAsync suppresses each row's own publish and
        // fires the RV+Occupation Tax pipeline at most once for the whole batch (see the
        // BulkSaveAllAsync_* recalculation tests below for that behavior).
        certService.Verify(s => s.CreateAsync(
            propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>(), It.IsAny<int?>(), true), Times.Once);
        certService.Verify(s => s.ToggleEnabledAsync(
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
    }

    // =========================================================================================
    // GO-LIVE BLOCKER regression: selecting multiple floors in the "Confirm Save Details" bulk
    // save previously let each floor's certificate publish its own PropertyCertificateChangedEvent
    // inline, so OccupationTaxApplicationService.ApplyAsync/SaveTaxesAsync ran once per floor
    // within the same request and collided on PolicyTaxDetails'/TransMast's unique keys. Fixed by
    // suppressing each row's own publish and firing the recalculation pipeline exactly once, after
    // every certificate in the batch is saved.
    // =========================================================================================

    [Fact]
    public async Task BulkSaveAllAsync_MultipleFloorsSameTaxableType_PublishesRecalculationExactlyOnce()
    {
        const int propertyId = 549441;
        const int certificateTypeId = 9;

        var taxableType = new PropertyCertificateTypeMasterEntity { IsTaxable = true };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(taxableType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { taxableType });

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.CreateAsync(
                propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), true))
            .ReturnsAsync(902);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var publisher = new Mock<IPublisher>();
        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork, publisher: publisher);

        // Same certificate type applied to two different floors in one bulk request -- mirrors the
        // reported repro (one CC document/number/date, multiple floors selected in one Save).
        var bulkDto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = propertyId,
            Certificates = new List<PropertyCertificateItemDto>
            {
                new() { CertificateTypeId = certificateTypeId, PropertyDetailsId = 9401, IsEnabled = true, CertificateNumber = "CC-001", CertificateDate = new DateTime(2026, 4, 17) },
                new() { CertificateTypeId = certificateTypeId, PropertyDetailsId = 9402, IsEnabled = true, CertificateNumber = "CC-001", CertificateDate = new DateTime(2026, 4, 17) }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, userId: 1);

        Assert.Equal(2, response.EnabledCount);
        Assert.Empty(response.Errors);
        certService.Verify(s => s.CreateAsync(
            propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>(), It.IsAny<int?>(), true), Times.Exactly(2));
        publisher.Verify(p => p.Publish(
            It.Is<PropertyCertificateChangedEvent>(e => e.PropertyId == propertyId),
            It.IsAny<CancellationToken>()), Times.Once, "one recalculation for the whole batch, not one per floor");
    }

    [Fact]
    public async Task BulkSaveAllAsync_NoTaxableCertificatesInBatch_DoesNotPublish()
    {
        const int propertyId = 549441;
        const int certificateTypeId = 11;

        var nonTaxableType = new PropertyCertificateTypeMasterEntity { IsTaxable = false };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(nonTaxableType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { nonTaxableType });

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.CreateAsync(
                propertyId, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), true))
            .ReturnsAsync(903);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var publisher = new Mock<IPublisher>();
        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork, publisher: publisher);

        var bulkDto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = propertyId,
            Certificates = new List<PropertyCertificateItemDto>
            {
                new() { CertificateTypeId = certificateTypeId, PropertyDetailsId = 9401, IsEnabled = true, CertificateNumber = "POSS-001", CertificateDate = new DateTime(2026, 4, 17) }
            }
        };

        await service.BulkSaveAllAsync(bulkDto, userId: 1);

        publisher.Verify(p => p.Publish(
            It.IsAny<PropertyCertificateChangedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveAllAsync_NewOcEarlierThanExistingActiveCc_ThrowsInvalidOperationException()
    {
        const int propertyId = 549441;
        const int ccTypeId = 8;
        const int ocTypeId = 9;
        const int existingCcId = 950;

        var ccType = new PropertyCertificateTypeMasterEntity { IsTaxable = true, CertificateTypeCode = "CC" };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(ccType, ccTypeId);
        var ocType = new PropertyCertificateTypeMasterEntity { IsTaxable = true, CertificateTypeCode = "OC" };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(ocType, ocTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { ccType, ocType });

        // Existing active CC dated 17-Apr-2026; the incoming batch tries to enable an OC dated
        // earlier (10-Apr-2026), which must never be savable.
        var existingCc = PropertyCertificateEntity.Create(propertyId, ccTypeId, "CC-001", new DateTime(2026, 4, 17));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(existingCc, existingCcId);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { existingCc });

        var unitOfWork = new Mock<IUnitOfWork>();
        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork);

        var bulkDto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = propertyId,
            Certificates = new List<PropertyCertificateItemDto>
            {
                new() { CertificateTypeId = ocTypeId, IsEnabled = true, CertificateNumber = "OC-001", CertificateDate = new DateTime(2026, 4, 10) }
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BulkSaveAllAsync(bulkDto, userId: 1));
        Assert.Contains("cannot be earlier than", ex.Message);

        // The whole batch must be rejected before the transaction (and any row writes) even begins.
        unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        certService.Verify(s => s.CreateAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveAllAsync_NewOcOnOrAfterExistingActiveCc_Succeeds()
    {
        const int propertyId = 549441;
        const int ccTypeId = 8;
        const int ocTypeId = 9;
        const int existingCcId = 950;

        var ccType = new PropertyCertificateTypeMasterEntity { IsTaxable = true, CertificateTypeCode = "CC" };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(ccType, ccTypeId);
        var ocType = new PropertyCertificateTypeMasterEntity { IsTaxable = true, CertificateTypeCode = "OC" };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(ocType, ocTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { ccType, ocType });

        // Existing active CC dated 17-Apr-2026; the incoming OC is dated after it -- a valid order.
        var existingCc = PropertyCertificateEntity.Create(propertyId, ccTypeId, "CC-001", new DateTime(2026, 4, 17));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(existingCc, existingCcId);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { existingCc });
        certService.Setup(s => s.CreateAsync(
                propertyId, ocTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), true))
            .ReturnsAsync(951);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork);

        var bulkDto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = propertyId,
            Certificates = new List<PropertyCertificateItemDto>
            {
                new() { CertificateTypeId = ocTypeId, IsEnabled = true, CertificateNumber = "OC-001", CertificateDate = new DateTime(2026, 5, 1) }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, userId: 1);

        Assert.Equal(1, response.EnabledCount);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task BulkSaveAllAsync_DisablingTaxableCertificate_PublishesUnderRecalculateOnDelete()
    {
        const int propertyId = 549441;
        const int certificateTypeId = 9;
        const int existingCertId = 950;

        var taxableType = new PropertyCertificateTypeMasterEntity { IsTaxable = true };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(taxableType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { taxableType });

        var existingCert = PropertyCertificateEntity.Create(propertyId, certificateTypeId, "CC-001", new DateTime(2026, 4, 17));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(existingCert, existingCertId);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { existingCert });
        certService.Setup(s => s.ToggleEnabledAsync(existingCertId, false, It.IsAny<int>(), It.IsAny<CancellationToken>(), true))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var publisher = new Mock<IPublisher>();
        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork, publisher: publisher);

        var bulkDto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = propertyId,
            Certificates = new List<PropertyCertificateItemDto>
            {
                new() { CertificateTypeId = certificateTypeId, PropertyDetailsId = null, IsEnabled = false }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, userId: 1);

        Assert.Equal(1, response.DisabledCount);
        certService.Verify(s => s.ToggleEnabledAsync(existingCertId, false, It.IsAny<int>(), It.IsAny<CancellationToken>(), true), Times.Once);
        publisher.Verify(p => p.Publish(
            It.Is<PropertyCertificateChangedEvent>(e => e.PropertyId == propertyId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkSaveAllAsync_ReSavingExistingCertificate_UpdatesRowAndPublishesExactlyOnce()
    {
        // Re-saving the same certificate (e.g. re-confirming the popup, or replacing the file for
        // an already-saved floor) must update the existing row, not create a second one, and must
        // still only trigger one recalculation for the whole batch.
        const int propertyId = 549441;
        const int certificateTypeId = 9;
        const int existingCertId = 951;

        var taxableType = new PropertyCertificateTypeMasterEntity { IsTaxable = true };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(taxableType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { taxableType });

        var existingCert = PropertyCertificateEntity.Create(propertyId, certificateTypeId, "CC-001", new DateTime(2026, 4, 17), propertyDetailsId: 9401);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(existingCert, existingCertId);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { existingCert });
        certService.Setup(s => s.UpdateAsync(
                existingCertId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), true))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var publisher = new Mock<IPublisher>();
        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork, publisher: publisher);

        var bulkDto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = propertyId,
            Certificates = new List<PropertyCertificateItemDto>
            {
                new() { CertificateTypeId = certificateTypeId, PropertyDetailsId = 9401, IsEnabled = true, CertificateNumber = "CC-001-REPLACED", CertificateDate = new DateTime(2026, 4, 17) }
            }
        };

        var response = await service.BulkSaveAllAsync(bulkDto, userId: 1);

        Assert.Equal(1, response.EnabledCount);
        certService.Verify(s => s.CreateAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>()), Times.Never,
            "an already-existing certificate must be updated, never re-created");
        certService.Verify(s => s.UpdateAsync(
            existingCertId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), true), Times.Once);
        publisher.Verify(p => p.Publish(
            It.Is<PropertyCertificateChangedEvent>(e => e.PropertyId == propertyId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFloorCertificatesAsync_SplitsSelectedFloorFromOtherFloors()
    {
        const int propertyId = 549441;
        const int selectedFloorId = 100;
        const int otherFloorId = 200;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { PropertyId = propertyId, IsActive = true },
            new() { PropertyId = propertyId, IsActive = true },
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(floors[0], selectedFloorId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(floors[1], otherFloorId);

        var detailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
        detailsRepo.Setup(r => r.GetQueryable()).Returns(floors.BuildMock());

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity>());

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());

        var service = BuildService(certService, typeRepo, detailsRepo: detailsRepo);

        var response = await service.GetFloorCertificatesAsync(propertyId, selectedFloorId);

        Assert.NotNull(response.SelectedFloor);
        Assert.Equal(selectedFloorId, response.SelectedFloor!.PropertyDetailsId);
        Assert.True(response.SelectedFloor.IsSelected);

        var other = Assert.Single(response.OtherFloors);
        Assert.Equal(otherFloorId, other.PropertyDetailsId);
        Assert.False(other.IsSelected);
    }

    [Fact]
    public async Task GetFloorCertificatesAsync_NoSelectedId_AllFloorsInOtherFloors()
    {
        const int propertyId = 549441;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { PropertyId = propertyId, IsActive = true },
            new() { PropertyId = propertyId, IsActive = true },
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(floors[0], 100);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(floors[1], 200);

        var detailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
        detailsRepo.Setup(r => r.GetQueryable()).Returns(floors.BuildMock());

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity>());

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());

        var service = BuildService(certService, typeRepo, detailsRepo: detailsRepo);

        var response = await service.GetFloorCertificatesAsync(propertyId, selectedPropertyDetailsId: null);

        Assert.Null(response.SelectedFloor);
        Assert.Equal(2, response.OtherFloors.Count);
    }

    /// <summary>
    /// Shared setup for the three ResolveEffectiveDate name-fallback tests below: one floor, one
    /// property-wise certificate whose CertificateTypeCode is deliberately left blank (as older/seed
    /// data may have it) so only the display-name heuristic can resolve it.
    /// </summary>
    private static async Task<FloorCertificatesResponseDto> RunNameFallbackScenario(string certificateTypeName, DateTime issueDate)
    {
        const int propertyId = 549500;
        const int floorId = 900;

        var floors = new List<PropertyDetailsEntity> { new() { PropertyId = propertyId, IsActive = true } };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(floors[0], floorId);

        var detailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
        detailsRepo.Setup(r => r.GetQueryable()).Returns(floors.BuildMock());

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity>());

        // Property-wise certificate (PropertyDetailsId = null) with a BLANK CertificateTypeCode --
        // only the CertificateTypeName heuristic can identify this as OC/CC/Electric Bill.
        var cert = PropertyCertificateEntity.Create(propertyId, certificateTypeId: 1, "CERT-001", issueDate);
        typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType))!.SetValue(cert, new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = certificateTypeName,
            CertificateTypeCode = string.Empty
        });

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { cert });
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { cert });

        var service = BuildService(certService, typeRepo, detailsRepo: detailsRepo);

        return await service.GetFloorCertificatesAsync(propertyId, selectedPropertyDetailsId: null);
    }

    [Fact]
    public async Task ResolveEffectiveDate_BlankCertificateTypeCode_NameContainsOccupancy_ReturnsOcDate()
    {
        var issueDate = new DateTime(2026, 3, 1);
        var response = await RunNameFallbackScenario("Occupancy Certificate", issueDate);

        var floor = Assert.Single(response.OtherFloors);
        Assert.Equal(issueDate, floor.OcDate);
        Assert.Null(floor.CcDate);
        Assert.Null(floor.ElectricBillDate);
    }

    [Fact]
    public async Task ResolveEffectiveDate_BlankCertificateTypeCode_NameContainsCompletion_ReturnsCcDate()
    {
        var issueDate = new DateTime(2026, 3, 1);
        var response = await RunNameFallbackScenario("Completion Certificate", issueDate);

        var floor = Assert.Single(response.OtherFloors);
        Assert.Equal(issueDate, floor.CcDate);
        Assert.Null(floor.OcDate);
        Assert.Null(floor.ElectricBillDate);
    }

    [Fact]
    public async Task ResolveEffectiveDate_BlankCertificateTypeCode_NameContainsElectricBill_ReturnsElectricBillDate()
    {
        var issueDate = new DateTime(2026, 3, 1);
        var response = await RunNameFallbackScenario("Electric Bill Statement", issueDate);

        var floor = Assert.Single(response.OtherFloors);
        Assert.Equal(issueDate, floor.ElectricBillDate);
        Assert.Null(floor.OcDate);
        Assert.Null(floor.CcDate);
    }

    [Fact]
    public async Task DeleteCertificateByTypeAsync_MatchFound_ResolvesIdAndDelegatesToDeleteAsync()
    {
        const int propertyId = 549349;
        const int certificateTypeId = 1;
        const int resolvedCertificateId = 777;

        var cert = PropertyCertificateEntity.Create(propertyId, certificateTypeId, "CC-001", DateTime.Now.AddDays(-5));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(cert, resolvedCertificateId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { cert });
        certService.Setup(s => s.DeleteAsync(resolvedCertificateId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(certService, typeRepo);

        await service.DeleteCertificateByTypeAsync(propertyId, certificateTypeId, propertyDetailsId: null, deletedBy: 1);

        certService.Verify(s => s.DeleteAsync(resolvedCertificateId, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCertificateByTypeAsync_NoMatch_ThrowsNotFound()
    {
        const int propertyId = 549349;
        const int certificateTypeId = 1;

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());

        var service = BuildService(certService, typeRepo);

        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(() =>
            service.DeleteCertificateByTypeAsync(propertyId, certificateTypeId, propertyDetailsId: null, deletedBy: 1));

        certService.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCertificateByTypeAsync_WithAttachedDocument_CascadeDeletesDocumentBeforeMetadata()
    {
        // Deleting the metadata row must not leave an orphaned, still-active document behind --
        // when the resolved certificate has a DocumentBinding, the document must be unlinked and
        // soft-deleted before (or alongside) the certificate row itself, so no active document
        // survives a metadata delete.
        const int propertyId = 549446;
        const int certificateTypeId = 1;
        const int resolvedCertificateId = 777;
        var documentGuid = Guid.NewGuid();

        var cert = PropertyCertificateEntity.Create(propertyId, certificateTypeId, "CC-001", DateTime.Now.AddDays(-5));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(cert, resolvedCertificateId);
        typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.DocumentBinding))!.SetValue(
            cert, new DocumentBindingEntity { Document = new DocumentEntity { DocumentGuid = documentGuid } });

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { cert });
        certService.Setup(s => s.UnlinkDocumentBindingAsync(resolvedCertificateId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        certService.Setup(s => s.DeleteAsync(resolvedCertificateId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var documentService = new Mock<IDocumentApplicationService>();
        documentService.Setup(d => d.DeleteDocumentAsync(documentGuid, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = BuildService(certService, typeRepo, documentService: documentService);

        await service.DeleteCertificateByTypeAsync(propertyId, certificateTypeId, propertyDetailsId: null, deletedBy: 1);

        certService.Verify(s => s.UnlinkDocumentBindingAsync(resolvedCertificateId, 1, It.IsAny<CancellationToken>()), Times.Once);
        documentService.Verify(d => d.DeleteDocumentAsync(documentGuid, 1, It.IsAny<CancellationToken>()), Times.Once);
        certService.Verify(s => s.DeleteAsync(resolvedCertificateId, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCertificateByTypeAsync_NoAttachedDocument_SkipsDocumentCleanup()
    {
        const int propertyId = 549349;
        const int certificateTypeId = 1;
        const int resolvedCertificateId = 777;

        // No DocumentBinding set -- this certificate never had a document attached.
        var cert = PropertyCertificateEntity.Create(propertyId, certificateTypeId, "CC-001", DateTime.Now.AddDays(-5));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(cert, resolvedCertificateId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { cert });
        certService.Setup(s => s.DeleteAsync(resolvedCertificateId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var documentService = new Mock<IDocumentApplicationService>();

        var service = BuildService(certService, typeRepo, documentService: documentService);

        await service.DeleteCertificateByTypeAsync(propertyId, certificateTypeId, propertyDetailsId: null, deletedBy: 1);

        certService.Verify(s => s.UnlinkDocumentBindingAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        documentService.Verify(d => d.DeleteDocumentAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        certService.Verify(s => s.DeleteAsync(resolvedCertificateId, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ------------------------------------------------------------------------------------------
    // ReplaceCertificateByTypeAsync -- the scoped replacement flow (2026-07-23 audit). Unlike
    // DeleteCertificateByTypeAsync (which must keep recalculating immediately on every standalone
    // delete, verified untouched above), this method suppresses BOTH its internal delete and
    // create calls and publishes exactly ONE PropertyCertificateChangedEvent against the final
    // (new-certificate-present) state -- never the momentarily-certificate-less intermediate state
    // a separate delete-then-save over two calls would expose.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task ReplaceCertificateByTypeAsync_MatchFound_SuppressesInternalCallsAndPublishesExactlyOnce()
    {
        const int propertyId = 549349;
        const int certificateTypeId = 1;
        const int resolvedCertificateId = 777;
        const int newCertificateId = 778;
        var newDate = new DateTime(2026, 5, 1);

        var cert = PropertyCertificateEntity.Create(propertyId, certificateTypeId, "OC-OLD", new DateTime(2024, 4, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(cert, resolvedCertificateId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyCertificateTypeMasterEntity { Id = certificateTypeId, IsTaxable = true, IsActive = true });

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { cert });
        certService.Setup(s => s.DeleteAsync(resolvedCertificateId, It.IsAny<int>(), It.IsAny<CancellationToken>(), true))
            .Returns(Task.CompletedTask);
        certService.Setup(s => s.CreateAsync(
                propertyId, certificateTypeId, "OC-NEW", newDate, It.IsAny<int>(), It.IsAny<CancellationToken>(), null, true))
            .ReturnsAsync(newCertificateId);

        var publisher = new Mock<IPublisher>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var service = BuildService(certService, typeRepo, unitOfWork: unitOfWork, publisher: publisher);

        var result = await service.ReplaceCertificateByTypeAsync(
            propertyId, certificateTypeId, oldPropertyDetailsId: null, newPropertyDetailsId: null,
            newCertificateNo: "OC-NEW", newIssueDate: newDate, userId: 1);

        Assert.Equal(newCertificateId, result);
        certService.Verify(s => s.DeleteAsync(resolvedCertificateId, 1, It.IsAny<CancellationToken>(), true), Times.Once);
        certService.Verify(s => s.CreateAsync(propertyId, certificateTypeId, "OC-NEW", newDate, 1, It.IsAny<CancellationToken>(), null, true), Times.Once);
        publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceCertificateByTypeAsync_NoMatch_ThrowsNotFound()
    {
        const int propertyId = 549349;
        const int certificateTypeId = 1;

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());

        var publisher = new Mock<IPublisher>();
        var service = BuildService(certService, typeRepo, publisher: publisher);

        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(() =>
            service.ReplaceCertificateByTypeAsync(
                propertyId, certificateTypeId, oldPropertyDetailsId: null, newPropertyDetailsId: null,
                newCertificateNo: "OC-NEW", newIssueDate: DateTime.Now, userId: 1));

        certService.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
        certService.Verify(s => s.CreateAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>()), Times.Never);
        publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCertificateByTypeAsync_StillPublishesUnsuppressed_NotAffectedByReplaceMethod()
    {
        // Regression guard: adding ReplaceCertificateByTypeAsync must NOT change
        // DeleteCertificateByTypeAsync's existing, safe, unsuppressed behavior -- it must keep
        // recalculating/cleaning up immediately on every standalone delete (it is a general-purpose,
        // independently-callable API with no guaranteed follow-up create).
        const int propertyId = 549349;
        const int certificateTypeId = 1;
        const int resolvedCertificateId = 777;

        var cert = PropertyCertificateEntity.Create(propertyId, certificateTypeId, "CC-001", DateTime.Now.AddDays(-5));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(cert, resolvedCertificateId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { cert });
        certService.Setup(s => s.DeleteAsync(resolvedCertificateId, It.IsAny<int>(), It.IsAny<CancellationToken>(), false))
            .Returns(Task.CompletedTask);

        var service = BuildService(certService, typeRepo);

        await service.DeleteCertificateByTypeAsync(propertyId, certificateTypeId, propertyDetailsId: null, deletedBy: 1);

        // The call site passes no suppressRecalculation argument at all -- confirms it still
        // resolves to the default (false), not some suppressed overload.
        certService.Verify(s => s.DeleteAsync(resolvedCertificateId, 1, It.IsAny<CancellationToken>(), false), Times.Once);
    }
}
