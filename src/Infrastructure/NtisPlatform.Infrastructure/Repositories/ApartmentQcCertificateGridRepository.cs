using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Read-only repository for the ApartmentQC certificate grid.
/// Every query is AsNoTracking — this feature never writes.
/// </summary>
public sealed class ApartmentQcCertificateGridRepository : IApartmentQcCertificateGridRepository
{
    private readonly ApplicationDbContext _context;

    public ApartmentQcCertificateGridRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApartmentQcCertificateGridDto?> GetGridAsync(int wardId, string propertyNo, CancellationToken cancellationToken = default)
    {
        // "Unit" = one PropertyMast row (one partition of this WardId+PropertyNo). All partitions
        // of the same property number together make up the whole apartment/building.
        var units = await _context.PropertyMast.AsNoTracking()
            .Where(p => p.WardId == wardId && p.PropertyNo == propertyNo && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.FlatOrShopNo, p.WingDetailId })
            .ToListAsync(cancellationToken);

        if (units.Count == 0)
        {
            return null;
        }

        var unitIds = units.Select(u => u.Id).ToList();

        var wingIds = units.Where(u => u.WingDetailId.HasValue).Select(u => u.WingDetailId!.Value).Distinct().ToList();
        var wings = wingIds.Count == 0
            ? new List<(int Id, string? WingName)>()
            : (await _context.WingDetailsMast.AsNoTracking()
                .Where(w => wingIds.Contains(w.Id) && w.IsActive)
                .Select(w => new { w.Id, w.WingName })
                .ToListAsync(cancellationToken))
                .Select(w => (w.Id, w.WingName))
                .ToList();

        var unitsPerWing = units
            .Where(u => u.WingDetailId.HasValue)
            .GroupBy(u => u.WingDetailId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var societyId = await _context.SocietyDetailsMast.AsNoTracking()
            .Where(s => s.PropertyId.HasValue && unitIds.Contains(s.PropertyId.Value) && s.IsActive)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var certTypes = await _context.PropertyCertificateTypeMasters.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(cancellationToken);

        var societyRows = new List<CertificateGridRowDto>();
        var wingRows = new List<CertificateGridRowDto>();
        var unitRows = new List<CertificateGridRowDto>();

        foreach (var type in certTypes)
        {
            // A certificate uploaded at any level applies to everything under it: Society covers
            // every wing and unit, a wing's own record covers that wing's units, unless THEY have
            // their own more specific record. So all three tiers are checked and returned
            // independently -- none suppresses another. E.g. a Society-wide certificate plus one
            // wing's own override plus one of that wing's unit's own override all show up as
            // three separate rows; readers interpret "no specific row for this wing/unit" as
            // "falls back to the next broader row that does exist".
            PropertyCertificateEntity? societyCert = null;
            if (societyId.HasValue)
            {
                societyCert = await _context.PropertyCertificates.AsNoTracking()
                    .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                    .Where(c => c.EntityType == "S" && c.SocietyDetailId == societyId.Value
                                && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (societyCert != null)
                {
                    societyRows.Add(BuildApartmentRow(type, societyCert, wings.Count, units.Count));
                }
            }

            // Wing scope — one row per wing: its own record if it has one, else the Society
            // record it inherits (tagged with THIS wing's id, since the Society cert's own
            // WingDetailId is null). A wing with neither gets no row.
            var wingCerts = wingIds.Count == 0
                ? new List<PropertyCertificateEntity>()
                : await _context.PropertyCertificates.AsNoTracking()
                    .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                    .Where(c => c.EntityType == "W" && c.WingDetailId.HasValue && wingIds.Contains(c.WingDetailId.Value)
                                && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                    .ToListAsync(cancellationToken);

            var latestPerWing = wingCerts
                .GroupBy(c => c.WingDetailId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedDate).First());

            foreach (var wingId in wingIds)
            {
                var effectiveCert = latestPerWing.GetValueOrDefault(wingId) ?? societyCert;
                if (effectiveCert == null)
                {
                    continue;
                }

                var wingName = wings.FirstOrDefault(w => w.Id == wingId).WingName;
                var unitsInWing = unitsPerWing.GetValueOrDefault(wingId);
                wingRows.Add(BuildWingRow(type, effectiveCert, wingId, wingName, unitsInWing));
            }

            // Unit scope — each unit-property can carry its own property-level record; aggregate coverage.
            var unitCerts = await _context.PropertyCertificates.AsNoTracking()
                .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                .Where(c => c.EntityType == "P" && c.PropertyDetailsId == null && c.PropertyId.HasValue
                            && unitIds.Contains(c.PropertyId.Value)
                            && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            if (unitCerts.Count > 0)
            {
                var latestPerUnit = unitCerts
                    .GroupBy(c => c.PropertyId!.Value)
                    .Select(g => g.OrderByDescending(c => c.CreatedDate).First())
                    .ToList();

                var representative = latestPerUnit.OrderByDescending(c => c.CreatedDate).First();
                var coveredUnitNumbers = latestPerUnit
                    .Join(units, c => c.PropertyId!.Value, u => u.Id, (c, u) => u.FlatOrShopNo)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n!)
                    .ToList();

                unitRows.Add(BuildUnitRow(type, representative, coveredUnitNumbers, latestPerUnit.Count, units.Count));
            }
        }

        RenumberRows(societyRows);
        RenumberRows(wingRows);
        RenumberRows(unitRows);

        return new ApartmentQcCertificateGridDto
        {
            WingCount = wings.Count,
            UnitCount = units.Count,
            SocietyCertificates = societyRows,
            WingCertificates = wingRows,
            UnitCertificates = unitRows,
        };
    }

    public async Task<ApartmentQcCertificateGridDto?> GetGridForWingAsync(int wingDetailsId, CancellationToken cancellationToken = default)
    {
        var wing = await _context.WingDetailsMast.AsNoTracking()
            .Where(w => w.Id == wingDetailsId && w.IsActive && !w.MarkedForDeletion)
            .Select(w => new { w.WingName, w.SocietyDetailsMastId })
            .FirstOrDefaultAsync(cancellationToken);

        if (wing is null)
        {
            return null;
        }

        var units = await _context.PropertyMast.AsNoTracking()
            .Where(p => p.WingDetailId == wingDetailsId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.FlatOrShopNo, p.WardId, p.PropertyNo })
            .ToListAsync(cancellationToken);

        if (units.Count == 0)
        {
            return null;
        }

        var unitIds = units.Select(u => u.Id).ToList();

        var societyId = await _context.SocietyDetailsMast.AsNoTracking()
            .Where(s => s.Id == wing.SocietyDetailsMastId && s.IsActive)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // A Society-level certificate applies to the whole apartment, not just this wing -- report
        // the true apartment-wide wing/unit counts on that row.
        var (apartmentWingCount, apartmentUnitCount) = await GetApartmentCountsAsync(units[0].WardId, units[0].PropertyNo, cancellationToken);

        var certTypes = await _context.PropertyCertificateTypeMasters.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(cancellationToken);

        var societyRows = new List<CertificateGridRowDto>();
        var wingRows = new List<CertificateGridRowDto>();
        var unitRows = new List<CertificateGridRowDto>();

        foreach (var type in certTypes)
        {
            // A certificate uploaded at any level applies to everything under it: Society covers
            // this whole wing, Wing covers this wing's own units. So all three are checked and
            // returned independently, each in its own list -- none suppresses another.
            PropertyCertificateEntity? societyCert = null;
            if (societyId.HasValue)
            {
                societyCert = await _context.PropertyCertificates.AsNoTracking()
                    .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                    .Where(c => c.EntityType == "S" && c.SocietyDetailId == societyId.Value
                                && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (societyCert != null)
                {
                    societyRows.Add(BuildApartmentRow(type, societyCert, apartmentWingCount, apartmentUnitCount));
                }
            }

            var wingCert = await _context.PropertyCertificates.AsNoTracking()
                .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                .Where(c => c.EntityType == "W" && c.WingDetailId == wingDetailsId
                            && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            // This wing's own record if it has one, else the Society record it inherits.
            var effectiveWingCert = wingCert ?? societyCert;
            if (effectiveWingCert != null)
            {
                wingRows.Add(BuildWingRow(type, effectiveWingCert, wingDetailsId, wing.WingName, units.Count));
            }

            var unitCerts = await _context.PropertyCertificates.AsNoTracking()
                .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                .Where(c => c.EntityType == "P" && c.PropertyDetailsId == null && c.PropertyId.HasValue
                            && unitIds.Contains(c.PropertyId.Value)
                            && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            if (unitCerts.Count > 0)
            {
                var latestPerUnit = unitCerts
                    .GroupBy(c => c.PropertyId!.Value)
                    .Select(g => g.OrderByDescending(c => c.CreatedDate).First())
                    .ToList();

                var representative = latestPerUnit.OrderByDescending(c => c.CreatedDate).First();
                var coveredUnitNumbers = latestPerUnit
                    .Join(units, c => c.PropertyId!.Value, u => u.Id, (c, u) => u.FlatOrShopNo)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n!)
                    .ToList();

                unitRows.Add(BuildUnitRow(type, representative, coveredUnitNumbers, latestPerUnit.Count, units.Count));
            }
        }

        RenumberRows(societyRows);
        RenumberRows(wingRows);
        RenumberRows(unitRows);

        return new ApartmentQcCertificateGridDto
        {
            WingCount = 1,
            UnitCount = units.Count,
            SocietyCertificates = societyRows,
            WingCertificates = wingRows,
            UnitCertificates = unitRows,
        };
    }

    public async Task<ApartmentQcCertificateGridDto?> GetGridForUnitAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var unit = await _context.PropertyMast.AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.Id, p.FlatOrShopNo, p.WingDetailId, p.WardId, p.PropertyNo })
            .FirstOrDefaultAsync(cancellationToken);

        if (unit is null)
        {
            return null;
        }

        string? wingName = null;
        int? societyId = null;

        if (unit.WingDetailId.HasValue)
        {
            var wing = await _context.WingDetailsMast.AsNoTracking()
                .Where(w => w.Id == unit.WingDetailId.Value && w.IsActive && !w.MarkedForDeletion)
                .Select(w => new { w.WingName, w.SocietyDetailsMastId })
                .FirstOrDefaultAsync(cancellationToken);

            if (wing != null)
            {
                wingName = wing.WingName;
                societyId = await _context.SocietyDetailsMast.AsNoTracking()
                    .Where(s => s.Id == wing.SocietyDetailsMastId && s.IsActive)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        // Fallback for units not linked to a wing -- same direct-property link the apartment-wide
        // grid uses.
        societyId ??= await _context.SocietyDetailsMast.AsNoTracking()
            .Where(s => s.PropertyId == propertyId && s.IsActive)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // A Society-level certificate applies to the whole apartment, not just this unit -- report
        // the true apartment-wide wing/unit counts on that row.
        var (apartmentWingCount, apartmentUnitCount) = await GetApartmentCountsAsync(unit.WardId, unit.PropertyNo, cancellationToken);

        var certTypes = await _context.PropertyCertificateTypeMasters.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(cancellationToken);

        var societyRows = new List<CertificateGridRowDto>();
        var wingRows = new List<CertificateGridRowDto>();
        var unitRows = new List<CertificateGridRowDto>();
        var floorRows = new List<CertificateGridRowDto>();

        foreach (var type in certTypes)
        {
            // A certificate uploaded at any level applies to everything under it: Society covers
            // this whole apartment (hence this unit too), Wing covers this unit's wing. So all
            // levels are checked and returned independently, each in its own list -- none
            // suppresses another.
            PropertyCertificateEntity? societyCert = null;
            if (societyId.HasValue)
            {
                societyCert = await _context.PropertyCertificates.AsNoTracking()
                    .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                    .Where(c => c.EntityType == "S" && c.SocietyDetailId == societyId.Value
                                && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (societyCert != null)
                {
                    societyRows.Add(BuildApartmentRow(type, societyCert, apartmentWingCount, apartmentUnitCount));
                }
            }

            if (unit.WingDetailId.HasValue)
            {
                var wingCert = await _context.PropertyCertificates.AsNoTracking()
                    .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                    .Where(c => c.EntityType == "W" && c.WingDetailId == unit.WingDetailId.Value
                                && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefaultAsync(cancellationToken);

                // This unit's wing's own record if it has one, else the Society record it inherits.
                var effectiveWingCert = wingCert ?? societyCert;
                if (effectiveWingCert != null)
                {
                    wingRows.Add(BuildWingRow(type, effectiveWingCert, unit.WingDetailId.Value, wingName, 1));
                }
            }

            var propertyCerts = await _context.PropertyCertificates.AsNoTracking()
                .Include(c => c.DocumentBinding).ThenInclude(db => db!.Document)
                .Where(c => c.EntityType == "P" && c.PropertyId == propertyId
                            && c.CertificateTypeId == type.Id && !c.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var unitCert = propertyCerts
                .Where(c => c.PropertyDetailsId == null)
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefault();

            if (unitCert != null)
            {
                var unitNumber = unit.FlatOrShopNo ?? string.Empty;
                unitRows.Add(BuildUnitRow(type, unitCert, new List<string> { unitNumber }, 1, 1));
            }

            var floorCerts = propertyCerts
                .Where(c => c.PropertyDetailsId.HasValue)
                .GroupBy(c => c.PropertyDetailsId!.Value)
                .Select(g => g.OrderByDescending(c => c.CreatedDate).First())
                .ToList();

            if (floorCerts.Count > 0)
            {
                var floorIds = floorCerts.Select(c => c.PropertyDetailsId!.Value).ToList();
                var floorNames = await (
                    from pd in _context.PropertyDetails.AsNoTracking()
                    join f in _context.FloorEntity.AsNoTracking() on pd.FloorId equals f.Id into fj
                    from f in fj.DefaultIfEmpty()
                    where floorIds.Contains(pd.Id)
                    select new { pd.Id, FloorName = f != null ? f.Description : null }
                ).ToDictionaryAsync(x => x.Id, x => x.FloorName, cancellationToken);

                foreach (var cert in floorCerts)
                {
                    var floorLabel = floorNames.GetValueOrDefault(cert.PropertyDetailsId!.Value) ?? "Floor";
                    floorRows.Add(BuildFloorRow(type, cert, floorLabel, unit.FlatOrShopNo));
                }
            }
        }

        RenumberRows(societyRows);
        RenumberRows(wingRows);
        RenumberRows(unitRows);
        RenumberRows(floorRows);

        return new ApartmentQcCertificateGridDto
        {
            WingCount = unit.WingDetailId.HasValue ? 1 : 0,
            UnitCount = 1,
            SocietyCertificates = societyRows,
            WingCertificates = wingRows,
            UnitCertificates = unitRows,
            FloorCertificates = floorRows,
        };
    }

    private async Task<(int WingCount, int UnitCount)> GetApartmentCountsAsync(int wardId, string? propertyNo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(propertyNo))
        {
            return (0, 0);
        }

        var unitCount = await _context.PropertyMast.AsNoTracking()
            .Where(p => p.WardId == wardId && p.PropertyNo == propertyNo && p.IsActive && !p.MarkedForDeletion)
            .CountAsync(cancellationToken);

        var wingCount = await _context.PropertyMast.AsNoTracking()
            .Where(p => p.WardId == wardId && p.PropertyNo == propertyNo && p.IsActive && !p.MarkedForDeletion && p.WingDetailId.HasValue)
            .Select(p => p.WingDetailId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

        return (wingCount, unitCount);
    }

    private static void RenumberRows(List<CertificateGridRowDto> rows)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = i + 1;
        }
    }

    private static CertificateGridRowDto BuildApartmentRow(
        NtisPlatform.Core.Entities.Master.PropertyCertificateTypeMasterEntity type,
        PropertyCertificateEntity cert,
        int wingCount,
        int unitCount)
        => new()
        {
            Level = "Apartment",
            ApplicableToLabel = "Entire Apartment",
            ApplicableToSubLabel = $"{wingCount} wings · {unitCount} units",
            SocietyDetailId = cert.SocietyDetailId,
            WingDetailId = cert.WingDetailId,
            PropertyId = cert.PropertyId,
            PropertyDetailsId = cert.PropertyDetailsId,
            CertificateTypeId = type.Id,
            CertificateTypeCode = type.CertificateTypeCode,
            CertificateTypeName = type.CertificateTypeName,
            CertificateDate = cert.IssueDate,
            CertificateNumber = cert.CertificateNo,
            Status = ResolveStatus(cert),
            HasDocument = cert.DocumentBindingId.HasValue,
            DocumentGuid = DocumentBindingHelper.GetSafeDocumentGuid(cert.DocumentBinding),
        };

    private static CertificateGridRowDto BuildWingRow(
        NtisPlatform.Core.Entities.Master.PropertyCertificateTypeMasterEntity type,
        PropertyCertificateEntity cert,
        int wingId,
        string? wingName,
        int unitsInWing)
        => new()
        {
            Level = "Wing",
            ApplicableToLabel = wingName ?? "Wing",
            ApplicableToSubLabel = $"{unitsInWing} units in this wing",
            SocietyDetailId = cert.SocietyDetailId,
            WingDetailId = wingId,
            PropertyId = cert.PropertyId,
            PropertyDetailsId = cert.PropertyDetailsId,
            CertificateTypeId = type.Id,
            CertificateTypeCode = type.CertificateTypeCode,
            CertificateTypeName = type.CertificateTypeName,
            CertificateDate = cert.IssueDate,
            CertificateNumber = cert.CertificateNo,
            Status = ResolveStatus(cert),
            HasDocument = cert.DocumentBindingId.HasValue,
            DocumentGuid = DocumentBindingHelper.GetSafeDocumentGuid(cert.DocumentBinding),
        };

    private static CertificateGridRowDto BuildUnitRow(
        NtisPlatform.Core.Entities.Master.PropertyCertificateTypeMasterEntity type,
        PropertyCertificateEntity representative,
        List<string> coveredUnitNumbers,
        int coveredCount,
        int totalUnitCount)
        => new()
        {
            Level = "Unit",
            ApplicableToLabel = string.Join(" ", coveredUnitNumbers),
            ApplicableToSubLabel = null,
            UnitsCoveredCount = coveredCount,
            UnitsTotalCount = totalUnitCount,
            UnitsMissingCount = totalUnitCount - coveredCount,
            CoveredUnitNumbers = coveredUnitNumbers,
            SocietyDetailId = representative.SocietyDetailId,
            WingDetailId = representative.WingDetailId,
            PropertyId = representative.PropertyId,
            PropertyDetailsId = representative.PropertyDetailsId,
            CertificateTypeId = type.Id,
            CertificateTypeCode = type.CertificateTypeCode,
            CertificateTypeName = type.CertificateTypeName,
            CertificateDate = representative.IssueDate,
            CertificateNumber = representative.CertificateNo,
            Status = ResolveStatus(representative),
            HasDocument = representative.DocumentBindingId.HasValue,
            DocumentGuid = DocumentBindingHelper.GetSafeDocumentGuid(representative.DocumentBinding),
        };

    private static CertificateGridRowDto BuildFloorRow(
        NtisPlatform.Core.Entities.Master.PropertyCertificateTypeMasterEntity type,
        PropertyCertificateEntity cert,
        string floorLabel,
        string? unitNumber)
        => new()
        {
            Level = "Floor",
            ApplicableToLabel = floorLabel,
            ApplicableToSubLabel = string.IsNullOrWhiteSpace(unitNumber) ? null : $"Unit {unitNumber}",
            SocietyDetailId = cert.SocietyDetailId,
            WingDetailId = cert.WingDetailId,
            PropertyId = cert.PropertyId,
            PropertyDetailsId = cert.PropertyDetailsId,
            CertificateTypeId = type.Id,
            CertificateTypeCode = type.CertificateTypeCode,
            CertificateTypeName = type.CertificateTypeName,
            CertificateDate = cert.IssueDate,
            CertificateNumber = cert.CertificateNo,
            Status = ResolveStatus(cert),
            HasDocument = cert.DocumentBindingId.HasValue,
            DocumentGuid = DocumentBindingHelper.GetSafeDocumentGuid(cert.DocumentBinding),
        };

    /// <summary>"Active" once the certificate has both a number and an issue date, else "Pending" — per business direction there is no separate active/inactive gate on top of this.</summary>
    private static string ResolveStatus(PropertyCertificateEntity cert)
        => !string.IsNullOrWhiteSpace(cert.CertificateNo) && cert.IssueDate.HasValue ? "Active" : "Pending";
}
