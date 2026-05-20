using AutoMapper;
using Xunit;
using NtisPlatform.Tests.Helpers;
using System;
using System.Linq;

namespace NtisPlatform.Tests;

/// <summary>
/// Test to validate AutoMapper configuration
/// This test documents intentionally unmapped properties and catches unexpected mapping errors
/// </summary>
public class AutoMapperValidationTest
{
    [Fact]
    public void AutoMapper_Configuration_ShouldBeValid_WithDocumentedUnmappedProperties()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(NtisPlatform.Application.Mappings.CapitalValueMappingProfile).Assembly);
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        try
        {
            config.AssertConfigurationIsValid();
            // If we get here, all mappings are valid!
        }
        catch (AutoMapperConfigurationException ex)
        {
            // Separate unmapped member errors from other configuration errors
            var unmappedMemberErrors = ex.Errors
                .Where(e => e.UnmappedPropertyNames?.Any() == true)
                .ToList();

            var otherErrors = ex.Errors
                .Where(e => e.UnmappedPropertyNames?.Any() != true)
                .ToList();

            // If there are non-unmapped-member errors, fail immediately
            // These indicate serious configuration issues (missing type maps, constructor problems, etc.)
            if (otherErrors.Any())
            {
                var errorDetails = string.Join("\n\n", otherErrors.Select((error, index) =>
                    $"Error {index + 1}: {error}"));

                // xUnit doesn't have Assert.Fail, use Assert.True(false, message) instead
                Assert.True(false,
                    $"AutoMapper configuration has {otherErrors.Count} non-unmapped-member error(s):\n\n" +
                    errorDetails +
                    "\n\n=== SOLUTION ===\n" +
                    "These are NOT unmapped property issues. Check for:\n" +
                    "- Missing CreateMap<> declarations\n" +
                    "- Constructor parameter mapping failures\n" +
                    "- Type conversion issues\n" +
                    "- Invalid member access in ForMember() expressions");
            }

            // Now handle unmapped member errors with allowlist
            var unmappedMembers = unmappedMemberErrors
                .SelectMany(error => error.UnmappedPropertyNames ?? Enumerable.Empty<string>())
                .Distinct()
                .ToList();

            // Known intentionally unmapped properties that are expected
            var expectedUnmappedPatterns = new[]
            {
              // Auto-generated IDs
              "Id",
              // Audit fields (populated by repository/interceptors)
              "CreatedBy", "UpdatedBy", "CreatedDate", "UpdatedDate", "MarkedForDeletion",
              // Navigation properties (managed by EF Core)
              "ConfigKey", "ConfigKeys", "Category", "Department", "Module", "ScreenGroup", "Ward",
              "PropertySeqNo", "MoujaId", "MarkedForDeletionDate",
              "FlagMaster", "PropertyTaxCalculationCVResults", "PropertyTaxCalculationRVResults", "PlotDetails", "PropertyDetails",
              "PropertyDetailsOld", "PropertyMastOld", "SocietyDetailsMast", "PropertyMastDetails",
              "TaxMaster", "User", "PropertyMast", "RateCVMaster", "YearMaster", // Navigation properties
              "AssessmentYearRange", "FloorGroup", "TypeOfUseGroup", "SubZone", // More navigation properties
              "Floor", "YearRangeCV", "ConstructionType", // Additional navigation properties
              "Office", "Role", "Screen", "RoleWiseScreenAccess", // User/Role related navigation
              "ULB", "Mouja", "Zone", "PropertyCategory", // Master data navigation
              "TypeOfUse", "SubTypeOfUse", "PropertyType", // Type related navigation
              "PolicyTaxDetails", "PolicyTaxDetailsCV", "TransMastOld", "TransMastCV", // Transaction navigation
              // Computed/derived fields (populated from navigation properties)
              "FloorFactorId", "NatureFactorId", "AgeFactorId", "UseFactorId",
              "ConstructionCode", "ConstructionDescription", "FromYear", "ToYear",
              "FloorCode", "FloorDescription",
              "ZoneCode", "ZoneName", "WardCode", "WardName",
              "TypeCode", "TypeName", "CategoryCode", "CategoryName",
              "SubZoneNo", "SubZoneName", "TypeOfUseGroupName", "FloorGroupName",
              "OpenPlotRate", "ResidentialRate", "OfficeRate", "ShopRate", "IndustrialRate",
              "SDRR", "SearchKey", "Type", // Computed/search fields
              // Collection navigation properties
              "PropertyAssessments", "UserDepartmentAllocations", "UserModuleAllocations",
              "UserRoleAllocations", "RuleScopes", "Taxes",
              // Other system fields
              "HasFactorData", "IsSystem", "IsDefault", "IsDeleted", "IsGenerated",
              // Contact information fields (intentionally unmapped for data mapping scenarios)
              "PinCode", "AlternateMobileNo", "OccupierMobileNo", "BuilderMobile",
              // Property entity fields not mapped from Create/Update DTOs
              "IsCombineProperty", "PropertyMastOldId", "PropertyAssessmentStatusId",
              "MobileNoRemarkId", "OccupierMobileNoRemarkId",
              // Owner/Occupier detail fields (present in entity but not in some DTOs)
              "OwnerTitle", "OwnerTitleEnglish", "OwnerNameEnglish", "OwnerName",
              "OccupierTitle", "OccupierName", "OccupierTitleEnglish", "OccupierNameEnglish",
              // Flat/Shop detail fields  
              "FlatOrShopNo", "FlatOrShopName", "FlatOrShopNoEnglish", "FlatOrShopNameEnglish",
              // Address detail fields
              "Address", "Location", "AddressEnglish", "LocationEnglish",
              // Contact fields
              "MobileNo",
              // Property classification fields
              "OpenPlot", "CSN", "PlotNo",
              // Newly added unmapped properties
             "UseFactorCVMaster", "NatureFactorCVMaster", "AgeFactorCVMaster",
             "RateMasterForCV", "TaxPercentageMasterCV", "Rates",
             "TaxPercentageMasterRV", "DepreciationMaster",
              "PropertyDetails", "PropertyMast", "Property", // Updated specific property names
              "RateSection", "RateSectionDetails", "BlockMaster",
              "WaterConnectionMaster", "WaterRateMaster"
          };

            // Check if all unmapped properties are in the expected list
            var unexpectedUnmapped = unmappedMembers
                .Where(member => !expectedUnmappedPatterns.Any(pattern =>
                    member.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (unexpectedUnmapped.Any())
            {
                var errorMessage = $"Found {unexpectedUnmapped.Count} unexpected unmapped properties (not in documented list):\n\n" +
                                 string.Join("\n", unexpectedUnmapped.Select((m, i) => $"{i + 1}. {m}")) +
                                 "\n\n=== SOLUTION ===\n" +
                                 "If these properties are intentionally unmapped (navigation properties, audit fields, computed fields),\n" +
                                 "add them to expectedUnmappedPatterns in AutoMapperValidationTest.cs\n\n" +
                                 "If they should be mapped, update the corresponding AutoMapper profile(s) to map these properties.";

                // xUnit doesn't have Assert.Fail, use Assert.True(false, message) instead
                Assert.True(false, errorMessage);
            }

            // If we get here, all unmapped properties are documented and expected
            Assert.True(true, $"AutoMapper validation passed. Found {unmappedMembers.Count} documented unmapped properties.");
        }
    }

    /// <summary>
    /// Verifies that the test helper CreateMapper can be instantiated without errors
    /// </summary>
    [Fact]
    public void AutoMapperTestHelper_CreateMapper_ShouldNotThrow()
    {
        var mapper = AutoMapperTestHelper.CreateMapper();
        Assert.NotNull(mapper);
    }
}
