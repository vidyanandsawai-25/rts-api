using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Read-only repository backing the apartment tax-details panel.
/// Every query is AsNoTracking - this feature never writes.
/// </summary>
public sealed class ApartmentTaxDetailsRepository : IApartmentTaxDetailsRepository
{
    private readonly ApplicationDbContext _context;

    public ApartmentTaxDetailsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApartmentTaxDetailsRawData?> GetTaxDetailsAsync(ApartmentTaxDetailsQueryParameters query, CancellationToken cancellationToken = default)
    {
        var propertyIds = await ResolvePropertyIdsAsync(query, cancellationToken);
        if (propertyIds.Count == 0)
        {
            return null;
        }

        var currentTaxes = new List<CurrentTaxByTypeDto>();
        if (query.TaxType is ApartmentTaxType.RV or ApartmentTaxType.Dual)
        {
            currentTaxes.Add(new CurrentTaxByTypeDto { TaxType = "RV", TaxHeads = await GetRvTaxHeadsAsync(propertyIds, cancellationToken) });
        }

        if (query.TaxType is ApartmentTaxType.CV or ApartmentTaxType.Dual)
        {
            currentTaxes.Add(new CurrentTaxByTypeDto { TaxType = "CV", TaxHeads = await GetCvTaxHeadsAsync(propertyIds, cancellationToken) });
        }

        var arrears = await GetArrearsTaxHeadsAsync(propertyIds, cancellationToken);
        var (wingMasterId, wingName, wingNo, societyName) = await ResolveWingContextAsync(propertyIds, query.WingMasterId, cancellationToken);

        return new ApartmentTaxDetailsRawData
        {
            PropertyCount = propertyIds.Count,
            WingMasterId = wingMasterId,
            WingName = wingName,
            WingNo = wingNo,
            SocietyName = societyName,
            CurrentTaxes = currentTaxes,
            Arrears = arrears,
        };
    }

    private async Task<List<int>> ResolvePropertyIdsAsync(ApartmentTaxDetailsQueryParameters query, CancellationToken cancellationToken)
    {
        var propertiesQuery = _context.PropertyMast.AsNoTracking()
            .Where(p => p.WardId == query.WardId && p.PropertyNo == query.PropertyNo && p.IsActive && !p.MarkedForDeletion);

        if (query.WingMasterId is int wingMasterId)
        {
            // WingMasterId is the WingMaster/WingEntity PK - PropertyMast only carries
            // WingDetailId directly, so resolve via WingDetailsMast.WingMasterId first.
            var wingDetailIdsForMaster = _context.WingDetailsMast.AsNoTracking()
                .Where(w => w.WingMasterId == wingMasterId)
                .Select(w => w.Id);

            propertiesQuery = propertiesQuery.Where(p => p.WingDetailId != null && wingDetailIdsForMaster.Contains(p.WingDetailId!.Value));
        }

        return await propertiesQuery.Select(p => p.Id).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Current Rateable Value tax heads, row-wise by policy code: NETTAX from
    /// ptis.PolicyTaxDetails first, then every other IsCurrent policy from PolicyTaxDetails, then
    /// the current finance year's ptis.TransMast rows (labeled "Net Pay") last.
    /// </summary>
    private async Task<List<TaxHeadAmountDto>> GetRvTaxHeadsAsync(List<int> propertyIds, CancellationToken cancellationToken)
    {
        var current = _context.PolicyTaxDetails.AsNoTracking()
            .Where(ptd => propertyIds.Contains(ptd.PropertyId) && ptd.IsActive && !ptd.MarkedForDeletion && ptd.IsCurrent);

        var netTax = await (
            from ptd in current
            join tm in _context.TaxMaster.AsNoTracking() on ptd.TaxId equals tm.Id
            join pcm in _context.PolicyCodeMaster.AsNoTracking() on ptd.PolicyCodeId equals pcm.Id
            where pcm.PolicyCode == PolicyCodes.NetTax
            group ptd by new { ptd.TaxId, tm.TaxName, pcm.PolicyCode } into g
            orderby g.Key.TaxName
            select new TaxHeadAmountDto
            {
                TaxId = g.Key.TaxId,
                TaxName = g.Key.TaxName,
                PolicyCode = g.Key.PolicyCode,
                TaxAmount = g.Sum(x => x.TaxAmount ?? 0m)
            }).ToListAsync(cancellationToken);

        var otherPolicies = await (
            from ptd in current
            join tm in _context.TaxMaster.AsNoTracking() on ptd.TaxId equals tm.Id
            join pcm in _context.PolicyCodeMaster.AsNoTracking() on ptd.PolicyCodeId equals pcm.Id
            where pcm.PolicyCode != PolicyCodes.NetTax
            group ptd by new { ptd.TaxId, tm.TaxName, pcm.PolicyCode, pcm.DisplayOrder } into g
            orderby g.Key.DisplayOrder, g.Key.TaxName
            select new TaxHeadAmountDto
            {
                TaxId = g.Key.TaxId,
                TaxName = g.Key.TaxName,
                PolicyCode = g.Key.PolicyCode,
                TaxAmount = g.Sum(x => x.TaxAmount ?? 0m)
            }).ToListAsync(cancellationToken);

        var transMast = await GetCurrentTransMastTaxHeadsAsync(propertyIds, "RV", cancellationToken);

        return netTax.Concat(otherPolicies).Concat(transMast).ToList();
    }

    /// <summary>
    /// Current Capital Value tax heads, row-wise by policy code: NETTAX from
    /// ptis.PolicyTaxDetailsCV first, then every other IsCurrent policy from PolicyTaxDetailsCV,
    /// then the current finance year's ptis.TransMast rows (labeled "Net Pay") last.
    /// </summary>
    private async Task<List<TaxHeadAmountDto>> GetCvTaxHeadsAsync(List<int> propertyIds, CancellationToken cancellationToken)
    {
        var current = _context.PolicyTaxDetailsCV.AsNoTracking()
            .Where(ptd => propertyIds.Contains(ptd.PropertyId) && ptd.IsActive && !ptd.MarkedForDeletion && ptd.IsCurrent);

        var netTax = await (
            from ptd in current
            join tm in _context.TaxMaster.AsNoTracking() on ptd.TaxId equals tm.Id
            join pcm in _context.PolicyCodeMaster.AsNoTracking() on ptd.PolicyCodeId equals pcm.Id
            where pcm.PolicyCode == PolicyCodes.NetTax
            group ptd by new { ptd.TaxId, tm.TaxName, pcm.PolicyCode } into g
            orderby g.Key.TaxName
            select new TaxHeadAmountDto
            {
                TaxId = g.Key.TaxId,
                TaxName = g.Key.TaxName,
                PolicyCode = g.Key.PolicyCode,
                TaxAmount = g.Sum(x => x.TaxAmount ?? 0m)
            }).ToListAsync(cancellationToken);

        var otherPolicies = await (
            from ptd in current
            join tm in _context.TaxMaster.AsNoTracking() on ptd.TaxId equals tm.Id
            join pcm in _context.PolicyCodeMaster.AsNoTracking() on ptd.PolicyCodeId equals pcm.Id
            where pcm.PolicyCode != PolicyCodes.NetTax
            group ptd by new { ptd.TaxId, tm.TaxName, pcm.PolicyCode, pcm.DisplayOrder } into g
            orderby g.Key.DisplayOrder, g.Key.TaxName
            select new TaxHeadAmountDto
            {
                TaxId = g.Key.TaxId,
                TaxName = g.Key.TaxName,
                PolicyCode = g.Key.PolicyCode,
                TaxAmount = g.Sum(x => x.TaxAmount ?? 0m)
            }).ToListAsync(cancellationToken);

        var transMast = await GetCurrentTransMastTaxHeadsAsync(propertyIds, "CV", cancellationToken);

        return netTax.Concat(otherPolicies).Concat(transMast).ToList();
    }

    /// <summary>
    /// Current finance year's (YearMaster.IsActive) ptis.TransMast tax heads for the given
    /// RV/CV <paramref name="calculationType"/>, restricted to non-retro-demand policy codes (the
    /// retro-demand ones are surfaced separately via <see cref="GetArrearsTaxHeadsAsync"/> once
    /// their year closes). Every matching row is labeled PolicyCode = "Net Pay" and collapsed to
    /// one row per tax head, regardless of which real policy code produced it.
    /// </summary>
    private async Task<List<TaxHeadAmountDto>> GetCurrentTransMastTaxHeadsAsync(List<int> propertyIds, string calculationType, CancellationToken cancellationToken)
    {
        return await (
            from tm in _context.TransMast.AsNoTracking()
            join ym in _context.YearMaster.AsNoTracking() on tm.FinanceYearId equals ym.Id
            join pcm in _context.PolicyCodeMaster.AsNoTracking() on tm.PolicyCodeId equals pcm.Id
            join tmm in _context.TaxMaster.AsNoTracking() on tm.TaxId equals tmm.Id
            where propertyIds.Contains(tm.PropertyId)
                && tm.CalculationType == calculationType
                && ym.IsActive
                && tm.IsActive && !tm.MarkedForDeletion
                && !pcm.IsRetroDemand
            group tm by new { tm.TaxId, tmm.TaxName } into g
            orderby g.Key.TaxName
            select new TaxHeadAmountDto
            {
                TaxId = g.Key.TaxId, 
                TaxName = g.Key.TaxName,
                PolicyCode = "Net Pay",
                TaxAmount = g.Sum(x => x.TaxAmount)
            }).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retro-demand (arrears) tax heads from ptis.TransMast for closed finance years (YearMaster.
    /// IsActive = false), restricted to PolicyCodeMaster rows flagged IsRetroDemand, grouped by
    /// (TaxId, PolicyCodeId) so each policy code shows as its own row with its real PolicyCode.
    /// Not split by RV/CV - returned as one combined array regardless of the requested
    /// <see cref="ApartmentTaxType"/>.
    /// </summary>
    private async Task<List<TaxHeadAmountDto>> GetArrearsTaxHeadsAsync(List<int> propertyIds, CancellationToken cancellationToken)
    {
        return await (
            from tm in _context.TransMast.AsNoTracking()
            join ym in _context.YearMaster.AsNoTracking() on tm.FinanceYearId equals ym.Id
            join pcm in _context.PolicyCodeMaster.AsNoTracking() on tm.PolicyCodeId equals pcm.Id
            join tmm in _context.TaxMaster.AsNoTracking() on tm.TaxId equals tmm.Id
            where propertyIds.Contains(tm.PropertyId)
                && !ym.IsActive
                && tm.IsActive && !tm.MarkedForDeletion
                && pcm.IsRetroDemand
            group tm by new { tm.TaxId, tmm.TaxName, pcm.PolicyCode } into g
            orderby g.Key.TaxName
            select new TaxHeadAmountDto
            {
                TaxId = g.Key.TaxId,
                TaxName = g.Key.TaxName,
                PolicyCode = g.Key.PolicyCode,
                TaxAmount = g.Sum(x => x.TaxAmount)
            }).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Wing/society context for the matched units. WingName/WingNo/WingMasterId are only
    /// populated when every matched unit agrees on it, since a whole-complex query (no
    /// WingMasterId filter) can legitimately span several wings.
    /// </summary>
    private async Task<(int? WingMasterId, string? WingName, string? WingNo, string? SocietyName)> ResolveWingContextAsync(
        List<int> propertyIds, int? requestedWingMasterId, CancellationToken cancellationToken)
    {
        var wingDetailIds = await _context.PropertyMast.AsNoTracking()
            .Where(p => propertyIds.Contains(p.Id) && p.WingDetailId != null)
            .Select(p => p.WingDetailId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (wingDetailIds.Count == 0)
        {
            return (requestedWingMasterId, null, null, null);
        }

        var wingContexts = await (
            from wdm in _context.WingDetailsMast.AsNoTracking()
            join wm in _context.WingEntity.AsNoTracking() on wdm.WingMasterId equals wm.Id into wmj
            from wm in wmj.DefaultIfEmpty()
            join sdm in _context.SocietyDetailsMast.AsNoTracking() on wdm.SocietyDetailsMastId equals sdm.Id into sdmj
            from sdm in sdmj.DefaultIfEmpty()
            where wingDetailIds.Contains(wdm.Id)
            select new
            {
                wdm.Id,
                wdm.WingMasterId,
                wdm.WingName,
                WingNo = wm != null ? wm.WingNo : null,
                SocietyName = sdm != null ? sdm.SocietyName : null
            }).ToListAsync(cancellationToken);

        var distinctSocietyNames = wingContexts.Select(w => w.SocietyName).Distinct().ToList();
        var societyName = distinctSocietyNames.Count == 1 ? distinctSocietyNames[0] : null;

        string? wingName = null;
        string? wingNo = null;
        int? wingMasterId = requestedWingMasterId;

        var distinctWingMasterIds = wingContexts.Select(w => w.WingMasterId).Distinct().ToList();
        if (distinctWingMasterIds.Count == 1)
        {
            wingMasterId = distinctWingMasterIds[0];

            var distinctWingNames = wingContexts.Select(w => w.WingName).Distinct().ToList();
            wingName = distinctWingNames.Count == 1 ? distinctWingNames[0] : null;

            var distinctWingNos = wingContexts.Select(w => w.WingNo).Distinct().ToList();
            wingNo = distinctWingNos.Count == 1 ? distinctWingNos[0] : null;
        }

        return (wingMasterId, wingName, wingNo, societyName);
    }
}
