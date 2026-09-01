using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.TaxEngine;

/// <summary>
/// Tests for <see cref="RVCalculationSignatureBuilder"/>, which hashes the property-owned data
/// that feeds an RV calculation so <c>RateableValueService</c> can decide whether to skip
/// recalculation. Coverage focuses on the two properties the design depends on:
/// - relevant changes (area, type of use, owner type, exemptions, certificates, social attributes,
///   renters) DO change the signature;
/// - unrelated master/policy data is intentionally excluded, so it never affects the signature.
/// </summary>
public class RVCalculationSignatureBuilderTests
{
    private static PropertyEntity BuildProperty(int categoryId = 1, int wardId = 5, int taxZoneId = 2, int? moujaId = 10, string csn = "CSN1") =>
        new PropertyEntity
        {
            Id = 1,
            CategoryId = categoryId,
            WardId = wardId,
            TaxZoneId = taxZoneId,
            MoujaId = moujaId,
            CSN = csn
        };

    private static PropertyAssessmentEntity BuildAssessment(int? ownerTypeId = 1) =>
        new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = 1,
            OwnerTypeId = ownerTypeId
        };

    private static PropertyDetailsEntity BuildDetail(int id = 1, int typeOfUseId = 1, double carpetAreaSqMeter = 100d) =>
        new PropertyDetailsEntity
        {
            Id = id,
            PropertyId = 1,
            TypeOfUseId = typeOfUseId,
            ConstructionTypeId = 1,
            ConstructionYear = "2020",
            AssessmentYear = "2020",
            CarpetAreaSqMeter = carpetAreaSqMeter,
            CarpetAreaSqFeet = carpetAreaSqMeter * 10.764,
            BuiltupAreaSqMeter = carpetAreaSqMeter,
            BuiltupAreaSqFeet = carpetAreaSqMeter * 10.764
        };

    private static PropertyCertificateEntity BuildCertificate(int id = 1, int certificateTypeId = 1, string? certificateNo = "CERT-1")
    {
        var certificate = PropertyCertificateEntity.Create(propertyId: 1, certificateTypeId: certificateTypeId, certificateNo: certificateNo);
        typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.Id))!.SetValue(certificate, id);
        return certificate;
    }

    private static Dictionary<string, object> BuildSocialAttributes(int hasLift = 5) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["HAS_LIFT"] = hasLift
        };

    private static string GenerateForDetail(
        PropertyDetailsEntity detail,
        PropertyEntity? property = null,
        PropertyAssessmentEntity? propertyAssessment = null,
        IReadOnlyCollection<int>? exemptedTaxIds = null,
        IReadOnlyList<PropertyCertificateEntity>? certificates = null,
        IReadOnlyDictionary<string, object>? socialAttributes = null)
    {
        return RVCalculationSignatureBuilder.GenerateSignature(
            financeYear: 2025,
            property: property ?? BuildProperty(),
            propertyAssessment: propertyAssessment ?? BuildAssessment(),
            details: new List<PropertyDetailsEntity> { detail },
            renters: new List<RenterMastEntity>(),
            exemptedTaxIds: exemptedTaxIds ?? new List<int>(),
            certificates: certificates ?? new List<PropertyCertificateEntity>(),
            socialAttributes: socialAttributes ?? new Dictionary<string, object>());
    }

    [Fact]
    public void GenerateSignature_SameInputs_ProducesIdenticalHash()
    {
        var hash1 = GenerateForDetail(BuildDetail());
        var hash2 = GenerateForDetail(BuildDetail());

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateSignature_DifferentCarpetArea_ProducesDifferentHash()
    {
        var hashOriginal = GenerateForDetail(BuildDetail(carpetAreaSqMeter: 100d));
        var hashChanged = GenerateForDetail(BuildDetail(carpetAreaSqMeter: 150d));

        Assert.NotEqual(hashOriginal, hashChanged);
    }

    [Fact]
    public void GenerateSignature_DifferentTypeOfUseId_ProducesDifferentHash()
    {
        var hashOriginal = GenerateForDetail(BuildDetail(typeOfUseId: 1));
        var hashChanged = GenerateForDetail(BuildDetail(typeOfUseId: 2));

        Assert.NotEqual(hashOriginal, hashChanged);
    }

    [Fact]
    public void GenerateSignature_DifferentPropertyCategory_ProducesDifferentHash()
    {
        var hashOriginal = GenerateForDetail(BuildDetail(), BuildProperty(categoryId: 1));
        var hashChanged = GenerateForDetail(BuildDetail(), BuildProperty(categoryId: 2));

        Assert.NotEqual(hashOriginal, hashChanged);
    }

    [Fact]
    public void GenerateSignature_DifferentOwnerTypeId_ProducesDifferentHash()
    {
        var detail = BuildDetail();

        var hashOriginal = GenerateForDetail(detail, propertyAssessment: BuildAssessment(ownerTypeId: 1));
        var hashChanged = GenerateForDetail(detail, propertyAssessment: BuildAssessment(ownerTypeId: 2));

        Assert.NotEqual(hashOriginal, hashChanged);
    }

    [Fact]
    public void GenerateSignature_DifferentFinanceYear_ProducesDifferentHash()
    {
        var detail = BuildDetail();
        var property = BuildProperty();
        var assessment = BuildAssessment();

        var hash2025 = RVCalculationSignatureBuilder.GenerateSignature(
            2025, property, assessment, new List<PropertyDetailsEntity> { detail }, new List<RenterMastEntity>(),
            new List<int>(), new List<PropertyCertificateEntity>(), new Dictionary<string, object>());

        var hash2026 = RVCalculationSignatureBuilder.GenerateSignature(
            2026, property, assessment, new List<PropertyDetailsEntity> { detail }, new List<RenterMastEntity>(),
            new List<int>(), new List<PropertyCertificateEntity>(), new Dictionary<string, object>());

        Assert.NotEqual(hash2025, hash2026);
    }

    [Fact]
    public void GenerateSignature_DifferentExemptedTaxIds_ProducesDifferentHash()
    {
        var detail = BuildDetail();

        var hashNoExemption = GenerateForDetail(detail, exemptedTaxIds: new List<int>());
        var hashWithExemption = GenerateForDetail(detail, exemptedTaxIds: new List<int> { 3 });

        Assert.NotEqual(hashNoExemption, hashWithExemption);
    }

    [Fact]
    public void GenerateSignature_DifferentCertificateNo_ProducesDifferentHash()
    {
        var detail = BuildDetail();

        var hashOriginal = GenerateForDetail(detail, certificates: new List<PropertyCertificateEntity> { BuildCertificate(certificateNo: "CERT-1") });
        var hashChanged = GenerateForDetail(detail, certificates: new List<PropertyCertificateEntity> { BuildCertificate(certificateNo: "CERT-2") });

        Assert.NotEqual(hashOriginal, hashChanged);
    }

    [Fact]
    public void GenerateSignature_DifferentSocialAttributeValue_ProducesDifferentHash()
    {
        // Covers both a direct edit on this property's social attributes AND an inherited change
        // from the "main" property (Apartment/Industry partitions) -- PropertyContextLoaderService
        // merges both into the same flattened dictionary before RateableValueService ever sees it,
        // so the signature builder cannot and need not distinguish the two sources.
        var detail = BuildDetail();

        var hashOriginal = GenerateForDetail(detail, socialAttributes: BuildSocialAttributes(hasLift: 5));
        var hashChanged = GenerateForDetail(detail, socialAttributes: BuildSocialAttributes(hasLift: 9));

        Assert.NotEqual(hashOriginal, hashChanged);
    }

    [Fact]
    public void GenerateSignature_SocialAttributeKeyOrderDoesNotAffectHash()
    {
        var detail = BuildDetail();

        var attributesAB = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["HAS_LIFT"] = true,
            ["NO_OF_WELL"] = 2
        };
        var attributesBA = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["NO_OF_WELL"] = 2,
            ["HAS_LIFT"] = true
        };

        var hashAB = GenerateForDetail(detail, socialAttributes: attributesAB);
        var hashBA = GenerateForDetail(detail, socialAttributes: attributesBA);

        Assert.Equal(hashAB, hashBA);
    }

    [Fact]
    public void GenerateSignature_RenterOrderDoesNotAffectHash()
    {
        var detail1 = BuildDetail(id: 1);
        var detail2 = BuildDetail(id: 2);
        var property = BuildProperty();
        var assessment = BuildAssessment();

        var renterA = new RenterMastEntity { Id = 1, PropertyDetailsId = 1, IsActive = true, RentMonthly = 500 };
        var renterB = new RenterMastEntity { Id = 2, PropertyDetailsId = 2, IsActive = true, RentMonthly = 700 };

        string Generate(List<RenterMastEntity> renters) =>
            RVCalculationSignatureBuilder.GenerateSignature(
                2025, property, assessment, new List<PropertyDetailsEntity> { detail1, detail2 }, renters,
                new List<int>(), new List<PropertyCertificateEntity>(), new Dictionary<string, object>());

        var hashAB = Generate(new List<RenterMastEntity> { renterA, renterB });
        var hashBA = Generate(new List<RenterMastEntity> { renterB, renterA });

        Assert.Equal(hashAB, hashBA);
    }
}
