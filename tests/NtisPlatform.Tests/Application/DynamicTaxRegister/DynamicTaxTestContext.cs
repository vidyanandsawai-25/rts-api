using System;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Tests.Application.DynamicTaxRegister;

/// <summary>
/// Shared in-memory <see cref="ApplicationDbContext"/> factory for the Dynamic Tax Register service
/// tests. These services take the DbContext directly (rather than a repository), so they need a
/// real context to exercise at all.
///
/// <para>
/// Uses the EF Core InMemory provider on purpose rather than SQLite: several entities in this model
/// declare <c>nvarchar(max)</c>, which SQLite cannot parse when it builds a schema, and none of
/// these tests are asserting anything about SQL generation.
/// </para>
/// </summary>
internal static class DynamicTaxTestContext
{
    /// <summary>A fresh, isolated database per call — tests must never share state.</summary>
    public static ApplicationDbContext Create() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"dtr-{Guid.NewGuid()}")
            // These services mutate and re-read within one call; the warning would otherwise fire
            // on transaction usage the InMemory provider silently ignores.
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    // ── seed helpers ────────────────────────────────────────────────────────────

    public static TaxCalculationModeMasterEntity Mode(
        int id, string code, bool value = false, bool condition = false,
        bool master = false, bool hybrid = false, bool isActive = true, int displayOrder = 0) => new()
        {
            Id = id,
            ModeCode = code,
            ModeName = code,
            DisplayOrder = displayOrder,
            UsesValueConfig = value,
            UsesConditionConfig = condition,
            UsesMasterConfig = master,
            UsesHybridConfig = hybrid,
            IsActive = isActive,
        };

    /// <summary>The four modes as 01_Seed_TaxCalculationModeMaster.sql defines them.</summary>
    public static TaxCalculationModeMasterEntity[] SeededModes() =>
    [
        Mode(1, "VALUE_BASED", value: true, displayOrder: 1),
        Mode(2, "CONDITION_BASED", condition: true, displayOrder: 2),
        Mode(3, "MASTER_BASED", master: true, displayOrder: 3),
        Mode(4, "HYBRID", condition: true, master: true, hybrid: true, displayOrder: 4),
    ];

    public static TaxMasterEntity Tax(
        int id, string code = "TAX", int calculationModeId = 1,
        int? ruleDefinitionId = null, bool isActive = true, int taxCategoryId = 1) => new()
        {
            Id = id,
            TaxCode = code,
            TaxName = code,
            TaxCategoryId = taxCategoryId,
            CalculationModeId = calculationModeId,
            RuleDefinitionId = ruleDefinitionId,
            IsActive = isActive,
        };

    public static TaxConditionRuleEntity ConditionRule(
        int id, int taxId, int sortOrder = 1, string resultMode = "FIXED", string resultBase = "NONE",
        decimal resultValue = 0m, string conditionsJson = "[]", bool isActive = true,
        int? assessmentYearRangeId = null, int? referenceTaxId = null, string? unitFieldId = null,
        int? ruleDefinitionId = null) => new()
        {
            Id = id,
            TaxId = taxId,
            SortOrder = sortOrder,
            ConditionsJson = conditionsJson,
            ResultMode = resultMode,
            ResultBase = resultBase,
            ResultValue = resultValue,
            IsActive = isActive,
            AssessmentYearRangeId = assessmentYearRangeId,
            ReferenceTaxId = referenceTaxId,
            UnitFieldId = unitFieldId,
            RuleDefinitionId = ruleDefinitionId,
        };
}
