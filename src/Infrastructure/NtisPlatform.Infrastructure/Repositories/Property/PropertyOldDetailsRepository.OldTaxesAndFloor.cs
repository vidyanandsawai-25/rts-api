using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Property;

/// <summary>Old taxes and old (historical) floor data access for the Property "Old Details" tab.</summary>
public partial class PropertyOldDetailsRepository
{
    public async Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast — read-only projection.
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Step 2: Get all taxes from TaxMaster where OldTaxStatus = true (regardless of IsActive)
        var oldTaxes = await _context.TaxMaster.AsNoTracking()
            .Where(t => t.OldTaxStatus)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new { t.Id, t.TaxName, t.TaxNameAlias })
            .ToListAsync(cancellationToken);

        if (!oldTaxes.Any())
        {
            // Return empty result if no old taxes are configured
            return new PropertyOldTaxesDetailsDto
            {
                PropertyId = propertyId,
                TaxYears = new List<OldTaxYearDto>()
            };
        }

        // Step 3: Build the result
        var result = new PropertyOldTaxesDetailsDto
        {
            PropertyId = propertyId,
            TaxYears = new List<OldTaxYearDto>()
        };

        // Step 4: Check if TransMastOld records exist for this property
        int? financeYearId = null;
        int? year = null;
        string? yearCode = null;
        Dictionary<int, decimal>? transactionLookup = null;

        if (property.PropertyMastOldId.HasValue)
        {
            var propertyMastOldId = property.PropertyMastOldId.Value;

            // First, get distinct FinanceYearId values, then join with YearMaster (optimized: prevents duplicate joins)
            // Use left join to handle orphaned FinanceYearId rows without matching YearMaster
            var latestYear = await _context.TransMastOld
                .Where(t => t.PropertyMastOldId == propertyMastOldId &&
                           t.IsActive &&
                           !t.MarkedForDeletion)
                .Select(t => t.FinanceYearId)
                .Distinct()
                .GroupJoin(_context.YearMaster,
                          financeYearId => financeYearId,
                          y => y.Id,
                          (financeYearId, yearGroup) => new
                          {
                              FinanceYearId = financeYearId,
                              YearInfo = yearGroup.DefaultIfEmpty().FirstOrDefault()
                          })
                .Select(x => new
                {
                    Id = x.FinanceYearId,
                    Year = x.YearInfo != null ? x.YearInfo.Year : (int?)null,
                    YearCode = x.YearInfo != null ? x.YearInfo.YearCode : null
                })
                .OrderByDescending(y => y.Year ?? 0) // Handle null years by sorting them last
                .ThenByDescending(y => y.Id) // If Year is null, use FinanceYearId as secondary sort
                .FirstOrDefaultAsync(cancellationToken);

            if (latestYear != null)
            {
                financeYearId = latestYear.Id;
                year = latestYear.Year;
                yearCode = latestYear.YearCode;

                // Now fetch only the transactions for the latest year
                var transMastOldData = await _context.TransMastOld
                    .Where(t => t.PropertyMastOldId == propertyMastOldId &&
                               t.FinanceYearId == latestYear.Id &&
                               t.IsActive &&
                               !t.MarkedForDeletion)
                    .Select(t => new { t.TaxId, t.TaxAmount })
                    .ToListAsync(cancellationToken);

                // Build lookup dictionary for O(1) access: TaxId -> TaxAmount
                transactionLookup = transMastOldData.ToDictionary(t => t.TaxId, t => t.TaxAmount);
            }
        }

        // Step 5: Build result with year data (null if no records exist)
        var taxes = new List<TaxDetailDto>();

        foreach (var tax in oldTaxes)
        {
            // Get tax amount from existing data or default to 0
            var taxAmount = 0m;
            if (transactionLookup != null && transactionLookup.TryGetValue(tax.Id, out var amount))
            {
                taxAmount = amount;
            }

            taxes.Add(new TaxDetailDto
            {
                TaxId = tax.Id,
                TaxName = tax.TaxNameAlias ?? tax.TaxName,
                TaxAmount = taxAmount
            });
        }

        result.TaxYears.Add(new OldTaxYearDto
        {
            FinanceYearId = financeYearId,
            Year = year,
            YearCode = yearCode,
            Taxes = taxes
        });

        return result;
    }

    // ---- Old Taxes: validation-data queries (the service applies the business rules) ----

    public async Task<Dictionary<int, int>> GetYearsByIdsAsync(IReadOnlyCollection<int> financeYearIds, CancellationToken cancellationToken = default)
    {
        return await _context.YearMaster
            .AsNoTracking()
            .Where(y => financeYearIds.Contains(y.Id))
            .ToDictionaryAsync(y => y.Id, y => y.Year, cancellationToken);
    }

    public async Task<List<int>> GetValidOldTaxIdsAsync(IReadOnlyCollection<int> taxIds, CancellationToken cancellationToken = default)
    {
        return await _context.TaxMaster
            .AsNoTracking()
            .Where(t => taxIds.Contains(t.Id) && t.OldTaxStatus)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<(int FinanceYearId, int TaxId)>> GetActiveOldTaxKeysAsync(int propertyMastOldId, IReadOnlyCollection<int> financeYearIds, CancellationToken cancellationToken = default)
    {
        var rows = await _context.TransMastOld
            .AsNoTracking()
            .Where(t => t.PropertyMastOldId == propertyMastOldId &&
                       financeYearIds.Contains(t.FinanceYearId) &&
                       t.IsActive &&
                       !t.MarkedForDeletion)
            .Select(t => new { t.FinanceYearId, t.TaxId })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.FinanceYearId, r.TaxId)).ToHashSet();
    }

    public async Task<Dictionary<int, string?>> GetYearCodeMapAsync(IReadOnlyCollection<int> financeYearIds, CancellationToken cancellationToken = default)
    {
        return await _context.YearMaster
            .AsNoTracking()
            .Where(y => financeYearIds.Contains(y.Id))
            .ToDictionaryAsync(y => y.Id, y => y.YearCode, cancellationToken);
    }

    public async Task<Dictionary<int, string>> GetTaxNameMapAsync(IReadOnlyCollection<int> taxIds, CancellationToken cancellationToken = default)
    {
        return await _context.TaxMaster
            .AsNoTracking()
            .Where(t => taxIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.TaxName, cancellationToken);
    }

    // ---- Old Taxes: transactional persistence (no business validation; caller validates first) ----

    public async Task<PropertyOldTaxesDetailsDto?> PersistNewOldTaxesAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default)
    {
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // Use execution strategy for resilience; the transaction makes the create atomic.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                var propertyMastOldId = await EnsurePropertyMastOldForPersistAsync(property.PropertyMastOldId, propertyId, cancellationToken);

                foreach (var yearDto in dto.TaxYears)
                {
                    foreach (var taxDto in yearDto.Taxes)
                    {
                        var newTransaction = new TransMastOldEntity
                        {
                            PropertyMastOldId = propertyMastOldId,
                            FinanceYearId = yearDto.FinanceYearId,
                            TaxId = taxDto.TaxId,
                            TaxAmount = taxDto.TaxAmount,
                            RVorCV = "RV",
                            RVorCVValue = 0m,
                            IsActive = true,
                            MarkedForDeletion = false,
                            CreatedDate = DateTime.Now
                        };

                        await _context.TransMastOld.AddAsync(newTransaction, cancellationToken);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await RecalculateOldTaxTotalsAsync(propertyMastOldId, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (transaction != null)
                    await transaction.CommitAsync(cancellationToken);

                return await GetOldTaxesDetailsAsync(propertyId, cancellationToken);
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<PropertyOldTaxesDetailsDto?> PersistUpsertedOldTaxesAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default)
    {
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        var propertyMastOldId = await EnsurePropertyMastOldForPersistAsync(property.PropertyMastOldId, propertyId, cancellationToken);

        // Prefetch all existing transactions (including soft-deleted) so the upsert can reactivate rows
        // instead of inserting duplicates that would violate the filtered unique index.
        var requestedYearIds = dto.TaxYears.Select(ty => ty.FinanceYearId).Distinct().ToList();
        var allExistingTransactions = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == propertyMastOldId &&
                       requestedYearIds.Contains(t.FinanceYearId))
            .ToListAsync(cancellationToken);

        var transactionLookup = allExistingTransactions
            .GroupBy(t => t.FinanceYearId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .GroupBy(t => t.TaxId)
                    .ToDictionary(
                        tg => tg.Key,
                        tg => tg
                            .OrderByDescending(t => t.IsActive && !t.MarkedForDeletion)
                            .First()));

        foreach (var yearDto in dto.TaxYears)
        {
            var hasYearTransactions = transactionLookup.TryGetValue(yearDto.FinanceYearId, out var yearTransactionsDict);

            foreach (var taxDto in yearDto.Taxes)
            {
                var existingTransaction = hasYearTransactions && yearTransactionsDict!.TryGetValue(taxDto.TaxId, out var trans)
                    ? trans
                    : null;

                if (existingTransaction != null)
                {
                    existingTransaction.TaxAmount = taxDto.TaxAmount;
                    existingTransaction.IsActive = true;
                    existingTransaction.MarkedForDeletion = false;
                    existingTransaction.MarkedForDeletionDate = null;
                    existingTransaction.UpdatedDate = DateTime.Now;
                }
                else
                {
                    var newTransaction = new TransMastOldEntity
                    {
                        PropertyMastOldId = propertyMastOldId,
                        FinanceYearId = yearDto.FinanceYearId,
                        TaxId = taxDto.TaxId,
                        TaxAmount = taxDto.TaxAmount,
                        RVorCV = "RV",
                        RVorCVValue = 0m,
                        IsActive = true,
                        MarkedForDeletion = false,
                        CreatedDate = DateTime.Now
                    };

                    await _context.TransMastOld.AddAsync(newTransaction, cancellationToken);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecalculateOldTaxTotalsAsync(propertyMastOldId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetOldTaxesDetailsAsync(propertyId, cancellationToken);
    }

    /// <summary>Returns the property's PropertyMastOld id, creating and linking a new row (with its own save) when none exists.</summary>
    private async Task<int> EnsurePropertyMastOldForPersistAsync(int? existingPropertyMastOldId, int propertyId, CancellationToken cancellationToken)
    {
        if (existingPropertyMastOldId.HasValue)
            return existingPropertyMastOldId.Value;

        var newPropertyMastOld = new PropertyMastOldEntity
        {
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        };
        await _context.PropertyMastOld.AddAsync(newPropertyMastOld, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var propertyEntity = await _context.PropertyMast.FindAsync(new object[] { propertyId }, cancellationToken);
        if (propertyEntity != null)
        {
            propertyEntity.PropertyMastOldId = newPropertyMastOld.Id;
            propertyEntity.UpdatedDate = DateTime.Now;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return newPropertyMastOld.Id;
    }

    /// <summary>Recomputes PropertyMastOld.OldTotalTax (excluding Interest) and OldGeneralTax from the current TransMastOld rows.</summary>
    private async Task RecalculateOldTaxTotalsAsync(int propertyMastOldId, CancellationToken cancellationToken)
    {
        var oldTaxes = await _context.TaxMaster.AsNoTracking()
            .Where(t => t.OldTaxStatus)
            .Select(t => new { t.Id, t.TaxName, t.TaxNameAlias })
            .ToListAsync(cancellationToken);

        var interestTaxId = oldTaxes.FirstOrDefault(t =>
            t.TaxName.Equals("Interest", StringComparison.OrdinalIgnoreCase) ||
            (t.TaxNameAlias != null && t.TaxNameAlias.Equals("Interest", StringComparison.OrdinalIgnoreCase)))?.Id;

        var generalTaxId = oldTaxes.FirstOrDefault(t =>
            t.TaxName.Equals("General Tax", StringComparison.OrdinalIgnoreCase) ||
            t.TaxName.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase) ||
            (t.TaxNameAlias != null && (t.TaxNameAlias.Equals("General Tax", StringComparison.OrdinalIgnoreCase) ||
                                        t.TaxNameAlias.Equals("GeneralTax", StringComparison.OrdinalIgnoreCase))))?.Id;

        var totalTaxFromTransMastOld = await _context.TransMastOld
            .Where(t => t.PropertyMastOldId == propertyMastOldId &&
                       t.IsActive &&
                       !t.MarkedForDeletion &&
                       (!interestTaxId.HasValue || t.TaxId != interestTaxId.Value))
            .SumAsync(t => (double?)t.TaxAmount, cancellationToken);

        double? generalTaxFromTransMastOld = null;
        if (generalTaxId.HasValue)
        {
            generalTaxFromTransMastOld = await _context.TransMastOld
                .Where(t => t.PropertyMastOldId == propertyMastOldId &&
                           t.IsActive &&
                           !t.MarkedForDeletion &&
                           t.TaxId == generalTaxId.Value)
                .SumAsync(t => (double?)t.TaxAmount, cancellationToken);
        }

        var propertyMastOldEntity = await _context.PropertyMastOld.FindAsync(new object[] { propertyMastOldId }, cancellationToken);
        if (propertyMastOldEntity != null)
        {
            propertyMastOldEntity.OldTotalTax = totalTaxFromTransMastOld;
            if (generalTaxId.HasValue)
            {
                propertyMastOldEntity.OldGeneralTax = generalTaxFromTransMastOld;
            }
            propertyMastOldEntity.UpdatedDate = DateTime.Now;
        }
    }

    public async Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        if (!property.PropertyMastOldId.HasValue)
            return new PropertyDetailsOldListDto { PropertyId = propertyId, FloorDetails = new List<PropertyDetailsOldDto>() };

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Query PropertyDetailsOld with joins to master tables by ID
        var query = from pd in _context.PropertyDetailsOld
                    where pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion

                    join f in _context.FloorEntity on pd.OldFloorId equals f.Id into floorJoin
                    from f in floorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join sf in _context.SubFloorEntity on pd.OldSubFloorId equals sf.Id into subFloorJoin
                    from sf in subFloorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join ct in _context.ConstructionTypeEntity on pd.OldConstructionTypeId equals ct.Id into constructionJoin
                    from ct in constructionJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join tu in _context.TypeOfUse on pd.OldTypeOfUseId equals tu.Id into typeOfUseJoin
                    from tu in typeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join stu in _context.SubTypeOfUse on pd.OldSubTypeOfUseId equals stu.Id into subTypeOfUseJoin
                    from stu in subTypeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    orderby pd.Id

                    select new
                    {
                        Id = pd.Id,
                        OldFloorId = pd.OldFloorId,
                        FloorDescription = f != null ? f.Description : null,
                        OldSubFloorId = pd.OldSubFloorId,
                        SubFloorDescription = sf != null ? sf.Description : null,
                        OldConstructionYear = pd.OldConstructionYear,
                        OldAssessmentYear = pd.OldAssessmentYear,
                        OldConstructionTypeId = pd.OldConstructionTypeId,
                        ConstructionTypeDescription = ct != null ? ct.Description : null,
                        OldTypeOfUseId = pd.OldTypeOfUseId,
                        TypeOfUseDescription = tu != null ? tu.Description : null,
                        OldSubTypeOfUseId = pd.OldSubTypeOfUseId,
                        SubTypeOfUseDescription = stu != null ? stu.Description : null,
                        OldCarpetAreaSqMeter = pd.OldCarpetAreaSqMeter,
                        OldCarpetAreaSqFeet = pd.OldCarpetAreaSqFeet,
                        OldBuiltupAreaSqMeter = pd.OldBuiltupAreaSqMeter,
                        OldBuiltupAreaSqFeet = pd.OldBuiltupAreaSqFeet,
                        MarkedForDeletion = pd.MarkedForDeletion,
                        MarkedForDeletionDate = pd.MarkedForDeletionDate
                    };

        var queryResults = await query.ToListAsync(cancellationToken);

        // Parse years in memory (cannot use TryParse in LINQ to Entities)
        var floorDetails = queryResults.Select(x => new PropertyDetailsOldDto
        {
            Id = x.Id,
            PropertyId = propertyId,
            OldFloorId = x.OldFloorId,
            FloorDescription = x.FloorDescription,
            OldSubFloorId = x.OldSubFloorId,
            SubFloorDescription = x.SubFloorDescription,
            OldConstructionYear = x.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(x.OldConstructionYear) && int.TryParse(x.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = x.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(x.OldAssessmentYear) && int.TryParse(x.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = x.OldConstructionTypeId,
            ConstructionTypeDescription = x.ConstructionTypeDescription,
            OldTypeOfUseId = x.OldTypeOfUseId,
            TypeOfUseDescription = x.TypeOfUseDescription,
            OldSubTypeOfUseId = x.OldSubTypeOfUseId,
            SubTypeOfUseDescription = x.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = x.OldCarpetAreaSqMeter.HasValue ? Math.Round(x.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = x.OldCarpetAreaSqFeet.HasValue ? Math.Round(x.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = x.OldBuiltupAreaSqMeter.HasValue ? Math.Round(x.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = x.OldBuiltupAreaSqFeet.HasValue ? Math.Round(x.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = x.MarkedForDeletion,
            MarkedForDeletionDate = x.MarkedForDeletionDate
        }).ToList();

        return new PropertyDetailsOldListDto
        {
            PropertyId = propertyId,
            FloorDetails = floorDetails
        };
    }

    public async Task<FloorDetailsOldPagedResult?> GetFloorDetailsOldPagedAsync(int propertyId, FloorDetailsOldQuery query, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        if (!property.PropertyMastOldId.HasValue)
        {
            // Normalize metadata for empty result set with unpaged mode
            var emptyPageSize = query.PageSize == -1 ? 1 : query.PageSize;
            var emptyPageNumber = query.PageSize == -1 ? 1 : query.PageNumber;
            return new FloorDetailsOldPagedResult
            {
                TotalCount = 0,
                PageNumber = emptyPageNumber,
                PageSize = emptyPageSize,
                Items = new List<PropertyDetailsOldDto>()
            };
        }

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Build base query with joins to master tables by ID
        var baseQuery = from pd in _context.PropertyDetailsOld
                        where pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion

                        join f in _context.FloorEntity on pd.OldFloorId equals f.Id into floorJoin
                        from f in floorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join sf in _context.SubFloorEntity on pd.OldSubFloorId equals sf.Id into subFloorJoin
                        from sf in subFloorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join ct in _context.ConstructionTypeEntity on pd.OldConstructionTypeId equals ct.Id into constructionJoin
                        from ct in constructionJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join tu in _context.TypeOfUse on pd.OldTypeOfUseId equals tu.Id into typeOfUseJoin
                        from tu in typeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        join stu in _context.SubTypeOfUse on pd.OldSubTypeOfUseId equals stu.Id into subTypeOfUseJoin
                        from stu in subTypeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                        select new
                        {
                            Id = pd.Id,
                            PropertyId = propertyId,
                            OldFloorId = pd.OldFloorId,
                            FloorDescription = f != null ? f.Description : null,
                            OldSubFloorId = pd.OldSubFloorId,
                            SubFloorDescription = sf != null ? sf.Description : null,
                            OldConstructionYear = pd.OldConstructionYear,
                            OldAssessmentYear = pd.OldAssessmentYear,
                            OldConstructionTypeId = pd.OldConstructionTypeId,
                            ConstructionTypeDescription = ct != null ? ct.Description : null,
                            OldTypeOfUseId = pd.OldTypeOfUseId,
                            TypeOfUseDescription = tu != null ? tu.Description : null,
                            OldSubTypeOfUseId = pd.OldSubTypeOfUseId,
                            SubTypeOfUseDescription = stu != null ? stu.Description : null,
                            OldCarpetAreaSqMeter = pd.OldCarpetAreaSqMeter,
                            OldCarpetAreaSqFeet = pd.OldCarpetAreaSqFeet,
                            OldBuiltupAreaSqMeter = pd.OldBuiltupAreaSqMeter,
                            OldBuiltupAreaSqFeet = pd.OldBuiltupAreaSqFeet,
                            MarkedForDeletion = pd.MarkedForDeletion,
                            MarkedForDeletionDate = pd.MarkedForDeletionDate
                        };

        // Step 3: Apply filters
        if (query.OldFloorId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldFloorId == query.OldFloorId.Value);

        if (query.OldSubFloorId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldSubFloorId == query.OldSubFloorId.Value);

        if (query.OldConstructionTypeId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldConstructionTypeId == query.OldConstructionTypeId.Value);

        if (query.OldTypeOfUseId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldTypeOfUseId == query.OldTypeOfUseId.Value);

        if (query.OldSubTypeOfUseId.HasValue)
            baseQuery = baseQuery.Where(x => x.OldSubTypeOfUseId == query.OldSubTypeOfUseId.Value);

        if (!string.IsNullOrWhiteSpace(query.OldConstructionYear))
            baseQuery = baseQuery.Where(x => x.OldConstructionYear == query.OldConstructionYear);

        if (!string.IsNullOrWhiteSpace(query.OldAssessmentYear))
            baseQuery = baseQuery.Where(x => x.OldAssessmentYear == query.OldAssessmentYear);

        // Step 4: Apply search term if provided
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            baseQuery = baseQuery.Where(x =>
                (x.FloorDescription != null && x.FloorDescription.ToLower().Contains(searchTerm)) ||
                (x.SubFloorDescription != null && x.SubFloorDescription.ToLower().Contains(searchTerm)) ||
                (x.ConstructionTypeDescription != null && x.ConstructionTypeDescription.ToLower().Contains(searchTerm)) ||
                (x.TypeOfUseDescription != null && x.TypeOfUseDescription.ToLower().Contains(searchTerm)) ||
                (x.SubTypeOfUseDescription != null && x.SubTypeOfUseDescription.ToLower().Contains(searchTerm))
            );
        }

        // Step 5: Apply sorting
        var isDescending = query.SortOrder?.ToLower() == "desc";
        var sortBy = query.SortBy?.ToLower();

        baseQuery = sortBy switch
        {
            "id" => isDescending ? baseQuery.OrderByDescending(x => x.Id) : baseQuery.OrderBy(x => x.Id),
            "oldfloorid" => isDescending ? baseQuery.OrderByDescending(x => x.OldFloorId) : baseQuery.OrderBy(x => x.OldFloorId),
            "oldsubfloorid" => isDescending ? baseQuery.OrderByDescending(x => x.OldSubFloorId) : baseQuery.OrderBy(x => x.OldSubFloorId),
            "oldconstructiontypeid" => isDescending ? baseQuery.OrderByDescending(x => x.OldConstructionTypeId) : baseQuery.OrderBy(x => x.OldConstructionTypeId),
            "oldtypeofuseid" => isDescending ? baseQuery.OrderByDescending(x => x.OldTypeOfUseId) : baseQuery.OrderBy(x => x.OldTypeOfUseId),
            "oldsubtypeofuseid" => isDescending ? baseQuery.OrderByDescending(x => x.OldSubTypeOfUseId) : baseQuery.OrderBy(x => x.OldSubTypeOfUseId),
            "oldconstructionyear" => isDescending ? baseQuery.OrderByDescending(x => x.OldConstructionYear) : baseQuery.OrderBy(x => x.OldConstructionYear),
            "oldassessmentyear" => isDescending ? baseQuery.OrderByDescending(x => x.OldAssessmentYear) : baseQuery.OrderBy(x => x.OldAssessmentYear),
            _ => baseQuery.OrderBy(x => x.Id)
        };

        // Step 6: Get total count
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Step 7: Apply pagination
        // Handle unpaged mode (PageSize == -1): return all records with normalized metadata
        var returnAllRecords = query.PageSize == -1;
        var pagedQuery = returnAllRecords
            ? baseQuery
            : baseQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize);

        var queryResults = await pagedQuery.ToListAsync(cancellationToken);

        // Step 8: Parse years in memory (cannot use TryParse in LINQ to Entities)
        var floorDetails = queryResults.Select(x => new PropertyDetailsOldDto
        {
            Id = x.Id,
            PropertyId = propertyId,
            OldFloorId = x.OldFloorId,
            FloorDescription = x.FloorDescription,
            OldSubFloorId = x.OldSubFloorId,
            SubFloorDescription = x.SubFloorDescription,
            OldConstructionYear = x.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(x.OldConstructionYear) && int.TryParse(x.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = x.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(x.OldAssessmentYear) && int.TryParse(x.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = x.OldConstructionTypeId,
            ConstructionTypeDescription = x.ConstructionTypeDescription,
            OldTypeOfUseId = x.OldTypeOfUseId,
            TypeOfUseDescription = x.TypeOfUseDescription,
            OldSubTypeOfUseId = x.OldSubTypeOfUseId,
            SubTypeOfUseDescription = x.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = x.OldCarpetAreaSqMeter.HasValue ? Math.Round(x.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = x.OldCarpetAreaSqFeet.HasValue ? Math.Round(x.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = x.OldBuiltupAreaSqMeter.HasValue ? Math.Round(x.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = x.OldBuiltupAreaSqFeet.HasValue ? Math.Round(x.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = x.MarkedForDeletion,
            MarkedForDeletionDate = x.MarkedForDeletionDate
        }).ToList();

        // Normalize pagination metadata for unpaged mode to avoid division by zero in TotalPages calculation
        // When PageSize == -1, set PageNumber = 1 and PageSize = max(1, totalCount) to ensure valid metadata
        var normalizedPageNumber = returnAllRecords ? 1 : query.PageNumber;
        var normalizedPageSize = returnAllRecords ? Math.Max(1, totalCount) : query.PageSize;

        return new FloorDetailsOldPagedResult
        {
            Items = floorDetails,
            TotalCount = totalCount,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize
        };
    }

    public async Task<PropertyDetailsOldDto?> GetFloorDetailsOldByIdAsync(int propertyId, int floorId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        if (!property.PropertyMastOldId.HasValue)
            return null;

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Query single PropertyDetailsOld record with joins
        var query = from pd in _context.PropertyDetailsOld
                    where pd.Id == floorId && pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion

                    join f in _context.FloorEntity on pd.OldFloorId equals f.Id into floorJoin
                    from f in floorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join sf in _context.SubFloorEntity on pd.OldSubFloorId equals sf.Id into subFloorJoin
                    from sf in subFloorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join ct in _context.ConstructionTypeEntity on pd.OldConstructionTypeId equals ct.Id into constructionJoin
                    from ct in constructionJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join tu in _context.TypeOfUse on pd.OldTypeOfUseId equals tu.Id into typeOfUseJoin
                    from tu in typeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join stu in _context.SubTypeOfUse on pd.OldSubTypeOfUseId equals stu.Id into subTypeOfUseJoin
                    from stu in subTypeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    select new
                    {
                        Id = pd.Id,
                        OldFloorId = pd.OldFloorId,
                        FloorDescription = f != null ? f.Description : null,
                        OldSubFloorId = pd.OldSubFloorId,
                        SubFloorDescription = sf != null ? sf.Description : null,
                        OldConstructionYear = pd.OldConstructionYear,
                        OldAssessmentYear = pd.OldAssessmentYear,
                        OldConstructionTypeId = pd.OldConstructionTypeId,
                        ConstructionTypeDescription = ct != null ? ct.Description : null,
                        OldTypeOfUseId = pd.OldTypeOfUseId,
                        TypeOfUseDescription = tu != null ? tu.Description : null,
                        OldSubTypeOfUseId = pd.OldSubTypeOfUseId,
                        SubTypeOfUseDescription = stu != null ? stu.Description : null,
                        OldCarpetAreaSqMeter = pd.OldCarpetAreaSqMeter,
                        OldCarpetAreaSqFeet = pd.OldCarpetAreaSqFeet,
                        OldBuiltupAreaSqMeter = pd.OldBuiltupAreaSqMeter,
                        OldBuiltupAreaSqFeet = pd.OldBuiltupAreaSqFeet,
                        MarkedForDeletion = pd.MarkedForDeletion,
                        MarkedForDeletionDate = pd.MarkedForDeletionDate
                    };

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        return new PropertyDetailsOldDto
        {
            Id = result.Id,
            PropertyId = propertyId,
            OldFloorId = result.OldFloorId,
            FloorDescription = result.FloorDescription,
            OldSubFloorId = result.OldSubFloorId,
            SubFloorDescription = result.SubFloorDescription,
            OldConstructionYear = result.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(result.OldConstructionYear) && int.TryParse(result.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = result.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(result.OldAssessmentYear) && int.TryParse(result.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = result.OldConstructionTypeId,
            ConstructionTypeDescription = result.ConstructionTypeDescription,
            OldTypeOfUseId = result.OldTypeOfUseId,
            TypeOfUseDescription = result.TypeOfUseDescription,
            OldSubTypeOfUseId = result.OldSubTypeOfUseId,
            SubTypeOfUseDescription = result.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = result.OldCarpetAreaSqMeter.HasValue ? Math.Round(result.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = result.OldCarpetAreaSqFeet.HasValue ? Math.Round(result.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = result.OldBuiltupAreaSqMeter.HasValue ? Math.Round(result.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = result.OldBuiltupAreaSqFeet.HasValue ? Math.Round(result.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = result.MarkedForDeletion,
            MarkedForDeletionDate = result.MarkedForDeletionDate
        };
    }

    public async Task<PropertyDetailsOldDto?> AddFloorDetailsOldAsync(int propertyId, AddPropertyDetailsOldDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Get or create PropertyMastOld for this property
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        int propertyMastOldId;

        // Step 2: Check if PropertyMastOld exists or create it
        if (property.PropertyMastOldId.HasValue)
        {
            propertyMastOldId = property.PropertyMastOldId.Value;
        }
        else
        {
            // Auto-create PropertyMastOld record (consistent with UpdateOldDetailsAsync behavior)
            var newPropertyMastOld = new PropertyMastOldEntity
            {
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = DateTime.Now
            };
            await _context.PropertyMastOld.AddAsync(newPropertyMastOld, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            propertyMastOldId = newPropertyMastOld.Id;

            // Update PropertyMast with the new PropertyMastOldId
            var propertyEntity = await _context.PropertyMast.FindAsync(new object[] { propertyId }, cancellationToken);
            if (propertyEntity != null)
            {
                propertyEntity.PropertyMastOldId = propertyMastOldId;
                propertyEntity.UpdatedDate = DateTime.Now;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        // Step 3: Create new entity (foreign-key references are validated by the service)
        var newEntity = new PropertyDetailsOldEntity
        {
            PropertyMastOldId = propertyMastOldId,
            OldFloorId = dto.OldFloorId,
            OldSubFloorId = dto.OldSubFloorId,
            OldConstructionYear = dto.OldConstructionYear,
            OldAssessmentYear = dto.OldAssessmentYear,
            OldConstructionTypeId = dto.OldConstructionTypeId,
            OldTypeOfUseId = dto.OldTypeOfUseId,
            OldSubTypeOfUseId = dto.OldSubTypeOfUseId,
            OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter,
            OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet,
            OldBuiltupAreaSqMeter = dto.OldBuiltupAreaSqMeter,
            OldBuiltupAreaSqFeet = dto.OldBuiltupAreaSqFeet,
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now
        };

        await _context.PropertyDetailsOld.AddAsync(newEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Step 5: Return the newly created record with joined data
        var query = from pd in _context.PropertyDetailsOld
                    where pd.Id == newEntity.Id

                    join f in _context.FloorEntity on pd.OldFloorId equals f.Id into floorJoin
                    from f in floorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join sf in _context.SubFloorEntity on pd.OldSubFloorId equals sf.Id into subFloorJoin
                    from sf in subFloorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join ct in _context.ConstructionTypeEntity on pd.OldConstructionTypeId equals ct.Id into constructionJoin
                    from ct in constructionJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join tu in _context.TypeOfUse on pd.OldTypeOfUseId equals tu.Id into typeOfUseJoin
                    from tu in typeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join stu in _context.SubTypeOfUse on pd.OldSubTypeOfUseId equals stu.Id into subTypeOfUseJoin
                    from stu in subTypeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    select new
                    {
                        Id = pd.Id,
                        OldFloorId = pd.OldFloorId,
                        FloorDescription = f != null ? f.Description : null,
                        OldSubFloorId = pd.OldSubFloorId,
                        SubFloorDescription = sf != null ? sf.Description : null,
                        OldConstructionYear = pd.OldConstructionYear,
                        OldAssessmentYear = pd.OldAssessmentYear,
                        OldConstructionTypeId = pd.OldConstructionTypeId,
                        ConstructionTypeDescription = ct != null ? ct.Description : null,
                        OldTypeOfUseId = pd.OldTypeOfUseId,
                        TypeOfUseDescription = tu != null ? tu.Description : null,
                        OldSubTypeOfUseId = pd.OldSubTypeOfUseId,
                        SubTypeOfUseDescription = stu != null ? stu.Description : null,
                        OldCarpetAreaSqMeter = pd.OldCarpetAreaSqMeter,
                        OldCarpetAreaSqFeet = pd.OldCarpetAreaSqFeet,
                        OldBuiltupAreaSqMeter = pd.OldBuiltupAreaSqMeter,
                        OldBuiltupAreaSqFeet = pd.OldBuiltupAreaSqFeet,
                        MarkedForDeletion = pd.MarkedForDeletion,
                        MarkedForDeletionDate = pd.MarkedForDeletionDate
                    };

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        return new PropertyDetailsOldDto
        {
            Id = result.Id,
            PropertyId = propertyId,
            OldFloorId = result.OldFloorId,
            FloorDescription = result.FloorDescription,
            OldSubFloorId = result.OldSubFloorId,
            SubFloorDescription = result.SubFloorDescription,
            OldConstructionYear = result.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(result.OldConstructionYear) && int.TryParse(result.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = result.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(result.OldAssessmentYear) && int.TryParse(result.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = result.OldConstructionTypeId,
            ConstructionTypeDescription = result.ConstructionTypeDescription,
            OldTypeOfUseId = result.OldTypeOfUseId,
            TypeOfUseDescription = result.TypeOfUseDescription,
            OldSubTypeOfUseId = result.OldSubTypeOfUseId,
            SubTypeOfUseDescription = result.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = result.OldCarpetAreaSqMeter.HasValue ? Math.Round(result.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = result.OldCarpetAreaSqFeet.HasValue ? Math.Round(result.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = result.OldBuiltupAreaSqMeter.HasValue ? Math.Round(result.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = result.OldBuiltupAreaSqFeet.HasValue ? Math.Round(result.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = result.MarkedForDeletion,
            MarkedForDeletionDate = result.MarkedForDeletionDate
        };
    }

    public async Task<PropertyDetailsOldDto?> UpdateFloorDetailsOldAsync(int propertyId, int floorId, UpdatePropertyDetailsOldDto dto, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return null;

        // The service guarantees a linked PropertyMastOld before calling; treat its absence as "no record to update".
        if (!property.PropertyMastOldId.HasValue)
            return null;

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Get the existing floor record
        var existingRecord = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.Id == floorId && pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion, cancellationToken);

        if (existingRecord == null)
            return null;

        // Step 3: Update the entity (foreign-key references are validated by the service)
        existingRecord.OldFloorId = dto.OldFloorId;
        existingRecord.OldSubFloorId = dto.OldSubFloorId;
        existingRecord.OldConstructionYear = dto.OldConstructionYear;
        existingRecord.OldAssessmentYear = dto.OldAssessmentYear;
        existingRecord.OldConstructionTypeId = dto.OldConstructionTypeId;
        existingRecord.OldTypeOfUseId = dto.OldTypeOfUseId;
        existingRecord.OldSubTypeOfUseId = dto.OldSubTypeOfUseId;
        existingRecord.OldCarpetAreaSqMeter = dto.OldCarpetAreaSqMeter;
        existingRecord.OldCarpetAreaSqFeet = dto.OldCarpetAreaSqFeet;
        existingRecord.OldBuiltupAreaSqMeter = dto.OldBuiltupAreaSqMeter;
        existingRecord.OldBuiltupAreaSqFeet = dto.OldBuiltupAreaSqFeet;
        existingRecord.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Step 5: Return the updated record with joined data
        var query = from pd in _context.PropertyDetailsOld
                    where pd.Id == floorId

                    join f in _context.FloorEntity on pd.OldFloorId equals f.Id into floorJoin
                    from f in floorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join sf in _context.SubFloorEntity on pd.OldSubFloorId equals sf.Id into subFloorJoin
                    from sf in subFloorJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join ct in _context.ConstructionTypeEntity on pd.OldConstructionTypeId equals ct.Id into constructionJoin
                    from ct in constructionJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join tu in _context.TypeOfUse on pd.OldTypeOfUseId equals tu.Id into typeOfUseJoin
                    from tu in typeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    join stu in _context.SubTypeOfUse on pd.OldSubTypeOfUseId equals stu.Id into subTypeOfUseJoin
                    from stu in subTypeOfUseJoin.Where(x => x.IsActive).DefaultIfEmpty()

                    select new
                    {
                        Id = pd.Id,
                        OldFloorId = pd.OldFloorId,
                        FloorDescription = f != null ? f.Description : null,
                        OldSubFloorId = pd.OldSubFloorId,
                        SubFloorDescription = sf != null ? sf.Description : null,
                        OldConstructionYear = pd.OldConstructionYear,
                        OldAssessmentYear = pd.OldAssessmentYear,
                        OldConstructionTypeId = pd.OldConstructionTypeId,
                        ConstructionTypeDescription = ct != null ? ct.Description : null,
                        OldTypeOfUseId = pd.OldTypeOfUseId,
                        TypeOfUseDescription = tu != null ? tu.Description : null,
                        OldSubTypeOfUseId = pd.OldSubTypeOfUseId,
                        SubTypeOfUseDescription = stu != null ? stu.Description : null,
                        OldCarpetAreaSqMeter = pd.OldCarpetAreaSqMeter,
                        OldCarpetAreaSqFeet = pd.OldCarpetAreaSqFeet,
                        OldBuiltupAreaSqMeter = pd.OldBuiltupAreaSqMeter,
                        OldBuiltupAreaSqFeet = pd.OldBuiltupAreaSqFeet,
                        MarkedForDeletion = pd.MarkedForDeletion,
                        MarkedForDeletionDate = pd.MarkedForDeletionDate
                    };

        var result = await query.FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        return new PropertyDetailsOldDto
        {
            Id = result.Id,
            PropertyId = propertyId,
            OldFloorId = result.OldFloorId,
            FloorDescription = result.FloorDescription,
            OldSubFloorId = result.OldSubFloorId,
            SubFloorDescription = result.SubFloorDescription,
            OldConstructionYear = result.OldConstructionYear,
            ConstructionYearValue = !string.IsNullOrEmpty(result.OldConstructionYear) && int.TryParse(result.OldConstructionYear, out int cyear) ? cyear : (int?)null,
            OldAssessmentYear = result.OldAssessmentYear,
            AssessmentYearValue = !string.IsNullOrEmpty(result.OldAssessmentYear) && int.TryParse(result.OldAssessmentYear, out int ayear) ? ayear : (int?)null,
            OldConstructionTypeId = result.OldConstructionTypeId,
            ConstructionTypeDescription = result.ConstructionTypeDescription,
            OldTypeOfUseId = result.OldTypeOfUseId,
            TypeOfUseDescription = result.TypeOfUseDescription,
            OldSubTypeOfUseId = result.OldSubTypeOfUseId,
            SubTypeOfUseDescription = result.SubTypeOfUseDescription,
            OldCarpetAreaSqMeter = result.OldCarpetAreaSqMeter.HasValue ? Math.Round(result.OldCarpetAreaSqMeter.Value, 2) : null,
            OldCarpetAreaSqFeet = result.OldCarpetAreaSqFeet.HasValue ? Math.Round(result.OldCarpetAreaSqFeet.Value, 2) : null,
            OldBuiltupAreaSqMeter = result.OldBuiltupAreaSqMeter.HasValue ? Math.Round(result.OldBuiltupAreaSqMeter.Value, 2) : null,
            OldBuiltupAreaSqFeet = result.OldBuiltupAreaSqFeet.HasValue ? Math.Round(result.OldBuiltupAreaSqFeet.Value, 2) : null,
            MarkedForDeletion = result.MarkedForDeletion,
            MarkedForDeletionDate = result.MarkedForDeletionDate
        };
    }

    public async Task<bool> DeleteFloorDetailsOldAsync(int propertyId, int floorId, CancellationToken cancellationToken = default)
    {
        // Step 1: Get PropertyMastOldId from PropertyMast
        var property = await _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.PropertyMastOldId })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            return false;

        if (!property.PropertyMastOldId.HasValue)
            return false;

        var propertyMastOldId = property.PropertyMastOldId.Value;

        // Step 2: Get the existing floor record
        var existingRecord = await _context.PropertyDetailsOld
            .FirstOrDefaultAsync(pd => pd.Id == floorId && pd.PropertyMastOldId == propertyMastOldId && pd.IsActive && !pd.MarkedForDeletion, cancellationToken);

        if (existingRecord == null)
            return false;

        // Step 3: Soft delete the record
        existingRecord.MarkedForDeletion = true;
        existingRecord.IsActive = false;
        existingRecord.MarkedForDeletionDate = DateTime.Now;
        existingRecord.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
