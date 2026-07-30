using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services.TaxEngine.OccupationTax;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Application-layer service for Occupation Tax (step 2 of the certificate-change pipeline).
/// Loads the property's real certificates, resolves property-wise vs. floor-wise scope, and
/// computes/persists the result.
/// </summary>
/// <remarks>
/// Floor-wise scope is driven by <see cref="PropertyCertificateEntity.PropertyDetailsId"/> (set by
/// the Building Permission "Apply to Selected Floor" flow), NOT by <see cref="PropertyOccupancyDetailsEntity"/>
/// (a separate, unrelated occupancy-tracking concept used elsewhere in the Rateable Value pipeline).
///
/// When one or more floors carry their own certificate, the engine is run once per floor (each
/// floor's own certificate overrides the property-wise certificate for that floor; floors with no
/// floor-wise certificate fall back to the property-wise one) and results are summed per finance
/// year before persisting — TransMast has no per-floor column, and PropertyAggregated is the only
/// persistence mode this engine implements (a per-floor ledger is deliberately not implemented),
/// so per-floor amounts are always aggregated to the property level before persisting.
///
/// Which real certificate date wins when more than one exists is governed by
/// <see cref="CertificateTaxGuidelineSettings.DatePriority1"/>..<c>DatePriority4</c> (default
/// OC, CC, ELECTRIC_BILL, RETROSPECTIVE — OC beats CC beats Electric Bill beats the
/// no-certificate-at-all fallback), read fresh from PTIS.CertificateTaxGuideline on every
/// computation instead of the previously-hardcoded precedence.
///
/// PTIS.PolicyTaxDetails is shared with the Rateable Value pipeline: NETTAX rows are written
/// independently by the RV pipeline and never touched here; this service writes its own rows
/// tagged via PolicyCodeId with the OC/CC/ELECTRIC_BILL family (the prorated onset year gets the
/// PARTIAL_x variant, e.g. PARTIAL_OC, every full year after gets the plain code, e.g. OC — see
/// <see cref="NtisPlatform.Core.Constants.PolicyCodes"/>), alongside its existing TransMast write.
///
/// KNOWN LIMITATION (out of scope for this pass): if a property is under Section 129, CC/OC/
/// Electric Bill tax should not apply to it, but this implementation has no Section 129 tracking
/// source to check against and does not attempt to guard for it. Do not infer or implement any
/// Section 129 behavior here without that tracking source existing first.
/// </remarks>
public sealed class OccupationTaxApplicationService : IOccupationTaxService
{
    /// <summary>Reserved TaxMaster row holding a policy group's precomputed total (see CapitalValueConstants.Tax.TaxTotalName).</summary>
    private const string TaxTotalCode = "TaxTotal";

    private readonly IOccupationTaxEngine _engine;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IRepository<PropertyCertificateEntity, int> _propertyCertificateRepository;
    private readonly IRepository<PolicyTaxDetailsEntity, int> _policyTaxDetailsRepository;
    private readonly IRepository<TransMastEntity, int> _transMastRepository;
    private readonly IRepository<YearMasterEntity, int> _yearRepository;
    private readonly IRepository<TaxPendingDetailsEntity, int> _taxPendingDetailsRepository;
    private readonly IRepository<TaxPendingDetailsRetroEntity, int> _taxPendingDetailsRetroRepository;
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
    private readonly IPolicyCodeLookupService _policyCodeLookup;
    private readonly IFinanceYearProvider _financeYearProvider;
    private readonly ICertificateTaxGuidelineReaderService _guidelineReader;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OccupationTaxApplicationService> _logger;

    public OccupationTaxApplicationService(
        IOccupationTaxEngine engine,
        IPropertyRepository propertyRepository,
        IRepository<PropertyCertificateEntity, int> propertyCertificateRepository,
        IRepository<PolicyTaxDetailsEntity, int> policyTaxDetailsRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<YearMasterEntity, int> yearRepository,
        IRepository<TaxPendingDetailsEntity, int> taxPendingDetailsRepository,
        IRepository<TaxPendingDetailsRetroEntity, int> taxPendingDetailsRetroRepository,
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IPolicyCodeLookupService policyCodeLookup,
        IFinanceYearProvider financeYearProvider,
        ICertificateTaxGuidelineReaderService guidelineReader,
        IUnitOfWork unitOfWork,
        ILogger<OccupationTaxApplicationService> logger)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
        _propertyCertificateRepository = propertyCertificateRepository ?? throw new ArgumentNullException(nameof(propertyCertificateRepository));
        _policyTaxDetailsRepository = policyTaxDetailsRepository ?? throw new ArgumentNullException(nameof(policyTaxDetailsRepository));
        _transMastRepository = transMastRepository ?? throw new ArgumentNullException(nameof(transMastRepository));
        _yearRepository = yearRepository ?? throw new ArgumentNullException(nameof(yearRepository));
        _taxPendingDetailsRepository = taxPendingDetailsRepository ?? throw new ArgumentNullException(nameof(taxPendingDetailsRepository));
        _taxPendingDetailsRetroRepository = taxPendingDetailsRetroRepository ?? throw new ArgumentNullException(nameof(taxPendingDetailsRetroRepository));
        _taxMasterRepository = taxMasterRepository ?? throw new ArgumentNullException(nameof(taxMasterRepository));
        _policyCodeLookup = policyCodeLookup ?? throw new ArgumentNullException(nameof(policyCodeLookup));
        _financeYearProvider = financeYearProvider ?? throw new ArgumentNullException(nameof(financeYearProvider));
        _guidelineReader = guidelineReader ?? throw new ArgumentNullException(nameof(guidelineReader));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ApplyAsync(int propertyId, int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Occupation Tax application triggered for property {PropertyId} by user {UserId}",
            propertyId, userId);

        var computation = await ComputeAsync(propertyId, cancellationToken);

        if (!computation.Result.IsValid)
        {
            _logger.LogWarning("Occupation Tax computation is invalid for property {PropertyId}: {Reason}",
                propertyId, computation.Result.RejectionReason);

            // A property/floor that had a valid CC/OC/Electric Bill certificate before and now has
            // none (certificate deleted, date cleared, certificate number cleared with
            // CERTIFICATE_REQUIRE_NO_AND_DATE on, invalid CC/OC order rejected, etc.) must not keep
            // showing that stale row on the Tax Details grid -- clean it up. Skip cleanup only when
            // the REASON is one of the two global module-off toggles (ENABLE_CERTIFICATE_BASED_TAX /
            // APPLY_ONLY_TAXABLE_CERT_TYPES): those represent "this whole feature is switched off",
            // not "this property has no valid certificate right now", and existing certificate-tax
            // data should be left exactly as it was while the feature is off.
            if (!computation.IsGlobalToggleOff)
            {
                var guideline = await LoadGuidelineAsync(cancellationToken);
                await CleanupStaleCertificateTaxRowsAsync(propertyId, userId, guideline, cancellationToken);
            }

            return;
        }

        await SaveTaxesAsync(propertyId, userId, computation, cancellationToken);
    }

    public async Task<OccupationTaxResult> PreviewAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Occupation Tax preview requested for property {PropertyId}", propertyId);

        var computation = await ComputeAsync(propertyId, cancellationToken);

        if (!computation.Result.IsValid)
        {
            throw new InvalidOperationException($"Occupation Tax preview failed: {computation.Result.RejectionReason}");
        }

        return computation.Result;
    }

    /// <summary>
    /// Carries the winning policy code/effective date alongside the engine result, so
    /// <see cref="SaveTaxesAsync"/> can tag PolicyTaxDetails rows without widening the public
    /// <see cref="OccupationTaxResult"/> contract other callers depend on.
    /// </summary>
    /// <param name="PolicyCode">
    /// The family used for every finance year, UNLESS <paramref name="YearPolicyCodes"/> overrides
    /// a specific year -- needed because a CC-then-OC merge (see <see cref="ComputeCcThenOcMerge"/>)
    /// mixes two families (CC-governed years and OC-governed years) in one computation.
    /// </param>
    private sealed record OccupationComputation(
        OccupationTaxResult Result,
        string PolicyCode,
        DateTime? EffectiveDate,
        FinanceYear CurrentFy,
        IReadOnlyDictionary<int, string>? YearPolicyCodes = null,
        bool IsGlobalToggleOff = false,
        OccupationTaxOptions? Options = null,
        bool IsNoCertificateFallback = false);

    /// <summary>Result of resolving a single property/floor's applicable certificate(s) and running the engine.</summary>
    private sealed record ResolvedComputation(
        OccupationTaxResult Result,
        string PolicyCode,
        DateTime? EffectiveDate,
        IReadOnlyDictionary<int, string>? YearPolicyCodes = null);

    /// <summary>
    /// Computes the Occupation Tax result for a property. DBA/lead/business-confirmed final schema:
    /// PTIS.PolicyTaxDetails holds exactly ONE active row per (PropertyId, PolicyCodeId, TaxId) --
    /// the CURRENT NETTAX rate only, no PolicyYear column and no per-year rate history. A single,
    /// current NETTAX snapshot is therefore applied uniformly to every finance year this
    /// computation produces (current year and every retro/arrears year alike) -- there is no
    /// year-specific historical rate to look up.
    /// </summary>
    private async Task<OccupationComputation> ComputeAsync(int propertyId, CancellationToken cancellationToken)
    {
        return await ComputeRawAsync(propertyId, cancellationToken);
    }

    /// <summary>
    /// This property's single, current NETTAX breakdown -- the ONLY NETTAX snapshot this schema can
    /// hold (see <see cref="ComputeAsync"/>), applied uniformly to every finance year a computation
    /// produces.
    /// </summary>
    private sealed record YearlyNetTaxSnapshot(
        decimal AnnualNetTax,
        decimal GeneralTaxPortion,
        int ComponentCount,
        PolicyTaxDetailsEntity? GeneralTaxDetail,
        IReadOnlyList<PolicyTaxDetailsEntity> Components);

    private async Task<YearlyNetTaxSnapshot> LoadNetTaxSnapshotAsync(int propertyId, CancellationToken cancellationToken)
    {
        var nettaxId = await _policyCodeLookup.GetIdAsync(PolicyCodes.NetTax, cancellationToken);

        var netTaxDetails = await _policyTaxDetailsRepository.GetQueryable()
            .Include(pt => pt.TaxMaster)
            .Where(pt => pt.PropertyId == propertyId && pt.PolicyCodeId == nettaxId && pt.IsActive && !pt.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        var annualNetTax = netTaxDetails.Sum(pt => pt.TaxAmount ?? 0m);

        var generalTaxDetail = netTaxDetails.FirstOrDefault(pt =>
            pt.TaxMaster != null &&
            (pt.TaxMaster.TaxName.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase) ||
             pt.TaxMaster.TaxCode.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase) ||
             pt.TaxMaster.TaxName.Contains("General", StringComparison.OrdinalIgnoreCase)));

        var generalTaxPortion = generalTaxDetail?.TaxAmount ?? (annualNetTax * 0.6m);

        var components = netTaxDetails.Where(pt => pt != generalTaxDetail && (pt.TaxAmount ?? 0m) > 0m).ToList();
        var componentCount = components.Count;
        if (componentCount == 0)
        {
            componentCount = 4;
        }

        return new YearlyNetTaxSnapshot(annualNetTax, generalTaxPortion, componentCount, generalTaxDetail, components);
    }

    /// <summary>
    /// Computes the Occupation Tax result for a property, routing to either the simple
    /// property-wise path (no floor-wise certificates exist) or the per-floor path (one or more
    /// floors carry their own certificate) per the Application-layer routing note in
    /// <see cref="OccupationTaxEngine.Compute"/>.
    /// </summary>
    private async Task<OccupationComputation> ComputeRawAsync(int propertyId, CancellationToken cancellationToken)
    {
        var guideline = await LoadGuidelineAsync(cancellationToken);

        if (string.Equals(guideline.GuidelineChangeApplyMode, "AUTO_RECALCULATION", StringComparison.OrdinalIgnoreCase))
        {
            // AUTO_RECALCULATION implies re-running every affected property when the guideline
            // itself changes -- a bulk job that is deliberately NOT built yet (separate, larger
            // piece of work: batching, performance, audit trail). Surface this loudly on every
            // run rather than silently behaving like NEXT_CALCULATION.
            _logger.LogWarning(
                "PTIS.CertificateTaxGuideline.GUIDELINE_CHANGE_APPLY_MODE is AUTO_RECALCULATION, but bulk " +
                "auto-recalculation is not implemented yet. Only the property whose certificate changed " +
                "(property {PropertyId}) is being recalculated now.", propertyId);
        }

        // The whole engine run for this property shares one FinanceYear built from the
        // guideline's configured FinancialYearStartMonth/Day, instead of hardcoding 01-Apr.
        var currentFy = new FinanceYear(
            _financeYearProvider.GetCurrentFinanceYear(),
            guideline.FinancialYearStartMonth,
            guideline.FinancialYearStartDay);

        if (!guideline.EnableCertificateBasedTax)
        {
            return new OccupationComputation(
                OccupationTaxResult.Rejected(propertyId,
                    "Certificate-based tax is disabled (PTIS.CertificateTaxGuideline.ENABLE_CERTIFICATE_BASED_TAX = 0)."),
                PolicyCodes.ElectricBill, null, currentFy, IsGlobalToggleOff: true);
        }

        if (!guideline.ApplyOnlyTaxableCertTypes)
        {
            // Per the confirmed business correction, a value of 0 here means "do not apply
            // certificate-based tax at all" -- not "allow all active certificate types".
            return new OccupationComputation(
                OccupationTaxResult.Rejected(propertyId,
                    "Certificate-based tax is disabled (PTIS.CertificateTaxGuideline.APPLY_ONLY_TAXABLE_CERT_TYPES = 0)."),
                PolicyCodes.ElectricBill, null, currentFy, IsGlobalToggleOff: true);
        }

        // Phase 1: Certificate Date Priority sequence resolved dynamically from guideline settings (DatePriority1..4)
        var priorityOrder = BuildPriorityOrder(guideline);

        var certificates = await _propertyCertificateRepository.GetQueryable()
            .Include(pc => pc.CertificateType)
            .Where(pc => pc.PropertyId == propertyId && pc.IsActive && !pc.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        var propertyWiseCertificates = certificates.Where(c => c.PropertyDetailsId == null).ToList();

        // ALLOW_FLOOR_WISE_CERTIFICATE_METADATA is the sole gate for whether floor-wise certificate
        // rows are considered as computation input at all. CERTIFICATE_TAX_SCOPE_MODE describes
        // final PERSISTENCE only (always property-aggregated regardless -- see
        // TAX_PERSISTENCE_MODE/SaveTaxesAsync) and must never additionally block floor-wise input;
        // see ResolveUseFloorWiseCertificates for why that's a deliberate, explicit correction.
        var floorWiseCertificates = ResolveUseFloorWiseCertificates(guideline, propertyId)
            ? certificates
                .Where(c => c.PropertyDetailsId.HasValue)
                .GroupBy(c => c.PropertyDetailsId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList())
            : new Dictionary<int, List<PropertyCertificateEntity>>();

        var options = await BuildOptionsAsync(propertyId, guideline, currentFy, cancellationToken);
        if (options == null)
        {
            return new OccupationComputation(
                OccupationTaxResult.Rejected(propertyId, $"No active NETTAX policy details found for property {propertyId}."),
                PolicyCodes.ElectricBill, null, currentFy);
        }

        var fallback = BuildNoCertificateFallbackConfig(guideline);

        if (floorWiseCertificates.Count == 0)
        {
            // No floor-wise certificates at all: single property-wise computation (unchanged
            // behavior/golden figures from before floor-wise support existed).
            var (oc, cc, bill, rejectionReason) = ExtractDates(propertyWiseCertificates, guideline);
            if (rejectionReason != null)
            {
                return new OccupationComputation(OccupationTaxResult.Rejected(propertyId, rejectionReason), PolicyCodes.ElectricBill, null, currentFy);
            }

            var isNoCertificateFallback = false;
            var resolved = ResolveAndCompute(oc, cc, bill, priorityOrder, options, guideline, currentFy, propertyId);

            if (resolved == null)
            {
                // No certificate anywhere for this property -- Phase 4: Retrospective fallback mode
                resolved = ComputeNoCertificateFallback(propertyId, options, guideline, currentFy);
                if (resolved != null)
                {
                    isNoCertificateFallback = true;
                }
            }

            if (resolved == null)
            {
                return new OccupationComputation(
                    OccupationTaxResult.Rejected(propertyId, "No CC/OC/Electric Bill certificate date is available for this scope -- certificate-based tax is never applied without one."),
                    PolicyCodes.ElectricBill, null, currentFy, Options: options);
            }

            return new OccupationComputation(resolved.Result, resolved.PolicyCode, resolved.EffectiveDate, currentFy, resolved.YearPolicyCodes, Options: options, IsNoCertificateFallback: isNoCertificateFallback);
        }

        // ---- Floor-wise path: run the engine once per floor, aggregate to property level. ----
        var floors = await _propertyRepository.GetPropertyDetailsByPropertyIdAsync(propertyId, cancellationToken);

        if (floors.Count == 0)
        {
            // Floor-wise certificates reference floors that no longer exist for this property —
            // shouldn't happen (PropertyCertificateService validates the floor at save time), but
            // fail loudly rather than silently falling back to property-wise scope.
            return new OccupationComputation(
                OccupationTaxResult.Rejected(
                    propertyId,
                    "Property has floor-wise certificates but no PropertyDetails/floor rows were found."),
                PolicyCodes.ElectricBill,
                null,
                currentFy);
        }

        var floorCount = floors.Count;
        var perFloorOptions = new OccupationTaxOptions
        {
            AnnualNetTax = options.AnnualNetTax / floorCount,
            GeneralTaxPortion = options.GeneralTaxPortion / floorCount,
            ComponentCount = options.ComponentCount,
            CompletionCertificateMultiplier = options.CompletionCertificateMultiplier,
            FloorDivisor = floorCount,
            DefaultRetroLookbackYears = options.DefaultRetroLookbackYears,
            RetroCutoffDate = options.RetroCutoffDate
        };

        var perFloorResults = new List<OccupationTaxResult>();
        var skippedFloors = new List<(int FloorId, string Reason)>();
        var computedFloors = new List<(PropertyDetailsEntity Floor, string PolicyCode, DateTime? EffectiveDate)>();
        var representativeYearPolicyCodes = new Dictionary<int, string>();

        foreach (var floor in floors)
        {
            // Floor-wise certificate overrides property-wise PER CERTIFICATE TYPE, not as an
            // all-or-nothing swap for the whole floor: if this floor has its own floor-wise OC but
            // no floor-wise CC, the property-wise CC must still apply to it (CC governs the whole
            // property unless a floor-specific CC overrides it) -- otherwise a floor-wise OC alone
            // would silently discard an otherwise-applicable property-wise CC/CC-then-OC split for
            // that floor. Floor-wise certs are listed first so ExtractDates's first-match-wins
            // (`ocDate ??= ...`) picks them over any property-wise certificate of the SAME type,
            // while property-wise certificates of a DIFFERENT type still fall through untouched.
            var certsForThisFloor = floorWiseCertificates.TryGetValue(floor.Id, out var fc) && fc.Count > 0
                ? fc.Concat(propertyWiseCertificates).ToList()
                : propertyWiseCertificates;

            var (oc, cc, bill, floorRejectionReason) = ExtractDates(certsForThisFloor, guideline);
            if (floorRejectionReason != null)
            {
                _logger.LogWarning(
                    "Occupation Tax skipped for property {PropertyId}, floor {PropertyDetailsId}: {Reason}",
                    propertyId, floor.Id, floorRejectionReason);
                skippedFloors.Add((floor.Id, floorRejectionReason));
                continue;
            }

            var resolved = ResolveAndCompute(oc, cc, bill, priorityOrder, perFloorOptions, guideline, currentFy, propertyId);

            if (resolved == null)
            {
                // This floor has no certificate date -- Phase 4: Retrospective mode
                resolved = ComputeNoCertificateFallback(propertyId, perFloorOptions, guideline, currentFy);
            }

            if (resolved == null)
            {
                _logger.LogWarning(
                    "Occupation Tax skipped for property {PropertyId}, floor {PropertyDetailsId}: No certificate date available",
                    propertyId, floor.Id);
                skippedFloors.Add((floor.Id, "No certificate date available"));
                continue;
            }

            var floorResult = resolved.Result;
            var floorPolicyCode = resolved.PolicyCode;
            var floorEffectiveDate = resolved.EffectiveDate;

            if (!floorResult.IsValid)
            {
                _logger.LogWarning(
                    "Occupation Tax skipped for property {PropertyId}, floor {PropertyDetailsId}: {Reason}",
                    propertyId, floor.Id, floorResult.RejectionReason);
                skippedFloors.Add((floor.Id, floorResult.RejectionReason ?? "unknown reason"));
                continue;
            }

            if (resolved.YearPolicyCodes != null)
            {
                foreach (var (financeYear, policyCode) in resolved.YearPolicyCodes)
                {
                    representativeYearPolicyCodes.TryAdd(financeYear, policyCode);
                }
            }

            perFloorResults.Add(floorResult);
            computedFloors.Add((floor, floorPolicyCode, floorEffectiveDate));
        }

        if (perFloorResults.Count == 0)
        {
            var reasons = string.Join("; ", skippedFloors.Select(s => $"floor {s.FloorId}: {s.Reason}"));
            return new OccupationComputation(
                OccupationTaxResult.Rejected(
                    propertyId,
                    $"No floor could be computed (all {floors.Count} floors skipped) - {reasons}"),
                PolicyCodes.ElectricBill,
                null,
                currentFy);
        }

        if (skippedFloors.Count > 0)
        {
            _logger.LogWarning(
                "Occupation Tax computed for property {PropertyId} from {ComputedCount}/{TotalCount} floors; " +
                "{SkippedCount} floor(s) had no certificate coverage and were excluded: {SkippedFloorIds}.",
                propertyId, perFloorResults.Count, floors.Count, skippedFloors.Count,
                string.Join(",", skippedFloors.Select(s => s.FloorId)));
        }

        // The Tax Details grid shows one applicable-policy row after NETTAX for the whole
        // property (TaxPersistenceMode is PROPERTY_AGGREGATED only) -- when floors disagree on
        // which policy code applies, FLOOR_POLICY_DISPLAY_RULE picks how the ONE representative
        // row is chosen.
        var representative = ResolveRepresentative(
            guideline, computedFloors, propertyWiseCertificates, priorityOrder, options, currentFy, propertyId);

        var aggregated = AggregateFloorResults(propertyId, perFloorResults);
        return new OccupationComputation(
            aggregated,
            representative.PolicyCode,
            representative.EffectiveDate,
            currentFy,
            representativeYearPolicyCodes.Count > 0 ? representativeYearPolicyCodes : null,
            Options: options);
    }

    /// <summary>
    /// Best-available floor area for "biggest floor" comparisons: built-up area preferred (sq
    /// metre, then sq feet), falling back to carpet area, then 0 if neither is recorded.
    /// </summary>
    private static double FloorArea(PropertyDetailsEntity floor) =>
        floor.BuiltupAreaSqMeter ?? floor.BuiltupAreaSqFeet ?? floor.CarpetAreaSqMeter ?? floor.CarpetAreaSqFeet ?? 0d;

    /// <summary>
    /// FLOOR_POLICY_DISPLAY_RULE picks which single policy code/date represents the whole property
    /// on the Tax Details grid when floors disagree. BIGGEST_AREA_FLOOR_POLICY (default): the
    /// biggest floor by built-up area (falling back to carpet area, then floor order for ties/all-
    /// null areas) wins, per the original business rule. PROPERTY_POLICY_ONLY: the property-wise
    /// certificate's own resolution is used instead, regardless of any floor's area -- if the
    /// property has no property-wise certificate of its own to resolve, this falls back to the
    /// biggest-floor representative (there is nothing else to represent it with) and logs why. An
    /// unsupported value logs a warning and falls back to BIGGEST_AREA_FLOOR_POLICY.
    /// </summary>
    private (PropertyDetailsEntity Floor, string PolicyCode, DateTime? EffectiveDate) ResolveRepresentative(
        CertificateTaxGuidelineSettings guideline,
        List<(PropertyDetailsEntity Floor, string PolicyCode, DateTime? EffectiveDate)> computedFloors,
        List<PropertyCertificateEntity> propertyWiseCertificates,
        string[] priorityOrder,
        OccupationTaxOptions options,
        FinanceYear currentFy,
        int propertyId)
    {
        // A formal certificate (CC/OC) governing ANY floor must never be masked by a bigger floor
        // that only has the Electric-Bill fallback -- mirrors the single-floor DATE_PRIORITY rule
        // (CC/OC always outrank Electric Bill) at the property-level summary too. Without this, a
        // small CC-governed floor's family never appears anywhere in the persisted, property-
        // aggregated PolicyTaxDetails row when a bigger floor falls back to property-wide Electric
        // Bill -- reported as "CC applied but Electric Bill still shows" on the Tax Details grid.
        // Area only breaks ties WITHIN the same tier (e.g. two CC floors, or two Electric-Bill floors).
        var formalCertFloors = computedFloors
            .Where(f => f.PolicyCode == PolicyCodes.Cc || f.PolicyCode == PolicyCodes.Oc)
            .ToList();
        var candidateFloors = formalCertFloors.Count > 0 ? formalCertFloors : computedFloors;

        var biggestFloor = candidateFloors.OrderByDescending(f => FloorArea(f.Floor)).First();

        switch (guideline.FloorPolicyDisplayRule)
        {
            case "BIGGEST_AREA_FLOOR_POLICY":
                return biggestFloor;

            case "PROPERTY_POLICY_ONLY":
                var (oc, cc, bill, rejectionReason) = ExtractDates(propertyWiseCertificates, guideline);
                var resolved = rejectionReason == null
                    ? ResolveAndCompute(oc, cc, bill, priorityOrder, options, guideline, currentFy, propertyId)
                    : null;

                if (resolved == null)
                {
                    _logger.LogInformation(
                        "PTIS.CertificateTaxGuideline.FLOOR_POLICY_DISPLAY_RULE is PROPERTY_POLICY_ONLY for " +
                        "property {PropertyId}, but there is no property-wise certificate to resolve a " +
                        "representative policy from -- falling back to the biggest-floor representative.",
                        propertyId);
                    return biggestFloor;
                }

                return (biggestFloor.Floor, resolved.PolicyCode, resolved.EffectiveDate);

            default:
                _logger.LogWarning(
                    "PTIS.CertificateTaxGuideline.FLOOR_POLICY_DISPLAY_RULE is '{Rule}', which is not " +
                    "supported -- only BIGGEST_AREA_FLOOR_POLICY and PROPERTY_POLICY_ONLY are implemented. " +
                    "Falling back to BIGGEST_AREA_FLOOR_POLICY for property {PropertyId}.",
                    guideline.FloorPolicyDisplayRule, propertyId);
                return biggestFloor;
        }
    }

    /// <summary>Loads the PTIS.CertificateTaxGuideline settings this engine run should use.</summary>
    private Task<CertificateTaxGuidelineSettings> LoadGuidelineAsync(CancellationToken cancellationToken) =>
        _guidelineReader.GetActiveSettingsAsync(cancellationToken);

    /// <summary>
    /// Resolves which PolicyCodeMaster code string represents "full year" vs "partial year" for
    /// each certificate family (CC/OC/ELECTRIC_BILL), plus their PolicyCodeMaster ids -- entirely
    /// guideline-driven (CC_FULL_POLICY_CODE/CC_PARTIAL_POLICY_CODE, etc.), not a hardcoded C#
    /// mapping, so a DBA-configured rename takes effect without a code change. PolicyCodes.Oc/.Cc/
    /// .ElectricBill remain fixed technical family keys (they identify which CERTIFICATE governs a
    /// year, not which POLICY CODE STRING is selected). Shared by <see cref="SaveTaxesAsync"/> and
    /// <see cref="CleanupStaleCertificateTaxRowsAsync"/> so both always agree on the current set of
    /// certificate-tax policy codes.
    /// </summary>
    /// <remarks>
    /// Resolves best-effort (<see cref="IPolicyCodeLookupService.GetExistingIdsAsync"/>, not
    /// <c>GetIdsAsync</c>): one family's PolicyCodeMaster rows being unseeded/misconfigured (e.g.
    /// OC never seeded on a property that has only ever used CC certificates) must not abort a
    /// computation for a DIFFERENT, correctly-configured family. <see cref="SaveTaxesAsync"/>
    /// separately validates that the family the current computation actually resolved to is fully
    /// present, so a genuinely-needed family still fails loudly.
    /// </remarks>
    private async Task<(Dictionary<string, (string Full, string Partial)> FamilyPolicyCodes, Dictionary<string, int> FamilyPolicyCodeIds)>
        ResolveFamilyPolicyCodesAsync(CertificateTaxGuidelineSettings guideline, CancellationToken cancellationToken)
    {
        var familyPolicyCodes = new Dictionary<string, (string Full, string Partial)>
        {
            [PolicyCodes.Oc] = (guideline.OcFullPolicyCode, guideline.OcPartialPolicyCode),
            [PolicyCodes.Cc] = (guideline.CcFullPolicyCode, guideline.CcPartialPolicyCode),
            [PolicyCodes.ElectricBill] = (guideline.ElectricBillFullPolicyCode, guideline.ElectricBillPartialPolicyCode),
        };

        var familyPolicyCodeIds = await _policyCodeLookup.GetExistingIdsAsync(
            familyPolicyCodes.Values.SelectMany(v => new[] { v.Full, v.Partial }).Distinct(),
            cancellationToken);

        foreach (var (family, (full, partial)) in familyPolicyCodes)
        {
            if (!familyPolicyCodeIds.ContainsKey(full) || !familyPolicyCodeIds.ContainsKey(partial))
            {
                _logger.LogWarning(
                    "PTIS.PolicyCodeMaster is missing a row for the '{Family}' certificate-tax family " +
                    "(expected codes '{Full}'/'{Partial}' from PTIS.CertificateTaxGuideline). Certificates " +
                    "resolving to this family will fail when persisted; other families are unaffected.",
                    family, full, partial);
            }
        }

        return (familyPolicyCodes, familyPolicyCodeIds);
    }

    /// <summary>
    /// Throws a clear, specific error if the certificate-tax family this computation actually
    /// resolved to (its own current-year family, plus any per-year family override from a CC/OC
    /// split) is missing from <paramref name="familyPolicyCodeIds"/> -- i.e. genuinely blocks
    /// persistence -- as opposed to an unrelated family being unresolvable, which
    /// <see cref="ResolveFamilyPolicyCodesAsync"/> already logged and otherwise ignores.
    /// </summary>
    private static void ValidateNeededFamilyPolicyCodesResolved(
        OccupationComputation computation,
        Dictionary<string, (string Full, string Partial)> familyPolicyCodes,
        Dictionary<string, int> familyPolicyCodeIds)
    {
        var neededFamilies = new HashSet<string> { computation.PolicyCode };
        if (computation.YearPolicyCodes != null)
        {
            foreach (var family in computation.YearPolicyCodes.Values)
            {
                neededFamilies.Add(family);
            }
        }

        foreach (var family in neededFamilies)
        {
            if (!familyPolicyCodes.TryGetValue(family, out var codes))
            {
                continue;
            }

            if (!familyPolicyCodeIds.ContainsKey(codes.Full) || !familyPolicyCodeIds.ContainsKey(codes.Partial))
            {
                throw new InvalidOperationException(
                    $"Cannot persist certificate tax: PTIS.PolicyCodeMaster has no active row for the " +
                    $"'{family}' family's configured codes ('{codes.Full}'/'{codes.Partial}' from " +
                    $"PTIS.CertificateTaxGuideline). Seed these rows before certificates of this type can be applied.");
            }
        }
    }

    /// <summary>
    /// Removes every certificate-tax-family row (CC/PARTIAL_CC, OC/PARTIAL_OC,
    /// ELECTRIC_BILL/PARTIAL_ELECTRIC_BILL, guideline-driven policy code names resolved via
    /// <see cref="ResolveFamilyPolicyCodesAsync"/>) for this property, from PolicyTaxDetails,
    /// TransMast, and -- for whichever (FinanceYear, TaxId) slots those PolicyTaxDetails rows
    /// occupied -- TaxPendingDetailsRetro/TaxPendingDetails too. Called from <see cref="ApplyAsync"/>
    /// when a run's computation is invalid because there is genuinely no certificate to compute
    /// from any more (certificate deleted, date cleared, certificate number cleared with
    /// CERTIFICATE_REQUIRE_NO_AND_DATE on, invalid CC/OC order rejected, etc.) -- a property/floor
    /// that had a valid CC/OC/Electric Bill row before must not keep showing that stale row on the
    /// Tax Details grid, nor keep a now-baseless pending/arrears row in the two pending tables,
    /// just because there is no new row to upsert in its place. NETTAX and every other policy
    /// family are never touched here, since only rows tagged under a certificate-tax family
    /// PolicyCodeId are ever selected. A TaxPendingDetails row with PendingFixed=true (set after a
    /// property combine) is left untouched, same as SaveTaxesAsync's own cleanup.
    /// </summary>
    private async Task CleanupStaleCertificateTaxRowsAsync(
        int propertyId, int userId, CertificateTaxGuidelineSettings guideline, CancellationToken cancellationToken)
    {
        if (!guideline.SaveInPolicyTaxDetails && !guideline.SaveInTransMast)
        {
            return;
        }

        var now = DateTime.Now;
        var changed = false;

        var stalePolicyTaxDetails = new List<PolicyTaxDetailsEntity>();
        if (guideline.SaveInPolicyTaxDetails)
        {
            var (_, familyPolicyCodeIds) = await ResolveFamilyPolicyCodesAsync(guideline, cancellationToken);
            var familyPolicyCodeIdValues = familyPolicyCodeIds.Values.ToList();

            stalePolicyTaxDetails = await _policyTaxDetailsRepository.GetQueryable()
                .Where(pt => pt.PropertyId == propertyId &&
                             familyPolicyCodeIdValues.Contains(pt.PolicyCodeId) &&
                             pt.IsActive && !pt.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            foreach (var pt in stalePolicyTaxDetails)
            {
                pt.IsActive = false;
                pt.MarkedForDeletion = true;
                pt.MarkedForDeletionDate = now;
                pt.UpdatedBy = userId;
                pt.UpdatedDate = now;
                await _policyTaxDetailsRepository.UpdateAsync(pt, cancellationToken);
                changed = true;
            }
        }

        if (guideline.SaveInTransMast && stalePolicyTaxDetails.Count > 0)
        {
            // TransMast has no PolicyCodeId column, so "which rows belong to certificate tax" is
            // derived from the TaxIds the just-cleared PolicyTaxDetails rows covered. No year
            // cross-reference is needed any more: TransMast only ever holds the CURRENT finance
            // year for certificate taxes (see SaveTaxesAsync), so every active row for these TaxIds
            // is, by construction, the stale current-year row.
            var staleTaxIds = stalePolicyTaxDetails.Select(pt => pt.TaxId).Distinct().ToList();

            var staleTransMasts = await _transMastRepository.GetQueryable()
                .Where(tm => tm.PropertyId == propertyId &&
                             staleTaxIds.Contains(tm.TaxId) &&
                             tm.CalculationType == "RV" &&
                             tm.IsActive && !tm.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            foreach (var tm in staleTransMasts)
            {
                tm.IsActive = false;
                tm.MarkedForDeletion = true;
                tm.MarkedForDeletionDate = now;
                tm.UpdatedBy = userId;
                tm.UpdatedDate = now;
                await _transMastRepository.UpdateAsync(tm, cancellationToken);
                changed = true;
            }

            // These TaxIds may also hold now-baseless rows in either pending table -- a TaxId that
            // never had one is simply not matched below. Every active retro/pending row for these
            // TaxIds is cleared, regardless of which pending year(s) it covers.
            var staleTaxPendingRetro = await _taxPendingDetailsRetroRepository.GetQueryable()
                .Where(tpr => tpr.PropertyId == propertyId &&
                              staleTaxIds.Contains(tpr.TaxId) &&
                              tpr.IsActive && !tpr.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            foreach (var tpr in staleTaxPendingRetro)
            {
                tpr.IsActive = false;
                tpr.MarkedForDeletion = true;
                tpr.MarkedForDeletionDate = now;
                tpr.UpdatedBy = userId;
                tpr.UpdatedDate = now;
                await _taxPendingDetailsRetroRepository.UpdateAsync(tpr, cancellationToken);
                changed = true;
            }

            var staleTaxPending = await _taxPendingDetailsRepository.GetQueryable()
                .Where(tp => tp.PropertyId == propertyId &&
                             staleTaxIds.Contains(tp.TaxId) &&
                             tp.IsActive && !tp.MarkedForDeletion && !tp.PendingFixed)
                .ToListAsync(cancellationToken);

            foreach (var tp in staleTaxPending)
            {
                tp.IsActive = false;
                tp.MarkedForDeletion = true;
                tp.MarkedForDeletionDate = now;
                tp.UpdatedBy = userId;
                tp.UpdatedDate = now;
                await _taxPendingDetailsRepository.UpdateAsync(tp, cancellationToken);
                changed = true;
            }
        }

        if (changed)
        {
            _logger.LogInformation(
                "Cleaned up stale certificate-tax rows for property {PropertyId}: no valid CC/OC/Electric " +
                "Bill certificate remains.", propertyId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Whether floor-wise certificate rows should be considered as computation input at all.
    /// ALLOW_FLOOR_WISE_CERTIFICATE_METADATA is the sole gate (1=yes, 0=no). CERTIFICATE_TAX_SCOPE_MODE
    /// is validated (a value that's neither PROPERTY_WISE nor FLOOR_WISE logs a warning) but never
    /// gates this decision -- it describes final PERSISTENCE only, which is always
    /// property-aggregated regardless of either setting's value (see TAX_PERSISTENCE_MODE /
    /// SaveTaxesAsync). An earlier version of this method treated
    /// CERTIFICATE_TAX_SCOPE_MODE=PROPERTY_WISE as "block floor-wise input", which silently produced
    /// zero certificate tax for a floor-wise-only certificate with no property-wise fallback --
    /// confirmed wrong per explicit business correction (a floor-wise certificate must still be
    /// taxed even when persistence itself is property-wise).
    /// </summary>
    private bool ResolveUseFloorWiseCertificates(CertificateTaxGuidelineSettings guideline, int propertyId)
    {
        return guideline.AllowFloorWiseCertificateMetadata;
    }

    /// <summary>
    /// Resolves the winning real-certificate date by walking DatePriority1..4 in order and taking
    /// the first entry whose corresponding date is present. An ELECTRIC_BILL entry whose adjusted
    /// date comes back null (NoDateRule-independent ElectricBillDateRule = NO_TAX) is treated as
    /// absent and the walk continues to the next priority entry rather than stopping. Reaching a
    /// RETROSPECTIVE entry (or exhausting the list) with nothing matched means "use the
    /// no-certificate fallback" (returns null).
    /// </summary>
    private static (string PolicyCode, DateTime Date)? ResolveWinner(
        DateTime? oc, DateTime? cc, DateTime? bill, string[] priorityOrder, CertificateTaxGuidelineSettings guideline)
    {
        foreach (var entry in priorityOrder)
        {
            if (entry == PolicyCodes.Oc && oc.HasValue)
            {
                return (PolicyCodes.Oc, oc.Value);
            }

            if (entry == PolicyCodes.Cc && cc.HasValue)
            {
                return (PolicyCodes.Cc, cc.Value);
            }

            if (entry == PolicyCodes.ElectricBill && bill.HasValue)
            {
                var adjusted = AdjustElectricBillDate(bill.Value, guideline);
                if (adjusted.HasValue)
                {
                    return (PolicyCodes.ElectricBill, FloorElectricBillDate(adjusted.Value, guideline));
                }
                // ElectricBillDateRule = NO_TAX: treat as absent, keep walking the priority order.
            }

            // "RETROSPECTIVE" is a CertificateTaxGuideline.DatePriority sentinel value, not a
            // PolicyCodeMaster row -- it means "no real certificate date matched, use the
            // no-certificate fallback" (which itself resolves to the ElectricBill family).
            if (entry == "RETROSPECTIVE")
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Adjusts an Electric Bill certificate date per CertificateTaxGuideline.ElectricBillDateRule.
    /// NO_TAX drops the date entirely (treated as absent). ADD_MONTHS shifts the date forward by
    /// ElectricBillAddMonths before use. EXACT_DATE and FROM_FY_START both currently hand the raw
    /// date to the engine unchanged: OccupationTaxEngine's Electricity-Bill condition always
    /// normalizes onset to that date's finance-year start (BR2) — day-accurate EXACT_DATE billing
    /// is not implemented at the engine level in this change, to avoid touching its
    /// already-approved BR1-BR7 golden figures.
    /// </summary>
    private static DateTime? AdjustElectricBillDate(DateTime billDate, CertificateTaxGuidelineSettings guideline)
    {
        if (string.Equals(guideline.ElectricBillDateRule, "NO_TAX", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var dateToUse = string.Equals(guideline.ElectricBillDateRule, "ADD_MONTHS", StringComparison.OrdinalIgnoreCase)
            ? billDate.AddMonths(guideline.ElectricBillAddMonths)
            : billDate;

        // Phase 3 Rule 1 (Normalization): Shift bill date to the start of its Financial Year (01-Apr-YYYY)
        var fy = FinanceYear.ForDate(dateToUse, guideline.FinancialYearStartMonth, guideline.FinancialYearStartDay);
        return fy.Start;
    }

    private static string[] BuildPriorityOrder(CertificateTaxGuidelineSettings guideline)
    {
        var rawPriorities = new[]
        {
            guideline.DatePriority1,
            guideline.DatePriority2,
            guideline.DatePriority3,
            guideline.DatePriority4
        };

        var resolved = rawPriorities
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToUpperInvariant() switch
            {
                "CC" or "CC DATE" or "COMMENCEMENT CERTIFICATE" or "COMPLETION CERTIFICATE" => PolicyCodes.Cc,
                "OC" or "OC DATE" or "OCCUPANCY CERTIFICATE" or "OCCUPATION CERTIFICATE" => PolicyCodes.Oc,
                "ELECTRIC_BILL" or "ELECTRIC BILL" or "ELECTRIC BILL DATE" or "ELECTRICITY BILL" => PolicyCodes.ElectricBill,
                "RETROSPECTIVE" or "RETROSPECTIVE (NO DATE)" or "NO DATE" => "RETROSPECTIVE",
                _ => p.Trim().ToUpperInvariant()
            })
            .Distinct()
            .ToArray();

        return resolved.Length > 0
            ? resolved
            : new[] { PolicyCodes.Cc, PolicyCodes.Oc, PolicyCodes.ElectricBill, "RETROSPECTIVE" };
    }

    /// <summary>
    /// Phase 3 Rule 2 (Hard Limit/Floor): Floors an Electric Bill effective date at minimum financial year (defaults to 2016).
    /// </summary>
    private static DateTime FloorElectricBillDate(DateTime effectiveDate, CertificateTaxGuidelineSettings guideline)
    {
        var floorYear = guideline.ElectricBillMinimumFinancialYear > 0 ? guideline.ElectricBillMinimumFinancialYear : 2016;
        var minimumDate = new DateTime(
            floorYear, guideline.FinancialYearStartMonth, guideline.FinancialYearStartDay);

        return effectiveDate < minimumDate ? minimumDate : effectiveDate;
    }

    /// <summary>
    /// Resolves the applicable certificate(s) for a single property/floor scope and runs the
    /// engine, handling the CC+OC-both-present case (gap comparison, invalid date order, merge)
    /// separately from the single-condition case (OC-only, CC-only, Bill-only). Returns null when
    /// there is no certificate to compute from at all (caller applies the no-certificate fallback).
    /// </summary>
    private ResolvedComputation? ResolveAndCompute(
        DateTime? oc, DateTime? cc, DateTime? bill,
        string[] priorityOrder, OccupationTaxOptions options, CertificateTaxGuidelineSettings guideline,
        FinanceYear currentFy, int propertyId)
    {
        // ENABLE_CC_TO_OC_SPLIT = 0 disables the whole CC/OC gap-comparison-and-merge machinery
        // (CC_OC_GAP_WITHIN_ACTION, CC_OC_GAP_EXCEEDED_ACTION, INVALID_CC_OC_DATE_ORDER_ACTION,
        // the CC-then-OC merge) regardless of what those settings say -- both dates present just
        // falls straight through to the single DATE_PRIORITY winner below, exactly as if only one
        // of CC/OC existed.
        if (oc.HasValue && cc.HasValue && guideline.EnableCcToOcSplit)
        {
            return ResolveCcOcCombination(cc.Value, oc.Value, priorityOrder, options, guideline, currentFy, propertyId);
        }

        var winner = ResolveWinner(oc, cc, bill, priorityOrder, guideline);
        return winner == null
            ? null
            : ComputeSingleCondition(winner.Value.PolicyCode, winner.Value.Date, options, guideline, currentFy, propertyId);
    }

    /// <summary>
    /// Handles a property/floor carrying BOTH a CC and an OC certificate: validates date order
    /// (INVALID_CC_OC_DATE_ORDER_ACTION), then measures the CC-to-OC gap
    /// (IGNORE_CC_TO_OC_WITHIN_VALUE/TYPE, CC_OC_GAP_COMPARISON) to pick CC_OC_GAP_WITHIN_ACTION or
    /// CC_OC_GAP_EXCEEDED_ACTION -- APPLY_OC_ONLY discards CC entirely; APPLY_CC_AND_OC and
    /// APPLY_CC_THEN_OC both resolve to the same CC-then-OC merge (see
    /// <see cref="ComputeCcThenOcMerge"/>).
    /// </summary>
    private ResolvedComputation? ResolveCcOcCombination(
        DateTime ccDate, DateTime ocDate, string[] priorityOrder, OccupationTaxOptions options,
        CertificateTaxGuidelineSettings guideline, FinanceYear currentFy, int propertyId)
    {
        if (ocDate < ccDate)
        {
            if (string.Equals(guideline.InvalidCcOcDateOrderAction, "REJECT", StringComparison.OrdinalIgnoreCase))
            {
                return new ResolvedComputation(
                    OccupationTaxResult.Rejected(propertyId,
                        $"OC date ({ocDate:d}) is earlier than CC date ({ccDate:d})."),
                    PolicyCodes.ElectricBill, null);
            }

            if (string.Equals(guideline.InvalidCcOcDateOrderAction, "IGNORE_INVALID_DATE", StringComparison.OrdinalIgnoreCase))
            {
                // Unlike USE_PRIORITY_AND_LOG, this ignores the invalid OC date specifically and
                // continues with CC directly (CC is guaranteed present in this branch) rather than
                // consulting DATE_PRIORITY -- an info/debug note, not a warning, since nothing here
                // is actually wrong from this action's point of view.
                _logger.LogInformation(
                    "Property {PropertyId}: OC date {OcDate:d} is earlier than CC date {CcDate:d}; " +
                    "ignoring the OC date and continuing with CC per IGNORE_INVALID_DATE.",
                    propertyId, ocDate, ccDate);
                return ComputeSingleCondition(PolicyCodes.Cc, ccDate, options, guideline, currentFy, propertyId);
            }

            // USE_PRIORITY_AND_LOG (default): log a warning, then fall back to a single-priority
            // winner rather than attempting the CC-then-OC merge on out-of-order dates.
            _logger.LogWarning(
                "Property {PropertyId}: OC date {OcDate:d} is earlier than CC date {CcDate:d}; " +
                "falling back to configured date priority.",
                propertyId, ocDate, ccDate);
            var priorityWinner = ResolveWinner(ocDate, ccDate, null, priorityOrder, guideline);
            return priorityWinner == null
                ? null
                : ComputeSingleCondition(priorityWinner.Value.PolicyCode, priorityWinner.Value.Date, options, guideline, currentFy, propertyId);
        }

        // Phase 2: CC & OC Rules (Gap Calculation)
        var gapDays = (ocDate - ccDate).Days;
        var thresholdValue = guideline.IgnoreCcToOcWithinValue > 0 ? guideline.IgnoreCcToOcWithinValue : 6;
        var thresholdUnit = string.IsNullOrWhiteSpace(guideline.IgnoreCcToOcWithinType) ? "MONTHS" : guideline.IgnoreCcToOcWithinType.ToUpperInvariant();

        var compResult = CompareGap(gapDays, thresholdValue, thresholdUnit);

        var isWithinThreshold = (guideline.CcOcGapComparison ?? string.Empty).ToUpperInvariant() switch
        {
            "LESS_THAN" => compResult < 0,
            "EQUAL" => compResult == 0,
            "GREATER_THAN" => compResult > 0,
            "GREATER_THAN_OR_EQUAL" => compResult >= 0,
            _ => compResult <= 0, // LESS_THAN_OR_EQUAL (default)
        };

        var action = isWithinThreshold
            ? (string.IsNullOrWhiteSpace(guideline.CcOcGapWithinAction) ? "APPLY_OC_ONLY" : guideline.CcOcGapWithinAction)
            : (string.IsNullOrWhiteSpace(guideline.CcOcGapExceededAction) ? "APPLY_CC_THEN_OC" : guideline.CcOcGapExceededAction);

        if (string.Equals(action, "APPLY_OC_ONLY", StringComparison.OrdinalIgnoreCase))
        {
            return ComputeSingleCondition(PolicyCodes.Oc, ocDate, options, guideline, currentFy, propertyId);
        }

        if (string.Equals(action, "APPLY_CC_ONLY", StringComparison.OrdinalIgnoreCase))
        {
            return ComputeSingleCondition(PolicyCodes.Cc, ccDate, options, guideline, currentFy, propertyId);
        }

        // APPLY_CC_THEN_OC or APPLY_CC_AND_OC: split billing (CC date to OC date Under Construction, OC date onwards Completed Building).
        return ComputeCcThenOcMerge(ccDate, ocDate, options, guideline, currentFy, propertyId);
    }

    /// <summary>Converts a CC-to-OC day gap into the configured unit and compares it to the threshold.</summary>
    private static int CompareGap(int gapDays, int thresholdValue, string unit)
    {
        var gapInUnits = unit switch
        {
            "DAYS" => gapDays,
            "YEARS" => gapDays / 365,
            _ => gapDays / 30, // MONTHS (default)
        };
        return gapInUnits.CompareTo(thresholdValue);
    }

    /// <summary>
    /// Runs the engine for a single resolved condition (OC-only, CC-only, or Bill-only).
    /// CC_ONLY_ACTION/OC_ONLY_ACTION = NO_TAX short-circuits before the engine ever runs.
    /// </summary>
    private ResolvedComputation? ComputeSingleCondition(
        string policyCode, DateTime date, OccupationTaxOptions options, CertificateTaxGuidelineSettings guideline,
        FinanceYear currentFy, int propertyId)
    {
        if (policyCode == PolicyCodes.Cc && string.Equals(guideline.CcOnlyAction, "NO_TAX", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (policyCode == PolicyCodes.Oc && string.Equals(guideline.OcOnlyAction, "NO_TAX", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalizedDate = NormalizeOnsetForCurrentYearProration(date, currentFy, guideline);
        var input = BuildInput(propertyId, policyCode, normalizedDate, options, guideline);
        var result = _engine.Compute(input, currentFy);
        result = ApplyOcPeriodMultiplier(result, policyCode, guideline);
        result = ApplyCcPeriodMultiplier(result, policyCode, guideline);
        result = ApplyElectricBillMultiplier(result, policyCode, guideline);
        return new ResolvedComputation(result, policyCode, date);
    }

    /// <summary>
    /// CC governs every finance year strictly before OC's onset finance year in full; OC governs
    /// its own onset year onward. Implemented by invoking the unchanged pure engine TWICE: once
    /// normally for OC, and once for CC with a SYNTHETIC "current finance year" set to the year
    /// immediately before OC's onset year -- this makes the engine treat "CC's last FULL year" as
    /// its own current year and everything older as CC's own (capped) retro years, with no new
    /// engine capability required.
    ///
    /// CC also governs a CARRYOVER portion of the OC onset year itself, from wherever CC's own
    /// coverage effectively starts within that year up to the day before OC's date:
    /// <c>ccDate</c> itself when CC's and OC's onset dates land in the SAME finance year (business
    /// rule, "Sudharit Vajavi Bhade Ambalbajavani" point 5), or that year's own FY-start when CC's
    /// onset finance year is strictly earlier -- CC has already been governing continuously since
    /// before this year began (the second engine call above only covers years strictly before the
    /// OC onset year, so without this carryover merge the days between that year's FY-start and
    /// OC's date would receive no tax at all whenever OC's date isn't exactly the FY start).
    /// OC governs ocDate..FY-end at OC_PERIOD_MULTIPLIER (already applied to ocResult above) -- see
    /// <see cref="BuildDateRangeYear"/> for the CC carryover portion's day-bounded proration. The
    /// two portions are summed into ONE merged year result and persisted as a single
    /// PolicyTaxDetails row (tagged OC, consistent with this method's existing "current governing
    /// state" tagging) so TransMast keeps its one-row-per-property/year/tax invariant -- a
    /// deliberate simplification: the guideline only says PolicyTaxDetails "may" carry separate
    /// CC/OC audit rows for this year, not that it must, and splitting the persisted row itself
    /// would need a wider change to SaveTaxesAsync's one-contributing-row-per-year assumption.
    /// </summary>
    private ResolvedComputation ComputeCcThenOcMerge(
        DateTime ccDate, DateTime ocDate, OccupationTaxOptions options, CertificateTaxGuidelineSettings guideline,
        FinanceYear currentFy, int propertyId)
    {
        var normalizedOcDate = NormalizeOnsetForCurrentYearProration(ocDate, currentFy, guideline);
        var ocInput = BuildInput(propertyId, PolicyCodes.Oc, normalizedOcDate, options, guideline);
        var ocResult = ApplyOcPeriodMultiplier(_engine.Compute(ocInput, currentFy), PolicyCodes.Oc, guideline);

        if (!ocResult.IsValid || ocResult.CurrentYear == null)
        {
            return new ResolvedComputation(ocResult, PolicyCodes.Oc, ocDate);
        }

        var yearPolicyCodes = new Dictionary<int, string> { [ocResult.CurrentYear.FinanceYear] = PolicyCodes.Oc };
        foreach (var year in ocResult.RetroYears)
        {
            yearPolicyCodes[year.FinanceYear] = PolicyCodes.Oc;
        }

        var ocOnsetFy = FinanceYear.ForDate(ocDate, currentFy.StartMonth, currentFy.StartDay);
        var ccOnsetFy = FinanceYear.ForDate(ccDate, currentFy.StartMonth, currentFy.StartDay);

        var mergedCurrentYear = ocResult.CurrentYear;
        var mergedRetroYears = ocResult.RetroYears;

        if (ccOnsetFy.StartYear < ocOnsetFy.StartYear)
        {
            var ccAnchorFy = new FinanceYear(ocOnsetFy.StartYear - 1, currentFy.StartMonth, currentFy.StartDay);
            var ccInput = BuildInput(propertyId, PolicyCodes.Cc, ccDate, options, guideline);
            var ccResult = ApplyCcPeriodMultiplier(_engine.Compute(ccInput, ccAnchorFy), PolicyCodes.Cc, guideline);

            if (ccResult.IsValid && ccResult.CurrentYear != null)
            {
                var ccYears = new List<OccupationTaxYearResult>(ccResult.RetroYears) { ccResult.CurrentYear };
                foreach (var year in ccYears)
                {
                    yearPolicyCodes[year.FinanceYear] = PolicyCodes.Cc;
                }

                var ccYearDict = ccYears.ToDictionary(y => y.FinanceYear);
                mergedRetroYears = mergedRetroYears
                    .Select(y => ccYearDict.TryGetValue(y.FinanceYear, out var ccY) ? ccY : y)
                    .Concat(ccYears.Where(cy => !mergedRetroYears.Any(my => my.FinanceYear == cy.FinanceYear)))
                    .Where(y => y.FinanceYear != mergedCurrentYear.FinanceYear)
                    .OrderBy(y => y.FinanceYear)
                    .ToList();
            }
        }

        // CC's carryover portion within the OC onset year: from ccDate (same-FY case) or that
        // year's own FY-start (CC onset FY strictly earlier -- covered in full by ccYears above
        // only for years BEFORE this one) up to the day before OC's date.
        var ccStartInOcOnsetYear = ccOnsetFy.StartYear == ocOnsetFy.StartYear ? ccDate : ocOnsetFy.Start;
        var ccDaysInOcOnsetYear = (ocDate - ccStartInOcOnsetYear).Days;

        if (ccDaysInOcOnsetYear > 0)
        {
            var ccPortion = ScaleYearResult(
                BuildDateRangeYear(options, ocOnsetFy, ccDaysInOcOnsetYear),
                guideline.CCPeriodMultiplier);

            OccupationTaxYearResult MergeCcPortionInto(OccupationTaxYearResult ocYear) => new()
            {
                FinanceYear = ocYear.FinanceYear,
                FinanceYearStart = ocYear.FinanceYearStart,
                FinanceYearEnd = ocYear.FinanceYearEnd,
                GeneralTax = ocYear.GeneralTax + ccPortion.GeneralTax,
                ComponentTax = ocYear.ComponentTax + ccPortion.ComponentTax,
                ComponentCount = ocYear.ComponentCount,
                IsProrated = true,
                ChargeableDays = ocYear.ChargeableDays + ccPortion.ChargeableDays,
                LeapAddbackApplied = false,
            };

            // The OC onset year is usually mergedCurrentYear, but a backdated correction can land
            // it in mergedRetroYears instead (e.g. discovering in FY2026 that CC+OC both fell in
            // FY2023) -- locate it wherever it actually is.
            if (mergedCurrentYear.FinanceYear == ocOnsetFy.StartYear)
            {
                mergedCurrentYear = MergeCcPortionInto(mergedCurrentYear);
            }
            else
            {
                mergedRetroYears = mergedRetroYears
                    .Select(y => y.FinanceYear == ocOnsetFy.StartYear ? MergeCcPortionInto(y) : y)
                    .ToList();
            }
        }

        var merged = new OccupationTaxResult
        {
            PropertyId = propertyId,
            IsValid = true,
            Condition = OccupationCondition.OccupationCertificate,
            CurrentYear = mergedCurrentYear,
            RetroYears = mergedRetroYears,
        };

        return new ResolvedComputation(merged, PolicyCodes.Oc, ocDate, yearPolicyCodes);
    }

    /// <summary>
    /// Builds a day-count-bounded prorated year result for an explicit chargeable-day count,
    /// unlike the pure engine's onset-to-FY-end proration -- used for the CC portion of a
    /// same-finance-year CC-then-OC split, where CC's period ends the day before OC's onset
    /// rather than running to the finance year's end.
    /// </summary>
    private static OccupationTaxYearResult BuildDateRangeYear(OccupationTaxOptions options, FinanceYear fy, int chargeableDays)
    {
        var componentTotal = options.AnnualNetTax - options.GeneralTaxPortion;
        var perComponent = options.ComponentCount > 0 ? componentTotal / options.ComponentCount : 0m;
        var factor = (decimal)chargeableDays / FinanceYear.ProrationBasisDays;

        return new OccupationTaxYearResult
        {
            FinanceYear = fy.StartYear,
            FinanceYearStart = fy.Start,
            FinanceYearEnd = fy.End,
            GeneralTax = Math.Round(options.GeneralTaxPortion * factor, 0, MidpointRounding.AwayFromZero),
            ComponentTax = Math.Round(perComponent * factor, 0, MidpointRounding.AwayFromZero),
            ComponentCount = options.ComponentCount,
            IsProrated = true,
            ChargeableDays = chargeableDays,
            LeapAddbackApplied = false,
        };
    }

    /// <summary>
    /// Normalizes the onset date fed to the engine for CURRENT_YEAR_PRORATION_START_RULE
    /// (EXACT_DATE/MONTH_START/FULL_YEAR) and ENABLE_CURRENT_YEAR_PRORATION, reusing the engine's
    /// existing exact-day proration math rather than adding a new formula. Only applies when the
    /// onset falls within the CURRENT finance year -- retrospective/historical onset years are
    /// unaffected by these current-year-only settings.
    /// </summary>
    private DateTime NormalizeOnsetForCurrentYearProration(
        DateTime onsetDate, FinanceYear currentFy, CertificateTaxGuidelineSettings guideline)
    {
        var onsetFy = FinanceYear.ForDate(onsetDate, currentFy.StartMonth, currentFy.StartDay);
        if (onsetFy.StartYear != currentFy.StartYear)
        {
            return onsetDate;
        }

        if (!guideline.EnableCurrentYearProration)
        {
            return currentFy.Start;
        }

        var effectiveRule = ResolveEffectiveProrationStartRule(guideline);
        return effectiveRule switch
        {
            "MONTH_START" => new DateTime(onsetDate.Year, onsetDate.Month, 1),
            "FULL_YEAR" => currentFy.Start,
            _ => onsetDate, // EXACT_DATE (default)
        };
    }

    /// <summary>
    /// PRORATION_METHOD (DAILY/MONTHLY/FULL_YEAR) is the primary driver of proration behavior --
    /// it maps directly onto CURRENT_YEAR_PRORATION_START_RULE's EXACT_DATE/MONTH_START/FULL_YEAR
    /// values, since the two settings describe the same concept. CURRENT_YEAR_PRORATION_START_RULE
    /// is still read and validated for consistency: a mismatch is logged loudly (not silently
    /// ignored) and the PRORATION_METHOD-derived behavior wins, so the guideline table can't end up
    /// with two settings that quietly disagree.
    /// </summary>
    private string ResolveEffectiveProrationStartRule(CertificateTaxGuidelineSettings guideline)
    {
        var derivedFromMethod = guideline.ProrationMethod switch
        {
            "DAILY" => "EXACT_DATE",
            "MONTHLY" => "MONTH_START",
            "FULL_YEAR" => "FULL_YEAR",
            _ => guideline.CurrentYearProrationStartRule,
        };

        if (!string.Equals(derivedFromMethod, guideline.CurrentYearProrationStartRule, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "PTIS.CertificateTaxGuideline: PRORATION_METHOD ('{ProrationMethod}') and " +
                "CURRENT_YEAR_PRORATION_START_RULE ('{StartRule}') disagree; using the behavior implied by " +
                "PRORATION_METHOD ('{Derived}').",
                guideline.ProrationMethod, guideline.CurrentYearProrationStartRule, derivedFromMethod);
        }

        return derivedFromMethod;
    }

    /// <summary>
    /// Applies CertificateTaxGuideline.ElectricBillMultiplier as a post-hoc scale, symmetric with
    /// <see cref="ApplyOcPeriodMultiplier"/> -- the engine has no concept of an Electric Bill
    /// multiplier, so this reuses the existing ScaleResult helper rather than adding new engine math.
    /// </summary>
    private static OccupationTaxResult ApplyElectricBillMultiplier(OccupationTaxResult result, string policyCode, CertificateTaxGuidelineSettings guideline)
    {
        if (policyCode != PolicyCodes.ElectricBill || guideline.ElectricBillMultiplier == 1.0m)
        {
            return result;
        }

        return ScaleResult(result, guideline.ElectricBillMultiplier);
    }

    private static OccupationTaxInput BuildInput(
        int propertyId, string policyCode, DateTime date, OccupationTaxOptions options, CertificateTaxGuidelineSettings guideline) => new()
    {
        PropertyId = propertyId,
        OccupationCertificateDate = policyCode == PolicyCodes.Oc ? date : null,
        CompletionCertificateDate = policyCode == PolicyCodes.Cc ? date : null,
        ElectricityBillDate = policyCode == PolicyCodes.ElectricBill ? date : null,
        Options = options
    };

    /// <summary>
    /// Applies CertificateTaxGuideline.OCPeriodMultiplier as a post-hoc scale when the winning
    /// policy is OC and the multiplier isn't the 1.0 no-op default. The engine itself has no
    /// concept of an OC multiplier (only CompletionCertificateMultiplier), so this reuses the
    /// existing ScaleResult helper rather than adding new engine-level math.
    /// </summary>
    private static OccupationTaxResult ApplyOcPeriodMultiplier(OccupationTaxResult result, string policyCode, CertificateTaxGuidelineSettings guideline)
    {
        if (policyCode != PolicyCodes.Oc || guideline.OCPeriodMultiplier == 1.0m)
        {
            return result;
        }

        return ScaleResult(result, guideline.OCPeriodMultiplier);
    }

    /// <summary>
    /// Applies CertificateTaxGuideline.CC_PERIOD_MULTIPLIER as a post-hoc scale, symmetric with
    /// <see cref="ApplyOcPeriodMultiplier"/>. Replaces the previous dead path where
    /// CCPeriodMultiplier only fed <see cref="OccupationTax.OccupationTaxOptions.CompletionCertificateMultiplier"/>,
    /// a value nothing else in the engine reads.
    /// </summary>
    private static OccupationTaxResult ApplyCcPeriodMultiplier(OccupationTaxResult result, string policyCode, CertificateTaxGuidelineSettings guideline)
    {
        if (policyCode != PolicyCodes.Cc || guideline.CCPeriodMultiplier == 1.0m)
        {
            return result;
        }

        return ScaleResult(result, guideline.CCPeriodMultiplier);
    }

    /// <summary>
    /// Builds the settings that used to govern the no-certificate default-retrospective fallback
    /// from PTIS.CertificateTaxGuideline. STRICT BUSINESS RULE (2026-07-21, explicit instruction):
    /// this fallback is now hard-disabled -- see <see cref="ComputeNoCertificateFallback"/> -- so
    /// none of these values are acted on any more. The guideline codes (NO_DATE_RULE,
    /// ENABLE_RETROSPECTIVE_TAX, NO_DATE_LOOKBACK_YEARS, DEFAULT_RETROSPECTIVE_MULTIPLIER,
    /// RETROSPECTIVE_CURRENT_YEAR_COUNT, RETROSPECTIVE_PENDING_YEAR_COUNT_MODE) are still read here
    /// only so a future, explicitly-requested reintroduction of a no-date rule has this plumbing
    /// ready; do not wire them back into ComputeNoCertificateFallback without a new, equally
    /// explicit instruction, since the current one is unambiguous: no certificate date at all means
    /// no CC/OC/Electric Bill tax and no row, full stop.
    /// </summary>
    private static NoCertificateFallbackConfig BuildNoCertificateFallbackConfig(CertificateTaxGuidelineSettings guideline) => new(
        EnableRetrospectiveTax: guideline.EnableRetrospectiveTax,
        Mode: guideline.NoDateRule,
        LookbackYears: guideline.LookbackYears,
        FinancialYearStartMonth: guideline.FinancialYearStartMonth,
        FinancialYearStartDay: guideline.FinancialYearStartDay,
        Multiplier: guideline.DefaultRetrospectiveMultiplier,
        CurrentYearCount: guideline.RetrospectiveCurrentYearCount,
        PendingYearCountMode: guideline.RetrospectivePendingYearCountMode);

    /// <summary>
    /// Phase 4: Retrospective Rules (Fallback & Loop Capping).
    /// When no certificate document (Null records) is found, tax engine enters Retrospective Mode.
    /// Max_Backdate_Years = 6; Start_Year = Current_Year - 6 (e.g. 2026 - 6 = 2020).
    /// Generates tax rows only for Start_Year (2020) to Current_Year (2026).
    /// </summary>
    private ResolvedComputation? ComputeNoCertificateFallback(
        int propertyId, OccupationTaxOptions options, CertificateTaxGuidelineSettings guideline, FinanceYear currentFy)
    {
        if (!guideline.EnableRetrospectiveTax || string.Equals(guideline.NoDateRule, "NO_TAX", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var totalSpanYears = guideline.LookbackYears > 0 ? guideline.LookbackYears : 6;
        var startYear = currentFy.StartYear - (totalSpanYears - 1);
        var onsetDate = new DateTime(startYear, guideline.FinancialYearStartMonth, guideline.FinancialYearStartDay);

        var retroOptions = new OccupationTaxOptions
        {
            AnnualNetTax = options.AnnualNetTax,
            GeneralTaxPortion = options.GeneralTaxPortion,
            ComponentCount = options.ComponentCount,
            CompletionCertificateMultiplier = options.CompletionCertificateMultiplier,
            FloorDivisor = options.FloorDivisor,
            DefaultRetroLookbackYears = totalSpanYears,
            RetroCutoffDate = options.RetroCutoffDate
        };
        return ComputeSingleCondition(PolicyCodes.ElectricBill, onsetDate, retroOptions, guideline, currentFy, propertyId);
    }

    private static OccupationTaxResult ScaleResult(OccupationTaxResult result, decimal multiplier)
    {
        if (!result.IsValid)
        {
            return result;
        }

        return new OccupationTaxResult
        {
            PropertyId = result.PropertyId,
            IsValid = true,
            Condition = result.Condition,
            CurrentYear = result.CurrentYear == null ? null : ScaleYearResult(result.CurrentYear, multiplier),
            RetroYears = result.RetroYears.Select(y => ScaleYearResult(y, multiplier)).ToList()
        };
    }

    private static OccupationTaxYearResult ScaleYearResult(OccupationTaxYearResult year, decimal multiplier) => new()
    {
        FinanceYear = year.FinanceYear,
        FinanceYearStart = year.FinanceYearStart,
        FinanceYearEnd = year.FinanceYearEnd,
        GeneralTax = Math.Round(year.GeneralTax * multiplier, 0, MidpointRounding.AwayFromZero),
        ComponentTax = Math.Round(year.ComponentTax * multiplier, 0, MidpointRounding.AwayFromZero),
        ComponentCount = year.ComponentCount,
        IsProrated = year.IsProrated,
        ChargeableDays = year.ChargeableDays,
        LeapAddbackApplied = year.LeapAddbackApplied
    };

    /// <summary>PTIS.CertificateTaxGuideline-driven settings for the no-certificate fallback rule.</summary>
    private sealed record NoCertificateFallbackConfig(
        bool EnableRetrospectiveTax, string Mode, int LookbackYears, int FinancialYearStartMonth, int FinancialYearStartDay,
        decimal Multiplier, int CurrentYearCount, string PendingYearCountMode);

    /// <summary>
    /// Sums each floor's current-year and retro-year amounts by finance year into one
    /// property-level result (TransMast has no per-floor column, so per-floor results must be
    /// aggregated before persisting).
    /// </summary>
    private static OccupationTaxResult AggregateFloorResults(int propertyId, List<OccupationTaxResult> perFloorResults)
    {
        var byYear = new SortedDictionary<int, OccupationTaxYearResult>();

        void Accumulate(OccupationTaxYearResult yearResult)
        {
            if (byYear.TryGetValue(yearResult.FinanceYear, out var existing))
            {
                byYear[yearResult.FinanceYear] = new OccupationTaxYearResult
                {
                    FinanceYear = existing.FinanceYear,
                    FinanceYearStart = existing.FinanceYearStart,
                    FinanceYearEnd = existing.FinanceYearEnd,
                    GeneralTax = existing.GeneralTax + yearResult.GeneralTax,
                    ComponentTax = existing.ComponentTax + yearResult.ComponentTax,
                    ComponentCount = existing.ComponentCount,
                    IsProrated = existing.IsProrated || yearResult.IsProrated,
                    ChargeableDays = existing.ChargeableDays,
                    LeapAddbackApplied = existing.LeapAddbackApplied || yearResult.LeapAddbackApplied
                };
            }
            else
            {
                byYear[yearResult.FinanceYear] = yearResult;
            }
        }

        foreach (var floorResult in perFloorResults)
        {
            if (floorResult.CurrentYear != null)
            {
                Accumulate(floorResult.CurrentYear);
            }

            foreach (var retroYear in floorResult.RetroYears)
            {
                Accumulate(retroYear);
            }
        }

        var currentFinanceYearStart = perFloorResults
            .Select(r => r.CurrentYear?.FinanceYear)
            .FirstOrDefault(y => y.HasValue);

        OccupationTaxYearResult? currentYear = currentFinanceYearStart.HasValue && byYear.TryGetValue(currentFinanceYearStart.Value, out var cy)
            ? cy
            : null;

        var retroYears = byYear
            .Where(kv => currentYear == null || kv.Key != currentYear.FinanceYear)
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value)
            .ToList();

        return new OccupationTaxResult
        {
            PropertyId = propertyId,
            IsValid = true,
            Condition = perFloorResults[0].Condition,
            CurrentYear = currentYear,
            RetroYears = retroYears
        };
    }

    /// <summary>
    /// Matches a certificate type against a well-known code (CC/OC/ELECTRIC_BILL), preferring
    /// CertificateTypeCode when populated and falling back to the display-name heuristic when it
    /// isn't (older/seed data may not have codes backfilled yet).
    /// </summary>
    private static bool MatchesCertificateType(PropertyCertificateTypeMasterEntity type, string code, params string[] nameContains)
    {
        if (!string.IsNullOrEmpty(type.CertificateTypeCode))
        {
            if (string.Equals(type.CertificateTypeCode, code, StringComparison.OrdinalIgnoreCase))
                return true;

            var codeUpper = type.CertificateTypeCode.ToUpperInvariant();
            if (nameContains.Any(n => codeUpper.Contains(n.ToUpperInvariant())))
                return true;
        }

        var name = type.CertificateTypeName?.ToLowerInvariant() ?? string.Empty;
        return nameContains.Any(n => name.Contains(n, StringComparison.OrdinalIgnoreCase)) || name == code.ToLowerInvariant();
    }

    /// <summary>
    /// Matches a certificate type against ANY of several codes -- used for Electric Bill, whose
    /// CertificateTypeCode may vary across seed data. Same precedence as
    /// <see cref="MatchesCertificateType"/>: CertificateTypeCode wins outright when populated
    /// (checked against every code in the set, plus a name-heuristic fallback against the code
    /// itself); the display-name heuristic is only consulted when CertificateTypeCode is blank.
    /// </summary>
    private static bool MatchesAnyCertificateType(PropertyCertificateTypeMasterEntity type, IReadOnlyCollection<string> codes, params string[] nameContains)
    {
        if (!string.IsNullOrEmpty(type.CertificateTypeCode))
        {
            if (codes.Any(c => string.Equals(type.CertificateTypeCode, c, StringComparison.OrdinalIgnoreCase)))
                return true;

            var codeUpper = type.CertificateTypeCode.ToUpperInvariant();
            if (nameContains.Any(n => codeUpper.Contains(n.ToUpperInvariant())) ||
                codeUpper.Contains("ELECTRIC") || codeUpper.Contains("BILL") || codeUpper.Contains("EB") || codeUpper.Contains("ELEC"))
                return true;
        }

        var name = type.CertificateTypeName?.ToLowerInvariant() ?? string.Empty;
        return nameContains.Any(n => name.Contains(n, StringComparison.OrdinalIgnoreCase)) ||
            codes.Any(c => string.Equals(name, c, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Splits CertificateTaxGuideline.ELECTRIC_BILL_CERTIFICATE_CODES on commas. A blank/whitespace
    /// value falls back to just the fixed <see cref="CertificateTypeCodes.ElectricBill"/> constant,
    /// so a misconfigured guideline row never silently stops recognizing the well-known default code.
    /// </summary>
    private static string[] ParseElectricBillCertificateCodes(CertificateTaxGuidelineSettings guideline)
    {
        var codes = guideline.ElectricBillCertificateCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return codes.Length == 0 ? new[] { CertificateTypeCodes.ElectricBill } : codes;
    }

    /// <summary>
    /// Classifies each certificate into its OC/CC/Electric-Bill date bucket. When
    /// CertificateRequireNoAndDate is on, a certificate missing CertificateNo or IssueDate is
    /// checked: if BOTH are missing, the certificate is never valid for tax and is always ignored
    /// (never rejects the whole property, regardless of the configured actions below -- there is no
    /// business signal left to act on). If only ONE is missing, the corresponding
    /// MissingCertificateNoAction/MissingCertificateDateAction applies: IGNORE_FOR_TAX skips just
    /// that certificate (as if it didn't exist); REJECT fails the whole computation with a non-null
    /// RejectionReason, which callers must check before resolving a winner.
    /// </summary>
    private static (DateTime? Oc, DateTime? Cc, DateTime? ElectricBill, string? RejectionReason) ExtractDates(
        IEnumerable<PropertyCertificateEntity> certificates, CertificateTaxGuidelineSettings guideline)
    {
        DateTime? ocDate = null;
        DateTime? ccDate = null;
        DateTime? electricityBillDate = null;
        var electricBillCodes = ParseElectricBillCertificateCodes(guideline);

        foreach (var pc in certificates)
        {
            if (pc.CertificateType == null) continue;

            if (guideline.CertificateRequireNoAndDate)
            {
                var missingNo = string.IsNullOrWhiteSpace(pc.CertificateNo);
                var missingDate = !pc.IssueDate.HasValue;

                if (missingNo && missingDate)
                {
                    // Both missing: never a hard failure, regardless of how the individual actions
                    // below are configured -- just treat this certificate as absent.
                    continue;
                }

                if (missingNo || missingDate)
                {
                    var action = missingNo ? guideline.MissingCertificateNoAction : guideline.MissingCertificateDateAction;

                    if (string.Equals(action, "REJECT", StringComparison.OrdinalIgnoreCase))
                    {
                        return (null, null, null,
                            $"Certificate {pc.Id} for type '{pc.CertificateType.CertificateTypeName}' is missing " +
                            $"{(missingNo ? "a certificate number" : "an issue date")}.");
                    }

                    // IGNORE_FOR_TAX (or any other configured action): treat this certificate as absent.
                    continue;
                }
            }

            if (MatchesCertificateType(pc.CertificateType, CertificateTypeCodes.OC, "occupancy", "occupation"))
            {
                ocDate ??= pc.IssueDate;
            }
            else if (MatchesCertificateType(pc.CertificateType, CertificateTypeCodes.CC, "completion"))
            {
                ccDate ??= pc.IssueDate;
            }
            else if (MatchesAnyCertificateType(pc.CertificateType, electricBillCodes, "electricity", "electric", "bill"))
            {
                electricityBillDate ??= pc.IssueDate;
            }
        }

        // Strict priority hierarchy: whenever a valid CC or OC date exists, Electric Bill is never
        // even considered, regardless of DATE_PRIORITY configuration -- backstops the reported UI
        // bug ("CC enabled but electric bill still applies") independent of guideline misconfiguration.
        if (ocDate.HasValue || ccDate.HasValue)
        {
            electricityBillDate = null;
        }

        return (ocDate, ccDate, electricityBillDate, null);
    }

    private async Task<OccupationTaxOptions?> BuildOptionsAsync(
        int propertyId, CertificateTaxGuidelineSettings guideline, FinanceYear currentFy, CancellationToken cancellationToken)
    {
        var nettaxId = await _policyCodeLookup.GetIdAsync(PolicyCodes.NetTax, cancellationToken);

        // Load PolicyTaxDetails for NETTAX -- these rows are owned/written by the RV pipeline;
        // this service only ever reads them as the annual baseline.
        var netTaxDetails = await _policyTaxDetailsRepository.GetQueryable()
            .Include(pt => pt.TaxMaster)
            .Where(pt => pt.PropertyId == propertyId && pt.PolicyCodeId == nettaxId && pt.IsActive && !pt.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        if (!netTaxDetails.Any())
        {
            _logger.LogWarning("No active NETTAX policy details found for property {PropertyId}. Skipping occupation tax computation.", propertyId);
            return null;
        }

        var annualNetTax = netTaxDetails.Sum(pt => pt.TaxAmount ?? 0m);

        var generalTaxDetail = netTaxDetails.FirstOrDefault(pt =>
            pt.TaxMaster != null &&
            (pt.TaxMaster.TaxName.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase) ||
             pt.TaxMaster.TaxCode.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase) ||
             pt.TaxMaster.TaxName.Contains("General", StringComparison.OrdinalIgnoreCase)));

        var generalTaxPortion = generalTaxDetail?.TaxAmount ?? (annualNetTax * 0.6m);

        var components = netTaxDetails.Where(pt => pt != generalTaxDetail && (pt.TaxAmount ?? 0m) > 0m).ToList();
        var componentCount = components.Count;
        if (componentCount == 0)
        {
            componentCount = 4;
        }

        // BuildRetroYears floors the retro window at the certificate's own onset year by default --
        // no "lookback years" truncation applies once a real OC/CC/Electric-Bill date is known; tax
        // is owed from that date forward, full stop. The ONLY legitimate floor above the onset year
        // is an explicit, deliberately-configured cut-off: MINIMUM_BACKDATE_FINANCIAL_YEAR (a real
        // "don't back-date past year X" business rule, distinct from NO_DATE_LOOKBACK_YEARS, which
        // exists solely for the no-certificate-date fallback below and is not consulted here).
        DateTime? retroCutoffDate = guideline.MinimumBackdateFinancialYear > 0
            ? new DateTime(guideline.MinimumBackdateFinancialYear, currentFy.StartMonth, currentFy.StartDay)
            : null;

        return new OccupationTaxOptions
        {
            AnnualNetTax = annualNetTax,
            GeneralTaxPortion = generalTaxPortion,
            ComponentCount = componentCount,
            CompletionCertificateMultiplier = guideline.CCPeriodMultiplier,
            FloorDivisor = 2,
            RetroCutoffDate = retroCutoffDate
        };
    }

    private async Task SaveTaxesAsync(
        int propertyId,
        int userId,
        OccupationComputation computation,
        CancellationToken cancellationToken)
    {
        var guideline = await LoadGuidelineAsync(cancellationToken);

        if (!guideline.SaveInPolicyTaxDetails && !guideline.SaveInTransMast)
        {
            // Nothing configured to persist -- skip the whole read/soft-delete/insert cycle below.
            return;
        }

        if (!string.Equals(guideline.TaxPersistenceMode, "PROPERTY_AGGREGATED", StringComparison.OrdinalIgnoreCase))
        {
            // PROPERTY_AGGREGATED is the only persistence mode this engine implements (see the
            // class-level remarks) -- a per-floor ledger (e.g. FLOOR_LEDGER) is deliberately not
            // built. Rather than silently producing property-aggregated data under a mode name
            // that promises something else, log loudly and continue with the only supported shape.
            _logger.LogWarning(
                "PTIS.CertificateTaxGuideline.TAX_PERSISTENCE_MODE is '{Mode}', which is not supported -- " +
                "only PROPERTY_AGGREGATED is implemented. Continuing with property-aggregated persistence " +
                "for property {PropertyId}.", guideline.TaxPersistenceMode, propertyId);
        }

        if (!guideline.DoNotUpdateNettax)
        {
            // Updating NETTAX from this engine is not implemented and not approved by business --
            // NETTAX is owned/written exclusively by the RV pipeline (see the class-level remarks)
            // and stays untouched below regardless of this setting. Surface the unsupported
            // configuration loudly rather than letting the guideline table imply a capability that
            // doesn't exist.
            _logger.LogWarning(
                "PTIS.CertificateTaxGuideline.DO_NOT_UPDATE_NETTAX is 0, but updating NETTAX from this engine " +
                "is not implemented. NETTAX will remain untouched for property {PropertyId} regardless of this " +
                "setting.", propertyId);
        }

        var result = computation.Result;

        var years = result.RetroYears.Select(y => y.FinanceYear)
            .Concat(new[] { result.CurrentYear!.FinanceYear })
            .Distinct()
            .ToList();

        // Now that retro years can span far further back than a fixed lookback cap (tax applies
        // from the certificate's actual date forward, with no lookback truncation), a plain
        // YearMaster.Year match can miss older rows whose Year/YearCode/StartDate don't line up
        // perfectly. Try several ways to identify the right row before giving up on a finance year.
        var allYearMasters = await _yearRepository.GetQueryable().ToListAsync(cancellationToken);
        var yearMasters = new Dictionary<int, int>();
        foreach (var fyYear in years)
        {
            var match = allYearMasters.FirstOrDefault(y =>
            {
                if (y.StartDate.HasValue && y.StartDate.Value.Year == fyYear) return true;
                if (y.Year == fyYear) return true;
                if (!string.IsNullOrEmpty(y.YearCode))
                {
                    var clean = y.YearCode.Trim();
                    if (clean.StartsWith($"{fyYear}-") || clean.StartsWith($"{fyYear}/") || clean.StartsWith(fyYear.ToString()))
                        return true;

                    var parts = clean.Split('-', '/');
                    if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out var startY))
                    {
                        if (startY == fyYear) return true;
                        if (startY < 100 && (2000 + startY) == fyYear) return true;
                    }
                }
                return false;
            }) ?? allYearMasters.FirstOrDefault(y => y.Year == fyYear + 1 || y.Year == fyYear);

            if (match != null)
            {
                yearMasters[fyYear] = match.Id;
            }
        }

        // PTIS.PolicyTaxDetails holds exactly ONE active NETTAX row per (PropertyId, TaxId) -- no
        // per-year history -- so the SAME current snapshot is used for every finance year this
        // computation touches (current year and every retro/arrears year alike).
        var netTaxSnapshot = await LoadNetTaxSnapshotAsync(propertyId, cancellationToken);
        var yearlySnapshots = years.ToDictionary(y => y, _ => netTaxSnapshot);

        var yearIds = yearMasters.Values.ToList();
        var taxIds = yearlySnapshots.Values
            .SelectMany(s => (s.GeneralTaxDetail != null ? new[] { s.GeneralTaxDetail } : Array.Empty<PolicyTaxDetailsEntity>())
                .Concat(s.Components))
            .Select(pt => pt.TaxId)
            .Distinct()
            .ToList();

        var now = DateTime.Now;

        // 1a. Load ALL existing TransMast rows for this property/these years/these taxes --
        // REGARDLESS of IsActive/MarkedForDeletion -- keyed by slot (FinanceYearId + TaxId,
        // TransMast's whole unique key besides PropertyId) -- only when SAVE_CERTIFICATE_TAX_IN_TRANSMAST
        // is on. GO-LIVE BLOCKER fix: this query previously filtered to IsActive && !MarkedForDeletion,
        // so a row soft-deleted by a prior CleanupStaleCertificateTaxRowsAsync run (or an earlier
        // SaveTaxesAsync's own "not reused, soft-delete" cleanup below) was invisible here -- the
        // upsert then took the "insert new" branch and collided on PropertyId+FinanceYearId+TaxId
        // with the still-present-but-inactive row, because PTIS.TransMast's live unique index
        // (UQ_TransMast_Property_Year_Tax) is DBA-managed via a separate SQL project (not EF
        // migrations) and is NOT filtered on IsActive/MarkedForDeletion -- an inactive row still
        // occupies its key. Loading by the unique key alone, regardless of active state, and always
        // reactivating (see UpsertTransMast below) is what actually prevents that collision; the
        // active-only filter defeated the entire purpose of the "update in place" upsert pattern.
        var existingTransMastsBySlot = new Dictionary<(int YearId, int TaxId), TransMastEntity>();
        if (guideline.SaveInTransMast)
        {
            // CalculationType == "RV" is required now that TransMast also holds CV rows
            // (TransMastCV was folded into it) -- without this filter, a CV row sharing the same
            // (FinanceYearId, TaxId) slot would be picked up here and incorrectly reused/reactivated
            // as if it were this certificate-driven RV row.
            var existingTransMasts = await _transMastRepository.GetQueryable()
                .Where(tm => tm.PropertyId == propertyId &&
                             tm.CalculationType == "RV" &&
                             yearIds.Contains(tm.FinanceYearId) &&
                             taxIds.Contains(tm.TaxId))
                .ToListAsync(cancellationToken);

            foreach (var tm in existingTransMasts)
            {
                existingTransMastsBySlot[(tm.FinanceYearId, tm.TaxId)] = tm;
            }
        }

        // 1b. Load ALL of this property's existing OC/CC/ELECTRIC_BILL-family PolicyTaxDetails rows
        // for these taxes -- REGARDLESS of IsActive/MarkedForDeletion, for the same reason as
        // TransMast above. DBA-confirmed final schema (2026-07-24): PTIS.PolicyTaxDetails has NO
        // PolicyYear column at all, and its real unique index is
        // UX_PolicyTaxDetails_Property_Year_PolicyCode_TaxId on (PropertyId, PolicyCodeId, TaxId)
        // WHERE IsActive=1 AND MarkedForDeletion=0 -- i.e. exactly ONE active row may ever exist per
        // (PropertyId, TaxId) across the WHOLE certificate-tax domain (not one per family, and
        // never one per year -- a retro-year row sharing the same PolicyCodeId as the current year
        // would violate this index outright). Certificate rows are therefore keyed by TaxId ALONE
        // here (grouped, not sloted by year): a re-computed certificate that changes which family
        // applies (e.g. property had CC before, now has OC), or that now has a different current-FY
        // amount, reuses and re-tags the SAME row -- only when SAVE_CERTIFICATE_TAX_IN_POLICY_TAX_DETAILS
        // is on. The NETTAX row this property's own PolicyCodeId=NETTAX slot holds is governed by the
        // same one-active-row-per-(PropertyId,PolicyCodeId,TaxId) rule and is likewise never
        // year-tagged; the RV pipeline owns and writes that row, this service only ever reads it.
        var (familyPolicyCodes, familyPolicyCodeIds) = await ResolveFamilyPolicyCodesAsync(guideline, cancellationToken);
        ValidateNeededFamilyPolicyCodesResolved(computation, familyPolicyCodes, familyPolicyCodeIds);

        var existingPolicyTaxDetails = new List<PolicyTaxDetailsEntity>();
        var existingPolicyTaxDetailsByTaxId = new Dictionary<int, List<PolicyTaxDetailsEntity>>();
        if (guideline.SaveInPolicyTaxDetails)
        {
            var familyPolicyCodeIdValues = familyPolicyCodeIds.Values.ToList();

            existingPolicyTaxDetails = await _policyTaxDetailsRepository.GetQueryable()
                .Where(pt => pt.PropertyId == propertyId &&
                             familyPolicyCodeIdValues.Contains(pt.PolicyCodeId))
                .ToListAsync(cancellationToken);

            existingPolicyTaxDetailsByTaxId = existingPolicyTaxDetails
                .GroupBy(pt => pt.TaxId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // 1c. Load existing TaxPendingDetails rows for this property/these taxes -- REGARDLESS of
        // which PendingYearId they carry (unlike TaxPendingDetailsRetro below), because
        // TaxPendingDetails is a SUMMARY table: exactly one row per (PropertyId, TaxId), not one per
        // year. A property whose data predates this fix may have MULTIPLE rows per TaxId (the bug
        // this fixes -- one row per pending year, mirroring TaxPendingDetailsRetro); grouping by
        // TaxId here, regardless of year, is what lets UpsertTaxPending below detect and collapse
        // those duplicates down to one summary row instead of only ever seeing whichever single
        // (year, tax) slot happens to match this run's own years.
        var existingTaxPendingDetails = await _taxPendingDetailsRepository.GetQueryable()
            .Where(tp => tp.PropertyId == propertyId && taxIds.Contains(tp.TaxId))
            .ToListAsync(cancellationToken);
        var existingTaxPendingDetailsByTaxId = existingTaxPendingDetails
            .GroupBy(tp => tp.TaxId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Load existing TaxPendingDetailsRetro rows for this property/these taxes, keyed the same
        // way as TransMast (YearMaster.Id + TaxId), for the same reactivate-in-place reasoning --
        // this table DOES keep one row per year, unlike TaxPendingDetails above. Loaded by TaxId
        // only (not scoped to this run's own yearIds), same rationale as existingTaxPendingDetails
        // above: retro years can now span far further back than before (no lookback truncation), so
        // a prior run's certificate could have written rows for years outside this run's yearIds --
        // those must still be visible here for the stale-row cleanup below to find and deactivate.
        var existingTaxPendingDetailsRetroBySlot = new Dictionary<(int YearId, int TaxId), TaxPendingDetailsRetroEntity>();
        var existingTaxPendingDetailsRetro = await _taxPendingDetailsRetroRepository.GetQueryable()
            .Where(tpr => tpr.PropertyId == propertyId && taxIds.Contains(tpr.TaxId))
            .ToListAsync(cancellationToken);
        foreach (var tpr in existingTaxPendingDetailsRetro)
        {
            existingTaxPendingDetailsRetroBySlot[(tpr.PendingYearId, tpr.TaxId)] = tpr;
        }

        // 2. Upsert TransMast + PolicyTaxDetails records. PARTIAL_x applies ONLY to the row for
        // the live, ongoing current finance year, and ONLY when the certificate's OWN effective
        // date actually falls within that current year -- not whenever the engine's per-year math
        // happens to be day-prorated. Those two things usually coincide (a certificate granted
        // mid-way through this year both dates into the current FY AND gets a prorated current-year
        // amount), but they diverge in two real cases this fix accounts for: a certificate dated
        // EXACTLY on the FY's first day (dates into the current FY, but the amount is a full,
        // unprorated year -- still genuinely "this year, not yet closed"), and an Electric Bill
        // under ELECTRIC_BILL_DATE_RULE=FROM_FY_START (the certificate's own date can fall in the
        // current FY, but its onset is always normalized to that FY's start, so the engine never
        // marks it prorated). A decades-old backdated certificate is never "partial", regardless of
        // its own year's proration, because its own date simply isn't in the current FY.
        var certificateDateIsInCurrentFy = computation.EffectiveDate.HasValue &&
            FinanceYear.ForDate(computation.EffectiveDate.Value, computation.CurrentFy.StartMonth, computation.CurrentFy.StartDay).StartYear
                == computation.CurrentFy.StartYear;

        var newTransMasts = new List<TransMastEntity>();
        var newPolicyTaxDetails = new List<PolicyTaxDetailsEntity>();
        var newTaxPendingDetails = new List<TaxPendingDetailsEntity>();
        var newTaxPendingDetailsRetro = new List<TaxPendingDetailsRetroEntity>();
        var reusedTransMastSlots = new HashSet<(int YearId, int TaxId)>();
        var reusedTaxPendingDetailsRetroSlots = new HashSet<(int YearId, int TaxId)>();

        // Accumulated per-TaxId retro totals (summed across ALL retro years processed by
        // AddYearRecords below) -- flushed to ONE UpsertTaxPending call per TaxId after the
        // per-year loop finishes, instead of writing a row per year. Per the confirmed final
        // business rule, every summary row is tagged with the PREVIOUS finance year (current FY - 1)
        // specifically -- not "whichever retro year happens to be most recent" -- resolved below via
        // CORE.YearMaster, never hardcoded.
        var pendingTotalsByTaxId = new Dictionary<int, decimal>();

        // Accumulated current-finance-year total across the general tax + component taxes actually
        // upserted into PolicyTaxDetails for this run's certificate-tax policy group (OC/PARTIAL_OC/
        // CC/PARTIAL_CC/ELECTRIC_BILL/PARTIAL_ELECTRIC_BILL) -- mirrors RVPersistenceService's NETTAX
        // "TaxTotal" row so PropertyRepository.GetTaxDetailsPivotedAsync can read a precomputed total
        // for certificate policy groups too, instead of summing TaxAmounts itself. Only ever set for
        // the current finance year (retro years never call UpsertPolicyTaxDetail at all), so exactly
        // one policy code owns it per run.
        var currentYearCertificateTaxTotal = 0m;
        int? currentYearCertificatePolicyCodeId = null;

        void AccumulatePendingTotal(int taxId, decimal amount)
        {
            pendingTotalsByTaxId[taxId] = pendingTotalsByTaxId.GetValueOrDefault(taxId) + amount;
        }

        // Tracks (by OBJECT REFERENCE, not .Id -- entities in mocked/unsaved test scenarios can
        // legitimately have Id == 0, so identity is the only reliable signal) every entity from
        // existingPolicyTaxDetails that got reused this run. PTIS.PolicyTaxDetails' real unique
        // index (UX_PolicyTaxDetails_Property_Year_PolicyCode_TaxId, on PropertyId/PolicyCodeId/TaxId
        // despite the "Year" in its name -- there is no PolicyYear column) includes PolicyCodeId, so
        // the database genuinely allows TWO active rows for the same (PropertyId, TaxId) under
        // DIFFERENT PolicyCodeIds (e.g. a leftover CC row alongside a newer OC row) --
        // something the slot-only dictionary below cannot represent, since it keeps only the
        // LAST-loaded row per slot and silently drops any other. Cleanup below iterates every row
        // from the original query (not the dictionary) so a dropped duplicate is still found and
        // deactivated instead of being left active forever.
        var reusedExistingPolicyTaxDetails = new HashSet<PolicyTaxDetailsEntity>();

        // Tracks (by object reference, same reasoning as reusedExistingPolicyTaxDetails above) every
        // TaxPendingDetails row kept as the ONE summary row for its TaxId. Any other row sharing that
        // same TaxId -- a duplicate left over from before this fix, which used to write one row per
        // pending year -- is NOT added here, so the cleanup pass below (which iterates every
        // originally-loaded row, not a slot dictionary) finds and deactivates it.
        var reusedExistingTaxPendingDetails = new HashSet<TaxPendingDetailsEntity>();

        void UpsertTransMast(int yearId, int taxId, decimal calculationValue, decimal taxAmount)
        {
            if (!guideline.SaveInTransMast)
            {
                return;
            }

            var slot = (yearId, taxId);
            if (existingTransMastsBySlot.TryGetValue(slot, out var existing))
            {
                existing.CalculationValue = calculationValue;
                existing.TaxAmount = taxAmount;
                existing.IsActive = true;
                existing.MarkedForDeletion = false;
                existing.MarkedForDeletionDate = null;
                existing.UpdatedBy = userId;
                existing.UpdatedDate = now;
                reusedTransMastSlots.Add(slot);
            }
            else
            {
                var newEntity = new TransMastEntity
                {
                    PropertyId = propertyId,
                    FinanceYearId = yearId,
                    CalculationType = "RV",
                    CalculationValue = calculationValue,
                    TaxId = taxId,
                    TaxAmount = taxAmount,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedBy = userId,
                    CreatedDate = now,
                    UpdatedBy = userId,
                    UpdatedDate = now
                };
                newTransMasts.Add(newEntity);

                // Defensive: if AddYearRecords is ever invoked twice for the same slot within this
                // SAME SaveTaxesAsync call (e.g. a future code path produces a duplicate finance
                // year in the computed result), track the entity we just created here too, so a
                // second call for this slot updates THIS entity instead of adding a second one.
                // No current code path is known to produce that duplicate (the pure engine,
                // AggregateFloorResults, and the CC-then-OC merge are all provably duplicate-free
                // by construction), but relying only on the DB-loaded `existingTransMastsBySlot`
                // snapshot would silently reintroduce a duplicate-insert risk if that ever changed.
                existingTransMastsBySlot[slot] = newEntity;
                reusedTransMastSlots.Add(slot);
            }
        }

        // Upserts the ONE current/final PolicyTaxDetails row for (PropertyId, TaxId) in the
        // certificate-tax domain -- never a row per finance year (see the load-query comment above
        // for why: the real unique index allows only one active row per (PropertyId, PolicyCodeId,
        // TaxId), and a retro year sharing its family's PolicyCodeId with the current year would
        // collide with it outright). Only ever called for the CURRENT finance year -- retro years
        // are recorded exclusively in TaxPendingDetailsRetro/TaxPendingDetails instead.
        void UpsertPolicyTaxDetail(int taxId, int policyCodeId, decimal? calculationValue, decimal taxAmount)
        {
            if (!guideline.SaveInPolicyTaxDetails)
            {
                return;
            }

            if (existingPolicyTaxDetailsByTaxId.TryGetValue(taxId, out var candidates) && candidates.Count > 0)
            {
                // Prefer a row already tagged with the target PolicyCodeId (a true no-op reactivation);
                // otherwise re-tag the first candidate in place. Every OTHER row sharing this TaxId
                // (a pre-fix year-wise duplicate, or a stale row left under a different family) is a
                // duplicate the real unique index could never actually support as a second active row
                // -- it is NOT added to reusedExistingPolicyTaxDetails, so the cleanup pass below
                // deactivates it.
                var primary = candidates.FirstOrDefault(c => c.PolicyCodeId == policyCodeId) ?? candidates[0];
                primary.PolicyCodeId = policyCodeId;
                primary.CalculationValue = calculationValue;
                primary.TaxAmount = taxAmount;
                primary.IsActive = true;
                primary.MarkedForDeletion = false;
                primary.MarkedForDeletionDate = null;
                primary.UpdatedBy = userId;
                primary.UpdatedDate = now;

                reusedExistingPolicyTaxDetails.Add(primary);
            }
            else
            {
                var newEntity = new PolicyTaxDetailsEntity
                {
                    PropertyId = propertyId,
                    PolicyCodeId = policyCodeId,
                    CalculationValue = calculationValue,
                    TaxId = taxId,
                    TaxAmount = taxAmount,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedBy = userId,
                    CreatedDate = now,
                    UpdatedBy = userId,
                    UpdatedDate = now
                };
                newPolicyTaxDetails.Add(newEntity);
                reusedExistingPolicyTaxDetails.Add(newEntity);
            }
        }

        // Retrospective/arrears years (finance years strictly before the live current year -- see
        // AddYearRecords) additionally get a matching pending-tax row in BOTH tables: TaxPendingDetailsRetro
        // (the year-wise, tax-wise breakdown -- the calculation's own record) and TaxPendingDetails
        // (the same rows, which is what SocietyOutstandingReportDataProvider/CombinePropertyService
        // already SUM(PendingAmount) across to get a property's total outstanding pending tax --
        // there is no separate "grand total" row; the total is derived by summing these at read
        // time, matching how those two consumers already query it). TaxPendingDetails additionally
        // respects PendingFixed: a row marked PendingFixed=true (set after a property combine, per
        // its own doc comment, to prevent double-counting) is never touched here -- neither updated
        // nor cleaned up -- since it represents a manually-reconciled amount outside this engine's
        // control.
        void UpsertTaxPendingRetro(int yearId, int taxId, decimal amount)
        {
            var slot = (yearId, taxId);
            if (existingTaxPendingDetailsRetroBySlot.TryGetValue(slot, out var existing))
            {
                existing.PendingAmount = amount;
                existing.IsActive = true;
                existing.MarkedForDeletion = false;
                existing.MarkedForDeletionDate = null;
                existing.UpdatedBy = userId;
                existing.UpdatedDate = now;
                reusedTaxPendingDetailsRetroSlots.Add(slot);
            }
            else
            {
                var newEntity = new TaxPendingDetailsRetroEntity
                {
                    PropertyId = propertyId,
                    PendingYearId = yearId,
                    TaxId = taxId,
                    PendingAmount = amount,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedBy = userId,
                    CreatedDate = now,
                    UpdatedBy = userId,
                    UpdatedDate = now
                };
                newTaxPendingDetailsRetro.Add(newEntity);
                existingTaxPendingDetailsRetroBySlot[slot] = newEntity;
                reusedTaxPendingDetailsRetroSlots.Add(slot);
            }
        }

        // Upserts the ONE summary row for (PropertyId, TaxId): amount is the already-summed total
        // across every retro year for this tax (see pendingTotalsByTaxId), never a single year's own
        // amount. previousFinanceYearId is the required PendingYearId FK value, and per the confirmed
        // final business rule it is SPECIFICALLY the previous finance year (current FY - 1) --
        // resolved once by the caller via CORE.YearMaster -- not just "some contributing year."
        void UpsertTaxPending(int taxId, decimal amount, int previousFinanceYearId)
        {
            if (existingTaxPendingDetailsByTaxId.TryGetValue(taxId, out var candidates) && candidates.Count > 0)
            {
                // A row already marked PendingFixed (manually reconciled after a property combine --
                // see CombinePropertyTaxService) is left untouched, but still marked "reused" so the
                // cleanup pass doesn't deactivate it. Any OTHER row sharing this TaxId (a pre-fix
                // duplicate) is deliberately left out of the reused set here, so it still gets
                // deactivated below even though this TaxId's fixed row survives untouched.
                var fixedRow = candidates.FirstOrDefault(c => c.PendingFixed);
                if (fixedRow != null)
                {
                    reusedExistingTaxPendingDetails.Add(fixedRow);
                    return;
                }

                // Prefer the row already tagged with the previous finance year, if one of the
                // duplicates happens to match, so reactivating an already-correct row is a true
                // no-op; otherwise just take the first candidate as the one summary row to keep.
                // Every OTHER candidate sharing this TaxId is a pre-fix duplicate (e.g. one row per
                // pending year, or tagged with some other year entirely) and is deactivated by the
                // cleanup pass below (it is not added to reusedExistingTaxPendingDetails).
                var primary = candidates.FirstOrDefault(c => c.PendingYearId == previousFinanceYearId) ?? candidates[0];
                primary.PendingAmount = amount;
                primary.PendingYearId = previousFinanceYearId;
                primary.IsActive = true;
                primary.MarkedForDeletion = false;
                primary.MarkedForDeletionDate = null;
                primary.UpdatedBy = userId;
                primary.UpdatedDate = now;
                reusedExistingTaxPendingDetails.Add(primary);
            }
            else
            {
                var newEntity = new TaxPendingDetailsEntity
                {
                    PropertyId = propertyId,
                    PendingYearId = previousFinanceYearId,
                    TaxId = taxId,
                    PendingAmount = amount,
                    PendingFixed = false,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedBy = userId,
                    CreatedDate = now,
                    UpdatedBy = userId,
                    UpdatedDate = now
                };
                newTaxPendingDetails.Add(newEntity);
                reusedExistingTaxPendingDetails.Add(newEntity);
            }
        }

        void AddYearRecords(OccupationTaxYearResult yearResult)
        {
            if (!yearMasters.TryGetValue(yearResult.FinanceYear, out var yearId))
            {
                _logger.LogWarning("Finance year {FinanceYear} not found in YearMaster for property {PropertyId}; skipping tax persistence for this year.", yearResult.FinanceYear, propertyId);
                return;
            }

            // Every finance year uses the SAME current NETTAX snapshot (see LoadNetTaxSnapshotAsync)
            // -- `yearlySnapshots` maps every year in `years` to that one snapshot up front, so this
            // lookup can only miss if `yearResult.FinanceYear` isn't one of `years`, which would be a
            // caller bug, not expected behavior.
            if (!yearlySnapshots.TryGetValue(yearResult.FinanceYear, out var yearSnapshot))
            {
                throw new InvalidOperationException(
                    $"No NETTAX snapshot for finance year {yearResult.FinanceYear} for property {propertyId} " +
                    "-- this finance year was not included in SaveTaxesAsync's `years` list.");
            }

            var yearFamilyCode = computation.YearPolicyCodes?.GetValueOrDefault(yearResult.FinanceYear) ?? computation.PolicyCode;
            var (fullCode, partialCode) = familyPolicyCodes[yearFamilyCode];
            var isCurrentFinanceYear = yearResult.FinanceYear == computation.CurrentFy.StartYear;
            var policyCode = isCurrentFinanceYear && certificateDateIsInCurrentFy ? partialCode : fullCode;
            var policyCodeId = familyPolicyCodeIds[policyCode];

            // A finance year strictly BEFORE the live current year is arrears/backlog -- tax that
            // should already have been billed had the certificate been known at the time. Only
            // those years get a TaxPendingDetailsRetro/TaxPendingDetails row; TransMast AND
            // PolicyTaxDetails are both reserved for the live current year ONLY. TransMast is read
            // without any year filter by CombinePropertyService.GetTaxDataAsync and
            // PropertyReassessmentService's old-vs-new comparison -- a retro-year row left there
            // would silently inflate both beyond the property's actual current-year demand.
            // PolicyTaxDetails cannot hold a row per year at all under the DBA-confirmed final
            // schema (no PolicyYear column, unique index on (PropertyId, PolicyCodeId, TaxId) only)
            // -- a retro year sharing its family's PolicyCodeId with the current year would collide
            // with it. Year-wise certificate history lives exclusively in TaxPendingDetailsRetro now.
            var isRetroYear = yearResult.FinanceYear < computation.CurrentFy.StartYear;

            // General Tax & Component Taxes: Reconstruct exact proportional scaling factor
            // (overall year tax / snapshot annual NETTAX) to scale individual components and general tax
            // directly from the property's baseline NETTAX rates, eliminating per-floor integer rounding drift.
            var overallFactor = (yearSnapshot.GeneralTaxPortion > 0m)
                ? (yearResult.IsProrated
                    ? (yearResult.GeneralTax + yearResult.ComponentTax * yearSnapshot.ComponentCount) / yearSnapshot.AnnualNetTax
                    : (yearResult.GeneralTax == yearSnapshot.GeneralTaxPortion
                        ? 1.0m
                        : yearResult.GeneralTax / yearSnapshot.GeneralTaxPortion))
                : (yearSnapshot.AnnualNetTax > 0m
                    ? (yearResult.GeneralTax + yearResult.ComponentTax * yearSnapshot.ComponentCount) / yearSnapshot.AnnualNetTax
                    : 1.0m);

            var generalTaxAmount = (yearResult.IsProrated || (yearSnapshot.GeneralTaxPortion > 0m && yearResult.GeneralTax == yearSnapshot.GeneralTaxPortion))
                ? yearResult.GeneralTax
                : Math.Round(yearSnapshot.GeneralTaxPortion * overallFactor, 0, MidpointRounding.AwayFromZero);

            if (yearSnapshot.GeneralTaxDetail != null)
            {
                if (!isRetroYear)
                {
                    UpsertTransMast(yearId, yearSnapshot.GeneralTaxDetail.TaxId, yearSnapshot.GeneralTaxDetail.CalculationValue ?? 0m, generalTaxAmount);
                    if (!computation.IsNoCertificateFallback)
                    {
                        UpsertPolicyTaxDetail(yearSnapshot.GeneralTaxDetail.TaxId, policyCodeId, yearSnapshot.GeneralTaxDetail.CalculationValue, generalTaxAmount);
                        currentYearCertificateTaxTotal += generalTaxAmount;
                        currentYearCertificatePolicyCodeId = policyCodeId;
                    }
                }

                if (isRetroYear && !computation.IsNoCertificateFallback)
                {
                    UpsertTaxPendingRetro(yearId, yearSnapshot.GeneralTaxDetail.TaxId, generalTaxAmount);
                    AccumulatePendingTotal(yearSnapshot.GeneralTaxDetail.TaxId, generalTaxAmount);
                }
            }

            foreach (var comp in yearSnapshot.Components)
            {
                var compTaxAmount = Math.Round((comp.TaxAmount ?? 0m) * overallFactor, 0, MidpointRounding.AwayFromZero);
                if (!isRetroYear)
                {
                    UpsertTransMast(yearId, comp.TaxId, comp.CalculationValue ?? 0m, compTaxAmount);
                    if (!computation.IsNoCertificateFallback)
                    {
                        UpsertPolicyTaxDetail(comp.TaxId, policyCodeId, comp.CalculationValue, compTaxAmount);
                        currentYearCertificateTaxTotal += compTaxAmount;
                        currentYearCertificatePolicyCodeId = policyCodeId;
                    }
                }

                if (isRetroYear && !computation.IsNoCertificateFallback)
                {
                    UpsertTaxPendingRetro(yearId, comp.TaxId, compTaxAmount);
                    AccumulatePendingTotal(comp.TaxId, compTaxAmount);
                }
            }
        }

        // Deduplicate by FinanceYear before persisting -- an explicit, visible safety net on top of
        // UpsertTransMast/UpsertPolicyTaxDetail's own same-call slot tracking above. No known code
        // path produces two entries for the same finance year in one computation (the pure engine,
        // AggregateFloorResults, and the CC-then-OC merge are all provably duplicate-free by
        // construction -- see their own comments), but persistence must not depend on that always
        // remaining true: keep the first occurrence per year (CurrentYear wins over a same-year
        // RetroYears entry, since it is added first below).
        var yearResultsToPersist = new List<OccupationTaxYearResult>();
        var seenFinanceYears = new HashSet<int>();

        if (result.CurrentYear != null && seenFinanceYears.Add(result.CurrentYear.FinanceYear))
        {
            yearResultsToPersist.Add(result.CurrentYear);
        }

        foreach (var retroYear in result.RetroYears)
        {
            if (seenFinanceYears.Add(retroYear.FinanceYear))
            {
                yearResultsToPersist.Add(retroYear);
            }
        }

        foreach (var yearResult in yearResultsToPersist)
        {
            AddYearRecords(yearResult);
        }

        // Upsert the certificate policy group's own precomputed "TaxTotal" row for the current
        // finance year -- same reserved TaxMaster row (TaxCode/TaxName = "TaxTotal") RVPersistenceService
        // writes for NETTAX, so the Tax Details grid can read every policy group's total the same way.
        if (currentYearCertificatePolicyCodeId.HasValue && currentYearCertificateTaxTotal > 0)
        {
            var taxTotalId = await _taxMasterRepository.GetQueryable()
                .Where(t => t.IsActive && t.TaxCode == TaxTotalCode)
                .Select(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (taxTotalId > 0)
            {
                UpsertPolicyTaxDetail(taxTotalId, currentYearCertificatePolicyCodeId.Value, null, currentYearCertificateTaxTotal);
            }
        }

        // Flush the accumulated per-TaxId retro totals to ONE summary row per TaxId -- this is the
        // only place UpsertTaxPending is called, deliberately after every retro year has already
        // been folded into pendingTotalsByTaxId above, so TaxPendingDetails never gets a row per
        // year the way TaxPendingDetailsRetro correctly does. Per the confirmed final business rule,
        // every summary row is tagged with the PREVIOUS finance year (current FY - 1) specifically --
        // resolved dynamically via CORE.YearMaster below, never hardcoded -- regardless of how far
        // back the retro window itself extends.
        if (pendingTotalsByTaxId.Count > 0)
        {
            var previousFinanceYear = computation.CurrentFy.StartYear - 1;
            if (!yearMasters.TryGetValue(previousFinanceYear, out var previousFinanceYearId))
            {
                previousFinanceYearId = await _yearRepository.GetQueryable()
                    .Where(y => y.Year == previousFinanceYear)
                    .Select(y => y.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (previousFinanceYearId == 0)
                {
                    throw new InvalidOperationException(
                        $"Cannot persist TaxPendingDetails summary rows: finance year {previousFinanceYear} " +
                        "(the previous FY relative to the current one) was not found in YearMaster.");
                }
            }

            foreach (var (taxId, total) in pendingTotalsByTaxId)
            {
                UpsertTaxPending(taxId, total, previousFinanceYearId);
            }
        }

        // Any existing row not reused above belongs to a slot this computation no longer produces
        // (e.g. a finance year that dropped out of the retrospective window) -- soft-delete it
        // rather than leaving a stale active row behind.
        foreach (var (slot, tm) in existingTransMastsBySlot)
        {
            if (reusedTransMastSlots.Contains(slot))
            {
                continue;
            }

            tm.MarkedForDeletion = true;
            tm.MarkedForDeletionDate = now;
            tm.IsActive = false;
            tm.UpdatedBy = userId;
            tm.UpdatedDate = now;
            await _transMastRepository.UpdateAsync(tm, cancellationToken);
        }

        // Iterates every ORIGINALLY-LOADED row (not existingPolicyTaxDetailsBySlot, which keeps only
        // one row per (Year, TaxId) slot and would silently skip a duplicate under a different
        // PolicyCodeId sharing that slot -- see the comment on reusedExistingPolicyTaxDetails above).
        foreach (var pt in existingPolicyTaxDetails)
        {
            if (reusedExistingPolicyTaxDetails.Contains(pt))
            {
                continue;
            }

            pt.MarkedForDeletion = true;
            pt.MarkedForDeletionDate = now;
            pt.IsActive = false;
            pt.UpdatedBy = userId;
            pt.UpdatedDate = now;
            await _policyTaxDetailsRepository.UpdateAsync(pt, cancellationToken);
        }

        // A pending-tax slot not reused above either dropped out of the retrospective window, or
        // (rarely) the year it belonged to is no longer retro at all -- either way, no longer
        // pending, so deactivate it. PendingFixed rows were already excluded from
        // reusedTaxPendingDetailsSlots-tracking-as-"skip" inside UpsertTaxPending itself (they ARE
        // added to the reused set there specifically so this loop leaves them alone).
        foreach (var (slot, tpr) in existingTaxPendingDetailsRetroBySlot)
        {
            if (reusedTaxPendingDetailsRetroSlots.Contains(slot))
            {
                continue;
            }

            tpr.MarkedForDeletion = true;
            tpr.MarkedForDeletionDate = now;
            tpr.IsActive = false;
            tpr.UpdatedBy = userId;
            tpr.UpdatedDate = now;
            await _taxPendingDetailsRetroRepository.UpdateAsync(tpr, cancellationToken);
        }

        // Iterates every ORIGINALLY-LOADED row (not existingTaxPendingDetailsByTaxId, which keeps a
        // full list per TaxId anyway, but the flat source list is the clearest way to guarantee every
        // duplicate -- including any pre-fix "one row per pending year" leftovers -- is considered).
        // A TaxId whose one summary row was kept in place is in reusedExistingTaxPendingDetails; any
        // other row for that same TaxId (or any TaxId dropped out of this computation entirely) is a
        // stale duplicate/obsolete row and is deactivated here, unless PendingFixed.
        foreach (var tp in existingTaxPendingDetails)
        {
            if (reusedExistingTaxPendingDetails.Contains(tp) || tp.PendingFixed)
            {
                continue;
            }

            tp.MarkedForDeletion = true;
            tp.MarkedForDeletionDate = now;
            tp.IsActive = false;
            tp.UpdatedBy = userId;
            tp.UpdatedDate = now;
            await _taxPendingDetailsRepository.UpdateAsync(tp, cancellationToken);
        }

        if (newTransMasts.Any())
        {
            await _transMastRepository.AddRangeAsync(newTransMasts, cancellationToken);
        }

        if (newPolicyTaxDetails.Any())
        {
            await _policyTaxDetailsRepository.AddRangeAsync(newPolicyTaxDetails, cancellationToken);
        }

        if (newTaxPendingDetailsRetro.Any())
        {
            await _taxPendingDetailsRetroRepository.AddRangeAsync(newTaxPendingDetailsRetro, cancellationToken);
        }

        if (newTaxPendingDetails.Any())
        {
            await _taxPendingDetailsRepository.AddRangeAsync(newTaxPendingDetails, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
