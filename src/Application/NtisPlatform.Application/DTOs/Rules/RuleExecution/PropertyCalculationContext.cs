using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    /// <summary>
    /// Aggregate root that carries the complete, validated state needed to execute
    /// property tax calculations and dynamic rule evaluation for a single property.
    ///
    /// <para>
    /// <b>Structure:</b><br/>
    /// — Strongly-typed entity collections (<see cref="Property"/>, <see cref="Details"/>, etc.)
    ///   are populated once by <c>PropertyContextLoaderService</c>.<br/>
    /// — Scalar / transient inputs are carried in <see cref="Parameters"/> as a
    ///   strongly-typed <see cref="PropertyCalculationParameters"/> object.<br/>
    /// — Use <see cref="CloneForDetail"/> to create a thread-safe, per-detail copy before
    ///   passing the context into the rule engine for parallel execution.
    /// </para>
    /// </summary>
    public sealed class PropertyCalculationContext
    {
        // ─── Entity Aggregates ──────────────────────────────────────────────────────

        /// <summary>The root property entity. Never null after a successful context load.</summary>
        public PropertyEntity Property { get; set; } = null!;

        /// <summary>The property assessment record. May be null — callers must handle the default.</summary>
        public PropertyAssessmentEntity? PropertyAssessment { get; set; }

        /// <summary>All active, non-deleted detail records for this property.</summary>
        public IReadOnlyList<PropertyDetailsEntity> Details { get; set; } = [];

        /// <summary>All active renter records linked to the detail IDs of this property.</summary>
        public IReadOnlyList<RenterMastEntity> Renters { get; set; } = [];

        /// <summary>All active Occupancy Certificate (OC) records linked to the detail IDs of this property.</summary>
        public IReadOnlyList<PropertyCertificateEntity> Certificates { get; set; } = [];

        /// <summary>All active assessment year ranges. Used to resolve per-detail YearRangeRVId during CloneForDetail.</summary>
        public IReadOnlyList<AssessmentYearRangeEntity> YearRanges { get; set; } = [];

        /// <summary>
        /// Pre-computed mapping of DetailId to YearRangeRVId based on each detail's AssessmentYear.
        /// Resolved once during context loading for efficient direct lookup during calculations.
        /// </summary>
        public IReadOnlyDictionary<int, int> DetailYearRangeRVIdMap { get; set; } = new Dictionary<int, int>();

        // ─── Calculation Parameters ─────────────────────────────────────────────────

        /// <summary>
        /// Strongly-typed scalar inputs and per-detail overrides for this calculation context.
        /// See <see cref="PropertyCalculationParameters"/> for the full list of available fields.
        /// </summary>
        public PropertyCalculationParameters Parameters { get; set; } = new();

        // ─── Cloning ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a lightweight, thread-safe copy of this context scoped to a single
        /// <paramref name="detail"/> and its resolved <paramref name="detailTypeOfUse"/>.
        ///
        /// <para>
        /// Entity collections are shared by reference (read-only, safe for concurrent reads).
        /// <see cref="Parameters"/> is deep-cloned so that per-detail overrides do not
        /// bleed across parallel iterations.
        /// </para>
        /// </summary>
        /// <param name="detail">The specific property detail being evaluated.</param>
        /// <param name="detailTypeOfUse">The type-of-use master record for that detail.</param>
        /// <returns>A new <see cref="PropertyCalculationContext"/> scoped to the given detail.</returns>
        public PropertyCalculationContext CloneForDetail(
            PropertyDetailsEntity detail,
            TypeOfUseEntity detailTypeOfUse)
        {
            return new PropertyCalculationContext
            {
                // Shared read-only references — safe for parallel reads
                Property = this.Property,
                PropertyAssessment = this.PropertyAssessment,
                Details = this.Details,
                Renters = this.Renters,
                Certificates = this.Certificates,
                YearRanges = this.YearRanges,

                // Deep-cloned with per-detail overrides applied
                Parameters = this.Parameters.CloneForDetail(detail, detailTypeOfUse, this.YearRanges.ToList())
            };
        }
    }
}
