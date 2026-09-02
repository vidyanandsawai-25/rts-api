using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Repository for property sign-off data access.
/// Reads and writes SignAuthorityMaster and PropertySignatureDetails tables.
/// Business rules are applied by the service layer.
/// </summary>
public class PropertySignatureRepository : IPropertySignatureRepository
{
    #region Constants

    private const string TaxTotalCode = "TaxTotal";
    private const string TaxTotalName = "TaxTotal";
 

    #endregion

    #region Fields

    private readonly ApplicationDbContext _context;
    private readonly ILogger<PropertySignatureRepository> _logger;

    #endregion

    #region Constructor

    public PropertySignatureRepository(
        ApplicationDbContext context,
        ILogger<PropertySignatureRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #endregion

    #region Authority Queries

    public async Task<List<SignAuthorityDto>> GetAuthoritiesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching all active signing authorities");

            return await _context.SignAuthorityMaster
                .AsNoTracking()
                .Where(a => a.IsActive)
                .OrderBy(a => a.SequenceOrder)
                .Select(a => new SignAuthorityDto
                {
                    Id = a.Id,
                    AuthorityName = a.AuthorityName,
                    AuthorityCode = a.AuthorityCode,
                    SequenceOrder = a.SequenceOrder
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching signing authorities from database");
            throw;
        }
    }

    #endregion

    #region Eligible Properties Queries

    public async Task<List<EligiblePropertyDto>> GetEligiblePropertiesAsync(int signAuthorityId,int? zoneId,int? wardId,CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Fetching eligible properties for SignAuthorityId={SignAuthorityId}, ZoneId={ZoneId}, WardId={WardId}",
                signAuthorityId, zoneId, wardId);

            var propertiesQuery = BuildActivePropertiesQuery();

            // Apply zone filter (via Ward → Zone)
            if (zoneId.HasValue)
                propertiesQuery = propertiesQuery.Where(p =>
                    _context.WardMaster.Any(w => w.IsActive && w.Id == p.WardId && w.ZoneId == zoneId.Value));

            // Apply ward filter
            if (wardId.HasValue)
                propertiesQuery = propertiesQuery.Where(p => p.WardId == wardId.Value);

            return await (
                from p in propertiesQuery
                join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id into wardJoin
                from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()
                join z in _context.ZoneMaster.AsNoTracking() on w.ZoneId equals z.Id into zoneJoin
                from z in zoneJoin.Where(x => x.IsActive).DefaultIfEmpty()
                select new EligiblePropertyDto
                {
                    PropertyId = p.Id,
                    PropertyNo = p.PropertyNo,
                    PartitionNo = p.PartitionNo,
                    WardName = w != null ? (w.Description ?? w.WardNo) : null,
                    ZoneName = z != null ? (z.Description ?? z.ZoneNo) : null
                }
            ).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching eligible properties for SignAuthorityId={SignAuthorityId}",
                signAuthorityId);
            throw;
        }
    }

    #endregion

    #region Sequential Validation Queries

    public async Task<List<PropertySignaturePendingExportAuthorityDto>> GetPendingExportAuthoritiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching pending export authorities");

            return await _context.SignAuthorityMaster
                .AsNoTracking()
                .Where(a => a.IsActive)
                .OrderBy(a => a.SequenceOrder)
                .Select(a => new PropertySignaturePendingExportAuthorityDto
                {
                    SignAuthorityId = a.Id,
                    AuthorityName = a.AuthorityName,
                    OfficerName = a.OfficerName,
                    SequenceOrder = a.SequenceOrder
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending export authorities");
            throw;
        }
    }

    public async Task<List<PropertySignaturePendingExportSourceDto>> GetPendingExportSourceDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching pending export source data");

            var signatureRows = await (
                from sig in _context.PropertySignatureDetails.AsNoTracking()
                join p in _context.PropertyMast.AsNoTracking() on sig.PropertyId equals p.Id
                join ward in _context.WardMaster.AsNoTracking() on p.WardId equals ward.Id
                join zone in _context.ZoneMaster.AsNoTracking() on ward.ZoneId equals zone.Id
                where sig.IsActive
                      && p.IsActive
                      && !p.MarkedForDeletion
                      && p.PropertyNo != null
                      && p.PropertyNo != ""
                      && ward.IsActive
                      && zone.IsActive
                select new
                {
                    sig.PropertyId,
                    sig.SignAuthorityId,
                    sig.NoticeNo,
                    sig.CreatedDate,
                    Zone = zone.ZoneNo,
                    ZoneSequenceNo = zone.SequenceNo,
                    ward.WardNo,
                    p.PropertyNo,
                    p.PartitionNo
                })
                .ToListAsync(cancellationToken);

            return signatureRows
                .GroupBy(x => x.PropertyId)
                .Select(g =>
                {
                    var first = g
                        .OrderBy(x => x.ZoneSequenceNo ?? 0)
                        .ThenBy(x => x.Zone)
                        .ThenBy(x => x.WardNo)
                        .ThenBy(x => x.PropertyNo)
                        .ThenBy(x => x.PartitionNo)
                        .First();
                    var latestNoticeNo = g
                        .OrderByDescending(x => x.CreatedDate)
                        .Select(x => x.NoticeNo)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                    return new PropertySignaturePendingExportSourceDto
                    {
                        PropertyId = g.Key,
                        Zone = first.Zone,
                        BuildingNo = string.IsNullOrWhiteSpace(first.PartitionNo)
                            ? first.WardNo + "-" + first.PropertyNo
                            : first.WardNo + "-" + first.PropertyNo + "-" + first.PartitionNo,
                        SrNoticeNo = latestNoticeNo ?? "",
                        SignedAuthorityIds = g
                            .Select(x => x.SignAuthorityId)
                            .Distinct()
                            .ToList()
                    };
                })
                .OrderBy(x => x.Zone)
                .ThenBy(x => x.BuildingNo)
                .ThenBy(x => x.SrNoticeNo)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending export source data");
            throw;
        }
    }

    public async Task<List<PropertySignaturePendingSignSourceDto>> GetPendingSignSourceDataAsync(
        int signAuthorityId,string? searchTerm = null,CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Fetching pending sign source data for  SignAuthorityId={SignAuthorityId}, SearchTerm={SearchTerm}",
             
                signAuthorityId,
                searchTerm);
            var approvedStatuses = new[]
            {
                "ApprovedByClerk",
                "ApprovedByTI",
                "ApprovedByAC",
                "ApprovedByDC",
                "ApprovedByACD"
            };
            var normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : searchTerm.Trim().ToUpper();

            var totalDemandQuery =
                from t in _context.TransMast.AsNoTracking()
                join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
                where t.IsActive
                      && !t.MarkedForDeletion
                      && tax.IsActive
                      && tax.TaxCode == TaxTotalCode
                      && tax.TaxName == TaxTotalName
                group t by t.PropertyId into g
                select new
                {
                    PropertyId = g.Key,
                    Demand = g.Sum(x => x.TaxAmount)
                };

            var rows = await (
                from sig in _context.PropertySignatureDetails.AsNoTracking()
                join auth in _context.SignAuthorityMaster.AsNoTracking() on sig.SignAuthorityId equals auth.Id
                join p in _context.PropertyMast.AsNoTracking() on sig.PropertyId equals p.Id
                join ward in _context.WardMaster.AsNoTracking() on p.WardId equals ward.Id
                join unitProperty in _context.PropertyMast.AsNoTracking()
                    on new { p.WardId, p.PropertyNo } equals new { unitProperty.WardId, unitProperty.PropertyNo }
                join demand in totalDemandQuery on unitProperty.Id equals demand.PropertyId into demandJoin
                from demand in demandJoin.DefaultIfEmpty()
                where sig.IsActive
                       && sig.SignAuthorityId == signAuthorityId
                      && (sig.SignStatus == null || !approvedStatuses.Contains(sig.SignStatus))
                      && (normalizedSearchTerm == null || (sig.NoticeNo != null && sig.NoticeNo!.Trim().ToUpper() == normalizedSearchTerm))
                      && auth.IsActive
                      && p.IsActive
                      && !p.MarkedForDeletion
                      && p.PropertyNo != null
                      && p.PropertyNo != ""
                      && ward.IsActive
                      && unitProperty.IsActive
                      && !unitProperty.MarkedForDeletion
                      && unitProperty.PropertyNo != null
                      && unitProperty.PropertyNo != ""
                select new
                {
                    SignatureId = sig.Id,
                    PropertyId = sig.PropertyId,
                    SignAuthorityId = sig.SignAuthorityId,
                    UnitPropertyId = unitProperty.Id,
                    WardNo = ward.WardNo,
                    PropertyNo = p.PropertyNo,
                    PartitionNo = p.PartitionNo,
                    sig.NoticeNo,
                    sig.SignStatus,
                    AuthorityCode = auth.AuthorityCode,
                    UnitDemand = demand == null ? (decimal?)null : demand.Demand
                })
                .ToListAsync(cancellationToken);

            return rows.Select(row => new PropertySignaturePendingSignSourceDto
            {
                SignatureId = row.SignatureId,
                PropertyId = row.PropertyId,
                SignAuthorityId = row.SignAuthorityId,
                UnitPropertyId = row.UnitPropertyId,
                WardNo = row.WardNo ?? "",
                PropertyNo = row.PropertyNo ?? "",
                PartitionNo = row.PartitionNo,
                SrNoticeNo = row.NoticeNo ?? "",
                SignStatus = row.SignStatus ?? "",
                AuthorityCode = row.AuthorityCode ?? "",
                UnitDemand = row.UnitDemand ?? 0m
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching pending sign source data for SignAuthorityId={SignAuthorityId}, SearchTerm={SearchTerm}",
                signAuthorityId,
                searchTerm);
            throw;
        }
    }

    public async Task<int?> GetSignAuthorityIdByUserRoleAsync(int userId,CancellationToken cancellationToken = default)
    {
        try
        {
            return await (
                from user in _context.UserMasters.AsNoTracking()
                join roleAllocation in _context.UserRoleAllocation.AsNoTracking() on user.Id equals roleAllocation.UserId
                join userRole in _context.UserRoleMasterEntity.AsNoTracking() on roleAllocation.UserRoleId equals userRole.Id
                join department in _context.DepartmentMasters.AsNoTracking() on userRole.DepartmentId equals department.Id
                join authority in _context.SignAuthorityMaster.AsNoTracking() on userRole.UserRoleName equals authority.AuthorityName
                where user.Id == userId
                      && department.DepartmentCode == "PTIS"
                      && user.IsActive
                      && roleAllocation.IsActive
                      && userRole.IsActive
                      && department.IsActive
                      && authority.IsActive
                orderby authority.SequenceOrder
                select (int?)authority.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving sign authority for UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<PropertySignatureUpdateSignSourceDto?> GetUpdateSignSourceAsync(
        int userId, int propertyId, int signAuthorityId,string signStatus,CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedSignStatus = signStatus.Trim().ToUpper();

            return await (
                from sig in _context.PropertySignatureDetails.AsNoTracking()
                join auth in _context.SignAuthorityMaster.AsNoTracking() on sig.SignAuthorityId equals auth.Id
                where sig.IsActive                    
                      && sig.PropertyId == propertyId
                      && sig.SignAuthorityId == signAuthorityId
                      && sig.SignStatus != null
                      && sig.SignStatus.Trim().ToUpper() == normalizedSignStatus
                      && auth.IsActive
                select new PropertySignatureUpdateSignSourceDto
                {
                    SignatureId = sig.Id,
                    UserId = sig.UserId,
                    PropertyId = sig.PropertyId,
                    SignAuthorityId = sig.SignAuthorityId,
                    AuthorityCode = auth.AuthorityCode,
                    NoticeNo = sig.NoticeNo,
                    SignStatus = sig.SignStatus ?? "",
                    IsActive = sig.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching update-sign source for UserId={UserId}, PropertyId={PropertyId}, SignAuthorityId={SignAuthorityId}",
                userId,
                propertyId,
                signAuthorityId);
            throw;
        }
    }

    public async Task<bool> SignatureExistsAsync(int propertyId,int signAuthorityId,CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.PropertySignatureDetails.AsNoTracking()
                .AnyAsync(sig => sig.IsActive && sig.PropertyId == propertyId && sig.SignAuthorityId == signAuthorityId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking signature existence for PropertyId={PropertyId}, SignAuthorityId={SignAuthorityId}",
                propertyId,
                signAuthorityId);
            throw;
        }
    }

    public async Task<(bool UserExists, bool PropertyExists, bool SignAuthorityExists)> GetUpdateSignReferenceStatusAsync(
        int userId,
        int propertyId,
        int signAuthorityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userExists = await _context.UserMasters
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Id == userId
                    && user.IsActive
                    && !user.MarkedForDeletion,
                    cancellationToken);

            var propertyExists = await _context.PropertyMast
                .AsNoTracking()
                .AnyAsync(property =>
                    property.Id == propertyId
                    && property.IsActive
                    && !property.MarkedForDeletion,
                    cancellationToken);

            var signAuthorityExists = await _context.SignAuthorityMaster
                .AsNoTracking()
                .AnyAsync(authority =>
                    authority.Id == signAuthorityId
                    && authority.IsActive,
                    cancellationToken);

            return (userExists, propertyExists, signAuthorityExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking update-sign references for UserId={UserId}, PropertyId={PropertyId}, SignAuthorityId={SignAuthorityId}",
                userId,
                propertyId,
                signAuthorityId);
            throw;
        }
    }

    public async Task<List<int>> GetSignedPropertyIdsAsync(List<int>propertyIds,int signAuthorityId,CancellationToken cancellationToken = default)
    {
        if (!propertyIds.Any())
            return new List<int>();

        try
        {
            _logger.LogDebug( "Fetching signed property IDs for SignAuthorityId={SignAuthorityId}, Count={Count}",
                signAuthorityId, propertyIds.Count);

            return await _context.PropertySignatureDetails
                .AsNoTracking()
                .Where(d => d.IsActive
                            && d.SignAuthorityId == signAuthorityId
                            && propertyIds.Contains(d.PropertyId))
                .Select(d => d.PropertyId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching signed property IDs for SignAuthorityId={SignAuthorityId}",
                signAuthorityId);
            throw;
        }
    }

    #endregion

    #region Duplicate Check Queries

    public async Task<List<int>> GetAlreadyApprovedPropertyIdsAsync(
        List<int> propertyIds,
        int signAuthorityId,
        CancellationToken cancellationToken = default)
    {
        if (!propertyIds.Any())
            return new List<int>();

        try
        {
            _logger.LogDebug(
                "Checking already approved properties for SignAuthorityId={SignAuthorityId}, Count={Count}",
                signAuthorityId, propertyIds.Count);

            return await _context.PropertySignatureDetails
                .AsNoTracking()
                .Where(d => d.IsActive
                            && d.SignAuthorityId == signAuthorityId
                            && propertyIds.Contains(d.PropertyId))
                .Select(d => d.PropertyId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking already approved properties for SignAuthorityId={SignAuthorityId}",
                signAuthorityId);
            throw;
        }
    }

    #endregion

    #region Save Approvals Commands

    public async Task<int> SaveApprovalsAsync(
        int userId,
        int signAuthorityId,
        List<PropertyApprovalItemDto> approvals,
        CancellationToken cancellationToken = default)
    {
        if (!approvals.Any())
            return 0;

        try
        {
            _logger.LogDebug(
                "Saving {Count} approvals for UserId={UserId}, SignAuthorityId={SignAuthorityId}",
                approvals.Count, userId, signAuthorityId);

            var now = DateTime.Now;

            var records = approvals.Select(a => new PropertySignatureDetailsEntity
            {
                UserId = userId,
                PropertyId = a.PropertyId,
                SignAuthorityId = signAuthorityId,

                IsActive = true,
                CreatedDate = now,
                CreatedBy = userId
            }).ToList();

            await _context.PropertySignatureDetails.AddRangeAsync(records, cancellationToken);
            var savedCount = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saved {SavedCount} approvals for UserId={UserId}, SignAuthorityId={SignAuthorityId}",
                savedCount, userId, signAuthorityId);

            return savedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error saving approvals for UserId={UserId}, SignAuthorityId={SignAuthorityId}",
                userId, signAuthorityId);
            throw;
        }
    }

    #endregion

    #region Update Sign Commands

    public async Task<bool> UpdateSignAsync(PropertySignatureUpdateSignCommandDto command, CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            if (_context.Database.IsRelational())
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var signatureExists = await _context.PropertySignatureDetails
                .AnyAsync(sig =>
                    sig.Id == command.SignatureId
                    && sig.IsActive,
                    cancellationToken);

            if (!signatureExists)
                return false;
 
            var now = DateTime.Now;

            if (_context.Database.IsRelational())
            {
                await _context.PropertySignatureDetails
                    .Where(sig =>
                        sig.Id == command.SignatureId
                        && sig.IsActive)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(sig => sig.SignStatus, command.UpdatedSignStatus)
                        .SetProperty(sig => sig.UpdatedDate, now)
                        .SetProperty(sig => sig.UpdatedBy, command.UpdatedBy),
                        cancellationToken);
            }
            else
            {
                var tracked = _context.PropertySignatureDetails.Local
                    .FirstOrDefault(sig => sig.Id == command.SignatureId);

                if (tracked != null)
                {
                    tracked.SignStatus = command.UpdatedSignStatus;
                    tracked.UpdatedDate = now;
                    tracked.UpdatedBy = command.UpdatedBy;
                }
                else
                {
                    var signature = new PropertySignatureDetailsEntity
                    {
                        Id = command.SignatureId,
                        SignStatus = command.UpdatedSignStatus,
                        UpdatedDate = now,
                        UpdatedBy = command.UpdatedBy
                    };

                    _context.PropertySignatureDetails.Attach(signature);
                    _context.Entry(signature).Property(sig => sig.SignStatus).IsModified = true;
                    _context.Entry(signature).Property(sig => sig.UpdatedDate).IsModified = true;
                    _context.Entry(signature).Property(sig => sig.UpdatedBy).IsModified = true;
                }
            }

            if (command.NextSignAuthorityId.HasValue
                && !string.IsNullOrWhiteSpace(command.NextSignStatus))
            {
                var nextExists = await _context.PropertySignatureDetails
                    .AnyAsync(sig =>
                        sig.IsActive
                        && sig.PropertyId == command.PropertyId
                        && sig.SignAuthorityId == command.NextSignAuthorityId.Value,
                        cancellationToken);

                if (!nextExists)
                {
                    await _context.PropertySignatureDetails.AddAsync(
                        new PropertySignatureDetailsEntity
                        {
                            UserId = command.UserId,
                            PropertyId = command.PropertyId,
                            SignAuthorityId = command.NextSignAuthorityId.Value,
                            IsActive = command.IsActive,
                            NoticeNo = command.NoticeNo,
                            SignStatus = command.NextSignStatus,
                            CreatedDate = now,
                            CreatedBy = command.UpdatedBy
                        },
                        cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex,
                "Error updating signature row SignatureId={SignatureId}",
                command.SignatureId);
            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    #endregion

    #region My Approvals Queries

    public async Task<List<SignatureApprovalDto>> GetMyApprovalsAsync(
        int userId,
        int signAuthorityId,
        int? zoneId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Fetching approvals for UserId={UserId}, SignAuthorityId={SignAuthorityId}, ZoneId={ZoneId}",
                userId, signAuthorityId, zoneId);

            var query = _context.PropertySignatureDetails
                .AsNoTracking()
                .Where(d => d.IsActive
                            && d.UserId == userId
                            && d.SignAuthorityId == signAuthorityId
                            && d.Property.IsActive
                            && !d.Property.MarkedForDeletion);

            if (zoneId.HasValue)
            {
                var wardIdsInZone = _context.WardMaster
                    .Where(w => w.IsActive && w.ZoneId == zoneId.Value)
                    .Select(w => w.Id);

                query = query.Where(d => wardIdsInZone.Contains(d.Property.WardId));
            }

            return await (
                from d in query
                join w in _context.WardMaster.AsNoTracking() on d.Property.WardId equals w.Id into wardJoin
                from w in wardJoin.Where(x => x.IsActive).DefaultIfEmpty()
                select new SignatureApprovalDto
                {
                    Id = d.Id,
                    PropertyId = d.PropertyId,
                    PropertyNo = d.Property.PropertyNo,
                    PartitionNo = d.Property.PartitionNo,
                    WardName = w != null ? (w.Description ?? w.WardNo) : null,
                    SignAuthorityId = d.SignAuthorityId,
                    AuthorityName = d.SignAuthority.AuthorityName,
                    ApprovedByUserName = d.User.UserName,

                    ApprovedOn = d.CreatedDate
                }
            ).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching approvals for UserId={UserId}, SignAuthorityId={SignAuthorityId}",
                userId, signAuthorityId);
            throw;
        }
    }

    #endregion

    #region Property Status Queries

    public async Task<PropertySignatureStatusDto?> GetPropertySignatureStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching signature status for PropertyId={PropertyId}", propertyId);

            var property = await _context.PropertyMast
                .AsNoTracking()
                .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
                .Select(p => new { p.Id, p.PropertyNo })
                .FirstOrDefaultAsync(cancellationToken);

            if (property == null)
                return null;

            // All authorities in sequence order
            var allAuthorities = await _context.SignAuthorityMaster
                .AsNoTracking()
                .Where(a => a.IsActive)
                .OrderBy(a => a.SequenceOrder)
                .ToListAsync(cancellationToken);

            // All approvals for this property
            var existingApprovals = await _context.PropertySignatureDetails
                .AsNoTracking()
                .Where(d => d.IsActive && d.PropertyId == propertyId)
                .Select(d => new
                {
                    d.SignAuthorityId,
                    d.User.UserName,

                    d.CreatedDate
                })
                .ToListAsync(cancellationToken);

            var statusList = allAuthorities.Select(auth =>
            {
                var approval = existingApprovals.FirstOrDefault(a => a.SignAuthorityId == auth.Id);
                return new AuthorityApprovalStatusDto
                {
                    SignAuthorityId = auth.Id,
                    AuthorityName = auth.AuthorityName,
                    SequenceOrder = auth.SequenceOrder,
                    IsApproved = approval != null,
                    ApprovedByUserName = approval?.UserName,
                    ApprovedOn = approval?.CreatedDate 
                };
            }).ToList();

            var pendingAuthority = statusList.FirstOrDefault(s => !s.IsApproved);

            return new PropertySignatureStatusDto
            {
                PropertyId = property.Id,
                PropertyNo = property.PropertyNo,
                Approvals = statusList,
                PendingAuthority = pendingAuthority?.AuthorityName,
                IsFullyApproved = pendingAuthority == null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching signature status for PropertyId={PropertyId}", propertyId);
            throw;
        }
    }

    #endregion

    #region Revoke Commands

    public async Task<bool> RevokeApprovalAsync(
        int propertyId,
        int signAuthorityId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Revoking approval for PropertyId={PropertyId}, SignAuthorityId={SignAuthorityId}",
                propertyId, signAuthorityId);

            var record = await _context.PropertySignatureDetails
                .Where(d => d.IsActive
                            && d.PropertyId == propertyId
                            && d.SignAuthorityId == signAuthorityId)
                .FirstOrDefaultAsync(cancellationToken);

            if (record == null)
                return false;

            record.IsActive = false;
            record.UpdatedDate = DateTime.Now;
            record.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Revoked approval for PropertyId={PropertyId}, SignAuthorityId={SignAuthorityId}",
                propertyId, signAuthorityId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error revoking approval for PropertyId={PropertyId}, SignAuthorityId={SignAuthorityId}",
                propertyId, signAuthorityId);
            throw;
        }
    }

    #endregion

    #region Grid Data Queries

    public async Task<SignAuthorityGridResponseDto> GetSignAuthorityGridDataAsync(
        PropertySearchRequestDto? searchRequest = null,CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching sign-off grid data, ZoneId={ZoneId}", searchRequest?.ZoneId);

            var result = new SignAuthorityGridResponseDto();

            // 1. Get all active signing authorities in SequenceOrder
            var authorities = await _context.SignAuthorityMaster.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.SequenceOrder).ToListAsync(cancellationToken);

            // 2. Get active zones
            var zonesQuery = _context.ZoneMaster.AsNoTracking().Where(z => z.IsActive);

            if (searchRequest?.ZoneId.HasValue == true)
                zonesQuery = zonesQuery.Where(z => z.Id == searchRequest.ZoneId.Value);

            var zones = await zonesQuery
                .OrderBy(z => z.SequenceNo ?? 0)
                .ThenBy(z => z.ZoneNo)
                .ToListAsync(cancellationToken);

            // 3. Populate per-zone data (N+1 issue - needs optimization in future)
            foreach (var zone in zones)
            {
                var zoneData = await GetZoneSignAuthorityDataAsync(
                    zone.Id,
                    zone.Description ?? zone.ZoneNo,
                    zone.ZoneNo,
                    authorities,
                    searchRequest,
                    cancellationToken);
                result.ZoneData.Add(zoneData);
            }

            // 4. Calculate total row (sum of all filtered zoneData list)
            result.TotalRow = CalculateTotals("TOTAL", result.ZoneData, authorities);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sign-off grid data");
            throw;
        }
    }

    public async Task<SignAuthorityWardGridResponseDto> GetSignAuthorityWardGridDataAsync(
        int zoneId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching ward-wise grid data for ZoneId={ZoneId}", zoneId);

            var paging = NormalizePaging(pageNumber, pageSize);
            var result = new SignAuthorityWardGridResponseDto();

            // 1. Get all active signing authorities in SequenceOrder
            var authorities = await _context.SignAuthorityMaster
                .AsNoTracking()
                .Where(a => a.IsActive)
                .OrderBy(a => a.SequenceOrder)
                .ToListAsync(cancellationToken);

            // 2. Fetch the zone name
            var zone = await _context.ZoneMaster
                .AsNoTracking()
                .FirstOrDefaultAsync(z => z.IsActive && z.Id == zoneId, cancellationToken);

            if (zone == null)
            {
                result.PageNumber = paging.PageSize == -1 ? 1 : paging.PageNumber;
                result.PageSize = paging.PageSize == -1 ? 0 : paging.PageSize;
                result.TotalCount = 0;
                result.TotalRow = CalculateTotals("TOTAL", result.ZoneData, authorities);
                return result;
            }

            var zoneName = zone.Description ?? zone.ZoneNo;
            var zoneNo = zone.ZoneNo;

            // 3. Get active wards for this zone
            IQueryable<WardEntity> wardsQuery = _context.WardMaster
                .AsNoTracking()
                .Where(w => w.IsActive && w.ZoneId == zoneId)
                .OrderBy(w => w.SequenceNo ?? 0)
                .ThenBy(w => w.WardNo);

            result.TotalCount = await wardsQuery.CountAsync(cancellationToken);
            result.PageNumber = paging.PageSize == -1 ? 1 : paging.PageNumber;
            result.PageSize = paging.PageSize == -1 ? result.TotalCount : paging.PageSize;

            if (paging.PageSize != -1)
            {
                wardsQuery = wardsQuery
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize);
            }

            var wards = await wardsQuery.ToListAsync(cancellationToken);

            // 4. Populate per-ward data (N+1 issue - needs optimization in future)
            foreach (var ward in wards)
            {
                var wardData = await GetWardSignAuthorityDataAsync(
                    zoneId,
                    zoneName,
                    zoneNo,
                    ward.Id,
                    ward.Description ?? ward.WardNo,
                    authorities,
                    cancellationToken);
                result.ZoneData.Add(wardData);
            }

            result.TotalRow = CalculateTotals("TOTAL", result.ZoneData, authorities);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ward-wise grid data for ZoneId={ZoneId}", zoneId);
            throw;
        }
    }

    private async Task<SignAuthorityZoneDataDto> GetWardSignAuthorityDataAsync(
        int zoneId,
        string zoneName,
        string zoneNo,
        int wardId,
        string wardName,
        List<SignAuthorityMasterEntity> authorities,
        CancellationToken cancellationToken)
    {
        var wardData = new SignAuthorityZoneDataDto
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            ZoneNo = zoneNo,
            WardId = wardId,
            WardName = wardName
        };

        // Base query for properties in this ward
        var baseQuery = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive
                        && !p.MarkedForDeletion
                        && p.PropertyNo != null
                        && p.PropertyNo != ""
                        && p.WardId == wardId);

        // Total structures (no PartitionNo)
        wardData.TotalStructure = await baseQuery
            .Where(p => p.PartitionNo == null || p.PartitionNo == "")
            .CountAsync(cancellationToken);

        // Total units (all properties)
        wardData.TotalUnit = await baseQuery.CountAsync(cancellationToken);
        wardData.TotalDemand = await GetCurrentTaxTotalDemandAsync(baseQuery.Select(p => p.Id), cancellationToken);

        foreach (var authority in authorities)
        {
            var approvedPropertyIds =
                _context.PropertySignatureDetails
                .AsNoTracking()
                .Where(d => d.IsActive
                            && d.SignAuthorityId == authority.Id
                            && d.Property.IsActive
                            && !d.Property.MarkedForDeletion
                            && d.Property.PropertyNo != null
                            && d.Property.PropertyNo != ""
                            && d.Property.WardId == wardId)
                .Select(d => d.PropertyId)
                .Distinct();

            wardData.Classifications.Add(await BuildSignAuthorityClassificationAsync(
                authority,
                wardData.TotalStructure,
                wardData.TotalUnit,
                approvedPropertyIds,
                cancellationToken));
        }

        return wardData;
    }


    private async Task<SignAuthorityZoneDataDto> GetZoneSignAuthorityDataAsync(
        int zoneId,
        string zoneName,
        string zoneNo,
        List<SignAuthorityMasterEntity> authorities,
        PropertySearchRequestDto? searchRequest,
        CancellationToken cancellationToken)
    {
        var wardIds = await _context.WardMaster
            .AsNoTracking()
            .Where(w => w.IsActive && w.ZoneId == zoneId)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        var zoneData = new SignAuthorityZoneDataDto
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            ZoneNo = zoneNo
        };

        if (!wardIds.Any())
            return zoneData;

        // Base query for properties in this zone
        var baseQuery = _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive
                        && !p.MarkedForDeletion
                        && p.PropertyNo != null
                        && p.PropertyNo != ""
                        && wardIds.Contains(p.WardId));

        // Total structures (no PartitionNo)
        zoneData.TotalStructure = await baseQuery
            .Where(p => p.PartitionNo == null || p.PartitionNo == "")
            .CountAsync(cancellationToken);

        // Total units (all properties)
        zoneData.TotalUnit = await baseQuery.CountAsync(cancellationToken);
        zoneData.TotalDemand = await GetCurrentTaxTotalDemandAsync(baseQuery.Select(p => p.Id), cancellationToken);

        foreach (var authority in authorities)
        {
            var approvedPropertyIds =
                _context.PropertySignatureDetails
                .AsNoTracking()
                .Where(d => d.IsActive
                            && d.SignAuthorityId == authority.Id
                            && d.Property.IsActive
                            && !d.Property.MarkedForDeletion
                            && d.Property.PropertyNo != null
                            && d.Property.PropertyNo != ""
                            && wardIds.Contains(d.Property.WardId))
                .Select(d => d.PropertyId)
                .Distinct();

            zoneData.Classifications.Add(await BuildSignAuthorityClassificationAsync(
                authority,
                zoneData.TotalStructure,
                zoneData.TotalUnit,
                approvedPropertyIds,
                cancellationToken));
        }

        return zoneData;
    }

    private async Task<SignAuthorityClassificationDto> BuildSignAuthorityClassificationAsync(
        SignAuthorityMasterEntity authority,
        int totalStructure,
        int totalUnit,
        IQueryable<int> approvedPropertyIds,
        CancellationToken cancellationToken)
    {
        var classification = new SignAuthorityClassificationDto
        {
            TypeId = authority.Id,
            Type = authority.AuthorityName
        };

        var signedUnit = await approvedPropertyIds.CountAsync(cancellationToken);
        if (signedUnit > 0)
        {
            classification.Structure = await (
                from propertyId in approvedPropertyIds
                join p in _context.PropertyMast.AsNoTracking() on propertyId equals p.Id
                where p.PartitionNo == null || p.PartitionNo == ""
                select p.Id
            ).CountAsync(cancellationToken);

            classification.CurrentDemand = await (
                from propertyId in approvedPropertyIds
                join t in _context.TransMast.AsNoTracking() on propertyId equals t.PropertyId
                join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
                where t.IsActive
                      && !t.MarkedForDeletion
                      && tax.IsActive
                      && tax.TaxCode == TaxTotalCode
                      && tax.TaxName == TaxTotalName
                select (decimal?)t.TaxAmount
            ).SumAsync(cancellationToken) ?? 0m;

            classification.OldDemand = await (
                from propertyId in approvedPropertyIds
                join p in _context.PropertyMast.AsNoTracking() on propertyId equals p.Id
                where p.PropertyMastOldId != null
                join tmo in _context.TransMastOld.AsNoTracking() on p.PropertyMastOldId equals tmo.PropertyMastOldId
                join tax in _context.TaxMaster.AsNoTracking() on tmo.TaxId equals tax.Id
                where tmo.IsActive
                      && !tmo.MarkedForDeletion
                      && tax.IsActive
                      && tax.TaxCode == TaxTotalCode
                      && tax.TaxName == TaxTotalName
                select (decimal?)tmo.TaxAmount
            ).SumAsync(cancellationToken) ?? 0m;

            classification.RetroDemand = await (
                from propertyId in approvedPropertyIds
                join tr in _context.TaxPendingDetailsRetro.AsNoTracking() on propertyId equals tr.PropertyId
                join tax in _context.TaxMaster.AsNoTracking() on tr.TaxId equals tax.Id
                where tr.IsActive
                      && !tr.MarkedForDeletion
                      && tax.IsActive
                      && tax.TaxCode == TaxTotalCode
                      && tax.TaxName == TaxTotalName
                select tr.PendingAmount
            ).SumAsync(cancellationToken) ?? 0m;

            classification.Unit = signedUnit;
            classification.TotalDemand = classification.CurrentDemand + classification.RetroDemand;
            classification.AdditionalRevenueGenerated = classification.CurrentDemand - classification.OldDemand;
        }

        classification.PendingStructure = Math.Max(0, totalStructure - classification.Structure);
        classification.PendingUnit = Math.Max(0, totalUnit - signedUnit);

        return classification;
    }

    private static SignAuthorityZoneDataDto CalculateTotals(
        string name,
        List<SignAuthorityZoneDataDto> zoneDataList,
        List<SignAuthorityMasterEntity> authorities)
    {
        var totals = new SignAuthorityZoneDataDto
        {
            ZoneName = name,
            TotalStructure = zoneDataList.Sum(z => z.TotalStructure),
            TotalUnit = zoneDataList.Sum(z => z.TotalUnit),
            TotalDemand = zoneDataList.Sum(z => z.TotalDemand)
        };

        foreach (var authority in authorities)
        {
            var classification = new SignAuthorityClassificationDto
            {
                TypeId = authority.Id,
                Type = authority.AuthorityName
            };

            var clsList = zoneDataList
                .SelectMany(z => z.Classifications)
                .Where(c => c.Type == authority.AuthorityName)
                .ToList();

            if (clsList.Any())
            {
                classification.Structure = clsList.Sum(c => c.Structure);
                classification.Unit = clsList.Sum(c => c.Unit);
                classification.PendingStructure = clsList.Sum(c => c.PendingStructure);
                classification.PendingUnit = clsList.Sum(c => c.PendingUnit);
                classification.OldDemand = clsList.Sum(c => c.OldDemand);
                classification.CurrentDemand = clsList.Sum(c => c.CurrentDemand);
                classification.RetroDemand = clsList.Sum(c => c.RetroDemand);
                classification.TotalDemand = clsList.Sum(c => c.TotalDemand);
                classification.AdditionalRevenueGenerated = clsList.Sum(c => c.AdditionalRevenueGenerated);
            }

            totals.Classifications.Add(classification);
        }

        return totals;
    }

    private async Task<decimal> GetCurrentTaxTotalDemandAsync(
        IQueryable<int> propertyIds,
        CancellationToken cancellationToken)
        => await (
            from propertyId in propertyIds
            join t in _context.TransMast.AsNoTracking() on propertyId equals t.PropertyId
            join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
            where t.IsActive
                  && !t.MarkedForDeletion
                  && tax.IsActive
                  && tax.TaxCode == TaxTotalCode
                  && tax.TaxName == TaxTotalName
            select (decimal?)t.TaxAmount
        ).SumAsync(cancellationToken) ?? 0m;

    public async Task<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>> GetBuildingWiseDataAsync(
        PropertySignatureBuildingWiseQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var paging = NormalizePaging(queryParameters.PageNumber, queryParameters.PageSize);
        var wardId = queryParameters.WardId;
        var workflowStageId = queryParameters.WorkflowStageId;
        var noticeNo = NormalizeOptionalText(queryParameters.NoticeNo);

        var authorities = await _context.SignAuthorityMaster
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SequenceOrder)
            .Select(a => new AuthoritySignatureSource(a.Id, a.AuthorityName, a.AuthorityCode, a.SequenceOrder))
            .ToListAsync(cancellationToken);

        var scopedBuildingKeysQuery = (
            from pwd in _context.PropertyWorkflowDetails.AsNoTracking()
            join p in _context.PropertyMast.AsNoTracking() on pwd.PropertyId equals p.Id
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            where pwd.WorkflowStageId == workflowStageId
                  && p.WardId == wardId
                  && p.IsActive
                  && !p.MarkedForDeletion
                  && p.PropertyNo != null
                  && p.PropertyNo != ""
                  && w.IsActive
            select new
            {
                PropertyId = p.Id,
                p.PropertyNo,
                w.WardNo,
                BuildingNo = w.WardNo + "-" + p.PropertyNo
            }).Distinct();

        if (noticeNo != null)
        {
            var noticePropertyIds = _context.PropertySignatureDetails
                .AsNoTracking()
                .Where(sig => sig.IsActive
                              && sig.NoticeNo != null
                              && sig.NoticeNo.Contains(noticeNo))
                .Select(sig => sig.PropertyId)
                .Distinct();

            scopedBuildingKeysQuery = scopedBuildingKeysQuery
                .Where(building => noticePropertyIds.Contains(building.PropertyId));
        }

        var scopedBuildingKeys = await scopedBuildingKeysQuery.ToListAsync(cancellationToken);

        if (!scopedBuildingKeys.Any())
            return CreatePagedResult(Enumerable.Empty<PropertySignatureSubGridDto>(), 0, paging.PageNumber, paging.PageSize);

        var scopedPropertyNos = scopedBuildingKeys.Select(x => x.PropertyNo).Distinct().ToList();

        var buildingPropertiesQuery = (
            from p in _context.PropertyMast.AsNoTracking()
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id
            where p.WardId == wardId
                  && scopedPropertyNos.Contains(p.PropertyNo!)
                  && p.IsActive
                  && !p.MarkedForDeletion
                  && p.PropertyNo != null
                  && p.PropertyNo != ""
                  && w.IsActive
            select new
            {
                PropertyId = p.Id,
                p.PropertyNo,
                p.PartitionNo,
                p.UPICId,
                w.WardNo,
                BuildingNo = w.WardNo + "-" + p.PropertyNo
            }).Distinct();

        var baseRows = await buildingPropertiesQuery.ToListAsync(cancellationToken);

        var demandRows = await (
            from p in buildingPropertiesQuery
            join t in _context.TransMast.AsNoTracking() on p.PropertyId equals t.PropertyId
            join tax in _context.TaxMaster.AsNoTracking() on t.TaxId equals tax.Id
            where t.IsActive
                  && !t.MarkedForDeletion
                  && tax.IsActive
                  && tax.TaxCode == TaxTotalCode
                  && tax.TaxName == TaxTotalName
            group t by t.PropertyId into g
            select new
            {
                PropertyId = g.Key,
                Demand = g.Sum(x => x.TaxAmount)
            })
            .ToListAsync(cancellationToken);

        var signatureRows = await (
            from p in buildingPropertiesQuery
            join sig in _context.PropertySignatureDetails.AsNoTracking() on p.PropertyId equals sig.PropertyId
            join auth in _context.SignAuthorityMaster.AsNoTracking() on sig.SignAuthorityId equals auth.Id
            where sig.IsActive
                  && auth.IsActive
            select new
            {
                p.PropertyId,
                sig.NoticeNo,
                p.UPICId,
                sig.SignAuthorityId,
                auth.AuthorityCode,
                auth.SequenceOrder
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var demandByPropertyId = demandRows.ToDictionary(x => x.PropertyId, x => x.Demand);
        var signaturesByPropertyId = signatureRows
            .GroupBy(x => x.PropertyId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = baseRows
            .GroupBy(p => p.BuildingNo)
            .OrderBy(g => g.Key)
            .Select(group =>
            {
                var propertyIds = group.Select(p => p.PropertyId).Distinct().ToList();
                var groupSignatures = propertyIds
                    .Where(signaturesByPropertyId.ContainsKey)
                    .SelectMany(id => signaturesByPropertyId[id])
                    .ToList();
                var mainPropertyId = group
                    .Where(p => string.IsNullOrWhiteSpace(p.PartitionNo))
                    .Select(p => p.PropertyId)
                    .FirstOrDefault();

                var noticeNo = mainPropertyId > 0 && signaturesByPropertyId.TryGetValue(mainPropertyId, out var mainSignatures)
                    ? mainSignatures
                        .Select(s => !string.IsNullOrWhiteSpace(s.NoticeNo) ? s.NoticeNo : s.UPICId)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                    : null;

                noticeNo ??= groupSignatures
                    .Select(s => !string.IsNullOrWhiteSpace(s.NoticeNo) ? s.NoticeNo : s.UPICId)
                    .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

                noticeNo ??= group
                    .Select(p => p.UPICId)
                    .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

                var signedAuthorityIds = groupSignatures
                    .Select(s => s.SignAuthorityId)
                    .ToHashSet();

                var row = new PropertySignatureSubGridDto
                {
                    BuildingNo = group.Key,
                    NoticeNo = noticeNo,
                    Units = propertyIds.Count,
                    TotalDemand = propertyIds.Sum(id => demandByPropertyId.GetValueOrDefault(id))
                };

                row.AuthoritySignatures = BuildAuthoritySignatures(authorities, signedAuthorityIds);

                return row;
            })
            .ToList();

        return CreatePagedResult(rows, rows.Count, paging.PageNumber, paging.PageSize);
    }

    public async Task<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>> GetPropertyWiseDataAsync(
        PropertySignaturePropertyWiseQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var paging = NormalizePaging(queryParameters.PageNumber, queryParameters.PageSize);
        var (wardNo, basePropertyNo) = ParseBuildingPropertyNo(queryParameters.PropertyNo);
        if (string.IsNullOrWhiteSpace(wardNo) || string.IsNullOrWhiteSpace(basePropertyNo))
            return CreatePagedResult(Enumerable.Empty<PropertySignaturePropertyWiseDto>(), 0, paging.PageNumber, paging.PageSize);

        var authorities = await _context.SignAuthorityMaster
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SequenceOrder)
            .Select(a => new AuthoritySignatureSource(a.Id, a.AuthorityName, a.AuthorityCode, a.SequenceOrder))
            .ToListAsync(cancellationToken);

        var propertyRowsQuery = (
            from p in _context.PropertyMast.AsNoTracking()
            join w in _context.WardMaster.AsNoTracking() on p.WardId equals w.Id into wardJoin
            from w in wardJoin.DefaultIfEmpty()
            join pc in _context.PropertyCategoryMaster.AsNoTracking().Where(x => x.IsActive) on p.CategoryId equals pc.Id into categoryJoin
            from pc in categoryJoin.DefaultIfEmpty()
            join pt in _context.PropertyTypeMasters.AsNoTracking().Where(x => x.IsActive) on p.PropertyTypeId equals pt.Id into typeJoin
            from pt in typeJoin.DefaultIfEmpty()
            join status in _context.PropertyAssessmentStatuses.AsNoTracking().Where(x => x.IsActive) on p.PropertyAssessmentStatusId equals status.Id into statusJoin
            from status in statusJoin.DefaultIfEmpty()
            join society in _context.SocietyDetailsMast.AsNoTracking().Where(x => x.IsActive && !x.MarkedForDeletion) on p.Id equals society.PropertyId into societyJoin
            from society in societyJoin.DefaultIfEmpty()
            join wing in _context.WingEntity.AsNoTracking().Where(x => x.IsActive) on society.WingId equals wing.Id into wingJoin
            from wing in wingJoin.DefaultIfEmpty()
            join oldProperty in _context.PropertyMastOld.AsNoTracking().Where(x => x.IsActive && !x.MarkedForDeletion) on p.PropertyMastOldId equals oldProperty.Id into oldJoin
            from oldProperty in oldJoin.DefaultIfEmpty()
            where p.IsActive
                  && !p.MarkedForDeletion
                  && w != null
                  && w.IsActive
                  && w.WardNo == wardNo
                  && p.PropertyNo == basePropertyNo
            select new
            {
                PropertyId = p.Id,
                p.WardId,
                WardNo = w.WardNo,
                p.PropertyNo,
                p.PartitionNo,
                p.OwnerName,
                p.OccupierName,
                p.Address,
                p.FlatOrShopNo,
                p.FlatOrShopName,
                p.PropertyMastOldId,
                p.UPICId,
                Description = pc != null ? pc.PropertyCategoryName : "",
                PropertyType = status != null ? status.StatusName : "",
                TypeDescription = pt != null ? pt.PropertyDescription : "",
                SocietyName = society != null ? society.SocietyName ?? society.SocietyNameEnglish : null,
                BuilderName = society != null ? society.BuilderName ?? society.BuilderNameEnglish : null,
                WingNo = wing != null ? wing.WingNo : null,
                OldWardNo = oldProperty != null ? oldProperty.OldWardNo : null,
                OldPropertyNo = oldProperty != null ? oldProperty.OldPropertyNo : null,
                OldPartitionNo = oldProperty != null ? oldProperty.OldPartitionNo : null,
                OldArea = oldProperty != null ? oldProperty.OldConstructionArea : null,
                OldUse = oldProperty != null ? oldProperty.OldUseType : null,
                OldYear = oldProperty != null ? oldProperty.OldConstructionYear : null,
                OldAssessmentYear = oldProperty != null ? oldProperty.OldAssessmentYear : null,
                OldRV = oldProperty != null ? oldProperty.OldRV : null,
                OldTax = oldProperty != null ? oldProperty.OldTotalTax : null
            });

        var searchType = NormalizeOptionalText(queryParameters.SearchType);
        if (searchType != null)
        {
            var searchProperty = ParsePropertyWiseSearchNo(searchType);
            if (!string.IsNullOrWhiteSpace(searchProperty.WardNo)
                && !string.IsNullOrWhiteSpace(searchProperty.PropertyNo))
            {
                var searchWardNo = searchProperty.WardNo.ToUpper();
                var searchPropertyNo = searchProperty.PropertyNo;

                propertyRowsQuery = string.IsNullOrWhiteSpace(searchProperty.PartitionNo)
                    ? propertyRowsQuery.Where(p =>
                        p.WardNo.ToUpper() == searchWardNo
                        && p.PropertyNo == searchPropertyNo)
                    : propertyRowsQuery.Where(p =>
                        p.WardNo.ToUpper() == searchWardNo
                        && p.PropertyNo == searchPropertyNo
                        && p.PartitionNo != null
                        && p.PartitionNo == searchProperty.PartitionNo);
            }
            else
            {
                propertyRowsQuery = propertyRowsQuery.Where(p =>
                    (p.PropertyNo != null && p.PropertyNo.Contains(searchType))
                    || (p.PartitionNo != null && p.PartitionNo.Contains(searchType)));
            }
        }

        var propertyRows = await propertyRowsQuery.ToListAsync(cancellationToken);

        if (!propertyRows.Any())
            return CreatePagedResult(Enumerable.Empty<PropertySignaturePropertyWiseDto>(), 0, paging.PageNumber, paging.PageSize);

        var propertyIds = propertyRows.Select(p => p.PropertyId).Distinct().ToList();

        var propertyMapRows = await _context.PropertyMapMasters
            .AsNoTracking()
            .Where(p => p.IsActive && propertyIds.Contains(p.Id) && p.ParentPropertyMapId.HasValue)
            .Select(p => new { PropertyId = p.Id, OldPropertyId = p.ParentPropertyMapId!.Value })
            .ToListAsync(cancellationToken);

        var mappedOldPropertyIdsByPropertyId = propertyMapRows
            .GroupBy(p => p.PropertyId)
            .ToDictionary(g => g.Key, g => g.First().OldPropertyId);

        var newDetailRows = await (
            from pd in _context.PropertyDetails.AsNoTracking()
            join tou in _context.TypeOfUse.AsNoTracking().Where(x => x.IsActive) on pd.TypeOfUseId equals tou.Id into typeOfUseJoin
            from tou in typeOfUseJoin.DefaultIfEmpty()
            where propertyIds.Contains(pd.PropertyId)
                  && pd.IsActive
                  && !pd.MarkedForDeletion
            select new
            {
                pd.PropertyId,
                Area = (decimal)(pd.BuiltupAreaSqMeter ?? 0),
                Use = tou != null ? tou.Description : null,
                Year = pd.ConstructionYear
            })
            .ToListAsync(cancellationToken);

        var newDetailsByPropertyId = newDetailRows
            .GroupBy(d => d.PropertyId)
            .ToDictionary(g => g.Key, g => new
            {
                Area = g.Sum(x => x.Area),
                Use = JoinDistinct(g.Select(x => x.Use)),
                Year = JoinDistinct(g.Select(x => x.Year))
            });

        var newRvRows = await _context.TransMast
            .AsNoTracking()
            .Where(t => propertyIds.Contains(t.PropertyId)
                        && t.IsActive
                        && !t.MarkedForDeletion
                        && t.CalculationType == "RV")
            .GroupBy(t => t.PropertyId)
            .Select(g => new
            {
                PropertyId = g.Key,
                RV = g.Max(x => x.CalculationValue)
            })
            .ToListAsync(cancellationToken);

        var newRvByPropertyId = newRvRows.ToDictionary(x => x.PropertyId, x => x.RV);

        var demandRows = await (
            from t in _context.TransMast.AsNoTracking()
            join tax in _context.TaxMaster.AsNoTracking().Where(x => x.IsActive) on t.TaxId equals tax.Id
            where propertyIds.Contains(t.PropertyId)
                  && t.IsActive
                  && !t.MarkedForDeletion
                  && tax.TaxCode == TaxTotalCode
                  && tax.TaxName == TaxTotalName
            group t by t.PropertyId into g
            select new
            {
                PropertyId = g.Key,
                Demand = g.Sum(x => x.TaxAmount)
            })
            .ToListAsync(cancellationToken);

        var demandByPropertyId = demandRows.ToDictionary(x => x.PropertyId, x => x.Demand);

        var oldPropertyIds = propertyRows
            .Select(p => p.PropertyMastOldId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Concat(mappedOldPropertyIdsByPropertyId.Values)
            .Distinct()
            .ToList();

        var oldDetailRows = await (
            from pdo in _context.PropertyDetailsOld.AsNoTracking()
            join tou in _context.TypeOfUse.AsNoTracking().Where(x => x.IsActive) on pdo.OldTypeOfUseId equals tou.Id into typeOfUseJoin
            from tou in typeOfUseJoin.DefaultIfEmpty()
            where oldPropertyIds.Contains(pdo.PropertyMastOldId)
                  && pdo.IsActive
                  && !pdo.MarkedForDeletion
            select new
            {
                OldPropertyId = pdo.PropertyMastOldId,
                Area = (decimal)(pdo.OldBuiltupAreaSqMeter ?? 0),
                Use = tou != null ? tou.Description : null,
                Year = pdo.OldConstructionYear
            })
            .ToListAsync(cancellationToken);

        var oldDetailsByOldPropertyId = oldDetailRows
            .GroupBy(d => d.OldPropertyId)
            .ToDictionary(g => g.Key, g => new
            {
                Area = g.Sum(x => x.Area),
                Use = JoinDistinct(g.Select(x => x.Use)),
                Year = JoinDistinct(g.Select(x => x.Year))
            });

        var oldTaxRows = await (
            from t in _context.TransMastOld.AsNoTracking()
            join tax in _context.TaxMaster.AsNoTracking().Where(x => x.IsActive) on t.TaxId equals tax.Id
            where oldPropertyIds.Contains(t.PropertyMastOldId)
                  && t.IsActive
                  && !t.MarkedForDeletion
                  && t.CalculationType == "RV"
                  && tax.TaxCode == TaxTotalCode
                  && tax.TaxName == TaxTotalName
            group t by t.PropertyMastOldId into g
            select new
            {
                OldPropertyId = g.Key,
                Tax = g.Sum(x => x.TaxAmount)
            })
            .ToListAsync(cancellationToken);

        var oldTaxByOldPropertyId = oldTaxRows.ToDictionary(x => x.OldPropertyId, x => x.Tax);

        var signatureRows = await _context.PropertySignatureDetails
            .AsNoTracking()
            .Where(sig => propertyIds.Contains(sig.PropertyId) && sig.IsActive)
            .Select(sig => new
            {
                sig.PropertyId,
                sig.SignAuthorityId
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var signedAuthorityIdsByPropertyId = signatureRows
            .GroupBy(s => s.PropertyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.SignAuthorityId).ToHashSet());

        var rows = propertyRows
            .OrderBy(p => p.PropertyNo)
            .ThenBy(p => string.IsNullOrWhiteSpace(p.PartitionNo) ? " " : p.PartitionNo)
            .Select(p =>
            {
                var oldPropertyId = p.PropertyMastOldId
                    ?? (mappedOldPropertyIdsByPropertyId.TryGetValue(p.PropertyId, out var mappedOldPropertyId)
                        ? mappedOldPropertyId
                        : 0);
                var hasOldPropertyId = oldPropertyId > 0;
                var totalDemand = demandByPropertyId.GetValueOrDefault(p.PropertyId);
                newDetailsByPropertyId.TryGetValue(p.PropertyId, out var newDetail);
                var oldDetail = hasOldPropertyId && oldDetailsByOldPropertyId.TryGetValue(oldPropertyId, out var oldDetailValue)
                    ? oldDetailValue
                    : null;
                var signedAuthorityIds = signedAuthorityIdsByPropertyId.GetValueOrDefault(p.PropertyId) ?? new HashSet<int>();

                var authoritySignatures = BuildAuthoritySignatures(authorities, signedAuthorityIds);

                var row = new PropertySignaturePropertyWiseDto
                {
                    PropertyId = p.PropertyId,
                    WardNo = p.WardNo,
                    NewPropertyNo = FormatPropertyNo(p.WardNo, p.PropertyNo, p.PartitionNo),
                    OldPropertyNo = FormatPropertyNo(p.OldWardNo, p.OldPropertyNo, p.OldPartitionNo),
                    Description = !string.IsNullOrWhiteSpace(p.Description) ? p.Description : p.TypeDescription,
                    OwnerName = p.OwnerName ?? "",
                    OccupierName = p.OccupierName ?? "",
                    Address = p.Address ?? "",
                    SocietyName = p.SocietyName ?? "",
                    BuilderName = p.BuilderName ?? "",
                    WingNo = p.WingNo ?? "",
                    FlatNo = p.FlatOrShopNo ?? p.FlatOrShopName ?? "",
                    PropertyType = p.PropertyType,
                    TotalDemand = totalDemand,
                    OldRecord = new PropertySignaturePropertyRecordDto
                    {
                        Area = FormatNumber(oldDetail?.Area ?? (decimal?)p.OldArea),
                        Use = FirstAvailable(oldDetail?.Use, p.OldUse),
                        Year = FirstAvailable(oldDetail?.Year, p.OldYear, p.OldAssessmentYear?.ToString()),
                        RV = FormatNumber((decimal?)p.OldRV),
                        Tax = FormatNumber(
                            hasOldPropertyId && oldTaxByOldPropertyId.TryGetValue(oldPropertyId, out var oldTax)
                                ? oldTax
                                : (decimal?)p.OldTax)
                    },
                    NewRecord = new PropertySignaturePropertyRecordDto
                    {
                        Area = FormatNumber(newDetail?.Area),
                        Use = FirstAvailable(newDetail?.Use),
                        Year = FirstAvailable(newDetail?.Year),
                        RV = FormatNumber(newRvByPropertyId.TryGetValue(p.PropertyId, out var newRv) ? newRv : null),
                        Tax = FormatNumber(totalDemand)
                    },
                    AuthoritySignatures = authoritySignatures
                };

                ApplyStaticSignatureFlags(row);
                return row;
            })
            .ToList();

        return CreatePagedResult(rows, rows.Count, paging.PageNumber, paging.PageSize);
    }

    private static (string WardNo, string PropertyNo) ParseBuildingPropertyNo(string propertyNo)
    {
        if (string.IsNullOrWhiteSpace(propertyNo))
            return (string.Empty, string.Empty);

        var parts = propertyNo.Trim().Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (string.Empty, propertyNo.Trim());
    }

    private static (string? WardNo, string? PropertyNo, string? PartitionNo) ParsePropertyWiseSearchNo(string propertyNo)
    {
        var parts = propertyNo
            .Trim()
            .Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            >= 3 => (parts[0], parts[1], string.Join("-", parts.Skip(2))),
            2 => (parts[0], parts[1], null),
            _ => (null, propertyNo.Trim(), null)
        };
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatPropertyNo(string? wardNo, string? propertyNo, string? partitionNo)
    {
        var parts = new[] { wardNo, propertyNo, partitionNo }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());

        return string.Join("-", parts);
    }

    private static string JoinDistinct(IEnumerable<string?> values)
    {
        var joined = string.Join(", ", values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .Distinct());

        return string.IsNullOrWhiteSpace(joined) ? "N/A" : joined;
    }

    private static string FirstAvailable(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "N/A";

    private static string FormatNumber(decimal? value)
        => value.HasValue && value.Value > 0 ? value.Value.ToString("0.##") : "N/A";

    private static List<PropertySignatureAuthoritySignDto> BuildAuthoritySignatures(
        IEnumerable<AuthoritySignatureSource> authorities,
        ISet<int> signedAuthorityIds)
        => authorities
            .Select(authority => new PropertySignatureAuthoritySignDto
            {
                SignAuthorityId = authority.Id,
                AuthorityName = authority.AuthorityName,
                AuthorityCode = authority.AuthorityCode,
                SequenceOrder = authority.SequenceOrder,
                IsSigned = signedAuthorityIds.Contains(authority.Id) ? 1 : 0
            })
            .ToList();

    private static (int PageNumber, int PageSize) NormalizePaging(int pageNumber, int pageSize)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize == -1 ? -1 : pageSize < 1 ? 10 : Math.Min(pageSize, 500);

        return (normalizedPageNumber, normalizedPageSize);
    }

    private static PropertySignaturePagedResultDto<T> CreatePagedResult<T>(
        IEnumerable<T> rows,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        if (pageSize == -1)
        {
            var allRows = rows.ToList();

            return new PropertySignaturePagedResultDto<T>
            {
                Items = allRows,
                TotalCount = totalCount,
                PageNumber = 1,
                PageSize = Math.Max(1, totalCount)
            };
        }

        var pagedRows = rows
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PropertySignaturePagedResultDto<T>
        {
            Items = pagedRows,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static void ApplyStaticSignatureFlags(PropertySignaturePropertyWiseDto row)
    {
        foreach (var sign in row.AuthoritySignatures)
        {
            switch (sign.SequenceOrder)
            {
                case 1:
                    row.ClerkSign = sign.IsSigned;
                    break;
                case 2:
                    row.TaxInspectorSign = sign.IsSigned;
                    break;
                case 3:
                    row.AssistantCommissionerSign = sign.IsSigned;
                    break;
                case 4:
                    row.DeputyCommissionerSign = sign.IsSigned;
                    break;
                case 5:
                    row.AdditionalCommissionerSign = sign.IsSigned;
                    break;
            }
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Builds base query for active properties.
    /// </summary>
    private IQueryable<PropertyEntity> BuildActivePropertiesQuery()
        => _context.PropertyMast
            .AsNoTracking()
            .Where(p => p.IsActive
                        && !p.MarkedForDeletion
                        && p.PropertyNo != null
                        && p.PropertyNo != "");

    #endregion

    private sealed record AuthoritySignatureSource(
        int Id,
        string AuthorityName,
        string AuthorityCode,
        int SequenceOrder);
}

