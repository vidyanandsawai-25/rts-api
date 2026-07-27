using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Tests.Helpers;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Critical regression tests for PropertyCertificateService bug fixes:
/// 1. ToggleEnabledAsync can re-enable disabled certificates
/// 2. UpdateAsync works on disabled certificates
/// 3. DeleteAsync works on disabled certificates
/// </summary>
public class PropertyCertificateServiceCriticalFixTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private Mock<IUnitOfWork> CreateMockUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return mock;
    }

    private static ICertificateTaxGuidelineReaderService CreateGuidelineReader()
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
                MinimumBackdateFinancialYear: 0,
                EnableCurrentYearProration: true, ProrationMethod: "DAILY", CurrentYearProrationStartRule: "EXACT_DATE",
                TaxPersistenceMode: "PROPERTY_AGGREGATED",
                SaveInPolicyTaxDetails: true, SaveInTransMast: true, DoNotUpdateNettax: true,
                RecalculateOnSave: true, RecalculateOnDelete: true, GuidelineChangeApplyMode: "NEXT_CALCULATION",
                CcPartialPolicyCode: "PARTIAL_CC", CcFullPolicyCode: "CC",
                OcPartialPolicyCode: "PARTIAL_OC", OcFullPolicyCode: "OC",
                ElectricBillPartialPolicyCode: "PARTIAL_ELECTRIC_BILL", ElectricBillFullPolicyCode: "ELECTRIC_BILL",
                CertificateTaxScopeMode: "FLOOR_WISE", AllowFloorWiseCertificateMetadata: true, EnableCcToOcSplit: true,
                ElectricBillCertificateCodes: "ELECTRIC_BILL", RetrospectiveCurrentYearCount: 1,
                RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT", FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY"));
        return mock.Object;
    }

    #region Critical Fix: ToggleEnabledAsync can re-enable disabled certificates

    [Fact]
    public async Task ToggleEnabledAsync_CanReEnableDisabledCertificate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);

        await context.SaveChangesAsync();

        // Create a disabled certificate (IsActive = false)
        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.Disable(); // Set IsActive to false
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;

        // Act: Re-enable the disabled certificate
        await service.ToggleEnabledAsync(certId, true, 2, CancellationToken.None);

        // Assert: Certificate should now be enabled
        var updated = await context.PropertyCertificates.FindAsync(certId);
        updated.Should().NotBeNull();
        updated!.IsActive.Should().BeTrue("certificate should be re-enabled");
        updated.UpdatedBy.Should().Be(2);
    }

    [Fact]
    public async Task ToggleEnabledAsync_DisableThenEnable_Works()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;

        // Act: Disable then enable
        await service.ToggleEnabledAsync(certId, false, 2, CancellationToken.None);
        await service.ToggleEnabledAsync(certId, true, 2, CancellationToken.None);

        // Assert: Should be enabled at the end
        var result = await context.PropertyCertificates.FindAsync(certId);
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue("certificate should be enabled after disable-enable cycle");
    }

    [Fact]
    public async Task ToggleEnabledAsync_ThrowsException_WhenCertificateMarkedForDeletion()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.MarkForDeletion();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(
            () => service.ToggleEnabledAsync(certificate.Id, true, 2, CancellationToken.None));
    }

    #endregion

    #region Critical Fix: UpdateAsync works on disabled certificates

    [Fact]
    public async Task UpdateAsync_CanUpdateDisabledCertificate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        // Create a disabled certificate
        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.Disable();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;
        var newCertNo = "CERT-002";
        var newDate = DateTime.Now.AddDays(-1); // Use past date to avoid validation error

        // Act: Update the disabled certificate
        await service.UpdateAsync(certId, newCertNo, newDate, 2, CancellationToken.None);

        // Assert: Certificate should be updated
        var updated = await context.PropertyCertificates.FindAsync(certId);
        updated.Should().NotBeNull();
        updated!.CertificateNo.Should().Be(newCertNo);
        updated.IssueDate.Should().Be(newDate);
        updated.UpdatedBy.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsException_WhenCertificateMarkedForDeletion()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.MarkForDeletion();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(
            () => service.UpdateAsync(certificate.Id, "NEW", DateTime.Now, 2, CancellationToken.None));
    }

    #endregion

    #region Critical Fix: DeleteAsync works on disabled certificates

    [Fact]
    public async Task DeleteAsync_CanDeleteDisabledCertificate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        // Create a disabled certificate
        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.Disable();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;

        // Act: Delete the disabled certificate
        await service.DeleteAsync(certId, 2, CancellationToken.None);

        // Assert: Certificate should be marked for deletion
        var deleted = await context.PropertyCertificates.FindAsync(certId);
        deleted.Should().NotBeNull();
        deleted!.MarkedForDeletion.Should().BeTrue();
        deleted.UpdatedBy.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenCertificateAlreadyMarkedForDeletion()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.MarkForDeletion();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        // Act & Assert: Should throw exception because already marked for deletion
        await Assert.ThrowsAsync<PropertyCertificateNotFoundException>(
            () => service.DeleteAsync(certificate.Id, 2, CancellationToken.None));
    }

    #endregion

    #region Integration: BulkSave scenario

    [Fact]
    public async Task BulkSave_ReenableExistingInactiveCertificate_Succeeds()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);

        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        // Create an inactive certificate
        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.Disable();
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        var certId = certificate.Id;

        // Act: Simulate bulk save re-enabling the certificate
        await service.UpdateAsync(certId, "CERT-002", DateTime.Now.AddDays(-1), 2, CancellationToken.None);
        await service.ToggleEnabledAsync(certId, true, 2, CancellationToken.None);

        // Assert: Certificate should be updated and enabled
        var result = await context.PropertyCertificates.FindAsync(certId);
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
        result.CertificateNo.Should().Be("CERT-002");
    }

    #endregion

    #region PropertyCertificateChangedEvent published when the certificate type is IsTaxable

    [Fact]
    public async Task CreateAsync_PublishesPropertyCertificateChangedEvent_WhenCertificateTypeIsTaxable()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var publisher = new Mock<IPublisher>();
        var service = new PropertyCertificateService(context, unitOfWork.Object, publisher.Object, CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        certType.IsTaxable = true;
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        await service.CreateAsync(1, 1, "CERT-001", DateTime.Now, createdBy: 5, CancellationToken.None);

        publisher.Verify(p => p.Publish(
            It.Is<NtisPlatform.Application.Events.PropertyCertificateChangedEvent>(e => e.PropertyId == 1 && e.UserId == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DoesNotPublish_WhenCertificateTypeIsNotTaxable()
    {
        // IsTaxable is separate from IsProtected -- a non-taxable type (e.g. "Possession
        // Certificate") must never re-trigger Occupation Tax recalculation.
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var publisher = new Mock<IPublisher>();
        var service = new PropertyCertificateService(context, unitOfWork.Object, publisher.Object, CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        certType.IsTaxable = false;
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        await service.CreateAsync(1, 1, "CERT-001", DateTime.Now, createdBy: 5, CancellationToken.None);

        publisher.Verify(p => p.Publish(
            It.IsAny<NtisPlatform.Application.Events.PropertyCertificateChangedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_PublishesPropertyCertificateChangedEvent_WhenCertificateTypeIsTaxable()
    {
        // The trigger must fire regardless of the certificate type's IsProtected flag (that flag
        // only gates whether the TYPE master row can be deleted) -- editing an EXISTING taxable
        // certificate's date/number must always re-run RV refresh + Occupation Tax.
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var publisher = new Mock<IPublisher>();
        var service = new PropertyCertificateService(context, unitOfWork.Object, publisher.Object, CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        certType.IsProtected = false;
        certType.IsTaxable = true;
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now.AddYears(-1));
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate); // seeded directly, not via the service, so no publish happened yet
        await context.SaveChangesAsync();

        await service.UpdateAsync(certificate.Id, "CERT-001", DateTime.Now.AddYears(-2), updatedBy: 7, CancellationToken.None);

        publisher.Verify(p => p.Publish(
            It.Is<NtisPlatform.Application.Events.PropertyCertificateChangedEvent>(e => e.PropertyId == 1 && e.UserId == 7),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleEnabledAsync_PublishesPropertyCertificateChangedEvent_WhenCertificateTypeIsTaxable()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var publisher = new Mock<IPublisher>();
        var service = new PropertyCertificateService(context, unitOfWork.Object, publisher.Object, CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        certType.IsTaxable = true;
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        await service.ToggleEnabledAsync(certificate.Id, false, updatedBy: 9, CancellationToken.None);

        publisher.Verify(p => p.Publish(
            It.Is<NtisPlatform.Application.Events.PropertyCertificateChangedEvent>(e => e.PropertyId == 1 && e.UserId == 9),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PublishesPropertyCertificateChangedEvent_WhenCertificateTypeIsTaxable()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var publisher = new Mock<IPublisher>();
        var service = new PropertyCertificateService(context, unitOfWork.Object, publisher.Object, CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        certType.IsTaxable = true;
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        await service.DeleteAsync(certificate.Id, deletedBy: 11, CancellationToken.None);

        publisher.Verify(p => p.Publish(
            It.Is<NtisPlatform.Application.Events.PropertyCertificateChangedEvent>(e => e.PropertyId == 1 && e.UserId == 11),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotPublish_WhenCertificateTypeIsNotTaxable()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var publisher = new Mock<IPublisher>();
        var service = new PropertyCertificateService(context, unitOfWork.Object, publisher.Object, CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        certType.IsTaxable = false;
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        await service.DeleteAsync(certificate.Id, deletedBy: 11, CancellationToken.None);

        publisher.Verify(p => p.Publish(
            It.IsAny<NtisPlatform.Application.Events.PropertyCertificateChangedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CertificateTaxGuideline RECALCULATE_ON_CERTIFICATE_SAVE/_DELETE gating

    private static Mock<ICertificateTaxGuidelineReaderService> CreateGuidelineReaderMock(bool recalculateOnSave, bool recalculateOnDelete)
    {
        var mock = new Mock<ICertificateTaxGuidelineReaderService>();
        mock.Setup(s => s.GetActiveSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateTaxGuidelineSettings(
                EnableCertificateBasedTax: true, ApplyOnlyTaxableCertTypes: true,
                DatePriority1: "CC", DatePriority2: "OC", DatePriority3: "ELECTRIC_BILL", DatePriority4: "RETROSPECTIVE",
                CertificateRequireNoAndDate: false, MissingCertificateNoAction: "IGNORE_FOR_TAX", MissingCertificateDateAction: "IGNORE_FOR_TAX",
                IgnoreCcToOcWithinValue: 6, IgnoreCcToOcWithinType: "MONTHS",
                CcOcGapComparison: "LESS_THAN_OR_EQUAL", CcOcGapWithinAction: "APPLY_OC_ONLY", CcOcGapExceededAction: "APPLY_CC_THEN_OC",
                InvalidCcOcDateOrderAction: "USE_PRIORITY_AND_LOG", CcOnlyAction: "APPLY_FROM_CC_DATE", OcOnlyAction: "APPLY_FROM_OC_DATE",
                FinancialYearStartMonth: 4, FinancialYearStartDay: 1,
                CCPeriodMultiplier: 1.0m, OCPeriodMultiplier: 1.0m,
                ElectricBillDateRule: "FROM_FY_START", ElectricBillAddMonths: 0, ElectricBillMultiplier: 1.0m,
                ElectricBillMinimumFinancialYear: 2016, EnableRetrospectiveTax: true,
                NoDateRule: "DEFAULT_RETROSPECTIVE", LookbackYears: 6, DefaultRetrospectiveMultiplier: 1.0m,
                MinimumBackdateFinancialYear: 0,
                EnableCurrentYearProration: true, ProrationMethod: "DAILY", CurrentYearProrationStartRule: "EXACT_DATE",
                TaxPersistenceMode: "PROPERTY_AGGREGATED",
                SaveInPolicyTaxDetails: true, SaveInTransMast: true, DoNotUpdateNettax: true,
                RecalculateOnSave: recalculateOnSave, RecalculateOnDelete: recalculateOnDelete, GuidelineChangeApplyMode: "NEXT_CALCULATION",
                CcPartialPolicyCode: "PARTIAL_CC", CcFullPolicyCode: "CC",
                OcPartialPolicyCode: "PARTIAL_OC", OcFullPolicyCode: "OC",
                ElectricBillPartialPolicyCode: "PARTIAL_ELECTRIC_BILL", ElectricBillFullPolicyCode: "ELECTRIC_BILL",
                CertificateTaxScopeMode: "FLOOR_WISE", AllowFloorWiseCertificateMetadata: true, EnableCcToOcSplit: true,
                ElectricBillCertificateCodes: "ELECTRIC_BILL", RetrospectiveCurrentYearCount: 1,
                RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT", FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY"));
        return mock;
    }

    [Fact]
    public async Task CreateAsync_DoesNotPublish_WhenRecalculateOnSaveIsDisabled()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var publisher = new Mock<IPublisher>();
        var guidelineReader = CreateGuidelineReaderMock(recalculateOnSave: false, recalculateOnDelete: true);
        var service = new PropertyCertificateService(context, unitOfWork.Object, publisher.Object, guidelineReader.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        certType.IsTaxable = true;
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        await service.CreateAsync(1, 1, "CERT-001", DateTime.Now, createdBy: 1, CancellationToken.None);

        publisher.Verify(p => p.Publish(
            It.IsAny<NtisPlatform.Application.Events.PropertyCertificateChangedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotPublish_WhenRecalculateOnDeleteIsDisabled()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var publisher = new Mock<IPublisher>();
        var guidelineReader = CreateGuidelineReaderMock(recalculateOnSave: true, recalculateOnDelete: false);
        var service = new PropertyCertificateService(context, unitOfWork.Object, publisher.Object, guidelineReader.Object);

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        certType.IsTaxable = true;
        context.PropertyCertificateTypeMasters.Add(certType);
        await context.SaveChangesAsync();

        var certificate = PropertyCertificateEntity.Create(1, 1, "CERT-001", DateTime.Now);
        certificate.CreatedBy = 1;
        certificate.CreatedDate = DateTime.Now;
        context.PropertyCertificates.Add(certificate);
        await context.SaveChangesAsync();

        await service.DeleteAsync(certificate.Id, deletedBy: 11, CancellationToken.None);

        publisher.Verify(p => p.Publish(
            It.IsAny<NtisPlatform.Application.Events.PropertyCertificateChangedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Floor validation: CreateAsync must reject inactive/deleted PropertyDetails rows

    [Fact]
    public async Task CreateAsync_ActiveValidFloor_Succeeds()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        context.PropertyDetails.Add(new PropertyDetailsEntity { Id = 501, PropertyId = 1, IsActive = true, MarkedForDeletion = false });
        await context.SaveChangesAsync();

        var certId = await service.CreateAsync(1, 1, "CERT-001", DateTime.Now, createdBy: 1, CancellationToken.None, propertyDetailsId: 501);

        var created = await context.PropertyCertificates.FindAsync(certId);
        created.Should().NotBeNull();
        created!.PropertyDetailsId.Should().Be(501);
    }

    [Fact]
    public async Task CreateAsync_InactiveFloor_ThrowsArgumentException()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        context.PropertyDetails.Add(new PropertyDetailsEntity { Id = 502, PropertyId = 1, IsActive = false, MarkedForDeletion = false });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(1, 1, "CERT-001", DateTime.Now, createdBy: 1, CancellationToken.None, propertyDetailsId: 502));
    }

    [Fact]
    public async Task CreateAsync_MarkedForDeletionFloor_ThrowsArgumentException()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        context.PropertyDetails.Add(new PropertyDetailsEntity { Id = 503, PropertyId = 1, IsActive = true, MarkedForDeletion = true });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(1, 1, "CERT-001", DateTime.Now, createdBy: 1, CancellationToken.None, propertyDetailsId: 503));
    }

    [Fact]
    public async Task CreateWithDocumentAsync_InactiveFloor_ThrowsArgumentException()
    {
        await using var context = CreateInMemoryContext();
        var unitOfWork = CreateMockUnitOfWork();
        var service = new PropertyCertificateService(context, unitOfWork.Object, Mock.Of<IPublisher>(), CreateGuidelineReader());

        var property = EntityTestHelpers.CreatePropertyEntity(1);
        context.PropertyMast.Add(property);
        var certType = EntityTestHelpers.CreatePropertyCertificateTypeMasterEntity(1);
        context.PropertyCertificateTypeMasters.Add(certType);
        context.PropertyDetails.Add(new PropertyDetailsEntity { Id = 504, PropertyId = 1, IsActive = false, MarkedForDeletion = false });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateWithDocumentAsync(
                propertyId: 1, certificateTypeId: 1, documentBindingId: 1, certificateNo: "CERT-001",
                issueDate: DateTime.Now, createdBy: 1, CancellationToken.None, propertyDetailsId: 504));
    }

    #endregion
}
