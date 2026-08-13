using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Value-based tax configuration engine over PTIS.TaxPercentageMasterRV
/// (per type-of-use percentages on the rateable value).
/// </summary>
public class ValueBasedTaxService : IValueBasedTaxService
{
    private readonly ApplicationDbContext _context;

    public ValueBasedTaxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ValueBasedTaxRowDto>> GetPercentagesAsync(
        int taxId,
        int? yearRangeRVId,
        string? userGroup,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query =
            from p in _context.TaxPercentageMasterRVs.AsNoTracking()
            join u in _context.TypeOfUse.AsNoTracking() on p.TypeOfUseId equals u.Id into uj
            from tou in uj.DefaultIfEmpty()
            where p.TaxId == taxId
            select new { p, tou };

        if (yearRangeRVId.HasValue)
        {
            query = query.Where(x => x.p.YearRangeRVId == yearRangeRVId.Value);
        }

        if (!string.IsNullOrWhiteSpace(userGroup))
        {
            var g = userGroup.Trim();
            query = query.Where(x => x.tou != null && x.tou.Type != null && x.tou.Type.StartsWith(g));
        }

        query = query.OrderBy(x => x.p.TypeOfUseId);

        var totalCount = await query.CountAsync(cancellationToken);

        var (normalizedPageNumber, effectivePageSize, skip) = PagingGuard.Normalize(pageNumber, pageSize, totalCount);
        pageNumber = normalizedPageNumber;

        var rows = await query
            .Skip(skip)
            .Take(effectivePageSize)
            .Select(x => new ValueBasedTaxRowDto
            {
                Id = x.p.Id,
                TaxId = x.p.TaxId,
                TypeOfUseId = x.p.TypeOfUseId,
                TypeOfUseCode = x.tou != null ? x.tou.TypeOfUseCode : null,
                Description = x.tou != null ? x.tou.Description : null,
                YearRangeRVId = x.p.YearRangeRVId,
                UserGroup = x.tou != null ? x.tou.Type : null,
                BaseType = x.p.BaseType,
                TaxPercentage = x.p.TaxPercentage
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ValueBasedTaxRowDto>(rows, totalCount, pageNumber, effectivePageSize);
    }

    public async Task<int> SaveAsync(
        SaveValueBasedTaxRequest request,
        CancellationToken cancellationToken = default)
    {
        // Reject duplicate TypeOfUseId within the same batch up front — letting them reach
        // SaveChangesAsync would trip the unique index mid-transaction and roll back every valid
        // row in the same request with an opaque DB exception instead of a clear error.
        var duplicateTypeOfUse = request.Rows
            .GroupBy(r => r.TypeOfUseId)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateTypeOfUse != null)
        {
            throw new ArgumentException(
                $"Duplicate row for TypeOfUseId={duplicateTypeOfUse.Key} — each type of use can only appear once per year.");
        }

        var supportsValueConfig = await _context.TaxMaster
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TaxId
                && t.CalculationModeMaster != null
                && t.CalculationModeMaster.UsesValueConfig, cancellationToken);
        if (!supportsValueConfig)
        {
            throw new ArgumentException($"TaxId={request.TaxId} does not exist or does not support value configuration.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var ids = request.Rows.Where(r => r.Id > 0).Select(r => r.Id).ToList();
            // Scoped to this TaxId — a stale or mismatched Id from another tax falls through to
            // the insert branch below instead of silently overwriting that other tax's row.
            var existing = await _context.TaxPercentageMasterRVs
                .Where(p => ids.Contains(p.Id) && p.TaxId == request.TaxId)
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var affected = 0;
            foreach (var row in request.Rows)
            {
                if (row.Id > 0 && existing.TryGetValue(row.Id, out var entity))
                {
                    entity.TypeOfUseId = row.TypeOfUseId;
                    entity.YearRangeRVId = request.YearRangeRVId;
                    entity.BaseType = request.BaseType;
                    entity.TaxPercentage = row.TaxPercentage;
                    entity.UpdatedBy = request.UpdatedBy;
                    entity.UpdatedDate = DateTime.UtcNow;
                }
                else
                {
                    _context.TaxPercentageMasterRVs.Add(new TaxPercentageMasterRVEntity
                    {
                        TaxId = request.TaxId,
                        TypeOfUseId = row.TypeOfUseId,
                        YearRangeRVId = request.YearRangeRVId,
                        BaseType = request.BaseType,
                        TaxPercentage = row.TaxPercentage,
                        IsActive = true,
                        CreatedBy = request.UpdatedBy,
                        CreatedDate = DateTime.UtcNow
                    });
                }
                affected++;
            }

            // Base Type is a tax+year-wide setting, not independent per row — apply it to EVERY
            // row for this tax+year, not just the ones in `Rows` above (typically only the
            // currently-loaded page under server-side pagination). Rows already touched by the
            // loop above are harmlessly re-set to the same value here.
            var otherRows = await _context.TaxPercentageMasterRVs
                .Where(p => p.TaxId == request.TaxId && p.YearRangeRVId == request.YearRangeRVId && p.BaseType != request.BaseType)
                .ToListAsync(cancellationToken);
            foreach (var p in otherRows)
            {
                p.BaseType = request.BaseType;
                p.UpdatedBy = request.UpdatedBy;
                p.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> BulkApplyAsync(
        BulkApplyValueBasedTaxRequest request,
        CancellationToken cancellationToken = default)
    {
        var query =
            from p in _context.TaxPercentageMasterRVs
            join u in _context.TypeOfUse on p.TypeOfUseId equals u.Id into uj
            from tou in uj.DefaultIfEmpty()
            where p.TaxId == request.TaxId && p.YearRangeRVId == request.YearRangeRVId
            select new { p, tou };

        if (!string.IsNullOrWhiteSpace(request.UserGroup))
        {
            var g = request.UserGroup.Trim();
            query = query.Where(x => x.tou != null && x.tou.Type != null && x.tou.Type.StartsWith(g));
        }

        var rows = await query.Select(x => x.p).ToListAsync(cancellationToken);
        foreach (var p in rows)
        {
            p.TaxPercentage = request.TaxPercentage;
            p.UpdatedBy = request.UpdatedBy;
            p.UpdatedDate = DateTime.UtcNow;
        }

        if (rows.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return rows.Count;
    }
}
