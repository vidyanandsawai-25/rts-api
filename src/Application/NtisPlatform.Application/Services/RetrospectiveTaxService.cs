using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RetrospectiveTax;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Re-implements the legacy "Retrospective Tax Details" dynamic-PIVOT SQL script in application code.
/// Read-only: resolves the property from Ward + PropertyNo (+ optional PartitionNo), then builds one
/// row per finance year with pending amounts per active tax head from PTIS.TaxPendingDetailsRetro.
///
/// Per the repo convention (see PropertyReassessmentService), everything is EF Core LINQ over
/// <see cref="IRepository{T,TKey}"/> — no raw SQL. The dynamic PIVOT is replaced by an in-memory
/// per-year, per-tax-head projection.
/// </summary>
public class RetrospectiveTaxService : IRetrospectiveTaxService
{
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _propertyAssessmentRepository;
    private readonly IRepository<TaxPendingDetailsRetroEntity, int> _retroRepository;
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;

    public RetrospectiveTaxService(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyAssessmentEntity, int> propertyAssessmentRepository,
        IRepository<TaxPendingDetailsRetroEntity, int> retroRepository,
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository)
    {
        _propertyRepository = propertyRepository;
        _propertyAssessmentRepository = propertyAssessmentRepository;
        _retroRepository = retroRepository;
        _taxMasterRepository = taxMasterRepository;
        _yearMasterRepository = yearMasterRepository;
    }

    public async Task<RetrospectiveTaxDto> GetRetrospectiveTaxAsync(
        RetrospectiveTaxQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var propertyId = await ResolvePropertyIdAsync(query, cancellationToken);

        var result = new RetrospectiveTaxDto { PropertyId = propertyId };

        var activeTaxes = await _taxMasterRepository.GetQueryable()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new { t.Id, t.TaxName })
            .ToListAsync(cancellationToken);

        result.TaxHeadNames = activeTaxes.Select(t => t.TaxName).ToList();

        if (activeTaxes.Count == 0)
            return result;

        var taxIds = activeTaxes.Select(t => t.Id).ToList();

        var retroRows = await _retroRepository.GetQueryable()
            .Where(r => r.PropertyId == propertyId
                        && r.IsActive
                        && !r.MarkedForDeletion
                        && taxIds.Contains(r.TaxId))
            .Select(r => new { r.PendingYearId, r.TaxId, r.PendingAmount })
            .ToListAsync(cancellationToken);

        if (retroRows.Count == 0)
            return result;

        var pendingYearIds = retroRows.Select(r => r.PendingYearId).Distinct().ToList();

        var years = await _yearMasterRepository.GetQueryable()
            .Where(y => pendingYearIds.Contains(y.Id))
            .Select(y => new { y.Id, y.Year, y.YearCode, y.StartDate, y.EndDate })
            .OrderBy(y => y.Year)
            .ToListAsync(cancellationToken);

        // Earliest recorded registration date for the property — used to prorate the earliest year below.
        var propertyRegDate = await _propertyAssessmentRepository.GetQueryable()
            .Where(pa => pa.PropertyId == propertyId
                         && pa.IsActive
                         && !pa.MarkedForDeletion
                         && pa.PropertyRegDate != null)
            .OrderBy(pa => pa.PropertyRegDate)
            .Select(pa => pa.PropertyRegDate)
            .FirstOrDefaultAsync(cancellationToken);

        var amountsByYear = retroRows
            .GroupBy(r => r.PendingYearId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.TaxId, r => r.PendingAmount ?? 0m));

        var earliestYearId = years.Count > 0 ? years[0].Id : (int?)null;

        result.Years = years.Select(y =>
        {
            amountsByYear.TryGetValue(y.Id, out var yearAmounts);
            var amounts = activeTaxes
                .Select(t => yearAmounts != null && yearAmounts.TryGetValue(t.Id, out var amt) ? amt : 0m)
                .ToList();

            return new RetrospectiveTaxYearRowDto
            {
                PendingYearId = y.Id,
                Year = y.Year,
                FinanceYear = y.YearCode ?? string.Empty,
                Days = ComputeDays(y.StartDate, y.EndDate, y.Id == earliestYearId ? propertyRegDate : null),
                Amounts = amounts,
                Total = amounts.Sum()
            };
        }).ToList();

        return result;
    }

    /// <summary>
    /// Full year: EndDate - StartDate + 1. When <paramref name="propertyRegDate"/> is supplied (only for
    /// the property's earliest year) and falls strictly after the year's start (and on/before its end),
    /// the year is prorated from that registration date instead.
    /// </summary>
    private static int? ComputeDays(DateTime? startDate, DateTime? endDate, DateTime? propertyRegDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            return null;

        var start = startDate.Value.Date;
        var end = endDate.Value.Date;

        if (propertyRegDate.HasValue)
        {
            var regDate = propertyRegDate.Value.Date;
            if (regDate > start && regDate <= end)
                start = regDate;
        }

        if (end < start)
        {
            return null;
        }

        return (end - start).Days + 1;
    }

    /// <summary>Mirrors the legacy property lookup: WardId + PropertyNo (+ optional exact PartitionNo).</summary>
    private async Task<int> ResolvePropertyIdAsync(RetrospectiveTaxQueryParameters query, CancellationToken cancellationToken)
    {
        var propertyNo = query.PropertyNo;
        var partitionNo = query.PartitionNo?.Trim();
        var hasPartition = !string.IsNullOrEmpty(partitionNo);

        var matches = await _propertyRepository.GetQueryable()
            .Where(p => p.WardId == query.WardId
                        && p.PropertyNo == propertyNo
                        && p.IsActive
                        && !p.MarkedForDeletion
                        && (hasPartition
                                ? p.PartitionNo == partitionNo
                                : (p.PartitionNo == null || p.PartitionNo == "")))
            .Select(p => p.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
            throw new ArgumentException("No property found for the supplied Ward, Property No and Partition No.");

        if (matches.Count > 1)
            throw new ArgumentException(
                "More than one property matches the supplied Ward and Property No. Please specify a Partition No.");

        return matches[0];
    }
}
