using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Builds a SHA256 signature from the property-owned data that feeds an RV calculation for a
/// property: the property/detail/renter/exemption/certificate data actually used by the formula
/// (deliberately excluding unrelated columns like OwnerName/Address so unrelated edits don't force
/// a recalculation), plus the merged social-attribute view
/// (<see cref="NtisPlatform.Application.DTOs.Rules.RuleExecution.PropertyCalculationParameters.SocialAttributes"/>)
/// the rule engine evaluates against -- which, for Apartment/Industry partitions, already includes
/// attributes inherited from the "main" property, so a change on either side invalidates the fast
/// path. Master/policy tables are intentionally excluded -- only changes to the property's own
/// (and inherited social) data should invalidate the fast path.
/// <see cref="RateableValueService"/> compares this against the last stored signature
/// (<see cref="RVCalculationSignatureEntity"/>) to decide whether recalculation is needed.
/// </summary>
public static class RVCalculationSignatureBuilder
{
    public static string GenerateSignature(
        int financeYear,
        PropertyEntity property,
        PropertyAssessmentEntity? propertyAssessment,
        IReadOnlyList<PropertyDetailsEntity> details,
        IReadOnlyList<RenterMastEntity> renters,
        IReadOnlyCollection<int> exemptedTaxIds,
        IReadOnlyList<PropertyCertificateEntity> certificates,
        IReadOnlyDictionary<string, object> socialAttributes)
    {
        var sb = new StringBuilder();

        sb.Append("FY:").Append(financeYear).Append(';');

        // Property-side: only the columns that actually feed the calculation.
        sb.Append("P:")
          .Append(property.CategoryId).Append(',')
          .Append(property.WardId).Append(',')
          .Append(property.TaxZoneId).Append(',')
          .Append(property.PropertyTypeId).Append(',')
          .Append(propertyAssessment?.OwnerTypeId)
          // CSN intentionally excluded so identifier-only edits don't force an RV recalculation.
          .Append(';');

        foreach (var d in details.OrderBy(x => x.Id))
        {
            sb.Append("D").Append(d.Id).Append(':')
              .Append(d.TypeOfUseId).Append(',')
              .Append(d.SubTypeOfUseId).Append(',')
              .Append(d.FloorId).Append(',')
              .Append(d.SubFloorId).Append(',')
              .Append(d.ConstructionTypeId).Append(',')
              .Append(d.ConstructionYear).Append(',')
              .Append(d.AssessmentYear).Append(',')
              .Append(FormatArea(d.CarpetAreaSqMeter)).Append(',')
              .Append(FormatArea(d.CarpetAreaSqFeet)).Append(',')
              .Append(FormatArea(d.BuiltupAreaSqMeter)).Append(',')
              .Append(FormatArea(d.BuiltupAreaSqFeet)).Append(',')
              .Append(d.IsRenter).Append(',')
              .Append(d.IsTaxable).Append(',')
              .Append(d.MarkedForDeletion).Append(',')
              .Append(d.IsOpenPlot)
              .Append(';');
        }

        foreach (var r in renters
                     .Where(x => x.IsActive && !x.MarkedForDeletion)
                     .OrderBy(x => x.Id))
        {
            sb.Append("R").Append(r.PropertyDetailsId).Append(':')
              .Append(r.RentMonthly).Append(',')
              .Append(r.FinalYearlyRent)
              .Append(';');
        }

        foreach (var taxId in exemptedTaxIds.OrderBy(x => x))
            sb.Append("EX").Append(taxId).Append(';');

        foreach (var c in certificates
                     .Where(x => x.IsActive && !x.MarkedForDeletion)
                     .OrderBy(x => x.Id))
        {
            sb.Append("C").Append(c.Id).Append(':')
              .Append(c.CertificateTypeId).Append(',')
              .Append(c.PropertyDetailsId).Append(',')
              .Append(c.CertificateNo).Append(',')
              .Append(c.IssueDate?.ToString("O", CultureInfo.InvariantCulture)).Append(',')
              .Append(c.TaxApplied)
              .Append(';');
        }

        // Flattened SocialAttributeCode -> value map, already merged by PropertyContextLoaderService
        // from this property AND (for Apartment/Industry partitions) its "main" property -- the same
        // merged view the rule engine evaluates against, so a change to either source invalidates this.
        foreach (var kvp in socialAttributes.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var formattedValue = kvp.Value switch
            {
                null => "null",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => kvp.Value.ToString() ?? "null"
            };
            sb.Append("SA:").Append(kvp.Key).Append('=').Append(formattedValue).Append(';');
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hashBytes);
    }

    private static string FormatArea(double? value) =>
        (value ?? 0d).ToString("R", CultureInfo.InvariantCulture);
}
