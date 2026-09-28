using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure;

public class ApartmentQcCertificateGridRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PropertyEntity CreateUnit(int id, int wardId, string propertyNo, string? partitionNo, string? flatOrShopNo, int? wingDetailId = null)
        => new()
        {
            Id = id,
            WardId = wardId,
            TaxZoneId = 1,
            PropertyNo = propertyNo,
            PartitionNo = partitionNo,
            FlatOrShopNo = flatOrShopNo,
            WingDetailId = wingDetailId,
            IsActive = true,
            MarkedForDeletion = false
        };

    private static PropertyCertificateTypeMasterEntity CreateType(int id, string code, string name, int displayOrder = 1)
        => new()
        {
            Id = id,
            CertificateTypeCode = code,
            CertificateTypeName = name,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

    [Fact]
    public async Task GetGridAsync_NoUnits_ReturnsNull()
    {
        using var context = CreateContext();
        var repository = new ApartmentQcCertificateGridRepository(context);

        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetGridAsync_SocietyLevelCertificate_ReturnsApartmentRow()
    {
        using var context = CreateContext();
        context.PropertyMast.AddRange(
            CreateUnit(1, 77, "1", null, "101", wingDetailId: 10),
            CreateUnit(2, 77, "1", "1-A", "102", wingDetailId: 20));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.AddRange(
            new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now },
            new WingDetailsMastEntity { Id = 20, SocietyDetailsMastId = 5, WingName = "B Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(1, "OC", "Occupancy Certificate"));

        var cert = PropertyCertificateEntity.Create(
            propertyId: null, certificateTypeId: 1, certificateNo: "PMC/OC/2010/1234", issueDate: new DateTime(2010, 3, 15),
            entityType: "S", societyDetailId: 5, wingDetailId: null);
        context.PropertyCertificates.Add(cert);
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.NotNull(result);
        Assert.Equal(2, result!.WingCount);
        Assert.Equal(2, result.UnitCount);
        var row = Assert.Single(result.SocietyCertificates);
        Assert.Equal("Apartment", row.Level);
        Assert.Equal("Entire Apartment", row.ApplicableToLabel);
        Assert.Equal("2 wings · 2 units", row.ApplicableToSubLabel);
        Assert.Equal("OC", row.CertificateTypeCode);
        Assert.Equal("PMC/OC/2010/1234", row.CertificateNumber);
        Assert.Equal("Active", row.Status);
        Assert.Equal(5, row.SocietyDetailId);
        Assert.Null(row.WingDetailId);
        Assert.Null(row.PropertyId);
    }

    [Fact]
    public async Task GetGridAsync_WingLevelCertificate_OnlyForWingWithRecord()
    {
        using var context = CreateContext();
        context.PropertyMast.AddRange(
            CreateUnit(1, 77, "1", null, "101", wingDetailId: 10),
            CreateUnit(2, 77, "1", "1-A", "102", wingDetailId: 10),
            CreateUnit(3, 77, "1", "1-B", "201", wingDetailId: 20));
        context.WingDetailsMast.AddRange(
            new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now },
            new WingDetailsMastEntity { Id = 20, SocietyDetailsMastId = 5, WingName = "B Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(2, "FN", "Fire NOC"));

        var cert = PropertyCertificateEntity.Create(
            propertyId: null, certificateTypeId: 2, certificateNo: "PCMC/FNOC/2023/567", issueDate: new DateTime(2023, 6, 20),
            entityType: "W", societyDetailId: 5, wingDetailId: 10);
        context.PropertyCertificates.Add(cert);
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.NotNull(result);
        var row = Assert.Single(result!.WingCertificates);
        Assert.Equal("Wing", row.Level);
        Assert.Equal("A Wing", row.ApplicableToLabel);
        Assert.Equal("2 units in this wing", row.ApplicableToSubLabel);
        Assert.Equal(5, row.SocietyDetailId);
        Assert.Equal(10, row.WingDetailId);
        Assert.Null(row.PropertyId);
    }

    [Fact]
    public async Task GetGridAsync_UnitLevelCertificates_AggregatesCoverageAndMissing()
    {
        using var context = CreateContext();
        context.PropertyMast.AddRange(
            CreateUnit(1, 77, "1", null, "101"),
            CreateUnit(2, 77, "1", "1-A", "102"),
            CreateUnit(3, 77, "1", "1-B", "103"));
        context.PropertyCertificateTypeMasters.Add(CreateType(3, "EB", "Electricity Bill"));

        context.PropertyCertificates.AddRange(
            PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: 3, certificateNo: "MSEB/2024/B-101", issueDate: new DateTime(2024, 7, 1)),
            PropertyCertificateEntity.Create(propertyId: 2, certificateTypeId: 3, certificateNo: "MSEB/2024/B-102", issueDate: new DateTime(2024, 7, 2)));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.NotNull(result);
        var row = Assert.Single(result!.UnitCertificates);
        Assert.Equal("Unit", row.Level);
        Assert.Equal(2, row.UnitsCoveredCount);
        Assert.Equal(3, row.UnitsTotalCount);
        Assert.Equal(1, row.UnitsMissingCount);
        Assert.Contains("101", row.CoveredUnitNumbers);
        Assert.Contains("102", row.CoveredUnitNumbers);
        Assert.DoesNotContain("103", row.CoveredUnitNumbers);
        Assert.Contains(row.PropertyId, new int?[] { 1, 2 });
    }

    [Fact]
    public async Task GetGridAsync_SocietyAndWingBothExist_ReturnsBothSeparateLists()
    {
        // A certificate uploaded at Society scope applies to every wing, but a wing with its own
        // override for the same type must ALSO show up -- both are independent facts, neither
        // suppresses the other.
        using var context = CreateContext();
        context.PropertyMast.AddRange(
            CreateUnit(1, 77, "1", null, "101", wingDetailId: 10),
            CreateUnit(2, 77, "1", "1-A", "102", wingDetailId: 10));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(4, "SS", "Structural Stability Certificate"));

        context.PropertyCertificates.AddRange(
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 4, certificateNo: "PWD/SSC/2022/089", issueDate: new DateTime(2022, 1, 10),
                entityType: "S", societyDetailId: 5, wingDetailId: null),
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 4, certificateNo: "STRAY-WING-CERT", issueDate: new DateTime(2023, 1, 1),
                entityType: "W", societyDetailId: 5, wingDetailId: 10));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.NotNull(result);
        var societyRow = Assert.Single(result!.SocietyCertificates);
        Assert.Equal("Apartment", societyRow.Level);
        Assert.Equal("PWD/SSC/2022/089", societyRow.CertificateNumber);
        var wingRow = Assert.Single(result.WingCertificates);
        Assert.Equal("Wing", wingRow.Level);
        Assert.Equal("STRAY-WING-CERT", wingRow.CertificateNumber);
    }

    [Fact]
    public async Task GetGridAsync_SomeWingsOverridden_OtherWingsShowInheritedSocietyCertificate()
    {
        // Reproduces the reported requirement directly: a society has 4 wings; one has its own
        // override certificate; the other three must EACH still show up in WingCertificates,
        // carrying the Society certificate but tagged with their own WingDetailId -- not just
        // implied by the single SocietyCertificates row.
        using var context = CreateContext();
        context.PropertyMast.AddRange(
            CreateUnit(1, 77, "1", null, "101", wingDetailId: 10),
            CreateUnit(2, 77, "1", "1-A", "102", wingDetailId: 20),
            CreateUnit(3, 77, "1", "1-B", "103", wingDetailId: 30),
            CreateUnit(4, 77, "1", "1-C", "104", wingDetailId: 40));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.AddRange(
            new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A", IsActive = true, CreatedDate = DateTime.Now },
            new WingDetailsMastEntity { Id = 20, SocietyDetailsMastId = 5, WingName = "B", IsActive = true, CreatedDate = DateTime.Now },
            new WingDetailsMastEntity { Id = 30, SocietyDetailsMastId = 5, WingName = "C", IsActive = true, CreatedDate = DateTime.Now },
            new WingDetailsMastEntity { Id = 40, SocietyDetailsMastId = 5, WingName = "D", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(1, "CC", "Completion Certificate"));
        context.PropertyCertificates.AddRange(
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 1, certificateNo: "Bharat420", issueDate: new DateTime(2026, 9, 1),
                entityType: "S", societyDetailId: 5, wingDetailId: null),
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 1, certificateNo: "AshwinBagmare", issueDate: new DateTime(2026, 9, 1),
                entityType: "W", societyDetailId: 5, wingDetailId: 10));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.NotNull(result);
        Assert.Equal(4, result!.WingCertificates.Count);
        Assert.Contains(result.WingCertificates, r => r.WingDetailId == 10 && r.CertificateNumber == "AshwinBagmare");
        Assert.Contains(result.WingCertificates, r => r.WingDetailId == 20 && r.CertificateNumber == "Bharat420");
        Assert.Contains(result.WingCertificates, r => r.WingDetailId == 30 && r.CertificateNumber == "Bharat420");
        Assert.Contains(result.WingCertificates, r => r.WingDetailId == 40 && r.CertificateNumber == "Bharat420");
    }

    [Fact]
    public async Task GetGridAsync_TypeWithNoRecords_OmittedFromGrid()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(CreateUnit(1, 77, "1", null, "101"));
        context.PropertyCertificateTypeMasters.AddRange(
            CreateType(1, "OC", "Occupancy Certificate"),
            CreateType(2, "BP", "Building Permission"));
        context.PropertyCertificates.Add(
            PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: 2, certificateNo: "PMC/BP/2005/0432", issueDate: new DateTime(2005, 5, 4)));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.NotNull(result);
        var row = Assert.Single(result!.UnitCertificates);
        Assert.Equal("BP", row.CertificateTypeCode);
    }

    [Fact]
    public async Task GetGridAsync_MissingNumberOrDate_ResolvesToPendingStatus()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(CreateUnit(1, 77, "1", null, "101"));
        context.PropertyCertificateTypeMasters.Add(CreateType(3, "EB", "Electricity Bill"));
        context.PropertyCertificates.Add(
            PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: 3));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.NotNull(result);
        var row = Assert.Single(result!.UnitCertificates);
        Assert.Equal("Pending", row.Status);
    }

    [Fact]
    public async Task GetGridAsync_HasDocument_ReflectsDocumentBindingPresence()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(CreateUnit(1, 77, "1", null, "101"));
        context.PropertyCertificateTypeMasters.Add(CreateType(1, "OC", "Occupancy Certificate"));
        var document = new DocumentEntity { Id = 1, DocumentGuid = Guid.NewGuid(), IsActive = true };
        context.Documents.Add(document);
        var binding = DocumentBindingEntity.CreateWithIntReference(
            documentId: 1, departmentId: 1, moduleId: 1,
            referenceTableName: "PropertyCertificate", referenceTableId: 1, referencePropertyName: "DocumentBindingId");
        binding.Id = 1;
        context.DocumentBindings.Add(binding);
        context.PropertyCertificates.Add(
            PropertyCertificateEntity.CreateWithDocument(propertyId: 1, certificateTypeId: 1, documentBindingId: 1,
                certificateNo: "PMC/OC/2010/1234", issueDate: new DateTime(2010, 3, 15)));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridAsync(wardId: 77, propertyNo: "1");

        Assert.NotNull(result);
        var row = Assert.Single(result!.UnitCertificates);
        Assert.True(row.HasDocument);
        Assert.Equal(document.DocumentGuid, row.DocumentGuid);
    }

    [Fact]
    public async Task GetGridForWingAsync_NoUnits_ReturnsNull()
    {
        using var context = CreateContext();
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now });
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForWingAsync(wingDetailsId: 10);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetGridForWingAsync_UnitLevelCertificate_OnlyAggregatesThisWingsUnits()
    {
        using var context = CreateContext();
        context.PropertyMast.AddRange(
            CreateUnit(1, 77, "1", null, "101", wingDetailId: 10),
            CreateUnit(2, 77, "1", "1-A", "102", wingDetailId: 10),
            CreateUnit(3, 77, "1", "1-B", "201", wingDetailId: 20));
        context.WingDetailsMast.AddRange(
            new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now },
            new WingDetailsMastEntity { Id = 20, SocietyDetailsMastId = 5, WingName = "B Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(3, "EB", "Electricity Bill"));
        context.PropertyCertificates.AddRange(
            PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: 3, certificateNo: "MSEB/2024/B-101", issueDate: new DateTime(2024, 7, 1)),
            PropertyCertificateEntity.Create(propertyId: 3, certificateTypeId: 3, certificateNo: "MSEB/2024/B-201", issueDate: new DateTime(2024, 7, 2)));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForWingAsync(wingDetailsId: 10);

        Assert.NotNull(result);
        Assert.Equal(2, result!.UnitCount);
        Assert.Empty(result.SocietyCertificates!);
        Assert.Empty(result.WingCertificates!);
        var row = Assert.Single(result.UnitCertificates!);
        Assert.Equal("Unit", row.Level);
        Assert.Equal(1, row.UnitsCoveredCount);
        Assert.Equal(2, row.UnitsTotalCount);
        Assert.Contains("101", row.CoveredUnitNumbers);
        Assert.DoesNotContain("201", row.CoveredUnitNumbers);
    }

    [Fact]
    public async Task GetGridForWingAsync_SocietyLevelCertificate_AppliesToThisWing()
    {
        // Reproduces the reported requirement: a certificate uploaded at Society scope applies to
        // every wing under it. Querying this wing (which has no Wing-level record of its own) must
        // show it both as its own SocietyCertificates entry (apartment-wide counts) AND as an
        // inherited WingCertificates entry tagged with this wing's own id/unit count -- so a caller
        // asking "what applies to this wing" sees it explicitly, not just implied by the Society row.
        using var context = CreateContext();
        context.PropertyMast.Add(CreateUnit(1, 77, "1", null, "101", wingDetailId: 10));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(1, "OC", "Occupancy Certificate"));
        context.PropertyCertificates.Add(PropertyCertificateEntity.Create(
            propertyId: null, certificateTypeId: 1, certificateNo: "Shubham1445", issueDate: new DateTime(2026, 1, 9),
            entityType: "S", societyDetailId: 5, wingDetailId: null));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForWingAsync(wingDetailsId: 10);

        Assert.NotNull(result);
        var societyRow = Assert.Single(result!.SocietyCertificates!);
        Assert.Equal("Apartment", societyRow.Level);
        Assert.Equal("Shubham1445", societyRow.CertificateNumber);
        var wingRow = Assert.Single(result.WingCertificates!);
        Assert.Equal("Wing", wingRow.Level);
        Assert.Equal("Shubham1445", wingRow.CertificateNumber);
        Assert.Equal(10, wingRow.WingDetailId);
        Assert.Empty(result.UnitCertificates!);
    }

    [Fact]
    public async Task GetGridForWingAsync_SocietyWingAndUnitAllExist_ReturnsThreeSeparateLists()
    {
        // All three levels have their own record for the same certificate type -- none suppresses
        // another; each is returned in its own JSON array.
        using var context = CreateContext();
        context.PropertyMast.Add(CreateUnit(1, 77, "1", null, "101", wingDetailId: 10));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(1, "OC", "Occupancy Certificate"));
        context.PropertyCertificates.AddRange(
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 1, certificateNo: "Shubham1445", issueDate: new DateTime(2026, 1, 9),
                entityType: "S", societyDetailId: 5, wingDetailId: null),
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 1, certificateNo: "kgudy420", issueDate: new DateTime(2026, 1, 9),
                entityType: "W", societyDetailId: 5, wingDetailId: 10),
            PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: 1, certificateNo: "420jjkgg", issueDate: new DateTime(2026, 1, 9),
                entityType: "P", societyDetailId: 5, wingDetailId: 10));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForWingAsync(wingDetailsId: 10);

        Assert.NotNull(result);
        var societyRow = Assert.Single(result!.SocietyCertificates!);
        Assert.Equal("Shubham1445", societyRow.CertificateNumber);
        var wingRow = Assert.Single(result.WingCertificates!);
        Assert.Equal("Wing", wingRow.Level);
        Assert.Equal("kgudy420", wingRow.CertificateNumber);
        var unitRow = Assert.Single(result.UnitCertificates!);
        Assert.Equal("Unit", unitRow.Level);
        Assert.Equal("420jjkgg", unitRow.CertificateNumber);
    }

    [Fact]
    public async Task GetGridForUnitAsync_NoUnit_ReturnsNull()
    {
        using var context = CreateContext();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForUnitAsync(propertyId: 999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetGridForUnitAsync_PropertyWiseCertificate_ReturnsUnitRowForThisUnitOnly()
    {
        using var context = CreateContext();
        context.PropertyMast.AddRange(
            CreateUnit(1, 77, "1", null, "101"),
            CreateUnit(2, 77, "1", "1-A", "102"));
        context.PropertyCertificateTypeMasters.Add(CreateType(3, "EB", "Electricity Bill"));
        context.PropertyCertificates.AddRange(
            PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: 3, certificateNo: "MSEB/2024/B-101", issueDate: new DateTime(2024, 7, 1)),
            PropertyCertificateEntity.Create(propertyId: 2, certificateTypeId: 3, certificateNo: "MSEB/2024/B-102", issueDate: new DateTime(2024, 7, 2)));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForUnitAsync(propertyId: 1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.UnitCount);
        var row = Assert.Single(result.UnitCertificates!);
        Assert.Equal("Unit", row.Level);
        Assert.Equal("MSEB/2024/B-101", row.CertificateNumber);
        Assert.Equal(new List<string> { "101" }, row.CoveredUnitNumbers);
    }

    [Fact]
    public async Task GetGridForUnitAsync_FloorWiseCertificate_ReturnsFloorRow()
    {
        using var context = CreateContext();
        context.PropertyMast.Add(CreateUnit(1, 77, "1", null, "101"));
        context.PropertyDetails.Add(new PropertyDetailsEntity { Id = 50, PropertyId = 1, FloorId = 3, TypeOfUseId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.FloorEntity.Add(new FloorEntity { Id = 3, Description = "First Floor", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(1, "OC", "Occupancy Certificate"));
        context.PropertyCertificates.Add(PropertyCertificateEntity.Create(
            propertyId: 1, certificateTypeId: 1, certificateNo: "PMC/OC/2024/FL1", issueDate: new DateTime(2024, 2, 1),
            propertyDetailsId: 50, entityType: "P"));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForUnitAsync(propertyId: 1);

        Assert.NotNull(result);
        var row = Assert.Single(result!.FloorCertificates!);
        Assert.Equal("Floor", row.Level);
        Assert.Equal("First Floor", row.ApplicableToLabel);
        Assert.Equal("PMC/OC/2024/FL1", row.CertificateNumber);
        Assert.Equal(1, row.PropertyId);
        Assert.Equal(50, row.PropertyDetailsId);
    }

    [Fact]
    public async Task GetGridForUnitAsync_SocietyAndWingLevelCertificates_ApplyToThisUnit()
    {
        // Reproduces the reported requirement: a certificate uploaded at Society or Wing scope
        // applies to every unit under it, so both must show up when querying one specific
        // property too, even though this unit has no record of its own.
        using var context = CreateContext();
        context.PropertyMast.Add(CreateUnit(1, 77, "1", null, "101", wingDetailId: 10));
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(1, "OC", "Occupancy Certificate"));
        context.PropertyCertificates.AddRange(
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 1, certificateNo: "Shubham1445", issueDate: new DateTime(2026, 1, 9),
                entityType: "S", societyDetailId: 5, wingDetailId: null),
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 1, certificateNo: "kgudy420", issueDate: new DateTime(2026, 1, 9),
                entityType: "W", societyDetailId: 5, wingDetailId: 10));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForUnitAsync(propertyId: 1);

        Assert.NotNull(result);
        var societyRow = Assert.Single(result!.SocietyCertificates!);
        Assert.Equal("Shubham1445", societyRow.CertificateNumber);
        var wingRow = Assert.Single(result.WingCertificates!);
        Assert.Equal("kgudy420", wingRow.CertificateNumber);
        Assert.Empty(result.UnitCertificates!);
        Assert.Empty(result.FloorCertificates!);
    }

    [Fact]
    public async Task GetGridForUnitAsync_AllFourLevelsExist_ReturnsFourSeparateLists()
    {
        // Society-level, this unit's Wing-level, this unit's own property-wise record, AND a
        // floor-wise record for this unit all exist for the same certificate type -- all four
        // are returned, each in its own JSON array, none suppressing another.
        using var context = CreateContext();
        context.PropertyMast.Add(CreateUnit(1, 77, "1", null, "101", wingDetailId: 10));
        context.PropertyDetails.Add(new PropertyDetailsEntity { Id = 50, PropertyId = 1, FloorId = 3, TypeOfUseId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.FloorEntity.Add(new FloorEntity { Id = 3, Description = "First Floor", IsActive = true, CreatedDate = DateTime.Now });
        context.SocietyDetailsMast.Add(new SocietyDetailsEntity { Id = 5, PropertyId = 1, IsActive = true, CreatedDate = DateTime.Now });
        context.WingDetailsMast.Add(new WingDetailsMastEntity { Id = 10, SocietyDetailsMastId = 5, WingName = "A Wing", IsActive = true, CreatedDate = DateTime.Now });
        context.PropertyCertificateTypeMasters.Add(CreateType(1, "OC", "Occupancy Certificate"));
        context.PropertyCertificates.AddRange(
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 1, certificateNo: "Shubham1445", issueDate: new DateTime(2026, 1, 9),
                entityType: "S", societyDetailId: 5, wingDetailId: null),
            PropertyCertificateEntity.Create(propertyId: null, certificateTypeId: 1, certificateNo: "kgudy420", issueDate: new DateTime(2026, 1, 9),
                entityType: "W", societyDetailId: 5, wingDetailId: 10),
            PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: 1, certificateNo: "420jjkgg", issueDate: new DateTime(2026, 1, 9),
                entityType: "P", societyDetailId: 5, wingDetailId: 10),
            PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: 1, certificateNo: "FL-001", issueDate: new DateTime(2026, 1, 9),
                propertyDetailsId: 50, entityType: "P", societyDetailId: 5, wingDetailId: 10));
        await context.SaveChangesAsync();

        var repository = new ApartmentQcCertificateGridRepository(context);
        var result = await repository.GetGridForUnitAsync(propertyId: 1);

        Assert.NotNull(result);
        Assert.Equal("Shubham1445", Assert.Single(result!.SocietyCertificates!).CertificateNumber);
        Assert.Equal("kgudy420", Assert.Single(result.WingCertificates!).CertificateNumber);
        Assert.Equal("420jjkgg", Assert.Single(result.UnitCertificates!).CertificateNumber);
        Assert.Equal("FL-001", Assert.Single(result.FloorCertificates!).CertificateNumber);
    }
}
