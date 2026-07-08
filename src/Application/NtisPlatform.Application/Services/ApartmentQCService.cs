using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application-layer service for Apartment QC.
/// Owns all business validation, assembly of raw fetched data into API DTOs,
/// and orchestrates reads and writes via <see cref="IApartmentQCRepository"/> and <see cref="IUnitOfWork"/>.
/// </summary>
public class ApartmentQCService : IApartmentQCService
{
    private readonly IApartmentQCRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ApartmentQCOptions     _options;

    public ApartmentQCService(
        IApartmentQCRepository repository,
        IUnitOfWork unitOfWork,
        IOptions<ApartmentQCOptions> options)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _options    = options.Value;
    }

    // ──────────────────────────────── READ ────────────────────────────────

    public async Task<PagedResult<PropertyApartmentTaxDto>> GetPagedAsync(
        ApartmentQCQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _repository.CountAsync(query, cancellationToken);
        var (pageNumber, pageSize, skip, take) = ResolvePagination(query, totalCount);
        var fetched = await _repository.FetchPagedDataAsync(query, skip, take, query.ResultType, cancellationToken: cancellationToken);
        var items   = BuildPerProperty(fetched);
        return new PagedResult<PropertyApartmentTaxDto>(
            items.Select(BuildDto).ToList(), totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<PropertyApartmentTaxDto>> GetByPropertyDetailAsync(
        int propertyId,
        ApartmentQCResultType resultType,
        CancellationToken cancellationToken = default)
    {
        var fetched = await _repository.FetchByPropertyDataAsync(propertyId, resultType, cancellationToken);
        if (fetched.Properties.Count == 0)
            return new PagedResult<PropertyApartmentTaxDto>(Array.Empty<PropertyApartmentTaxDto>(), 0, 1, 1);

        var items    = BuildPerDetail(fetched.Properties[0], fetched);
        var dtos     = items.Select(BuildDto).ToList();
        var pageSize = dtos.Count > 0 ? dtos.Count : 1;
        return new PagedResult<PropertyApartmentTaxDto>(dtos, dtos.Count, pageNumber: 1, pageSize: pageSize);
    }

    public Task<ApartmentQCFilterOptionsDto> GetFilterOptionsAsync(
        ApartmentQCQueryParameters query,
        string? field,
        CancellationToken cancellationToken = default)
    {
        // Parse the user-supplied string to a typed enum — string routing is a business decision.
        ApartmentQCFilterColumn? column = null;
        if (field != null && Enum.TryParse<ApartmentQCFilterColumn>(field.Trim(), ignoreCase: true, out var parsed))
            column = parsed;

        // Strip column-specific filters so the repo sees full scope (not the currently-applied filter).
        // "Which params are scope vs. column filter" is a business rule — it lives here, not in Infrastructure.
        return _repository.GetFilterOptionsAsync(ScopeOnly(query), column, cancellationToken);
    }

    private static ApartmentQCQueryParameters ScopeOnly(ApartmentQCQueryParameters q) => new()
    {
        WardId     = q.WardId,
        PropertyNo = q.PropertyNo,
        PropertyId = q.PropertyId,
        PartType   = q.PartType,
        Type       = q.Type
        // Wing, ApartmentType, FlatOrShopNo, PropertyType intentionally omitted —
        // filter-options must reflect the full scope, not the currently-selected column filter.
    };

    public Task<OldPropertyLookupDto?> GetOldPropertyDataAsync(
        string oldPropertyNo,
        CancellationToken cancellationToken = default)
        => _repository.GetOldPropertyDataByNoAsync(oldPropertyNo, cancellationToken);

    public async Task<byte[]> ExportToExcelAsync(
        ApartmentQCQueryParameters query,
        ApartmentQCResultType resultType = ApartmentQCResultType.Dual,
        CancellationToken cancellationToken = default)
    {
        // Honour all filter/search/sort params from the caller — but ignore pagination:
        // an export returns every matching row up to the configured cap.
        var totalCount = await _repository.CountAsync(query, cancellationToken);

        if (totalCount > _options.MaxExportRowCount)
            throw new InvalidOperationException(
                $"Export refused: {totalCount} rows match the current filter, which exceeds the cap of {_options.MaxExportRowCount}. " +
                "Narrow the filter (e.g. add WardId / PropertyNo) and try again.");

        IReadOnlyList<PropertyApartmentTaxDto> dtos;
        if (totalCount == 0)
        {
            dtos = Array.Empty<PropertyApartmentTaxDto>();
        }
        else
        {
            var fetched = await _repository.FetchPagedDataAsync(
                query, skip: 0, take: totalCount, resultType, cancellationToken);
            dtos = BuildPerProperty(fetched).Select(BuildDto).ToList();
        }

        return BuildExcelBytes(dtos, resultType);
    }

    // ──────────────────────────────── WRITE ────────────────────────────────

    public async Task<ApartmentQCBulkUpdateResultDto?> UpdateDetailAsync(
        int propertyId,
        List<UpdateApartmentQCDetailsDto> dtos,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Request-level validation (business rules) ──────────────────────
        var preFailures = ValidateBulkUpdateRequest(dtos, out var detailIds);
        if (preFailures.Count > 0)
        {
            return new ApartmentQCBulkUpdateResultDto
            {
                TotalRequested = dtos.Count,
                Updated        = 0,
                Failures       = preFailures
            };
        }

        // ── 2. Property existence check ──────────────────────────────────────
        if (!await _repository.PropertyExistsAsync(propertyId, cancellationToken))
            return null; // maps to HTTP 404 in the controller

        // ── 3. FK existence validation (batched — one round-trip per master table) ──
        var rowFailures = await ValidateFkExistenceAsync(dtos, cancellationToken);
        if (rowFailures.Count > 0)
        {
            return new ApartmentQCBulkUpdateResultDto
            {
                TotalRequested = dtos.Count,
                Updated        = 0,
                Failures       = rowFailures
            };
        }

        // ── 4. Load tracked PropertyDetails entities (scoped to propertyId) ──────
        var detailsById = await _repository.GetTrackedDetailsForUpdateAsync(
            propertyId, detailIds, cancellationToken);

        // Report any DetailId that is not found / does not belong to this property.
        var notFoundFailures = new List<ApartmentQCBulkUpdateFailureDto>();
        foreach (var id in detailIds)
        {
            if (!detailsById.ContainsKey(id))
            {
                notFoundFailures.Add(new ApartmentQCBulkUpdateFailureDto
                {
                    DetailId = id,
                    Reason   = $"PropertyDetails {id} not found for property {propertyId} or is inactive."
                });
            }
        }

        if (notFoundFailures.Count > 0)
        {
            return new ApartmentQCBulkUpdateResultDto
            {
                TotalRequested = dtos.Count,
                Updated        = 0,
                Failures       = notFoundFailures
            };
        }

        // ── 5. Apply patches and persist via IUnitOfWork ──────────────────────
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _repository.ApplyDetailPatches(detailsById, dtos, updatedBy);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return new ApartmentQCBulkUpdateResultDto
        {
            TotalRequested   = dtos.Count,
            Updated          = dtos.Count,
            UpdatedDetailIds = dtos.Select(d => d.DetailId).ToList()
        };
    }

    public async Task<bool> SyncRoomAggregatesAsync(
        int propertyDetailsId,
        int? updatedBy,
        CancellationToken cancellationToken = default)
    {
        const double SqMeterToSqFeet = 10.7639;
        const double BuiltUpFactor   = 1.20;

        var (totalAreaSqMtr, roomCount) =
            await _repository.GetRoomAggregatesAsync(propertyDetailsId, cancellationToken);

        var property = await _repository.GetTrackedPropertyDetailsByIdAsync(propertyDetailsId, cancellationToken);
        if (property is null) return false;

        property.CarpetAreaSqMeter  = totalAreaSqMtr;
        property.CarpetAreaSqFeet   = totalAreaSqMtr * SqMeterToSqFeet;
        property.BuiltupAreaSqMeter = totalAreaSqMtr * BuiltUpFactor;
        property.BuiltupAreaSqFeet  = totalAreaSqMtr * BuiltUpFactor * SqMeterToSqFeet;
        property.NoOfRooms          = roomCount;
        property.UpdatedDate        = DateTime.Now;
        if (updatedBy.HasValue)
            property.UpdatedBy = updatedBy;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<BasicDetailsPatchOutcome> UpdateBasicDetailsAsync(
        int propertyId,
        UpdateApartmentQCBasicDetailsDto dto,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (!HasAnyUpdateableField(dto))
            throw new ArgumentException("At least one field must be provided for update.");

        var outcome = await _repository.PrepareBasicDetailsPatchAsync(
            propertyId, dto, updatedBy, cancellationToken);

        if (outcome != BasicDetailsPatchOutcome.Success)
            return outcome;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return BasicDetailsPatchOutcome.Success;
    }

    // ──────────────────────── PRIVATE: BUSINESS ASSEMBLY ─────────────────────
    // These methods were previously in Infrastructure (ApartmentQCRepository).
    // Business logic — deciding which renter is "latest", how to aggregate areas,
    // how to collect unique sets — belongs in Application, not Infrastructure.

    /// <summary>
    /// Aggregates the fetched data into one <see cref="ApartmentQCRawData"/> per property
    /// (the paged list view: areas summed, collection-valued fields de-duplicated).
    /// </summary>
    private static IReadOnlyList<ApartmentQCRawData> BuildPerProperty(ApartmentQCFetchedData fetched)
    {
        var aggregates = new Dictionary<int, PropertyDetailsAggregate>();
        foreach (var group in fetched.Details.GroupBy(d => d.PropertyId))
        {
            var agg = new PropertyDetailsAggregate();
            foreach (var d in group)
            {
                agg.CarpetSqM       += d.CarpetAreaSqMeter  ?? 0;
                agg.CarpetSqF       += d.CarpetAreaSqFeet   ?? 0;
                agg.BuiltupSqM      += d.BuiltupAreaSqMeter ?? 0;
                agg.BuiltupSqF      += d.BuiltupAreaSqFeet  ?? 0;
                agg.TotalNoOfRooms  += d.NoOfRooms          ?? 0;
                if (d.Floor            != null) agg.Floors.Add(d.Floor);
                if (d.SubFloor         != null) agg.SubFloors.Add(d.SubFloor);
                if (d.TypeOfUse        != null) agg.TypesOfUse.Add(d.TypeOfUse);
                if (d.Type             != null) agg.Types.Add(d.Type);
                if (d.ConstructionType != null) agg.ConstructionTypes.Add(d.ConstructionType);
                if (d.ConstructionYear != null) agg.ConstructionYears.Add(d.ConstructionYear);
                if (d.AssessmentYear   != null) agg.AssessmentYears.Add(d.AssessmentYear);
                if (fetched.Occupancies.TryGetValue(d.Id, out var oc) && oc.OccupancyDate.HasValue
                    && (agg.MinOcDate == null || oc.OccupancyDate < agg.MinOcDate))
                {
                    agg.MinOcDate = oc.OccupancyDate;
                }
                if (d.Id > agg.LatestDetailId) agg.LatestDetailId = d.Id;
            }
            aggregates[group.Key] = agg;
        }

        var result = new List<ApartmentQCRawData>(fetched.Properties.Count);
        foreach (var p in fetched.Properties)
        {
            fetched.OldData.TryGetValue(p.Id, out var oldData);
            fetched.WardZones.TryGetValue(p.WardId, out var wardZone);
            fetched.Tm.TryGetValue(p.Id, out var tm);
            fetched.Tmcv.TryGetValue(p.Id, out var tmcv);
            fetched.Tmrv.TryGetValue(p.Id, out var tmrv);
            fetched.Tp.TryGetValue(p.Id, out var tp);
            fetched.Tpcv.TryGetValue(p.Id, out var tpcv);
            fetched.Tprv.TryGetValue(p.Id, out var tprv);
            aggregates.TryGetValue(p.Id, out var agg);

            var renter = (agg != null && fetched.Renters.TryGetValue(agg.LatestDetailId, out var r)) ? r : null;

            result.Add(MapBase(p, wardZone, oldData, tm, tmcv, tmrv, tp, tpcv, tprv) with
            {
                CarpetAreaSqMeter  = agg?.CarpetSqM        ?? 0,
                CarpetAreaSqFeet   = agg?.CarpetSqF        ?? 0,
                BuiltupAreaSqMeter = agg?.BuiltupSqM       ?? 0,
                BuiltupAreaSqFeet  = agg?.BuiltupSqF       ?? 0,
                NoOfRooms          = agg?.TotalNoOfRooms   > 0 ? agg.TotalNoOfRooms : null,
                Floors             = agg?.Floors            ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
                SubFloors          = agg?.SubFloors          ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
                TypesOfUse         = agg?.TypesOfUse         ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
                Types              = agg?.Types              ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
                ConstructionTypes  = agg?.ConstructionTypes  ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
                ConstructionYears  = agg?.ConstructionYears  ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
                AssessmentYears    = agg?.AssessmentYears    ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
                OCDate             = agg?.MinOcDate,
                RenterName         = renter?.RenterName,
                RenterNameEnglish  = renter?.RenterNameEnglish,
                RentYearly         = renter?.FinalYearlyRent,
                RentMonthly        = renter?.RentMonthly
            });
        }
        return result;
    }

    /// <summary>
    /// Produces one <see cref="ApartmentQCRawData"/> per PropertyDetails row
    /// for the expanded (per-detail) view of a single property.
    /// </summary>
    private static IReadOnlyList<ApartmentQCRawData> BuildPerDetail(
        ApartmentQCPropertyData p,
        ApartmentQCFetchedData  fetched)
    {
        fetched.OldData.TryGetValue(p.Id, out var oldData);
        fetched.WardZones.TryGetValue(p.WardId, out var wardZone);
        fetched.Tm.TryGetValue(p.Id, out var tm);
        fetched.Tmcv.TryGetValue(p.Id, out var tmcv);
        fetched.Tmrv.TryGetValue(p.Id, out var tmrv);
        fetched.Tp.TryGetValue(p.Id, out var tp);
        fetched.Tpcv.TryGetValue(p.Id, out var tpcv);
        fetched.Tprv.TryGetValue(p.Id, out var tprv);

        var details = fetched.Details.Where(d => d.PropertyId == p.Id).ToList();
        var result  = new List<ApartmentQCRawData>(details.Count);

        foreach (var d in details)
        {
            var renter    = fetched.Renters.TryGetValue(d.Id, out var r)      ? r                    : null;
            var occupancy = fetched.Occupancies.TryGetValue(d.Id, out var oc) ? oc.OccupancyDate     : null;
            var rv        = fetched.RvCalc.TryGetValue(d.Id, out var rvRow)   ? rvRow                : null;
            var cv        = fetched.CvCalc.TryGetValue(d.Id, out var cvRow)   ? cvRow                : null;

            result.Add(MapBase(p, wardZone, oldData, tm, tmcv, tmrv, tp, tpcv, tprv) with
            {
                PDNId              = d.Id,
                NoOfRooms          = d.NoOfRooms,
                CarpetAreaSqMeter  = d.CarpetAreaSqMeter  ?? 0,
                CarpetAreaSqFeet   = d.CarpetAreaSqFeet   ?? 0,
                BuiltupAreaSqMeter = d.BuiltupAreaSqMeter ?? 0,
                BuiltupAreaSqFeet  = d.BuiltupAreaSqFeet  ?? 0,
                Floors             = d.Floor            != null ? new[] { d.Floor }            : Array.Empty<string>(),
                SubFloors          = d.SubFloor         != null ? new[] { d.SubFloor }         : Array.Empty<string>(),
                TypesOfUse         = d.TypeOfUse        != null ? new[] { d.TypeOfUse }        : Array.Empty<string>(),
                Types              = d.Type             != null ? new[] { d.Type }             : Array.Empty<string>(),
                ConstructionTypes  = d.ConstructionType != null ? new[] { d.ConstructionType } : Array.Empty<string>(),
                ConstructionYears  = d.ConstructionYear != null ? new[] { d.ConstructionYear } : Array.Empty<string>(),
                AssessmentYears    = d.AssessmentYear   != null ? new[] { d.AssessmentYear }   : Array.Empty<string>(),
                SubTypesOfUse      = d.SubTypeOfUse     != null ? new[] { d.SubTypeOfUse }     : Array.Empty<string>(),
                OCDate             = occupancy,
                RenterName         = renter?.RenterName,
                RenterNameEnglish  = renter?.RenterNameEnglish,
                RentYearly         = renter?.FinalYearlyRent,
                RentMonthly        = renter?.RentMonthly,
                RateableValue      = rv?.RateableValue,
                CalcYearlyRent     = rv?.YearlyRent,
                CalcMonthlyRate    = rv?.MonthlyRate,
                CalcYearlyRate     = rv?.YearlyRate,
                CalcDepreciation   = rv?.Depreciation,
                CalcAnnualRentalValue = rv?.AnnualRentalValue,
                CalcMaintenance    = rv?.Maintenance,
                CapitalValue       = cv?.CapitalValue,
                CalcBaseValue      = cv?.BaseValue,
                CalcFloorFactor    = cv?.FloorFactor,
                CalcAgeFactor      = cv?.AgeFactor,
                CalcNatureFactor   = cv?.NatureFactor,
                CalcUseFactor      = cv?.UseFactor,
                CalcSDRR           = cv?.SDRR,
                FloorFactorId      = cv?.FloorFactorId,
                AgeFactorId        = cv?.AgeFactorId,
                NatureFactorId     = cv?.NatureFactorId,
                UseFactorId        = cv?.UseFactorId
            });
        }
        return result;
    }

    private static ApartmentQCRawData MapBase(
        ApartmentQCPropertyData     p,
        ApartmentQCWardData?        wardZone,
        ApartmentQCOldPropertyData? oldData,
        ApartmentQCTransactionData?   tm,
        ApartmentQCTransactionCVData? tmcv,
        ApartmentQCTransactionRVData? tmrv,
        ApartmentQCTaxPendingData?    tp,
        ApartmentQCTaxPendingData?    tpcv,
        ApartmentQCTaxPendingData?    tprv)
    {
        return new ApartmentQCRawData
        {
            Id                    = p.Id,
            TaxZoneId             = p.TaxZoneId,
            WardId                = p.WardId,
            WardNo                = wardZone?.WardNo,
            ZoneNo                = wardZone?.ZoneNo,
            RawPropertyNo         = p.PropertyNo,
            PartitionNo           = p.PartitionNo,
            MobileNo              = p.MobileNo,
            EmailId               = p.EmailId,
            FlatOrShopNo          = p.FlatOrShopNo,
            FlatOrShopName        = p.FlatOrShopName,
            FlatOrShopNoEnglish   = p.FlatOrShopNoEnglish,
            FlatOrShopNameEnglish = p.FlatOrShopNameEnglish,
            OwnerName             = p.OwnerName,
            OwnerNameEnglish      = p.OwnerNameEnglish,
            OccupierName          = p.OccupierName,
            OccupierNameEnglish   = p.OccupierNameEnglish,
            PartType              = p.PartType,
            PropertyType          = p.PropertyType,
            PropertyTypeName      = p.PropertyTypeName,
            BHK                   = p.BHK,
            Wing                  = p.Wing,
            ApartmentType         = p.ApartmentType,
            OldPropertyNo         = oldData?.OldPropertyNo,
            OldConstructionArea   = oldData?.OldConstructionArea,
            OldConstructionYear   = oldData?.OldConstructionYear,
            OldUseType            = oldData?.OldUseType,
            OldConstructionType   = oldData?.OldConstructionType,
            OldRV                 = oldData?.OldRV,
            OldTotalTax           = oldData?.OldTotalTax,
            OldCSN                = oldData?.OldCSN,
            RVorCVValue           = tm?.RVorCVValue,
            CapitalValue          = tmcv?.CapitalValue,
            RateableValue         = tmrv?.RateableValue,
            TmTaxAmount           = tm?.TmTaxAmount    ?? 0m,
            TmcvTaxAmount         = tmcv?.TmcvTaxAmount ?? 0m,
            TmrvTaxAmount         = tmrv?.TmrvTaxAmount ?? 0m,
            TpPendingAmount       = tp?.PendingAmount   ?? 0m,
            TpcvPendingAmount     = tpcv?.PendingAmount  ?? 0m,
            TprvPendingAmount     = tprv?.PendingAmount  ?? 0m
        };
    }

    // ──────────────────────── PRIVATE: VALIDATION ─────────────────────────

    /// <summary>
    /// Validates the bulk-update payload for request-level rules:
    /// batch-size limit, null rows, no-op rows (no fields to update), and duplicate DetailIds.
    /// Returns an empty list when all checks pass; populates <paramref name="detailIds"/> on success.
    /// </summary>
    private List<ApartmentQCBulkUpdateFailureDto> ValidateBulkUpdateRequest(
        List<UpdateApartmentQCDetailsDto> dtos,
        out HashSet<int> detailIds)
    {
        detailIds = new HashSet<int>();
        var failures   = new List<ApartmentQCBulkUpdateFailureDto>();
        var duplicates = new HashSet<int>();
        var seenIds    = new HashSet<int>();

        if (dtos.Count > _options.MaxBulkUpdateBatchSize)
        {
            failures.Add(new ApartmentQCBulkUpdateFailureDto
            {
                DetailId = 0,
                Reason   = $"Batch size {dtos.Count} exceeds the maximum of {_options.MaxBulkUpdateBatchSize} rows per request."
            });
            return failures;
        }

        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            if (dto is null)
            {
                failures.Add(new ApartmentQCBulkUpdateFailureDto
                    { DetailId = 0, Reason = $"Row {i}: entry is null." });
                continue;
            }

            if (!HasAnyUpdateableField(dto))
            {
                failures.Add(new ApartmentQCBulkUpdateFailureDto
                {
                    DetailId = dto.DetailId,
                    Reason   = $"Row {i} (DetailId {dto.DetailId}): at least one updatable field must be provided."
                });
            }

            if (dto.DetailId > 0 && !seenIds.Add(dto.DetailId))
                duplicates.Add(dto.DetailId);
        }

        if (duplicates.Count > 0)
        {
            failures.Add(new ApartmentQCBulkUpdateFailureDto
            {
                DetailId = 0,
                Reason   = $"Duplicate DetailId(s) in payload: {string.Join(", ", duplicates)}. Each DetailId may appear at most once."
            });
        }

        if (failures.Count == 0)
            detailIds = seenIds;

        return failures;
    }

    /// <summary>
    /// Validates FK existence across all master tables referenced in the batch.
    /// One DB round-trip per master table; reports every offending DetailId at once.
    /// </summary>
    private async Task<List<ApartmentQCBulkUpdateFailureDto>> ValidateFkExistenceAsync(
        List<UpdateApartmentQCDetailsDto> dtos,
        CancellationToken cancellationToken)
    {
        var failures = new List<ApartmentQCBulkUpdateFailureDto>();

        // Floor
        var requestedFloorIds = dtos.Where(d => d.FloorId.HasValue).Select(d => d.FloorId!.Value).ToList();
        if (requestedFloorIds.Count > 0)
        {
            var existing = await _repository.GetExistingFloorIdsAsync(requestedFloorIds, cancellationToken);
            foreach (var dto in dtos.Where(d => d.FloorId.HasValue && !existing.Contains(d.FloorId!.Value)))
            {
                failures.Add(new ApartmentQCBulkUpdateFailureDto
                {
                    DetailId  = dto.DetailId,
                    Reason    = $"FloorId {dto.FloorId} does not exist.",
                    Field     = nameof(dto.FloorId),
                    InvalidId = dto.FloorId
                });
            }
        }

        // ConstructionType
        var requestedCtIds = dtos.Where(d => d.ConstructionTypeId.HasValue).Select(d => d.ConstructionTypeId!.Value).ToList();
        if (requestedCtIds.Count > 0)
        {
            var existing = await _repository.GetExistingConstructionTypeIdsAsync(requestedCtIds, cancellationToken);
            foreach (var dto in dtos.Where(d => d.ConstructionTypeId.HasValue && !existing.Contains(d.ConstructionTypeId!.Value)))
            {
                failures.Add(new ApartmentQCBulkUpdateFailureDto
                {
                    DetailId  = dto.DetailId,
                    Reason    = $"ConstructionTypeId {dto.ConstructionTypeId} does not exist.",
                    Field     = nameof(dto.ConstructionTypeId),
                    InvalidId = dto.ConstructionTypeId
                });
            }
        }

        // TypeOfUse
        var requestedTuIds = dtos.Where(d => d.TypeOfUseId.HasValue).Select(d => d.TypeOfUseId!.Value).ToList();
        if (requestedTuIds.Count > 0)
        {
            var existing = await _repository.GetExistingTypeOfUseIdsAsync(requestedTuIds, cancellationToken);
            foreach (var dto in dtos.Where(d => d.TypeOfUseId.HasValue && !existing.Contains(d.TypeOfUseId!.Value)))
            {
                failures.Add(new ApartmentQCBulkUpdateFailureDto
                {
                    DetailId  = dto.DetailId,
                    Reason    = $"TypeOfUseId {dto.TypeOfUseId} does not exist.",
                    Field     = nameof(dto.TypeOfUseId),
                    InvalidId = dto.TypeOfUseId
                });
            }
        }

        // SubTypeOfUse
        var requestedStuIds = dtos.Where(d => d.SubTypeOfUseId.HasValue).Select(d => d.SubTypeOfUseId!.Value).ToList();
        if (requestedStuIds.Count > 0)
        {
            var existing = await _repository.GetExistingSubTypeOfUseIdsAsync(requestedStuIds, cancellationToken);
            foreach (var dto in dtos.Where(d => d.SubTypeOfUseId.HasValue && !existing.Contains(d.SubTypeOfUseId!.Value)))
            {
                failures.Add(new ApartmentQCBulkUpdateFailureDto
                {
                    DetailId  = dto.DetailId,
                    Reason    = $"SubTypeOfUseId {dto.SubTypeOfUseId} does not exist.",
                    Field     = nameof(dto.SubTypeOfUseId),
                    InvalidId = dto.SubTypeOfUseId
                });
            }
        }

        return failures;
    }

    // ──────────────────────── PRIVATE: MAPPING ────────────────────────────

    private (int pageNumber, int pageSize, int skip, int take) ResolvePagination(
        ApartmentQCQueryParameters query,
        int totalCount)
    {
        var isUnpaged  = query.PageSize == -1;
        var requested  = isUnpaged ? (totalCount > 0 ? totalCount : 1) : Math.Max(1, query.PageSize);
        var pageSize   = Math.Min(requested, _options.MaxUnpagedPageSize);
        var pageNumber = isUnpaged ? 1 : Math.Max(1, query.PageNumber);
        var skip       = (pageNumber - 1) * pageSize;
        return (pageNumber, pageSize, skip, pageSize);
    }

    /// <summary>
    /// Maps an internal <see cref="ApartmentQCRawData"/> record to the API response DTO.
    /// Complex transformations (collection → string, computed tax totals) are handled here
    /// rather than in AutoMapper because they involve multi-step business calculations.
    /// </summary>
    private static PropertyApartmentTaxDto BuildDto(ApartmentQCRawData raw)
    {
        var ward = raw.WardNo ?? string.Empty;
        var pno  = raw.RawPropertyNo ?? string.Empty;
        var formattedPropertyNo = string.IsNullOrEmpty(raw.PartitionNo)
            ? $"{ward}-{pno}"
            : $"{ward}-{pno}-{raw.PartitionNo}";

        return new PropertyApartmentTaxDto
        {
            Id                    = raw.Id,
            PDNId                 = raw.PDNId,
            TaxZoneId             = raw.TaxZoneId,
            ZoneNo                = raw.ZoneNo,
            WardId                = raw.WardId,
            WardNo                = raw.WardNo,
            PropertyNo            = formattedPropertyNo,
            MobileNo              = raw.MobileNo,
            EmailId               = raw.EmailId,
            OldPropertyNo         = raw.OldPropertyNo,
            OldConstructionArea   = raw.OldConstructionArea,
            OldConstructionYear   = raw.OldConstructionYear,
            OldUseType            = raw.OldUseType,
            OldConstructionType   = raw.OldConstructionType,
            OldRV                 = raw.OldRV,
            OldTotalTax           = raw.OldTotalTax,
            OldCSN                = raw.OldCSN,
            FlatOrShopNo          = raw.FlatOrShopNo,
            FlatOrShopName        = raw.FlatOrShopName,
            FlatOrShopNoEnglish   = raw.FlatOrShopNoEnglish,
            FlatOrShopNameEnglish = raw.FlatOrShopNameEnglish,
            OwnerName             = raw.OwnerName,
            OwnerNameEnglish      = raw.OwnerNameEnglish,
            OccupierName          = raw.OccupierName,
            OccupierNameEnglish   = raw.OccupierNameEnglish,
            PartType              = raw.PartType,
            PropertyType          = raw.PropertyType,
            PropertyTypeName      = raw.PropertyTypeName,
            BHK                   = raw.BHK,
            Wing                  = raw.Wing,
            Floor                 = string.Join(", ", raw.Floors),
            SubFloor              = string.Join(", ", raw.SubFloors),
            TypeOfUse             = string.Join(", ", raw.TypesOfUse),
            Type                  = string.Join(", ", raw.Types),
            ApartmentType         = raw.ApartmentType,
            NoOfRooms             = raw.NoOfRooms,
            ConstructionType      = string.Join(", ", raw.ConstructionTypes),
            ConstructionYear      = string.Join(", ", raw.ConstructionYears),
            AssessmentYear        = string.Join(", ", raw.AssessmentYears),
            SubTypeOfUse          = raw.SubTypesOfUse.Count > 0 ? string.Join(", ", raw.SubTypesOfUse) : null,
            OCDate                = raw.OCDate,
            CarpetASqMtr          = raw.CarpetAreaSqMeter,
            CarpetASqFt           = raw.CarpetAreaSqFeet,
            BuiltupASqMtr         = raw.BuiltupAreaSqMeter,
            BuiltupASqFt          = raw.BuiltupAreaSqFeet,
            RenterName            = raw.RenterName,
            RenterNameEnglish     = raw.RenterNameEnglish,
            RentYearly            = raw.RentYearly ?? 0,
            RentMonthly           = raw.RentMonthly ?? 0,
            RVorCVValue           = raw.RVorCVValue,
            CapitalValue          = raw.CapitalValue,
            RateableValue         = raw.RateableValue,
            NewTaxTotal           = raw.TmTaxAmount   + raw.TpPendingAmount,
            NewTaxTotalCV         = raw.TmcvTaxAmount + raw.TpcvPendingAmount,
            NewTaxTotalRV         = raw.TmrvTaxAmount + raw.TprvPendingAmount,
            YearlyRent            = raw.CalcYearlyRent,
            MonthlyRate           = raw.CalcMonthlyRate,
            YearlyRate            = raw.CalcYearlyRate,
            Depreciation          = raw.CalcDepreciation,
            AnnualRentalValue     = raw.CalcAnnualRentalValue,
            Maintenance           = raw.CalcMaintenance,
            SDRR                  = raw.CalcSDRR,
            BaseValue             = raw.CalcBaseValue,
            FloorFactor           = raw.CalcFloorFactor,
            AgeFactor             = raw.CalcAgeFactor,
            NatureFactor          = raw.CalcNatureFactor,
            UseFactor             = raw.CalcUseFactor,
            FloorFactorId         = raw.FloorFactorId,
            AgeFactorId           = raw.AgeFactorId,
            NatureFactorId        = raw.NatureFactorId,
            UseFactorId           = raw.UseFactorId
        };
    }

    private static bool HasAnyUpdateableField(UpdateApartmentQCDetailsDto dto) =>
        dto.FloorId.HasValue            ||
        dto.ConstructionTypeId.HasValue ||
        dto.TypeOfUseId.HasValue        ||
        dto.SubTypeOfUseId.HasValue     ||
        dto.ConstructionYear != null    ||
        dto.AssessmentYear   != null;

    private static bool HasAnyUpdateableField(UpdateApartmentQCBasicDetailsDto dto) =>
        dto.OwnerName      != null ||
        dto.OccupierName   != null ||
        dto.RenterName     != null ||
        dto.PropertyType.HasValue  ||
        dto.BHK            != null ||
        dto.MobileNo       != null ||
        dto.EmailId        != null ||
        dto.Wing           != null ||
        dto.FlatOrShopNo   != null ||
        dto.FlatOrShopName != null ||
        dto.OldPropertyNo  != null;

    // ──────────────────────── PRIVATE: EXCEL EXPORT ───────────────────────

    private sealed record ExcelColumnSpec(
        string Header,
        Func<PropertyApartmentTaxDto, object?> Value,
        ExportSection Section = ExportSection.Common);

    private enum ExportSection { Common, RV, CV }

    private static readonly IReadOnlyList<ExcelColumnSpec> ExcelSchema = new ExcelColumnSpec[]
    {
        // ── Common columns (always included) ──────────────────────────────
        new("Zone",                   d => d.ZoneNo),
        new("Ward",                   d => d.WardNo),
        new("Property No",            d => d.PropertyNo),
        new("Old Property No",        d => d.OldPropertyNo),
        new("Wing Name",              d => d.Wing),
        new("Flat No.",               d => d.FlatOrShopNo),
        new("Owner Name",             d => d.OwnerName),
        new("Occupier Name",          d => d.OccupierName),
        new("Renter Name",            d => d.RenterName),
        new("Rent",                   d => d.RentYearly),
        new("Description",            d => d.PropertyTypeName),
        new("Type",                   d => d.ApartmentType),
        new("Floor",                  d => d.Floor),
        new("Construction Year",      d => d.ConstructionYear),
        new("Assessment Year",        d => d.AssessmentYear),
        new("Construction Type",      d => d.ConstructionType),
        new("BHK",                    d => d.BHK),
        new("Carpet Area (Sq.Mtr)",   d => d.CarpetASqMtr),
        new("Carpet Area (Sq.Ft)",    d => d.CarpetASqFt),
        new("Buildup Area (Sq.Mtr)",  d => d.BuiltupASqMtr),
        new("Buildup Area (Sq.Ft)",   d => d.BuiltupASqFt),
        new("Old Construction Area",  d => d.OldConstructionArea),
        new("Old RV",                 d => d.OldRV),
        new("Old Tax",                d => d.OldTotalTax),
        new("Mobile No",              d => d.MobileNo),
        new("Email ID",               d => d.EmailId),
        new("OC Date",                d => d.OCDate),

        // ── RV section (Rateable or Dual) ─────────────────────────────────
        new("Yearly Rent",            d => d.YearlyRent,         ExportSection.RV),
        new("Monthly Rate",           d => d.MonthlyRate,        ExportSection.RV),
        new("Yearly Rate",            d => d.YearlyRate,         ExportSection.RV),
        new("Depreciation",           d => d.Depreciation,       ExportSection.RV),
        new("Annual Rental Value",    d => d.AnnualRentalValue,  ExportSection.RV),
        new("Maintenance",            d => d.Maintenance,        ExportSection.RV),
        new("New RV",                 d => d.RateableValue,      ExportSection.RV),
        new("New Tax RV",             d => d.NewTaxTotalRV,      ExportSection.RV),

        // ── CV section (Capital or Dual) ──────────────────────────────────
        new("SDRR",                   d => d.SDRR,               ExportSection.CV),
        new("Base Value",             d => d.BaseValue,           ExportSection.CV),
        new("Floor Factor",           d => d.FloorFactor,         ExportSection.CV),
        new("Age Factor",             d => d.AgeFactor,           ExportSection.CV),
        new("Nature Factor",          d => d.NatureFactor,        ExportSection.CV),
        new("Use Factor",             d => d.UseFactor,           ExportSection.CV),
        new("Capital Value",          d => d.CapitalValue,        ExportSection.CV),
        new("New Tax CV",             d => d.NewTaxTotalCV,       ExportSection.CV),
    };

    private const string DecimalFormat = "#,##0.00";
    private const string DateFormat    = "yyyy-MM-dd";

    // PartType value from PropertyTypeMasters → Excel worksheet name.
    private static readonly (string PartType, string SheetName)[] PartTypeSheets =
    {
        ("R",       "Residential"),
        ("C",       "Commercial"),
        ("Amenity", "Amenities"),
    };

    private static byte[] BuildExcelBytes(
        IReadOnlyList<PropertyApartmentTaxDto> rows,
        ApartmentQCResultType resultType)
    {
        var columns = resultType switch
        {
            ApartmentQCResultType.Rateable => ExcelSchema.Where(c => c.Section != ExportSection.CV).ToArray(),
            ApartmentQCResultType.Capital  => ExcelSchema.Where(c => c.Section != ExportSection.RV).ToArray(),
            _                              => ExcelSchema.ToArray()   // Dual = all columns
        };

        // Group rows by PartType so each sheet gets only its own data.
        var byPartType = rows
            .GroupBy(d => d.PartType?.Trim() ?? string.Empty)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PropertyApartmentTaxDto>)g.ToList());

        using var workbook = new XLWorkbook();

        foreach (var (partType, sheetName) in PartTypeSheets)
        {
            var sheet     = workbook.Worksheets.Add(sheetName);
            var sheetRows = byPartType.TryGetValue(partType, out var list)
                ? list
                : Array.Empty<PropertyApartmentTaxDto>();
            WriteSheet(sheet, columns, sheetRows);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteSheet(
        IXLWorksheet worksheet,
        ExcelColumnSpec[] columns,
        IReadOnlyList<PropertyApartmentTaxDto> rows)
    {
        // Header row.
        for (var c = 0; c < columns.Length; c++)
        {
            var cell = worksheet.Cell(1, c + 1);
            cell.Value           = columns[c].Header;
            cell.Style.Font.Bold = true;
        }

        // Data rows.
        for (var r = 0; r < rows.Count; r++)
        {
            var dto = rows[r];
            for (var c = 0; c < columns.Length; c++)
            {
                var raw  = columns[c].Value(dto);
                var cell = worksheet.Cell(r + 2, c + 1);

                if (raw is null) { cell.Clear(); continue; }

                switch (raw)
                {
                    case decimal d:
                        cell.Value = d;
                        cell.Style.NumberFormat.Format = DecimalFormat;
                        break;
                    case DateTime dt:
                        cell.Value = dt;
                        cell.Style.NumberFormat.Format = DateFormat;
                        break;
                    case int i:    cell.Value = i;    break;
                    case long l:   cell.Value = l;    break;
                    case bool b:   cell.Value = b;    break;
                    default:       cell.Value = raw.ToString(); break;
                }
            }
        }

        worksheet.Columns().AdjustToContents();
    }

    // ──────────────────────── PRIVATE NESTED TYPE ─────────────────────────

    private sealed class PropertyDetailsAggregate
    {
        public decimal             CarpetSqM         { get; set; }
        public decimal             CarpetSqF         { get; set; }
        public decimal             BuiltupSqM        { get; set; }
        public decimal             BuiltupSqF        { get; set; }
        public int                 TotalNoOfRooms    { get; set; }
        public HashSet<string>     Floors            { get; } = new();
        public HashSet<string>     SubFloors         { get; } = new();
        public HashSet<string>     TypesOfUse        { get; } = new();
        public HashSet<string>     Types             { get; } = new();
        public HashSet<string>     ConstructionTypes { get; } = new();
        public HashSet<string>     ConstructionYears { get; } = new();
        public HashSet<string>     AssessmentYears   { get; } = new();
        public DateTime?           MinOcDate         { get; set; }
        public int                 LatestDetailId    { get; set; }
    }
}
