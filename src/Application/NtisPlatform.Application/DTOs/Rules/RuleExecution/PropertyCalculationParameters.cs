using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    /// <summary>
    /// Carries all scalar and transient calculation parameters for property tax rule execution.
    /// This is the strongly-typed alternative to a <c>Dictionary&lt;string, object&gt;</c> parameter bag.
    ///
    /// <para>
    /// <b>Design intent:</b><br/>
    /// — Scalar inputs resolved once per property load (<see cref="FinanceYear"/>, <see cref="ConstructionYearValue"/>, etc.)
    ///   are set by <c>PropertyContextLoaderService</c> and remain constant across all detail iterations.<br/>
    /// — Per-detail overrides (<see cref="Detail"/>, <see cref="DetailTypeOfUse"/>) are populated only
    ///   during a <c>CloneForDetail</c> operation and are scoped to a single detail's rule evaluation.
    /// </para>
    ///
    /// <para>
    /// <b>Extensibility:</b><br/>
    /// Add new calculation parameters as strongly-typed properties here.
    /// The compiler will immediately guide you to every call site that needs updating.
    /// </para>
    /// </summary>
    public sealed class PropertyCalculationParameters
    {
        // ─── Global Parameters (set once per property load) ────────────────────────

        /// <summary>
        /// The financial year for which tax is being calculated (e.g. 2026 for FY 2026-27).
        /// Derived from the current calendar date at the time of calculation.
        /// </summary>
        public int FinanceYear { get; set; }

        /// <summary>
        /// The parsed integer value of <c>PropertyDetailsEntity.ConstructionYear</c>.
        /// Used to compute <c>PropertyAge</c> and to resolve the applicable year range.
        /// </summary>
        public int ConstructionYearValue { get; set; }

        /// <summary>
        /// The primary key of the <c>AssessmentYearRangeEntity</c> that matches
        /// <see cref="ConstructionYearValue"/>. Used to filter tax percentage records.
        /// </summary>
        public int YearRangeRVId { get; set; }

        /// <summary>
        /// Whether the property has a lift, resolved from <c>PropertySocialDetailsEntity</c>
        /// via the social attribute code <c>"HAS_LIFT"</c>.
        /// Kept for backward compatibility — also available via <see cref="SocialAttributes"/>["HAS_LIFT"].
        /// </summary>
        public bool HasLift { get; set; }

        /// <summary>
        /// Whether the property has a club house, resolved from <c>PropertySocialDetailsEntity</c>
        /// via the social attribute code <c>"HAS_CLUB_HOUSE"</c>.
        /// Kept for backward compatibility — also available via <see cref="SocialAttributes"/>["HAS_CLUB_HOUSE"].
        /// </summary>
        public bool HAS_CLUB_HOUSE { get; set; }

        /// <summary>
        /// Whether the property has a swimming pool, resolved from <c>PropertySocialDetailsEntity</c>
        /// via the social attribute code <c>"HAS_SWIMMING_POOL"</c>.
        /// Kept for backward compatibility — also available via <see cref="SocialAttributes"/>["HAS_SWIMMING_POOL"].
        /// </summary>
        public bool HAS_SWIMMING_POOL { get; set; }

        /// <summary>
        /// All active social attributes for the property, keyed by <c>SocialAttributeCode</c>.
        /// Values are typed CLR objects: <c>bool</c> for BIT, <c>int</c> for INT,
        /// <c>decimal</c> for DECIMAL, <c>string</c> for TEXT.
        ///
        /// <para>
        /// <b>Adding a new attribute to the rule engine:</b><br/>
        /// 1. Add the attribute row to <c>PTIS.SocialAttributeMaster</c> (DB only).<br/>
        /// 2. Ensure the property data entry saves a row in <c>PropertySocialDetails</c>.<br/>
        /// 3. Reference it in a rule expression as <c>input.HAS_SOLAR</c>, <c>input.NO_OF_WELL</c>, etc.<br/>
        /// No C# code changes are required.
        /// </para>
        /// </summary>
        public Dictionary<string, object> SocialAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // ─── Per-Detail Overrides (set during CloneForDetail) ──────────────────────

        /// <summary>
        /// The specific <c>PropertyDetailsEntity</c> record being evaluated in the
        /// current rule execution cycle. Null at the context root level.
        /// </summary>
        public PropertyDetailsEntity? Detail { get; set; }

        /// <summary>
        /// The <c>TypeOfUseEntity</c> that corresponds to <see cref="Detail"/>.
        /// Null at the context root level.
        /// </summary>
        public TypeOfUseEntity? DetailTypeOfUse { get; set; }

        /// <summary>
        /// Creates a shallow copy of the global parameters, then overrides
        /// the per-detail fields for a new detail evaluation scope.
        /// Called exclusively by <see cref="PropertyCalculationContext.CloneForDetail"/>.
        /// </summary>
        internal PropertyCalculationParameters CloneForDetail(
            PropertyDetailsEntity detail,
            TypeOfUseEntity detailTypeOfUse)
        {
            return new PropertyCalculationParameters
            {
                // Global — unchanged across all detail iterations
                FinanceYear = this.FinanceYear,
                ConstructionYearValue = this.ConstructionYearValue,
                YearRangeRVId = this.YearRangeRVId,
                HasLift = this.HasLift,
                HAS_CLUB_HOUSE = this.HAS_CLUB_HOUSE,
                HAS_SWIMMING_POOL = this.HAS_SWIMMING_POOL,
                SocialAttributes = this.SocialAttributes,

                // Per-detail — overridden for this clone
                Detail = detail,
                DetailTypeOfUse = detailTypeOfUse
            };
        }
    }
}
