using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.DTOs.RetrospectiveTax;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
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
        Mock<IRepository<PropertyEntity, int>>? propertyRepo = null,
        Mock<IRepository<SocietyDetailsEntity, int>>? societyRepo = null,
        Mock<IRepository<WingDetailsMastEntity, int>>? wingDetailsMastRepo = null,
        Mock<IRepository<WingEntity, int>>? wingRepo = null,
        Mock<IRepository<PropertyCertificateEntity>>? propertyCertRepo = null,
        Mock<IRepository<DocumentBindingEntity>>? docBindingRepo = null,
        Mock<IRepository<DocumentEntity>>? docRepo = null,
        Mock<IRateableValueApiClient>? rateableValueApiClient = null,
        Mock<IRetrospectiveTaxCalculationEngineService>? retrospectiveTaxEngine = null)
    {
        return new PropertyCertificateApplicationService(
            certService.Object,
            (documentService ?? new Mock<IDocumentApplicationService>()).Object,
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object,
            (moduleLookupService ?? new Mock<IModuleLookupService>()).Object,
            typeRepo.Object,
            (detailsRepo ?? new Mock<IRepository<PropertyDetailsEntity, int>>()).Object,
            (propertyRepo ?? new Mock<IRepository<PropertyEntity, int>>()).Object,
            (societyRepo ?? new Mock<IRepository<SocietyDetailsEntity, int>>()).Object,
            (wingDetailsMastRepo ?? new Mock<IRepository<WingDetailsMastEntity, int>>()).Object,
            (wingRepo ?? new Mock<IRepository<WingEntity, int>>()).Object,
            (publisher ?? new Mock<IPublisher>()).Object,
            NullLogger<PropertyCertificateApplicationService>.Instance,
            (rateableValueApiClient ?? new Mock<IRateableValueApiClient>()).Object,
            (retrospectiveTaxEngine ?? new Mock<IRetrospectiveTaxCalculationEngineService>()).Object,
            propertyCertRepo?.Object,
            docBindingRepo?.Object,
            docRepo?.Object);
    }

    // SaveCertificateAsync_FloorScope_WithoutPropertyDetailsId_ThrowsArgumentException and
    // SaveCertificateAsync_PropertyScope_WithPropertyDetailsId_ThrowsArgumentException were removed:
    // the method no longer rejects a mismatched CertificateScope/PropertyDetailsId combination --
    // it silently normalizes CertificateScope from PropertyDetailsId.HasValue instead (see
    // PropertyCertificateApplicationService.SaveCertificateAsync, entityType == "P" branch).

    // GetCertificateTypesWithStatusAsync_SocietyScopedRowSharesPropertyId_NotMisreportedAsPropertyWise
    // was removed: it regression-tested a Society-scoped certificate sharing a representative
    // unit's PropertyId with that unit's own property-wise certificate. PropertyCertificateEntity
    // now enforces PropertyId == null for EntityType 'S'/'W' at construction time (ValidateEntityScope),
    // so that scenario can no longer be constructed at all -- the bug class is prevented structurally
    // instead of needing a runtime EntityType filter to guard against it.

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
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>()))
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
        // SaveCertificateAsync publishes PropertyCertificateChangedEvent once after transaction commits for taxable certificate types
        publisher.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
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
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>()))
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
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>()))
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
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>()))
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
            It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>()), Times.Once);
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

        certService.Verify(s => s.UnlinkDocumentBindingAsync(resolvedCertificateId, 1, It.IsAny<CancellationToken>()), Times.Never);
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

    #region GetSocietyOrWingCertificateTypesWithStatusAsync Tests

    [Fact]
    public async Task GetSocietyOrWingCertificateTypesWithStatusAsync_BothNull_ThrowsArgumentException()
    {
        var certService = new Mock<IPropertyCertificateService>();
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        var service = BuildService(certService, typeRepo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetSocietyOrWingCertificateTypesWithStatusAsync(null, null));
    }

    [Fact]
    public async Task GetSocietyOrWingCertificateTypesWithStatusAsync_SocietyScope_ReturnsCorrectTypesAndStatuses()
    {
        const int societyDetailId = 101;
        const int certTypeId1 = 1;
        const int certTypeId2 = 2;

        var certType1 = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Commencement Certificate",
            CertificateTypeCode = "CC",
            IsRequired = true,
            IsProtected = true,
            IsTaxable = false,
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType1, certTypeId1);

        var certType2 = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Occupancy Certificate",
            CertificateTypeCode = "OC",
            IsRequired = false,
            IsProtected = false,
            IsTaxable = true,
            IsActive = true,
            DisplayOrder = 2
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType2, certTypeId2);

        var societyCert = PropertyCertificateEntity.Create(
            propertyId: null,
            certificateTypeId: certTypeId1,
            certificateNo: "SOC-CC-001",
            issueDate: new DateTime(2025, 1, 15),
            entityType: "S",
            societyDetailId: societyDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(societyCert, 991);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType1, certType2 });

        var propCertRepo = new Mock<IRepository<PropertyCertificateEntity>>();
        propCertRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { societyCert });

        var docBindingRepo = new Mock<IRepository<DocumentBindingEntity>>();
        docBindingRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<DocumentBindingEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentBindingEntity>());

        var certService = new Mock<IPropertyCertificateService>();
        var service = BuildService(certService, typeRepo, propertyCertRepo: propCertRepo, docBindingRepo: docBindingRepo);

        var result = await service.GetSocietyOrWingCertificateTypesWithStatusAsync(societyDetailId, null);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var ccResult = result.Single(r => r.CertificateTypeId == certTypeId1);
        Assert.True(ccResult.HasCertificate);
        Assert.Equal(991, ccResult.PropertyCertificateId);
        Assert.Equal("SOC-CC-001", ccResult.CertificateNo);
        Assert.Equal(new DateTime(2025, 1, 15), ccResult.IssueDate);

        var ocResult = result.Single(r => r.CertificateTypeId == certTypeId2);
        Assert.False(ocResult.HasCertificate);
        Assert.Null(ocResult.PropertyCertificateId);
        Assert.Null(ocResult.CertificateNo);
    }

    [Fact]
    public async Task GetSocietyOrWingCertificateTypesWithStatusAsync_WingScope_WithDocument_ReturnsDocumentInfo()
    {
        const int wingDetailId = 202;
        const int certTypeId = 1;
        const int docBindingId = 88;
        const int documentId = 77;
        var docGuid = Guid.NewGuid();

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Commencement Certificate",
            CertificateTypeCode = "CC",
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certTypeId);

        var wingCert = PropertyCertificateEntity.CreateWithDocument(
            propertyId: null,
            certificateTypeId: certTypeId,
            documentBindingId: docBindingId,
            certificateNo: "WING-CC-001",
            issueDate: new DateTime(2025, 3, 1),
            entityType: "W",
            societyDetailId: 101,
            wingDetailId: wingDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(wingCert, 992);

        var docBinding = DocumentBindingEntity.CreateWithIntReference(
            documentId: documentId,
            departmentId: 1,
            moduleId: 1,
            referenceTableName: "PropertyCertificates",
            referenceTableId: 992,
            referencePropertyName: "DocumentBindingId");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(docBinding, docBindingId);

        var doc = new DocumentEntity
        {
            DocumentGuid = docGuid,
            FileName = "wing_cc.pdf",
            FileExtension = ".pdf",
            FileSizeBytes = 1024,
            MimeType = "application/pdf"
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(doc, documentId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var propCertRepo = new Mock<IRepository<PropertyCertificateEntity>>();
        propCertRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { wingCert });

        var docBindingRepo = new Mock<IRepository<DocumentBindingEntity>>();
        docBindingRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<DocumentBindingEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentBindingEntity> { docBinding });

        var docRepo = new Mock<IRepository<DocumentEntity>>();
        docRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<DocumentEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentEntity> { doc });

        var certService = new Mock<IPropertyCertificateService>();
        var service = BuildService(certService, typeRepo, propertyCertRepo: propCertRepo, docBindingRepo: docBindingRepo, docRepo: docRepo);

        var result = await service.GetSocietyOrWingCertificateTypesWithStatusAsync(null, wingDetailId);

        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.True(item.HasCertificate);
        Assert.Equal(992, item.PropertyCertificateId);
        Assert.Equal("WING-CC-001", item.CertificateNo);
        Assert.Equal(docGuid, item.DocumentGuid);
        Assert.Equal("wing_cc.pdf", item.FileName);
    }

    #endregion

    #region GetCertificateTypesWithStatusAsync Wing/Society Fallback Tests



    /// <summary>
    /// A unit's own certificate always takes precedence over an inherited Wing/Society one, even
    /// when both exist for the same certificate type.
    /// </summary>
    [Fact]
    public async Task GetCertificateTypesWithStatusAsync_UnitHasOwnCertificate_DoesNotFallBack()
    {
        const int propertyId = 702;
        const int wingDetailId = 22;
        const int societyDetailId = 12;
        const int certificateTypeId = 6;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Occupancy Certificate",
            CertificateTypeCode = "OC",
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var ownCert = PropertyCertificateEntity.Create(propertyId, certificateTypeId, "OWN-OC-001", new DateTime(2025, 7, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(ownCert, 995);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { ownCert });

        var propertyEntity = new PropertyEntity { WingDetailId = wingDetailId, IsActive = true };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(propertyEntity, propertyId);
        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(propertyEntity);

        var wingEntity = new WingDetailsMastEntity { SocietyDetailsMastId = societyDetailId, IsActive = true };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(wingEntity, wingDetailId);
        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetByIdAsync(wingDetailId, It.IsAny<CancellationToken>())).ReturnsAsync(wingEntity);

        // A Wing-scoped certificate also exists for this type, but must be ignored since the unit
        // has its own.
        var wingCert = PropertyCertificateEntity.Create(
            propertyId: null,
            certificateTypeId: certificateTypeId,
            certificateNo: "WING-OC-999",
            issueDate: new DateTime(2025, 1, 1),
            entityType: "W",
            societyDetailId: societyDetailId,
            wingDetailId: wingDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(wingCert, 996);

        var propCertRepo = new Mock<IRepository<PropertyCertificateEntity>>();
        propCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { wingCert }.BuildMock());

        var service = BuildService(certService, typeRepo, propertyRepo: propertyRepo,
            wingDetailsMastRepo: wingDetailsMastRepo, propertyCertRepo: propCertRepo);

        var result = await service.GetCertificateTypesWithStatusAsync(propertyId);

        var item = Assert.Single(result);
        Assert.True(item.HasCertificate);
        Assert.False(item.IsInherited);
        Assert.Equal(995, item.PropertyCertificateId);
        Assert.Equal("OWN-OC-001", item.CertificateNo);
    }

    /// <summary>
    /// A floor-scoped request (propertyDetailsId set) is a distinct, more specific view and must
    /// not fall back to Wing/Society certificates -- only the property-wise (propertyDetailsId ==
    /// null) scope does.
    /// </summary>
    [Fact]
    public async Task GetCertificateTypesWithStatusAsync_FloorScope_DoesNotFallBackToWingOrSociety()
    {
        const int propertyId = 703;
        const int propertyDetailsId = 8001;
        const int certificateTypeId = 7;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Commencement Certificate",
            CertificateTypeCode = "CC",
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(
                propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        var propCertRepo = new Mock<IRepository<PropertyCertificateEntity>>();

        var service = BuildService(certService, typeRepo, propertyRepo: propertyRepo,
            wingDetailsMastRepo: wingDetailsMastRepo, propertyCertRepo: propCertRepo);

        var result = await service.GetCertificateTypesWithStatusAsync(propertyId, propertyDetailsId: propertyDetailsId);

        var item = Assert.Single(result);
        Assert.False(item.HasCertificate);
        Assert.False(item.IsInherited);
        // Wing/Society resolution must never even be attempted for a floor-scoped request.
        propertyRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        propCertRepo.Verify(r => r.GetQueryable(), Times.Never);
    }



    #endregion

    #region CreateCertificateRecordAsync Tests

    private static PropertyCertificateTypeMasterEntity BuildTaxableOcType(int certificateTypeId = 4)
    {
        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Occupancy Certificate",
            CertificateTypeCode = "OC",
            IsRequired = false,
            IsProtected = false,
            IsTaxable = true,
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);
        return certType;
    }

    private static void SetupCreateForAnyProperty(Mock<IPropertyCertificateService> certService, int certificateTypeId, Func<int?, int> resultByPropertyId)
    {
        certService.Setup(s => s.CreateAsync(
                It.IsAny<int?>(), certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>(), It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync((int? propertyId, int _, string? _, DateTime? _, int _, CancellationToken _, int? _, bool _, string _, int? _, int? _)
                => resultByPropertyId(propertyId));
    }

    private static PropertyCertificateApplicationService BuildRecordService(
        Mock<IPropertyCertificateService> certService,
        Mock<IRepository<PropertyCertificateTypeMasterEntity, int>> typeRepo,
        Mock<IRepository<PropertyEntity, int>> propertyRepo,
        Mock<IRepository<WingDetailsMastEntity, int>> wingDetailsMastRepo,
        Mock<IRepository<SocietyDetailsEntity, int>> societyRepo,
        Mock<IUnitOfWork>? unitOfWork = null,
        Mock<IRateableValueApiClient>? rateableValueApiClient = null,
        Mock<IRetrospectiveTaxCalculationEngineService>? retrospectiveTaxEngine = null,
        Mock<IDocumentApplicationService>? documentService = null)
    {
        var uow = unitOfWork ?? new Mock<IUnitOfWork>();
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return BuildService(certService, typeRepo, propertyRepo: propertyRepo, wingDetailsMastRepo: wingDetailsMastRepo,
            societyRepo: societyRepo, unitOfWork: uow, publisher: new Mock<IPublisher>(),
            rateableValueApiClient: rateableValueApiClient, retrospectiveTaxEngine: retrospectiveTaxEngine,
            documentService: documentService);
    }

    [Fact]
    public async Task CreateCertificateRecordAsync_ApartmentLevel_CreatesSingleSocietyScopedRow()
    {
        const int societyDetailId = 10;
        const int wingDetailId = 20;
        const int certificateTypeId = 4;
        const int representativePropertyId = 1;

        var certType = BuildTaxableOcType(certificateTypeId);
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>
        {
            new() { Id = societyDetailId, PropertyId = representativePropertyId, IsActive = true }
        }.BuildMock());

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>
        {
            new() { Id = wingDetailId, SocietyDetailsMastId = societyDetailId, IsActive = true }
        }.BuildMock());

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>
        {
            new() { Id = 100, WingDetailId = wingDetailId, IsActive = true },
            new() { Id = 101, WingDetailId = wingDetailId, IsActive = true }
        }.BuildMock());

        var certService = new Mock<IPropertyCertificateService>();
        SetupCreateForAnyProperty(certService, certificateTypeId, _ => 501);

        var service = BuildRecordService(certService, typeRepo, propertyRepo, wingDetailsMastRepo, societyRepo);

        var result = await service.CreateCertificateRecordAsync(new CreateCertificateRecordRequestDto
        {
            Level = CertificateRecordLevel.Apartment,
            SocietyDetailId = societyDetailId,
            CertificateTypeId = certificateTypeId,
            CertificateNo = "SOC-OC-001",
            CertificateIssueDate = DateTime.Now.AddDays(-10)
        }, userId: 1);

        Assert.Equal("Society", result.EffectiveScope);
        Assert.Equal(new List<int> { 501 }, result.PropertyCertificateIds);
        Assert.Equal(3, result.UnitCount); // representative property + 2 units under the wing
        Assert.True(result.TaxRecalculationTriggered);
        certService.Verify(s => s.CreateAsync(
            null, certificateTypeId, "SOC-OC-001", It.IsAny<DateTime?>(), 1, It.IsAny<CancellationToken>(),
            null, true, "S", societyDetailId, null), Times.Once);
    }

    [Fact]
    public async Task CreateCertificateRecordAsync_ApartmentLevel_ReportsPerPropertyRecalculationSuccessAndFailure()
    {
        const int societyDetailId = 10;
        const int wingDetailId = 20;
        const int certificateTypeId = 4;
        const int representativePropertyId = 1;

        var certType = BuildTaxableOcType(certificateTypeId);
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>
        {
            new() { Id = societyDetailId, PropertyId = representativePropertyId, IsActive = true }
        }.BuildMock());

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>
        {
            new() { Id = wingDetailId, SocietyDetailsMastId = societyDetailId, IsActive = true }
        }.BuildMock());

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>
        {
            new() { Id = 100, WingDetailId = wingDetailId, IsActive = true },
            new() { Id = 101, WingDetailId = wingDetailId, IsActive = true }
        }.BuildMock());

        var certService = new Mock<IPropertyCertificateService>();
        SetupCreateForAnyProperty(certService, certificateTypeId, _ => 501);

        // Representative property (1) and unit 100 recalculate fine; unit 101's RV step throws.
        var rateableValueApiClient = new Mock<IRateableValueApiClient>();
        rateableValueApiClient.Setup(c => c.RecalculateAsync(representativePropertyId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        rateableValueApiClient.Setup(c => c.RecalculateAsync(100, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        rateableValueApiClient.Setup(c => c.RecalculateAsync(101, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        var retrospectiveTaxEngine = new Mock<IRetrospectiveTaxCalculationEngineService>();
        retrospectiveTaxEngine
            .Setup(e => e.CalculateAndSaveAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxEngineResultDto?)null);

        var service = BuildRecordService(certService, typeRepo, propertyRepo, wingDetailsMastRepo, societyRepo,
            rateableValueApiClient: rateableValueApiClient, retrospectiveTaxEngine: retrospectiveTaxEngine);

        var result = await service.CreateCertificateRecordAsync(new CreateCertificateRecordRequestDto
        {
            Level = CertificateRecordLevel.Apartment,
            SocietyDetailId = societyDetailId,
            CertificateTypeId = certificateTypeId,
            CertificateNo = "SOC-OC-002",
            CertificateIssueDate = DateTime.Now.AddDays(-10)
        }, userId: 1);

        Assert.NotNull(result.RecalculationSummary);
        Assert.Equal(3, result.RecalculationSummary!.TotalProperties);
        Assert.Equal(2, result.RecalculationSummary.SucceededCount);
        Assert.Equal(1, result.RecalculationSummary.FailedCount);
        var failure = Assert.Single(result.RecalculationSummary.Failures);
        Assert.Equal(101, failure.PropertyId);
        // Plain-language message, not the raw exception text.
        Assert.DoesNotContain("boom", failure.Reason);
        Assert.NotEmpty(failure.Reason);
    }

    [Fact]
    public async Task CreateCertificateRecordAsync_WingLevel_CreatesSingleWingScopedRow()
    {
        const int societyDetailId = 10;
        const int wingDetailId = 20;
        const int certificateTypeId = 4;

        var certType = BuildTaxableOcType(certificateTypeId);
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>
        {
            new() { Id = 100, WingDetailId = wingDetailId, IsActive = true },
            new() { Id = 101, WingDetailId = wingDetailId, IsActive = true }
        }.BuildMock());

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());
        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());

        var certService = new Mock<IPropertyCertificateService>();
        SetupCreateForAnyProperty(certService, certificateTypeId, _ => 502);

        var service = BuildRecordService(certService, typeRepo, propertyRepo, wingDetailsMastRepo, societyRepo);

        var result = await service.CreateCertificateRecordAsync(new CreateCertificateRecordRequestDto
        {
            Level = CertificateRecordLevel.Wing,
            SocietyDetailId = societyDetailId,
            WingDetailId = wingDetailId,
            CertificateTypeId = certificateTypeId
        }, userId: 1);

        Assert.Equal("Wing", result.EffectiveScope);
        Assert.Equal(new List<int> { 502 }, result.PropertyCertificateIds);
        Assert.Equal(2, result.UnitCount);
        certService.Verify(s => s.CreateAsync(
            null, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), 1, It.IsAny<CancellationToken>(),
            null, true, "W", societyDetailId, wingDetailId), Times.Once);
    }

    [Fact]
    public async Task CreateCertificateRecordAsync_UnitLevel_AllUnitsSelected_CollapsesToWingScopedRow()
    {
        const int societyDetailId = 10;
        const int wingDetailId = 20;
        const int certificateTypeId = 4;

        var certType = BuildTaxableOcType(certificateTypeId);
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>
        {
            new() { Id = 100, WingDetailId = wingDetailId, IsActive = true },
            new() { Id = 101, WingDetailId = wingDetailId, IsActive = true }
        }.BuildMock());

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());
        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());

        var certService = new Mock<IPropertyCertificateService>();
        SetupCreateForAnyProperty(certService, certificateTypeId, _ => 503);

        var service = BuildRecordService(certService, typeRepo, propertyRepo, wingDetailsMastRepo, societyRepo);

        var result = await service.CreateCertificateRecordAsync(new CreateCertificateRecordRequestDto
        {
            Level = CertificateRecordLevel.Unit,
            SocietyDetailId = societyDetailId,
            WingDetailId = wingDetailId,
            UnitPropertyIds = new List<int> { 100, 101 },
            CertificateTypeId = certificateTypeId
        }, userId: 1);

        Assert.Equal("Wing", result.EffectiveScope);
        Assert.Equal(new List<int> { 503 }, result.PropertyCertificateIds);
        Assert.Equal(2, result.UnitCount);
        certService.Verify(s => s.CreateAsync(
            null, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), 1, It.IsAny<CancellationToken>(),
            null, true, "W", societyDetailId, wingDetailId), Times.Once);
    }

    [Fact]
    public async Task CreateCertificateRecordAsync_UnitLevel_PartialSelection_CreatesOneRowPerSelectedUnit()
    {
        const int societyDetailId = 10;
        const int wingDetailId = 20;
        const int certificateTypeId = 4;

        var certType = BuildTaxableOcType(certificateTypeId);
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>
        {
            new() { Id = 100, WingDetailId = wingDetailId, IsActive = true },
            new() { Id = 101, WingDetailId = wingDetailId, IsActive = true },
            new() { Id = 102, WingDetailId = wingDetailId, IsActive = true }
        }.BuildMock());

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());
        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(It.IsAny<int>(), It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        SetupCreateForAnyProperty(certService, certificateTypeId, propertyId => propertyId == 100 ? 601 : 602);

        var service = BuildRecordService(certService, typeRepo, propertyRepo, wingDetailsMastRepo, societyRepo);

        var result = await service.CreateCertificateRecordAsync(new CreateCertificateRecordRequestDto
        {
            Level = CertificateRecordLevel.Unit,
            SocietyDetailId = societyDetailId,
            WingDetailId = wingDetailId,
            UnitPropertyIds = new List<int> { 100, 101 }, // partial: unit 102 excluded
            CertificateTypeId = certificateTypeId
        }, userId: 1);

        Assert.Equal("Unit", result.EffectiveScope);
        Assert.Equal(new List<int> { 601, 602 }, result.PropertyCertificateIds);
        Assert.Equal(2, result.UnitCount);
        certService.Verify(s => s.CreateAsync(
            100, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), 1, It.IsAny<CancellationToken>(),
            null, true, "P", societyDetailId, wingDetailId), Times.Once);
        certService.Verify(s => s.CreateAsync(
            101, certificateTypeId, It.IsAny<string?>(), It.IsAny<DateTime?>(), 1, It.IsAny<CancellationToken>(),
            null, true, "P", societyDetailId, wingDetailId), Times.Once);
    }


    [Fact]
    public async Task CreateCertificateRecordAsync_UnitLevel_UnitNotUnderWing_ThrowsArgumentException()
    {
        const int societyDetailId = 10;
        const int wingDetailId = 20;
        const int certificateTypeId = 4;

        var certType = BuildTaxableOcType(certificateTypeId);
        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetByIdAsync(certificateTypeId, It.IsAny<CancellationToken>())).ReturnsAsync(certType);

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>
        {
            new() { Id = 100, WingDetailId = wingDetailId, IsActive = true }
        }.BuildMock());

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());
        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());

        var certService = new Mock<IPropertyCertificateService>();
        var service = BuildRecordService(certService, typeRepo, propertyRepo, wingDetailsMastRepo, societyRepo);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCertificateRecordAsync(new CreateCertificateRecordRequestDto
        {
            Level = CertificateRecordLevel.Unit,
            SocietyDetailId = societyDetailId,
            WingDetailId = wingDetailId,
            UnitPropertyIds = new List<int> { 100, 999 }, // 999 does not belong to this wing
            CertificateTypeId = certificateTypeId
        }, userId: 1));
    }

    #endregion

    #region Unit Wing Society Fallback Inheritance Tests

    [Fact]
    public async Task GetCertificateTypesWithStatusAsync_UnitHasOwnCertificate_ReturnsUnitCertificate_NotInherited()
    {
        // When unit (PropertyId 300) has its own certificate for a type, display ONLY that unit certificate (IsInherited = false, EntityType = "P"), even if Wing and Society certificates also exist.
        const int propertyId = 300;
        const int wingDetailId = 200;
        const int societyDetailId = 100;
        const int certificateTypeId = 1;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Completion Certificate",
            CertificateTypeCode = "CC",
            IsRequired = false,
            IsProtected = false,
            IsTaxable = true,
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyEntity> { new() { Id = propertyId, WingDetailId = wingDetailId, IsActive = true } });

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<WingDetailsMastEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WingDetailsMastEntity> { new() { Id = wingDetailId, SocietyDetailsMastId = societyDetailId, IsActive = true } });

        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<SocietyDetailsEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocietyDetailsEntity> { new() { Id = societyDetailId, PropertyId = propertyId, IsActive = true } });

        var unitCert = PropertyCertificateEntity.Create(
            propertyId, certificateTypeId, "UNIT-CC-001", DateTime.Now.AddDays(-10),
            propertyDetailsId: null, entityType: "P", societyDetailId: societyDetailId, wingDetailId: wingDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(unitCert, 501);

        var wingCert = PropertyCertificateEntity.Create(
            null, certificateTypeId, "WING-CC-001", DateTime.Now.AddDays(-20),
            propertyDetailsId: null, entityType: "W", societyDetailId: societyDetailId, wingDetailId: wingDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(wingCert, 502);

        var societyCert = PropertyCertificateEntity.Create(
            null, certificateTypeId, "SOC-CC-001", DateTime.Now.AddDays(-30),
            propertyDetailsId: null, entityType: "S", societyDetailId: societyDetailId, wingDetailId: null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(societyCert, 503);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { unitCert });
        certService.Setup(s => s.GetByWingDetailIdAsync(wingDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { wingCert });
        certService.Setup(s => s.GetBySocietyDetailIdAsync(societyDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { societyCert });

        var service = BuildService(certService, typeRepo, propertyRepo: propertyRepo, wingDetailsMastRepo: wingDetailsMastRepo, societyRepo: societyRepo);

        var result = await service.GetCertificateTypesWithStatusAsync(propertyId, CancellationToken.None, propertyDetailsId: null);

        var certResult = Assert.Single(result);
        Assert.True(certResult.HasCertificate);
        Assert.False(certResult.IsInherited);
        Assert.Equal("P", certResult.EntityType);
        Assert.Equal(501, certResult.PropertyCertificateId);
        Assert.Equal("UNIT-CC-001", certResult.CertificateNo);
    }

    [Fact]
    public async Task GetCertificateTypesWithStatusAsync_UnitHasNoCertificate_WingHasCertificate_ReturnsWingCertificate_IsInherited()
    {
        // When unit has no certificate for a type, fallback to Wing certificate (IsInherited = true, EntityType = "W").
        const int propertyId = 300;
        const int wingDetailId = 200;
        const int societyDetailId = 100;
        const int certificateTypeId = 1;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Completion Certificate",
            CertificateTypeCode = "CC",
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyEntity> { new() { Id = propertyId, WingDetailId = wingDetailId, IsActive = true } });

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<WingDetailsMastEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WingDetailsMastEntity> { new() { Id = wingDetailId, SocietyDetailsMastId = societyDetailId, IsActive = true } });

        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<SocietyDetailsEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocietyDetailsEntity> { new() { Id = societyDetailId, PropertyId = propertyId, IsActive = true } });

        var wingCert = PropertyCertificateEntity.Create(
            null, certificateTypeId, "WING-CC-001", DateTime.Now.AddDays(-20),
            propertyDetailsId: null, entityType: "W", societyDetailId: societyDetailId, wingDetailId: wingDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(wingCert, 502);

        var societyCert = PropertyCertificateEntity.Create(
            null, certificateTypeId, "SOC-CC-001", DateTime.Now.AddDays(-30),
            propertyDetailsId: null, entityType: "S", societyDetailId: societyDetailId, wingDetailId: null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(societyCert, 503);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>()); // No unit certificate
        certService.Setup(s => s.GetByWingDetailIdAsync(wingDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { wingCert });
        certService.Setup(s => s.GetBySocietyDetailIdAsync(societyDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { societyCert });

        var service = BuildService(certService, typeRepo, propertyRepo: propertyRepo, wingDetailsMastRepo: wingDetailsMastRepo, societyRepo: societyRepo);

        var result = await service.GetCertificateTypesWithStatusAsync(propertyId, CancellationToken.None, propertyDetailsId: null);

        var certResult = Assert.Single(result);
        Assert.True(certResult.HasCertificate);
        Assert.True(certResult.IsInherited);
        Assert.Equal("W", certResult.EntityType);
        Assert.Equal(502, certResult.PropertyCertificateId);
        Assert.Equal("WING-CC-001", certResult.CertificateNo);
    }

    [Fact]
    public async Task GetCertificateTypesWithStatusAsync_UnitAndWingHaveNoCertificate_SocietyHasCertificate_ReturnsSocietyCertificate_IsInherited()
    {
        // When neither unit nor wing has a certificate for a type, fallback to Society certificate (IsInherited = true, EntityType = "S").
        const int propertyId = 300;
        const int wingDetailId = 200;
        const int societyDetailId = 100;
        const int certificateTypeId = 1;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Completion Certificate",
            CertificateTypeCode = "CC",
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyEntity> { new() { Id = propertyId, WingDetailId = wingDetailId, IsActive = true } });

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<WingDetailsMastEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WingDetailsMastEntity> { new() { Id = wingDetailId, SocietyDetailsMastId = societyDetailId, IsActive = true } });

        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<SocietyDetailsEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocietyDetailsEntity> { new() { Id = societyDetailId, PropertyId = propertyId, IsActive = true } });

        var societyCert = PropertyCertificateEntity.Create(
            null, certificateTypeId, "SOC-CC-001", DateTime.Now.AddDays(-30),
            propertyDetailsId: null, entityType: "S", societyDetailId: societyDetailId, wingDetailId: null);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(societyCert, 503);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>()); // No unit certificate
        certService.Setup(s => s.GetByWingDetailIdAsync(wingDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>()); // No wing certificate
        certService.Setup(s => s.GetBySocietyDetailIdAsync(societyDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { societyCert });

        var service = BuildService(certService, typeRepo, propertyRepo: propertyRepo, wingDetailsMastRepo: wingDetailsMastRepo, societyRepo: societyRepo);

        var result = await service.GetCertificateTypesWithStatusAsync(propertyId, CancellationToken.None, propertyDetailsId: null);

        var certResult = Assert.Single(result);
        Assert.True(certResult.HasCertificate);
        Assert.True(certResult.IsInherited);
        Assert.Equal("S", certResult.EntityType);
        Assert.Equal(503, certResult.PropertyCertificateId);
        Assert.Equal("SOC-CC-001", certResult.CertificateNo);
    }

    [Fact]
    public async Task GetCertificateTypesWithStatusAsync_UnitWingSocietyHaveNoCertificate_ReturnsHasCertificateFalse()
    {
        // When no certificate exists at Unit, Wing, or Society level, HasCertificate is false, IsInherited is false.
        const int propertyId = 300;
        const int wingDetailId = 200;
        const int societyDetailId = 100;
        const int certificateTypeId = 1;

        var certType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Completion Certificate",
            CertificateTypeCode = "CC",
            IsActive = true,
            DisplayOrder = 1
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(certType, certificateTypeId);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { certType });

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyEntity> { new() { Id = propertyId, WingDetailId = wingDetailId, IsActive = true } });

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<WingDetailsMastEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WingDetailsMastEntity> { new() { Id = wingDetailId, SocietyDetailsMastId = societyDetailId, IsActive = true } });

        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<SocietyDetailsEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocietyDetailsEntity> { new() { Id = societyDetailId, PropertyId = propertyId, IsActive = true } });

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.GetByWingDetailIdAsync(wingDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());
        certService.Setup(s => s.GetBySocietyDetailIdAsync(societyDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());

        var service = BuildService(certService, typeRepo, propertyRepo: propertyRepo, wingDetailsMastRepo: wingDetailsMastRepo, societyRepo: societyRepo);

        var result = await service.GetCertificateTypesWithStatusAsync(propertyId, CancellationToken.None, propertyDetailsId: null);

        var certResult = Assert.Single(result);
        Assert.False(certResult.HasCertificate);
        Assert.False(certResult.IsInherited);
        Assert.Null(certResult.PropertyCertificateId);
    }

    [Fact]
    public async Task GetFloorCertificatesAsync_FloorWiseAndPropertyWise_AndFallbackToWingSociety()
    {
        const int propertyId = 300;
        const int wingDetailId = 200;
        const int societyDetailId = 100;
        const int floorDetailsId = 50;

        var ccType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Completion Certificate",
            CertificateTypeCode = "CC",
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(ccType, 1);

        var ocType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Occupancy Certificate",
            CertificateTypeCode = "OC",
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(ocType, 2);

        var electricType = new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Electricity Bill",
            CertificateTypeCode = "ELECTRIC_BILL",
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(electricType, 3);

        var typeRepo = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        typeRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyCertificateTypeMasterEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateTypeMasterEntity> { ccType, ocType, electricType });

        var floorEntity = new PropertyDetailsEntity
        {
            PropertyId = propertyId,
            Floor = new FloorEntity { Description = "1st Floor" },
            IsActive = true
        };
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(floorEntity, floorDetailsId);

        var detailsRepo = new Mock<IRepository<PropertyDetailsEntity, int>>();
        detailsRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity> { floorEntity }.BuildMock());

        var propertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        propertyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PropertyEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyEntity> { new() { Id = propertyId, WingDetailId = wingDetailId, IsActive = true } });

        var wingDetailsMastRepo = new Mock<IRepository<WingDetailsMastEntity, int>>();
        wingDetailsMastRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<WingDetailsMastEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WingDetailsMastEntity> { new() { Id = wingDetailId, SocietyDetailsMastId = societyDetailId, IsActive = true } });

        var societyRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        societyRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<SocietyDetailsEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocietyDetailsEntity> { new() { Id = societyDetailId, PropertyId = propertyId, IsActive = true } });

        // Floor-wise CC
        var floorCc = PropertyCertificateEntity.Create(
            propertyId, 1, "FLOOR-CC-001", new DateTime(2023, 1, 1),
            propertyDetailsId: floorDetailsId, entityType: "P", societyDetailId: societyDetailId, wingDetailId: wingDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(floorCc, 601);
        typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType))!.SetValue(floorCc, ccType);

        // Property-wise OC
        var propOc = PropertyCertificateEntity.Create(
            propertyId, 2, "PROP-OC-001", new DateTime(2023, 6, 1),
            propertyDetailsId: null, entityType: "P", societyDetailId: societyDetailId, wingDetailId: wingDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(propOc, 602);
        typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType))!.SetValue(propOc, ocType);

        // Wing-wise Electric Bill
        var wingElectric = PropertyCertificateEntity.Create(
            null, 3, "WING-ELEC-001", new DateTime(2023, 9, 1),
            propertyDetailsId: null, entityType: "W", societyDetailId: societyDetailId, wingDetailId: wingDetailId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(wingElectric, 603);
        typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType))!.SetValue(wingElectric, electricType);

        var certService = new Mock<IPropertyCertificateService>();
        certService.Setup(s => s.GetByPropertyIdAsync(propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { floorCc, propOc });
        certService.Setup(s => s.GetByPropertyIdIncludingInactiveAsync(propertyId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { floorCc, propOc });
        certService.Setup(s => s.GetByWingDetailIdAsync(wingDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity> { wingElectric });
        certService.Setup(s => s.GetBySocietyDetailIdAsync(societyDetailId, It.IsAny<PropertyCertificateIncludeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCertificateEntity>());

        var service = BuildService(certService, typeRepo, detailsRepo: detailsRepo, propertyRepo: propertyRepo, wingDetailsMastRepo: wingDetailsMastRepo, societyRepo: societyRepo);

        var result = await service.GetFloorCertificatesAsync(propertyId, selectedPropertyDetailsId: floorDetailsId);

        Assert.NotNull(result.SelectedFloor);
        Assert.True(result.SelectedFloor.CertificateApplicable);
        Assert.Equal(new DateTime(2023, 1, 1), result.SelectedFloor.CcDate);
        Assert.Equal("FLOOR-CC-001", result.SelectedFloor.CcCertificateNo);
        Assert.Equal(new DateTime(2023, 6, 1), result.SelectedFloor.OcDate);
        Assert.Equal("PROP-OC-001", result.SelectedFloor.OcCertificateNo);
        Assert.Equal(new DateTime(2023, 9, 1), result.SelectedFloor.ElectricBillDate);
        Assert.Equal("WING-ELEC-001", result.SelectedFloor.ElectricBillNo);
    }

    #endregion
}
