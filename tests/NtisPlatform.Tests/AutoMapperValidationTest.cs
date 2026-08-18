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
              // Description field (intentionally not mapped in RuleFields)
              "Description",
              // Navigation properties (managed by EF Core)
              "ConfigKey", "ConfigKeys", "Category", "Department", "Module", "ScreenGroup", "Ward",
              "PropertySeqNo", "MoujaId", "MarkedForDeletionDate",
              "FlagMaster", "PropertyTaxCalculationCVResults", "RVCalculationResults", "PlotDetails", "PropertyDetails",
              "PropertyDetailsOld", "PropertyMastOld", "SocietyDetailsMast", "PropertyMastDetails", "MergeDetail",
              "TaxMaster", "User", "PropertyMast", "RateCVMaster", "YearMaster", "DocumentBinding", "DocumentBindingId", // Navigation properties
              "AssessmentYearRange", "FloorGroup", "TypeOfUseGroup", "SubZone", // More navigation properties
              "Floor", "YearRangeCV", "ConstructionType", // Additional navigation properties
              "Office", "Role", "Screen", "RoleWiseScreenAccess", // User/Role related navigation
              "ULB", "Mouja", "Zone", "PropertyCategory", // Master data navigation
              "TypeOfUse", "SubTypeOfUse", "PropertyType", // Type related navigation
              "PolicyTaxDetails", "PolicyTaxDetailsCV", "TransMastOld", "TransMastCV", // Transaction navigation
              "PropertySocialDetails", // Property social details navigation property
              // Computed/derived fields (populated from navigation properties)
              "FloorFactorId", "NatureFactorId", "AgeFactorId", "UseFactorId",
              "ConstructionCode", "ConstructionDescription", "FromYear", "ToYear",
              "FloorCode", "FloorDescription",
              "ZoneCode", "ZoneName", "WardCode", "WardName",
              "TypeCode", "TypeName", "CategoryCode", "CategoryName", "PolicyCode",
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
              "MobileNoRemarkId", "OccupierMobileNoRemarkId", "TotalPlotArea",
              // Plot dimensions (mapped only in specific UpdatePropertyMastDto scenario)
              "Length", "Width",
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
              "PropertyDetails", "PropertyMast", "Property", "Master", // Updated specific property names
              "RateSection", "RateSectionDetails", "BlockMaster",
              "WaterConnectionMaster", "WaterRateMaster",
              "PropertyAssessmentDetails", "PropertyCertificates", "PropertyTaxCalculationSection129Results",
              "RoomWiseSubmissionDetails", "PropertyImagesMast", "PropertySocialDetails", "TaxPendingDetails",
              "WaterConnectionMaster", "TaxPendingDetailsArchive", "TaxPendingDetailsCV", "TaxPendingDetailsLookup",
              "TaxPendingDetailsRetro", "TaxPendingDetailsRV", "TransMast", "TransMastArchive", "TransMastLookup","AllowedValues",
             // Workflow navigation properties (intentionally unmapped - EF Core managed)
             "WorkflowHistory", "WorkflowDetails", "WorkflowStage",
             // Rule exclusion properties (navigation properties)
             "SkipRules", "ExclusionsTriggered", "ExclusionsSkippedBy",
             // RuleScope navigation property and derived display name
             "RuleScope", "RuleScopeName",
                // Additional entity fields that are unmapped
                "IsActive", "IsCurrent", "ConstructionYear", "AssessmentYear", "CarpetAreaSqMeter", "CarpetAreaSqFeet",
                "BuiltupAreaSqMeter", "BuiltupAreaSqFeet", "NoOfRooms", "IsRenter", "IsTaxable",
                "Renters", "RenterDetails", "AreaSqMtr", "HeightMtr", "Base1Mtr", "Base2Mtr", "Shape",
                "RoomNo", "AssessmentRemark", "FlatSystemRemark", "CombPropRemark", "AdharCardNo",
                "PrarupYadiPublishDate", "AntimYadiPublishDate", "PartOCDate", "BHK", "WingNo",
                "TotalBuiltupAreaSqFeet", "TotalBuiltupAreaSqMeter", "Latitude", "Longitude",
                "NoOfCommercialToilets", "WingName", "PartitionNo",
                // Computed/derived rate fields (populated from navigation properties after mapping)
                "RateAmount",
                // Local record types used in data layer projections (not entity mappings)
                "RVorCV", "CalculationType", "CalculationAnnualValue", "TmTaxAmount", "TmcvTaxAmount", "TmrvTaxAmount", "PendingAmount",
                "Remark", "Application", "LoginTime", "LastActivityTime", "LogoutTime",
                // SocietyDetailsEntity fields unmapped from SocietyWingDetails cross-mapping
                "SocietyName", "SocietyAddress", "SecretaryName", "ManagerName",
                "LandOwnerName", "BuilderName",
                "SecretaryNameEnglish", "SocietyNameEnglish", "SocietyAddressEnglish",
                "ManagerNameEnglish", "LandOwnerNameEnglish", "BuilderNameEnglish",
                "ManagerMobileNo", "ManagerMobileNoRemarkId",
                "SecretaryMobileNo", "SecretaryMobileNoRemarkId",
                "BuilderMobileNo", "BuilderMobileNoRemarkId",
                "SocietyEmailId", "SecretaryEmailId", "ManagerEmailId",
                // PropertyCertificateTypeMasterEntity.PolicyCode (nav prop to PolicyCodeMaster)
                "PolicyCode",
                // AssetLeaseRentDetailsDto - display-only fields with no formatter implemented yet
                "LeaseDurationDisplay", "RentAmountDisplay",
                // AssetLeaseRentDetailsEntity - workflow fields owned by the dedicated Reject/Verify/Approve
                // endpoints (LeaseRejectDto / LeaseWorkflowActionDto), intentionally absent from Create/Update DTOs
                "RejectionReason", "IsRejection", "RejectionBy", "RejectionDate",
                "IsVerified", "VerifiedBy", "VerifiedDate",
                "IsApproved", "ApprovedBy", "ApprovedDate",
                // AssetLeaseRentDetailsEntity navigation properties (EF Core managed)
                "Asset", "History",
                // AssetMasterDto - name-resolution/computed fields resolved by dedicated services
                // (AssetPhotoApplicationService, AssetDocumentApplicationService) or derived from child records
                "Photos", "Documents", "AssetCondition", "CapitalValue", "AssetLife",
                // AssetFieldValueDto / InventoryUnitResponseDto - resolved via a nav-property join not yet
                // modeled on the entity (tracked as follow-up, currently unused DTOs)
                "AssetName", "AssetNo", "FieldDefinitionName", "Condition", "DepreciationRate",
                // InventoryUnitResponseDto - computed/display fields with no entity source yet
                "ConditionFactor", "CVFormula",
                // SubUnitsDetailsDto - stubbed display fields, never populated
                "CVCalculationFormula", "RoomDetails",
                // Document-metadata DTOs (PropertyCertificateDto, ULBDocumentDto, etc.) - file
                // metadata joined in by their controller via
                // IDocumentApplicationService.GetDocumentByBindingAsync, not by AutoMapper
                "OriginalFileName", "FileSizeBytes",
                // InventoryItemCategoryDto.AssetCategoryName - resolved via a GetAllAsync-only SQL join
                // against AssetCategoryEntity (CategoryName), not part of the base entity<->dto map.
                // Listed explicitly even though it also happens to contain the "CategoryName" pattern
                // above, so this mapping's intent doesn't rely on incidental substring overlap.
                "AssetCategoryName",
                // InventoryItemNameDto.InventoryItemCategoryName - resolved via a GetAllAsync-only SQL
                // join against InventoryItemCategoryEntity (TypeName), not part of the base entity<->dto
                // map. Listed explicitly for the same reason as AssetCategoryName above.
                "InventoryItemCategoryName",
                // InventoryItemModelDto.InventoryItemName - resolved via a GetAllAsync-only SQL join
                // against InventoryItemNameEntity (SubTypeName), not part of the base entity<->dto map
                "InventoryItemName",
                // UserEntity two-factor authentication fields - security-sensitive, owned entirely by
                // ITwoFactorAuthenticationService/TwoFactorController and never exposed through any
                // AutoMapper-mapped DTO (status is returned via TwoFactorStatusResponseDto, built by hand).
                "TwoFactorEnabled", "TwoFactorSecretEncrypted", "TwoFactorEnabledAt", "SecurityStamp", "TwoFactorRequired",
                // UserEntity password-expiry / account-level OTP throttle fields - same category as
                // the two-factor fields above: internal auth-flow state, never exposed through a
                // mapped DTO (LoginResponseDto.RequiresPasswordChange/Throttled are built by hand).
                "PasswordChangedAt", "OtpChallengeFailCount", "OtpChallengeLockedUntilAt",
                // TaxMasterEntity.RuleDefinition - navigation property to the selected DynamicTaxRuleEntity
                // (EF Core managed); CreateTaxMasterDto/UpdateTaxMasterDto only carry RuleDefinitionId.
                "RuleDefinition",
                // Retrospective Tax Rule Engine navigation properties (EF Core managed); the
                // corresponding *Id foreign keys are what the DTOs/mapping profiles actually carry.
                "Rule", "Calculation", "AppliedRule", "AppliedTaxPolicy"
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
