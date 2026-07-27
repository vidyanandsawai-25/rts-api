using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Specialized repository implementation for Property entity
/// Provides custom query methods for property-related operations
/// </summary>
public class PropertyRepository : Repository<PropertyEntity, int>, IPropertyRepository
{
    private readonly IFinanceYearProvider _financeYearProvider;

    public PropertyRepository(ApplicationDbContext context, IFinanceYearProvider financeYearProvider) : base(context)
    {
        _financeYearProvider = financeYearProvider;
    }

    public async Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var policies = await GetTaxDetailsPivotedAsync(
            propertyId,
            isCapitalValue: false,
            excludeEducationEmploymentTax: true,  // Hide education/employment tax in details-taxes API
            cancellationToken);

        if (policies == null)
            return null;

        return new PropertyTaxDetailsDto
        {
            PropertyId = propertyId,
            Policies = policies
        };
    }

    /// <summary>
    /// Private helper method to query and pivot tax details from a given tax details table.
    /// Joins with TaxMaster, filters by active/deleted flags, orders by DisplayOrder, and groups by PolicyCode.
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="isCapitalValue">Whether to query CapitalValue or RateableValue tax details</param>
    /// <param name="excludeEducationEmploymentTax">If true, excludes Education and Employment taxes from results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pivoted PolicyTaxDetail objects, or null if property not found or no data exists</returns>
    private async Task<List<PolicyTaxDetail>?> GetTaxDetailsPivotedAsync(
        int propertyId,
        bool isCapitalValue,
        bool excludeEducationEmploymentTax = false,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Check if property exists
        var propertyExists = await _context.PropertyMast
            .AnyAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (!propertyExists)
            return null;

        // Step 2: Query tax details with TaxMaster join, ordered by DisplayOrder
        List<(string PolicyCode, string PolicyName, string TaxName, decimal? TaxAmount, int TaxId)> taxData;

        if (isCapitalValue)
        {
            // PolicyTaxDetailsCV has no separate display name column, so the code itself is used
            // as the name (matching the RV side's fallback when PolicyCodeMaster is missing).
            taxData = await (from td in _context.PolicyTaxDetailsCV
                             join tm in _context.TaxMaster on td.TaxId equals tm.Id
                             join tc in _context.TaxCategoryMaster on tm.TaxCategoryId equals tc.Id
                             join pcm in _context.PolicyCodeMaster on td.PolicyCodeId equals pcm.Id into pcmJoin
                             from pcm in pcmJoin.DefaultIfEmpty()
                             where td.PropertyId == propertyId && td.IsActive && !td.MarkedForDeletion
                                && tm.IsActive && (pcm == null || pcm.IsActive)
                             orderby tm.DisplayOrder
                             select new ValueTuple<string, string, string, decimal?, int>(
                                 pcm != null ? pcm.PolicyCode : string.Empty,
                                 pcm != null ? pcm.PolicyCode : string.Empty,
                                 tm.TaxName,
                                 td.TaxAmount,
                                 td.TaxId
                             ))
                            .ToListAsync(cancellationToken);
        }
        else
        {
            // DBA/lead/business-confirmed schema: PolicyTaxDetails holds exactly ONE active row per
            // (PropertyId, PolicyCodeId, TaxId) -- the CURRENT state only, no per-year history and no
            // PolicyYear column. Retro/arrears years live only in TaxPendingDetails/TaxPendingDetailsRetro,
            // never here, so no year-based filter is needed: IsActive/MarkedForDeletion already scope
            // this query to the property's current, active determination.
            taxData = await (from td in _context.PolicyTaxDetails
                             join tm in _context.TaxMaster on td.TaxId equals tm.Id
                             join tc in _context.TaxCategoryMaster on tm.TaxCategoryId equals tc.Id
                             join pcm in _context.PolicyCodeMaster on td.PolicyCodeId equals pcm.Id into pcmJoin
                             from pcm in pcmJoin.DefaultIfEmpty()
                             where td.PropertyId == propertyId && td.IsActive && !td.MarkedForDeletion
                                && tm.IsActive && (pcm == null || pcm.IsActive)
                             orderby tm.DisplayOrder
                             select new ValueTuple<string, string, string, decimal?, int>(
                                 pcm != null ? pcm.PolicyCode : string.Empty,
                                 pcm != null ? pcm.PolicyName : string.Empty,
                                 tm.TaxName,
                                 td.TaxAmount,
                                 td.TaxId
                             ))
                            .ToListAsync(cancellationToken);
        }

        // Step 3: Return null if no tax details found
        if (taxData.Count == 0)
            return null;

        // Step 3b (RV only): PolicyTaxDetails holds the raw annual Rateable Value amount, but
        // OccupationTaxApplicationService writes certificate-driven (prorated/retrospective)
        // amounts for the CURRENT finance year into TransMast, not back into PolicyTaxDetails.
        // Without this, the Tax Details panel would never reflect a CC/OC/Electricity-Bill date
        // change even though the certificate-change pipeline ran successfully end-to-end. Use the
        // TransMast amount per TaxId when one exists for the current finance year; otherwise fall
        // back to the raw PolicyTaxDetails amount (e.g. the certificate-change pipeline has never
        // run for this property yet). Safe now that taxData (above) is itself already filtered to
        // the current finance year -- this can no longer misattribute a retro year's row.
        Dictionary<int, decimal> transMastOverridesByTaxId = new();
        if (!isCapitalValue)
        {
            var currentYear = _financeYearProvider.GetCurrentFinanceYear();
            var currentFinanceYearId = await _context.YearMaster
                .Where(y => y.Year == currentYear)
                .Select(y => (int?)y.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentFinanceYearId.HasValue)
            {
                transMastOverridesByTaxId = await _context.TransMast
                    .Where(tm => tm.PropertyId == propertyId
                        && tm.FinanceYearId == currentFinanceYearId.Value
                        && tm.CalculationType == "RV"
                        && tm.IsActive
                        && !tm.MarkedForDeletion)
                    .GroupBy(tm => tm.TaxId)
                    .Select(g => new { TaxId = g.Key, Amount = g.Sum(x => x.TaxAmount) })
                    .ToDictionaryAsync(x => x.TaxId, x => x.Amount, cancellationToken);
            }
        }

        // Step 4: Group by PolicyCode and create pivoted structure
        var policies = taxData
            .GroupBy(x => x.Item1)
            .Select(g =>
            {
                var policyName = g.First().Item2;

                var taxAmounts = g
                    .GroupBy(x => x.Item3)
                    .Select(tg => new TaxAmountDetail
                    {
                        TaxName = tg.Key,
                        // Apply the TransMast override once per distinct TaxId (not once per
                        // source PolicyTaxDetails row) -- multiple rows sharing a TaxId, or
                        // multiple TaxIds sharing a TaxName, would otherwise double-count it.
                        TaxAmount = tg
                            .GroupBy(x => x.Item5)
                            .Sum(taxIdGroup => transMastOverridesByTaxId.TryGetValue(taxIdGroup.Key, out var overridden)
                                ? overridden
                                : taxIdGroup.Sum(x => x.Item4 ?? 0))
                    })
                    .ToList();

                var taxTotal = taxAmounts.Sum(t => t.TaxAmount);

                return new PolicyTaxDetail
                {
                    PolicyCode = g.Key,
                    PolicyName = policyName,
                    TaxAmounts = taxAmounts,
                    TaxTotal = taxTotal
                };
            })
            .ToList();

        // Step 5 (RV only): attach the year-wise retro/arrears breakdown from
        // TaxPendingDetailsRetro (joined with YearMaster for its YearCode) onto whichever policy
        // group(s) belong to the certificate-tax family (OC/CC/Electric-Bill and their PARTIAL_
        // variants) -- this is purely additive display data alongside the current-year TaxAmounts
        // computed above; it does not change what those current-year figures are, and it does not
        // reintroduce retro-year rows into the main taxData query (Step 2 above still correctly
        // filters PolicyTaxDetails to the current finance year only).
        if (!isCapitalValue)
        {
            // Floor the pending-years breakdown at the earliest active taxable formal certificate's
            // (CC or OC) finance year when formal certificates exist, or Electric Bill when only
            // Electric Bill exists -- ensuring Electric Bill does not apply or floor tax before CC/OC onset.
            var certsQuery = _context.PropertyCertificates
                .Where(pc => pc.PropertyId == propertyId && pc.IsActive && !pc.MarkedForDeletion
                    && pc.IssueDate.HasValue && pc.CertificateType != null && pc.CertificateType.IsTaxable);

            var formalCertDate = await certsQuery
                .Where(pc => pc.CertificateType != null &&
                             ((pc.CertificateType.CertificateTypeCode != null && (pc.CertificateType.CertificateTypeCode.Contains("CC") || pc.CertificateType.CertificateTypeCode.Contains("OC"))) ||
                              (pc.CertificateType.CertificateTypeName != null && (pc.CertificateType.CertificateTypeName.Contains("Completion") ||
                                                                                   pc.CertificateType.CertificateTypeName.Contains("Commencement") ||
                                                                                   pc.CertificateType.CertificateTypeName.Contains("Occupancy") ||
                                                                                   pc.CertificateType.CertificateTypeName.Contains("Occupation")))))
                .OrderBy(pc => pc.IssueDate)
                .Select(pc => (DateTime?)pc.IssueDate)
                .FirstOrDefaultAsync(cancellationToken);

            var earliestCertIssueDate = formalCertDate ?? await certsQuery
                .OrderBy(pc => pc.IssueDate)
                .Select(pc => (DateTime?)pc.IssueDate)
                .FirstOrDefaultAsync(cancellationToken);

            int? certStartYear = null;
            if (earliestCertIssueDate.HasValue)
            {
                var certDate = earliestCertIssueDate.Value.Date;
                certStartYear = certDate.Month >= 4 ? certDate.Year : certDate.Year - 1;
            }

            var retroPendingQuery = (from tpr in _context.TaxPendingDetailsRetro
                                     join ym in _context.YearMaster on tpr.PendingYearId equals ym.Id
                                     join tm in _context.TaxMaster on tpr.TaxId equals tm.Id
                                     where tpr.PropertyId == propertyId && tpr.IsActive && !tpr.MarkedForDeletion
                                     select new
                                     {
                                         tpr.PendingYearId,
                                         ym.YearCode,
                                         ym.Year,
                                         tm.TaxName,
                                         PendingAmount = tpr.PendingAmount ?? 0m,
                                         tm.DisplayOrder
                                     });

            if (certStartYear.HasValue)
            {
                retroPendingQuery = retroPendingQuery.Where(x => x.Year >= certStartYear.Value);
            }

            var retroPendingRows = await retroPendingQuery.ToListAsync(cancellationToken);

            if (retroPendingRows.Any())
            {
                // Resolve which certificate (OC/CC/ELECTRIC_BILL) actually governed EACH pending
                // year individually, so the grid can tag every year's row with its own real
                // certificate instead of inheriting whichever policy group's PolicyCode string
                // happens to match below (a property whose certificate history changed over time
                // -- e.g. a CC-only period followed by a later OC -- can have retro years governed
                // by different certificate types).
                var certificatesForYearTagging = await (from pc in _context.PropertyCertificates
                                                        join pct in _context.PropertyCertificateTypeMasters on pc.CertificateTypeId equals pct.Id
                                                        where pc.PropertyId == propertyId && pc.IsActive && !pc.MarkedForDeletion
                                                           && pct.IsActive && pc.IssueDate.HasValue
                                                        orderby pc.IssueDate!.Value
                                                        select new
                                                        {
                                                            pc.IssueDate,
                                                            pct.CertificateTypeCode,
                                                            pct.CertificateTypeName
                                                        })
                                                        .ToListAsync(cancellationToken);

                string ResolveYearPolicyCode(int financeYearStart)
                {
                    var fyEnd = new DateTime(financeYearStart + 1, 3, 31);
                    var applicableCert = certificatesForYearTagging
                        .Where(c => c.IssueDate!.Value.Date <= fyEnd)
                        .OrderByDescending(c => c.IssueDate!.Value)
                        .FirstOrDefault();

                    if (applicableCert == null)
                    {
                        return "CC";
                    }

                    var codeUpper = (applicableCert.CertificateTypeCode ?? string.Empty).ToUpperInvariant().Trim();
                    var nameUpper = (applicableCert.CertificateTypeName ?? string.Empty).ToUpperInvariant().Trim();

                    if (codeUpper.Contains("OC") || nameUpper.Contains("OCCUPANCY") || nameUpper.Contains("OCCUPATION"))
                        return "OC";
                    if (codeUpper.Contains("ELECTRIC") || codeUpper.Contains("EB") || codeUpper.Contains("BILL") || nameUpper.Contains("ELECTRIC") || nameUpper.Contains("BILL"))
                        return "ELECTRIC_BILL";
                    if (codeUpper.Contains("CC") || nameUpper.Contains("COMMENCEMENT") || nameUpper.Contains("COMPLETION"))
                        return "CC";

                    return applicableCert.CertificateTypeCode ?? "CC";
                }

                var pendingYearsList = retroPendingRows
                    .GroupBy(r => new { r.PendingYearId, r.YearCode, r.Year })
                    .OrderBy(g => g.Key.Year)
                    .Select(gy =>
                    {
                        var amounts = gy
                            .OrderBy(r => r.DisplayOrder)
                            .Select(r => new TaxAmountDetail
                            {
                                TaxName = r.TaxName,
                                TaxAmount = r.PendingAmount
                            })
                            .ToList();

                        return new PendingYearTaxDetail
                        {
                            PendingYearId = gy.Key.PendingYearId,
                            YearCode = gy.Key.YearCode,
                            PolicyCode = ResolveYearPolicyCode(gy.Key.Year),
                            TaxAmounts = amounts,
                            TaxTotal = amounts.Sum(a => a.TaxAmount)
                        };
                    })
                    .ToList();

                // Synthesize policy groups for any certificate policy code (CC, OC, ELECTRIC_BILL) present in pending years that is missing from policies
                var distinctPolicyCodesInRetro = pendingYearsList
                    .Select(p => p.PolicyCode)
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var retroPolicyCode in distinctPolicyCodesInRetro)
                {
                    var existingPolicy = policies.FirstOrDefault(p =>
                        string.Equals(p.PolicyCode, retroPolicyCode, StringComparison.OrdinalIgnoreCase) ||
                        p.PolicyCode.StartsWith(retroPolicyCode, StringComparison.OrdinalIgnoreCase) ||
                        retroPolicyCode.StartsWith(p.PolicyCode, StringComparison.OrdinalIgnoreCase));

                    if (existingPolicy == null)
                    {
                        var policyMasterRow = await _context.PolicyCodeMaster
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.PolicyCode == retroPolicyCode, cancellationToken);

                        var policyName = policyMasterRow?.PolicyName ?? retroPolicyCode switch
                        {
                            "CC" => "Completion Certificate",
                            "OC" => "Occupancy Certificate",
                            "ELECTRIC_BILL" => "Electricity Bill",
                            _ => retroPolicyCode
                        };

                        var synthPolicy = new PolicyTaxDetail
                        {
                            PolicyCode = retroPolicyCode,
                            PolicyName = policyName,
                            TaxAmounts = new List<TaxAmountDetail>(),
                            TaxTotal = 0m,
                            PendingYears = pendingYearsList.Where(p => string.Equals(p.PolicyCode, retroPolicyCode, StringComparison.OrdinalIgnoreCase)).ToList()
                        };

                        policies.Add(synthPolicy);
                    }
                }

                foreach (var policy in policies)
                {
                    if (policy.PolicyCode.Equals("NETTAX", StringComparison.OrdinalIgnoreCase))
                    {
                        policy.PendingYears = new List<PendingYearTaxDetail>();
                        continue;
                    }

                    var matchingPendingYears = pendingYearsList
                        .Where(p => string.Equals(p.PolicyCode, policy.PolicyCode, StringComparison.OrdinalIgnoreCase) ||
                                   policy.PolicyCode.Contains(p.PolicyCode, StringComparison.OrdinalIgnoreCase) ||
                                   p.PolicyCode.Contains(policy.PolicyCode, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    policy.PendingYears = matchingPendingYears.Any()
                        ? matchingPendingYears
                        : (policy.PolicyCode.Contains("OC", StringComparison.OrdinalIgnoreCase) ||
                           policy.PolicyCode.Contains("CC", StringComparison.OrdinalIgnoreCase) ||
                           policy.PolicyCode.Contains("ELECTRIC", StringComparison.OrdinalIgnoreCase)
                            ? pendingYearsList
                            : new List<PendingYearTaxDetail>());
                }
            }
        }

        // Step 6: order policy groups by PolicyCodeMaster.Id (DBA-configured display order) so the
        // Tax Details grid presents them consistently instead of relying on incidental query order.
        var policyMasterOrder = await _context.PolicyCodeMaster
            .AsNoTracking()
            .Select(p => new { p.PolicyCode, p.Id })
            .ToDictionaryAsync(p => p.PolicyCode, p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        policies = policies
            .OrderBy(p => policyMasterOrder.TryGetValue(p.PolicyCode, out var order) ? order : int.MaxValue)
            .ThenBy(p => p.PolicyCode)
            .ToList();

        return policies;
    }

    public async Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var policies = await GetTaxDetailsPivotedAsync(
            propertyId,
            isCapitalValue: true,
            excludeEducationEmploymentTax: false,  // Show all taxes for CV
            cancellationToken);

        if (policies == null)
            return null;

        return new PropertyTaxDetailsCVDto
        {
            PropertyId = propertyId,
            Policies = policies
        };
    }

    public async Task<PropertyTaxApartmentDetailsDto?> GetAggregatedPropertyTaxDetailsAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedPropertyNo = string.IsNullOrWhiteSpace(dto.PropertyNo) ? null : dto.PropertyNo.ToLower();
        var normalizedPartType = string.IsNullOrWhiteSpace(dto.PartType) ? null : dto.PartType.ToLower();
        var normalizedPartitionNo = string.IsNullOrWhiteSpace(dto.PartitionNo) ? null : dto.PartitionNo.ToLower();

        var totalwingList = await _context.Set<WingEntity>().AsNoTracking()
            .Where(d => d.IsActive && d.WingNo != null)
            .Select(d => d.WingNo.ToLower())
            .ToListAsync(cancellationToken);

        var isPartitionInWingList = normalizedPartitionNo != null && totalwingList.Contains(normalizedPartitionNo);

        var propertyIds = await (from pm in _context.PropertyMast.AsNoTracking()
                                 join pt in _context.PropertyTypeMasters.AsNoTracking() on pm.PropertyTypeId equals pt.Id
                                 where (dto.WardId == null || pm.WardId == dto.WardId) &&
                                       (normalizedPropertyNo == null || (pm.PropertyNo != null && pm.PropertyNo.ToLower().Contains(normalizedPropertyNo))) &&
                                       (normalizedPartitionNo == null || (pm.PartitionNo != null && 
                                           (isPartitionInWingList 
                                               ? pm.PartitionNo.ToLower().Contains(normalizedPartitionNo) 
                                               : pm.PartitionNo.ToLower() == normalizedPartitionNo))) &&
                                       (normalizedPartType == null || (pt.PartType != null && pt.PartType.ToLower().Contains(normalizedPartType))) &&
                                       (dto.PropertyId == null || pm.Id == dto.PropertyId) &&
                                       pm.IsActive && !pm.MarkedForDeletion &&
                                       pt.IsActive
                                 select pm.Id)
                                .ToListAsync(cancellationToken);

        if (propertyIds == null || !propertyIds.Any())
            return null;

        var taxAmountList = await (from tmrv in _context.TransMast.AsNoTracking()
                                   join tm in _context.TaxMaster.AsNoTracking() on tmrv.TaxId equals tm.Id
                                   join ym in _context.YearMaster.AsNoTracking() on tmrv.FinanceYearId equals ym.Id
                                   where propertyIds.Contains(tmrv.PropertyId)
                                      && tmrv.CalculationType == "RV"
                                      && tmrv.IsActive && !tmrv.MarkedForDeletion
                                      && tm.IsActive
                                      && ym.IsActive
                                   group tmrv by new { tm.TaxName, tm.DisplayOrder } into g
                                   orderby g.Key.DisplayOrder
                                   select new TaxAmountDto
                                   {
                                       TaxName = g.Key.TaxName,
                                       TaxAmount = g.Sum(x => x.TaxAmount),
                                       DisplayOrder = g.Key.DisplayOrder
                                   })
                                  .ToListAsync(cancellationToken);

        if (!taxAmountList.Any())
            return null;

        return new PropertyTaxApartmentDetailsDto
        {
            PropertyId = propertyIds.Count == 1 ? propertyIds[0] : 0,
            PropertyCount = propertyIds.Count,
            TaxAmounts = taxAmountList
        };
    }

    public async Task<PropertyTaxApartmentDetailsCVDto?> GetAggregatedPropertyTaxDetailsCVAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedPropertyNo = string.IsNullOrWhiteSpace(dto.PropertyNo) ? null : dto.PropertyNo.ToLower();
        var normalizedPartType = string.IsNullOrWhiteSpace(dto.PartType) ? null : dto.PartType.ToLower();
        var normalizedPartitionNo = string.IsNullOrWhiteSpace(dto.PartitionNo) ? null : dto.PartitionNo.ToLower();

        var totalwingList = await _context.Set<WingEntity>().AsNoTracking()
            .Where(d => d.IsActive && d.WingNo != null)
            .Select(d => d.WingNo.ToLower())
            .ToListAsync(cancellationToken);

        var isPartitionInWingList = normalizedPartitionNo != null && totalwingList.Contains(normalizedPartitionNo);

        var propertyIds = await (from pm in _context.PropertyMast.AsNoTracking()
                                 join pt in _context.PropertyTypeMasters.AsNoTracking() on pm.PropertyTypeId equals pt.Id
                                 where (dto.WardId == null || pm.WardId == dto.WardId) &&
                                       (normalizedPropertyNo == null || (pm.PropertyNo != null && pm.PropertyNo.ToLower().Contains(normalizedPropertyNo))) &&
                                       (normalizedPartitionNo == null || (pm.PartitionNo != null && 
                                           (isPartitionInWingList 
                                               ? pm.PartitionNo.ToLower().Contains(normalizedPartitionNo) 
                                               : pm.PartitionNo.ToLower() == normalizedPartitionNo))) &&
                                       (normalizedPartType == null || (pt.PartType != null && pt.PartType.ToLower().Contains(normalizedPartType))) &&
                                       (dto.PropertyId == null || pm.Id == dto.PropertyId) &&
                                       pm.IsActive && !pm.MarkedForDeletion &&
                                       pt.IsActive
                                 select pm.Id)
                                .ToListAsync(cancellationToken);

        if (propertyIds == null || !propertyIds.Any())
            return null;

        var taxAmountList = await (from tmcv in _context.TransMast.AsNoTracking()
                                   join tm in _context.TaxMaster.AsNoTracking() on tmcv.TaxId equals tm.Id
                                   join ym in _context.YearMaster.AsNoTracking() on tmcv.FinanceYearId equals ym.Id
                                   where propertyIds.Contains(tmcv.PropertyId)
                                      && tmcv.CalculationType == "CV"
                                      && tmcv.IsActive && !tmcv.MarkedForDeletion
                                      && tm.IsActive
                                      && ym.IsActive
                                   group tmcv by new { tm.TaxName, tm.DisplayOrder } into g
                                   orderby g.Key.DisplayOrder
                                   select new TaxAmountDto
                                   {
                                       TaxName = g.Key.TaxName,
                                       TaxAmount = g.Sum(x => x.TaxAmount),
                                       DisplayOrder = g.Key.DisplayOrder
                                   })
                                  .ToListAsync(cancellationToken);

        if (!taxAmountList.Any())
            return null;

        return new PropertyTaxApartmentDetailsCVDto
        {
            PropertyId = propertyIds.Count == 1 ? propertyIds[0] : 0,
            PropertyCount = propertyIds.Count,
            TaxAmounts = taxAmountList
        };
    }


    public async Task<List<BuildingGenerateStructureDto>?> GetGenerateBuildingStructureAsync(BuildingGenerateDetailsDto dto, CancellationToken cancellationToken = default)
    {
        int iFromFloor = 1;
        int iToFloor = 1;
        int number;

        string? floorCode = "";
        if (dto.GenerationType.ToLower() == "HC".ToLower() & dto.FromFloor != dto.ToFloor)
        {
            throw new InvalidOperationException("From floor and to floor must be same");
        }
        else if (dto.GenerationType.ToLower() == "VC".ToLower() & dto.NoOfFlatOnOneFloor > 1)
        {
            throw new InvalidOperationException("Vertical Custom Generation no of flat in one floor must be 1");
        }


        if (int.TryParse(dto.FromFloor, out number) && number >= 1 && number <= 1000)
        {
            iFromFloor = Convert.ToInt32(dto.FromFloor);
            iToFloor = Convert.ToInt32(dto.ToFloor);

            if (iFromFloor > iToFloor)
            {
                throw new InvalidOperationException("From Floor cannot be greater than To Floor");
            }
        }

        else
        {

            floorCode = dto.FromFloor?.ToString();

            if (dto.GenerationType.ToLower() != "hc" && dto.GenerationType.ToLower() != "vc")
            {
                throw new InvalidOperationException("Select horizontal custom or vertical custom for generation");
            }


        }


        // Step 1: Validate input parameters


        if (dto.NoOfFlatOnOneFloor <= 0)
        {
            throw new InvalidOperationException("No Of Flat On One Floor must be greater than zero");
        }

        if (dto.Prifix != "" && dto.Prifix != null)
        {
            dto.Prifix = dto.Prifix + "-";
        }

        // Step 2: Validate WingId exists and get WingNo
        var wingNo = await _context.Set<WingEntity>()
            .Where(w => w.Id == dto.WingId && w.IsActive)
            .Select(w => w.WingNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (wingNo == null)
        {
            throw new InvalidOperationException("Wing Not Found");
        }

        // Step 3: Get existing property count for partition number calculation
        // This is equivalent to @LastPropertyNo in the SQL query
        var lastPropertyNo = await (from p in _context.PropertyMast
                                    join s in _context.SocietyDetailsMast on p.SocietyDetailId equals s.Id
                                    where p.WardId == dto.WardId
                                          && p.PropertyNo == dto.PropertyNo
                                          && s.WingId == dto.WingId
                                          && p.IsActive
                                          && !p.MarkedForDeletion
                                          && s.IsActive
                                          && !s.MarkedForDeletion
                                    select p).CountAsync(cancellationToken);

        // Step 4: Generate floor and unit sequences (equivalent to CTEs in SQL)
        // Floors CTE: SELECT @FromFloor AS FloorNo UNION ALL SELECT FloorNo + 1 FROM Floors WHERE FloorNo < @ToFloor
        var floors = Enumerable.Range(iFromFloor, iToFloor - iFromFloor + 1).ToList();

        // Units CTE: SELECT 1 AS UnitNo UNION ALL SELECT UnitNo + 1 FROM Units WHERE UnitNo < @NoOfFlatOnOneFloor
        var units = Enumerable.Range(1, dto.NoOfFlatOnOneFloor).ToList();

        // Step 5: Vertical Generation - Cross join ordered by UnitNo, then FloorNo

        // Determine generation type flags
        var isVertical = dto.GenerationType.Equals("V", StringComparison.OrdinalIgnoreCase) ||
                         dto.GenerationType.Equals("VC", StringComparison.OrdinalIgnoreCase);
        var isHorizontal = dto.GenerationType.Equals("H", StringComparison.OrdinalIgnoreCase) ||
                           dto.GenerationType.Equals("HC", StringComparison.OrdinalIgnoreCase);
        var isHC = dto.GenerationType.Equals("HC", StringComparison.OrdinalIgnoreCase);

        if (!isVertical && !isHorizontal)
        {
            throw new InvalidOperationException("Invalid Generation Type");
        }

        // Normalize prefix
        var prefix = !string.IsNullOrEmpty(dto.Prifix) ? $"{dto.Prifix}" : string.Empty;
        var normalizedType = dto.GenerationType.ToUpperInvariant();

        // Create cross join of units and floors
        var crossJoin = from u in units
                        from f in floors
                        select (FloorNo: f, UnitNo: u);

        // Apply ordering based on generation type
        // Vertical (V, VC): order by UnitNo then FloorNo
        // Horizontal (H, HC): order by FloorNo then UnitNo
        var orderedItems = isVertical
            ? crossJoin.OrderBy(x => x.UnitNo).ThenBy(x => x.FloorNo)
            : crossJoin.OrderBy(x => x.FloorNo).ThenBy(x => x.UnitNo);

        var floorlst = await _context.FloorEntity.Where(f => f.IsActive)
                            .ToListAsync(cancellationToken);

        // Generate result with floor multiplier (HC uses 0, others use FloorNo - 1)
        return orderedItems
            .Select((item, index) => new BuildingGenerateStructureDto
            {
                WardId = dto.WardId,
                PropertyNo = dto.PropertyNo,
                WingId = dto.WingId,
                RowNo = index + 1,
                FloorNo = item.FloorNo,
                floorCode = string.IsNullOrEmpty(floorCode) ? item.FloorNo.ToString() : floorCode,
                PropertyFloorId = floorlst.Where(e => e.FloorCode == (string.IsNullOrEmpty(floorCode) ? item.FloorNo.ToString() : floorCode)).Select(e => e.Id).FirstOrDefault(),
                UnitNo = item.UnitNo,
                FlatNo = $"{prefix}{dto.FlatStart + (isHC ? 0 : (item.FloorNo - 1) * dto.IncrementedBy) + (item.UnitNo - 1)}",
                PartitionNo = $"{wingNo}{index + 1 + lastPropertyNo}",
                GenerationType = normalizedType
            })
            .ToList();

    }


    public async Task<List<SocietyAminityDetailsDto>?> GetSocietyAmenityDetailsAsync(int SocietyDetailId, bool isAmenity, CancellationToken cancellationToken = default)
    {
        var amenityProperties = await (
            from pm in _context.PropertyMast
            join ptm in _context.PropertyTypeMasters on pm.PropertyTypeId equals ptm.Id
            join wm in _context.WardMaster on pm.WardId equals wm.Id
            join sdm in _context.SocietyDetailsMast on pm.SocietyDetailId equals sdm.Id
            join we in _context.WingEntity on sdm.WingId equals we.Id
            where pm.SocietyDetailId == SocietyDetailId
    && !string.IsNullOrEmpty(pm.PartitionNo)
    && pm.PartitionNo != we.WingNo
    && pm.MarkedForDeletion != true
    && pm.IsActive == true
    && (
                        isAmenity
                            ? ptm.PartType == PartTypeConstants.Amenity
                            : ptm.PartType != PartTypeConstants.Amenity
                     )
            orderby pm.Id descending

            select new SocietyAminityDetailsDto
            {
                PropertyId = pm.Id,
                SocietyDetailId = pm.SocietyDetailId ?? 0,
                WardId = pm.WardId,
                WardNo = wm.WardNo,
                wingId = we.Id,
                WingNo = we.WingNo,
                WingName = sdm.WingName,
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                PartType = ptm.PartType
            })
            .ToListAsync(cancellationToken);

        return amenityProperties;
    }

    public async Task<List<PropertySocietyDetailsDto>?> GetSocietyWingListAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var property = await (
        from p in _context.PropertyMast
        join w in _context.WardMaster
            on p.WardId equals w.Id
        where p.Id == propertyId
              && p.IsActive
              && !p.MarkedForDeletion
        select new
        {
            p.Id,
            WardId = w.Id,
            WardNo = w.WardNo,
            PropertyNo = p.PropertyNo
        }
    ).FirstOrDefaultAsync(cancellationToken);


        if (property == null)
            return null;

        var amenityProperties = await (
      from sdm in _context.SocietyDetailsMast
      join we in _context.WingEntity on sdm.WingId equals we.Id into wingJoin
      from we in wingJoin.Where(x => x.IsActive).DefaultIfEmpty()
      where sdm.PropertyId == property.Id
            && sdm.IsActive
            && !sdm.MarkedForDeletion
      select new PropertySocietyDetailsDto
      {
          PropertyId = sdm.PropertyId,
          SocietyDetailId = sdm.Id,
          WingId = sdm.WingId,
          WingNo = we != null ? we.WingNo : null,
          WardNo = property.WardNo,
          PropertyNo = property.PropertyNo,
          WingName = sdm.WingName,
          SocietyName = sdm.SocietyName,
          SocietyAddress = sdm.SocietyAddress,
          SecretaryName = sdm.SecretaryName,
          ManagerName = sdm.ManagerName,
          LandOwnerName = sdm.LandOwnerName,
          BuilderName = sdm.BuilderName,
          SocietyNameEnglish = sdm.SocietyNameEnglish,
          SocietyAddressEnglish = sdm.SocietyAddressEnglish,
          SecretaryNameEnglish = sdm.SecretaryNameEnglish,
          ManagerNameEnglish = sdm.ManagerNameEnglish,
          LandOwnerNameEnglish = sdm.LandOwnerNameEnglish,
          BuilderNameEnglish = sdm.BuilderNameEnglish,
          ManagerMobileNo = sdm.ManagerMobileNo,
          SecretaryMobileNo = sdm.SecretaryMobileNo,
          SocietyEmailId = sdm.SocietyEmailId,
          SecretaryEmailId = sdm.SecretaryEmailId,
          ManagerEmailId = sdm.ManagerEmailId,
          PropertyCount = _context.PropertyMast
              .Where(pm => pm.SocietyDetailId == sdm.Id
                  && !string.IsNullOrEmpty(pm.PartitionNo)
                  && pm.IsActive
                  && !pm.MarkedForDeletion)
              .Join(_context.PropertyTypeMasters,
                  pm => pm.PropertyTypeId,
                  ptm => ptm.Id,
                  (pm, ptm) => ptm)
              .Count(ptm => ptm.PartType != PartTypeConstants.Amenity && ptm.IsActive),
          AminityCount = _context.PropertyMast
              .Where(pm => pm.SocietyDetailId == sdm.Id
                  && !string.IsNullOrEmpty(pm.PartitionNo)
                  && pm.IsActive
                  && !pm.MarkedForDeletion)
              .Join(_context.PropertyTypeMasters,
                  pm => pm.PropertyTypeId,
                  ptm => ptm.Id,
                  (pm, ptm) => ptm)
              .Count(ptm => ptm.PartType == PartTypeConstants.Amenity && ptm.IsActive),
      })
      .AsNoTracking()
      .ToListAsync(cancellationToken);
        return amenityProperties;
    }

    public async Task<List<BuildingListDto>?> GetBuildingListAsync(int wardId, CancellationToken cancellationToken = default)
    {
        var WardDetails = await _context.WardMaster
     .Where(p => p.Id == wardId && p.IsActive)
     .Select(p => new { p.Id })
     .FirstOrDefaultAsync(cancellationToken);

        if (WardDetails == null)
            return null;

        // Step 1: Query builing list properties as per ward
        var buildingProperties = await (from pm in _context.PropertyMast
                                        join pcm in _context.PropertyCategoryMaster on pm.CategoryId equals pcm.Id
                                        join wm in _context.WardMaster on pm.WardId equals wm.Id
                                        where pm.WardId == wardId
                                        && string.IsNullOrEmpty(pm.PartitionNo)
                                         && pm.IsActive
                                         && !pm.MarkedForDeletion
                                         && wm.IsActive
                                         && pcm.IsActive

                                        select new BuildingListDto
                                        {
                                            PropertyId = pm.Id,
                                            WardNo = wm.WardNo,
                                            CatPropertyCategoryName = pcm.PropertyCategoryName,
                                            PropertyNo = pm.PropertyNo,
                                            PartitionNo = pm.PartitionNo
                                        })
                                      .ToListAsync(cancellationToken);


        return buildingProperties;
    }



    public async Task<bool> IsPropertyExists(int wardId, string propertyNo, int? propertyId)
    {
        return await _context.PropertyMast.AnyAsync(x => 
            x.WardId == wardId && 
            x.PropertyNo == propertyNo && 
            (x.PartitionNo == "" || x.PartitionNo == null) && x.MarkedForDeletion==false &&
            (!propertyId.HasValue || x.Id != propertyId.Value));
    }

    private static CreateNewPropertyResponseDto Failure(string message)
    {
        return new CreateNewPropertyResponseDto
        {
            Success = false,
            Message = message
        };
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        return values.Any(v =>
            source.Contains(v, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all RoomWiseMinusData entities by list of RoomWiseSubmissionId values.
    /// Used during property deletion to mark all minus data records for deletion.
    /// This entity only has RoomWiseSubmissionId column (no PropertyId), so we query by parent RoomWiseSubmissionDetails IDs.
    /// </summary>
    public async Task<List<RoomWiseMinusDataEntity>> GetRoomWiseMinusBySubmissionIdsAsync(List<int> roomWiseSubmissionIds, CancellationToken cancellationToken = default)
    {
        return await _context.RoomWiseMinusData
            .Where(x => roomWiseSubmissionIds.Contains(x.RoomWiseSubmissionId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets PropertyDetails entities for a property.
    /// Used as the first step in property deletion to identify related PropertyDetailsId values.
    /// </summary>
    public async Task<List<PropertyDetailsEntity>> GetPropertyDetailsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyDetails
            .Where(pd => pd.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #region PropertyTaxCalculationRVResults - Entity has BOTH PropertyId AND PropertyDetailsId

    /// <summary>
    /// Gets all PropertyTaxCalculationRVResults for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All RV results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    public async Task<List<RVCalculationResultsEntity>> GetRvResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.RVCalculationResults
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region PropertyTaxCalculationSection129Results - Entity has BOTH PropertyId AND PropertyDetailsId

    /// <summary>
    /// Gets all PropertyTaxCalculationSection129Results for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All Section129 results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    public async Task<List<PropertyTaxCalculationSection129ResultsEntity>> GetSection129ResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyTaxCalculationSection129Results
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Entities with ONLY PropertyDetailsId (no PropertyId column)

    /// <summary>
    /// Gets PropertyOccupancyDetails by PropertyDetailsId list.
    /// This entity only has PropertyDetailId column (no PropertyId), so simple query is sufficient.
    /// </summary>
    public async Task<List<PropertyOccupancyDetailsEntity>> GetPropertyOccupancyByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyOccupancyDetails
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets RenterMast by PropertyDetailsId list.
    /// This entity only has PropertyDetailsId column (no PropertyId), so simple query is sufficient.
    /// </summary>
    public async Task<List<RenterMastEntity>> GetRentersByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.RenterMast
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailsId))
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region RoomWiseSubmissionDetails - Entity has BOTH PropertyId AND PropertyDetailsId (nullable)

    /// <summary>
    /// Gets all RoomWiseSubmissionDetails for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient to catch all records.
    /// Catches all records regardless of PropertyDetailsId state (NULL, valid, or orphaned).
    /// Use this method when deleting a property to ensure no orphaned records remain.
    /// </summary>
    public async Task<List<RoomWiseSubmissionDetailsEntity>> GetRoomWiseSubmissionByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.RoomWiseSubmissionDetails
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Entities with ONLY PropertyId - BaseEntity only (no IHardDeletable)

    /// <summary>
    /// Gets PropertySocialDetails by PropertyId.
    /// This entity extends BaseEntity but does NOT implement IHardDeletable.
    /// Used for deactivation (IsActive=false) during property deletion.
    /// </summary>
    public async Task<List<PropertySocialDetailsEntity>> GetPropertySocialDetailsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PropertySocialDetailsEntity>()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets WaterConnectionMaster by PropertyId.
    /// This entity extends BaseEntity but does NOT implement IHardDeletable.
    /// Used for deactivation (IsActive=false) during property deletion.
    /// </summary>
    public async Task<List<WaterConnectionMasterEntity>> GetWaterConnectionsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WaterConnectionMasterEntity>()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(cancellationToken);
    }

    #endregion

    // TODO: Uncomment when database table structure is finalized for PropertyTaxCalculationCVResultsEntity
    //public async Task<List<PropertyTaxCalculationCVResultsEntity>> GetCvResultsByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    //{
    //    return await _context.PropertyTaxCalculationCVResults
    //        .AsNoTracking()
    //        .Where(x => propertyDetailIds.Contains(x.PropertyDetailsIds))
    //        .ToListAsync(cancellationToken);
    //}

    /// <summary>
    /// Gets RenterDetail entities by PropertyDetailsId list.
    /// This entity only has PropertyDetailsId column (no PropertyId), so simple query is sufficient.
    /// Used during property deletion to identify and mark all renter detail records.
    /// </summary>
    public async Task<List<RenterDetailEntity>> GetRenterDetailsByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default)
    {
        return await _context.RenterDetails
            .AsNoTracking()
            .Where(x => propertyDetailIds.Contains(x.PropertyDetailsId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all related entities for a property that need to be marked for deletion.
    /// Returns entities implementing IHardDeletable.
    /// 
    /// NOTE: Queries are executed sequentially to avoid DbContext concurrency issues.
    /// EF Core's DbContext is not thread-safe and cannot handle parallel queries on the same instance.
    /// </summary>
    public async Task<List<IHardDeletable>> GetRelatedEntitiesForDeletionAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Build the list of related entities
        var relatedEntities = new List<IHardDeletable>();

        // Execute queries sequentially to avoid DbContext concurrency issues
        // Each query is independent but must run one at a time on the same DbContext

        var applyTaxes = await _context.ApplyTaxesMaster.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(applyTaxes);

        var flags = await _context.FlagMaster.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(flags);

        var plots = await _context.PlotDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(plots);

        var policyTax = await _context.PolicyTaxDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(policyTax);

        var assessmentDetails = await _context.PropertyAssessmentDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(assessmentDetails);

        var images = await _context.PropertyImagesMast.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(images);

        // Note: PropertySocialDetails and WaterConnectionMaster do not implement IHardDeletable.
        // These entities are now handled using DeactivatePropertyEntities() in PropertyService.MarkPropertyDetailsAndRelatedAsync().
        // They only get IsActive=false and UpdatedDate set, without MarkedForDeletion flags.

        // Note: PropertyTaxCalculationCVResultsEntity and RenterDetailEntity use PropertyDetailsId (not PropertyId).
        // They are handled in PropertyService.MarkPropertyDetailsAndRelatedAsync() method with TODO comments there.

        var taxPending = await _context.TaxPendingDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPending);

        var taxPendingArchive = await _context.TaxPendingDetailsArchive.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingArchive);

        var taxPendingCV = await _context.TaxPendingDetailsCV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingCV);

        var taxPendingLookup = await _context.TaxPendingDetailsLookup.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingLookup);

        var taxPendingRetro = await _context.TaxPendingDetailsRetro.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingRetro);

        var taxPendingRV = await _context.TaxPendingDetailsRV.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(taxPendingRV);

        // TransMast now holds both RV and CV rows (CalculationType discriminator), so this single
        // query already covers what a separate TransMastCV load used to cover.
        var transMast = await _context.TransMast.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMast);

        var transMastArchive = await _context.TransMastArchive.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastArchive);

        var transMastLookup = await _context.TransMastLookup.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(transMastLookup);

        // TODO: Uncomment when database table structure is finalized

        //var propertyCertificates = await _context.PropertyCertificates.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        //relatedEntities.AddRange(propertyCertificates);

        var propertyAssessments = await _context.PropertyMastDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        relatedEntities.AddRange(propertyAssessments);

        //var societyDetails = await _context.SocietyDetails.Where(x => x.PropertyId == propertyId).ToListAsync(cancellationToken);
        //relatedEntities.AddRange(societyDetails);

        return relatedEntities;
    }

    /// <summary>
    /// Marks a collection of entities for soft deletion using the same logic as Repository.DeleteAsync.
    /// Sets MarkedForDeletion to true, MarkedForDeletionDate to current time (if not already set),
    /// IsActive to false, and UpdatedDate to current time for entities implementing BaseEntity.
    /// This method ensures consistency with the deletion logic in the base Repository class.
    /// </summary>
    /// <typeparam name="T">Entity type that implements IHardDeletable</typeparam>
    /// <param name="entities">The entities to mark for deletion</param>
    public void MarkEntitiesForDeletion<T>(IEnumerable<T> entities) where T : class, IHardDeletable
    {
        var deletionTime = DateTime.Now;

        foreach (var entity in entities)
        {
            // Set hard deletion flags
            entity.MarkedForDeletion = true;

            // Only set deletion date if not already set (preserves original deletion timestamp)
            if (!entity.MarkedForDeletionDate.HasValue)
            {
                entity.MarkedForDeletionDate = deletionTime;
            }

            // Set IsActive and UpdatedDate if the entity is a BaseEntity
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsActive = false;
                baseEntity.UpdatedDate = deletionTime;
            }

            // Mark entity as modified in EF Core
            _context.Entry(entity).State = EntityState.Modified;
        }
    }
    /// <summary>
    /// Deactivates a collection of BaseEntity-derived entities by setting IsActive = false and UpdatedDate = now.
    /// Does NOT touch MarkedForDeletion or MarkedForDeletionDate.
    /// Used for entities that don't implement IHardDeletable (e.g., PropertySocialDetails, WaterConnectionMaster).
    /// </summary>
    /// <param name="entities">The entities to deactivate</param>
    public void DeactivatePropertyEntities(IEnumerable<BaseEntity> entities)
    {
        var now = DateTime.Now;
        foreach (var entity in entities)
        {
            entity.IsActive = false;
            entity.UpdatedDate = now;
            _context.Entry(entity).State = EntityState.Modified;
        }
    }

    public async Task<CreateBulkPropertyResponseDto?> CreateBulkPropertyAsync(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default)
    {

        // Transaction
        PropertyEntity? property = null;
        PropertyAssessmentEntity? propertyMastDetails = null;
        try
        {

            // Property insert
            property = new PropertyEntity
            {
                TaxZoneId = dto.TaxZoneId,
                WardId = dto.WardId,
                PropertyNo = dto.PropertyNo.Trim(),
                PartitionNo = dto.PartitionNo.Trim(),
                PropertySeqNo = dto.PropertySeqNo,
                PropertyTypeId = dto.PropertyTypeId,
                CategoryId = dto.CategoryId,
                OwnerTitle = string.Empty,
                OwnerTitleEnglish = string.Empty,
                OpenPlot = dto.OpenPlot,
                OwnerName = dto.OwnerName,
                OwnerNameEnglish = dto.OwnerNameEnglish,
                FlatOrShopNo = dto.FlatOrShopNo,
                FlatOrShopNoEnglish = dto.FlatOrShopNoEnglish,
                Address = dto?.Address,
                AddressEnglish = dto?.AddressEnglish,
                Location = dto?.Location,
                LocationEnglish = dto?.LocationEnglish,
                SocietyDetailId = dto?.SocietyDetailId,
                PropertyFloorId = dto?.PropertyFloorId,

                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = dto?.CreatedBy
            };

            _context.PropertyMast.Add(property);
            var propertySaveResult = await _context.SaveChangesAsync(cancellationToken);

            // Assessment insert 
            propertyMastDetails = new PropertyAssessmentEntity
            {
                PropertyId = property.Id,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedBy = dto?.CreatedBy
            };

            _context.PropertyMastDetails.Add(propertyMastDetails);
            var assessmentSaveResult = await _context.SaveChangesAsync(cancellationToken);

            if (dto != null && dto.ConstructionTypeId != null && dto.TypeOfUseId != null && dto.SubTypeOfUseId != null && dto.ConstructionYear != null) 
            {
                // PropertyDetails insert
                var propertyDetails = new PropertyDetailsEntity
                {
                    PropertyId = property.Id,
                    FloorId = property!.PropertyFloorId!.Value,
                    ConstructionTypeId = dto!.ConstructionTypeId!.Value,
                    TypeOfUseId = dto.TypeOfUseId!.Value,
                    SubTypeOfUseId = dto.SubTypeOfUseId,
                    ConstructionYear = dto.ConstructionYear,
                    IsActive = true,
                    MarkedForDeletion = false,
                    CreatedBy = dto?.CreatedBy
                };

                _context.PropertyDetails.Add(propertyDetails);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new CreateBulkPropertyResponseDto
            {
                PropertyId = property!.Id,
                Success = true,
                Message = "Property generated successfully."
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Another user modified the same record mid-transaction
            return new CreateBulkPropertyResponseDto
            {
                Success = false,
                Message = $"A concurrency conflict occurred. Please retry. detail: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            // Any unexpected error
            return new CreateBulkPropertyResponseDto
            {
                Success = false,
                Message = $"An unexpected error occurred : {ex.Message}"
            };
        }
    }
    public async Task<PropertyEntity?> CheckBuildingIfExists(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMast.FirstOrDefaultAsync(x => x.WardId == dto.WardId && x.PropertyNo == dto.PropertyNo && x.PartitionNo == "" && x.MarkedForDeletion==false, cancellationToken);
    }
    public async Task<PropertyCategoryEntity?> GetBuildingCategory(int CategoryId, CancellationToken cancellationToken = default)
    {
        return await _context.PropertyCategoryMaster.FirstOrDefaultAsync(x => x.Id == CategoryId, cancellationToken);
    }
    public async Task<PropertyTypeMasterEntity?> GetAmenityPropertyType(CancellationToken cancellationToken = default)
    {
        return await _context.PropertyTypeMasters.FirstOrDefaultAsync(x => x.PartType == PartTypeConstants.Amenity, cancellationToken);
    }
    public async Task<bool> CheckPropertyIfExists(
     CreateBulkPropertyDto dto,
     CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMast.AnyAsync(
            x => x.WardId == dto.WardId
              && x.PropertyNo == dto.PropertyNo
              && x.PartitionNo == dto.PartitionNo && x.MarkedForDeletion==false,
            cancellationToken);
    }
    public async Task<bool> CheckPropertyFlatIfExists(
  CreateBulkPropertyDto dto,
  CancellationToken cancellationToken = default)
    {
        return await _context.PropertyMast.AnyAsync(
            x => x.WardId == dto.WardId
              && x.PropertyNo == dto.PropertyNo
              && x.SocietyDetailId == dto.SocietyDetailId
              && x.FlatOrShopNo == dto.FlatOrShopNo && x.MarkedForDeletion == false,
            cancellationToken);
    }

}

