using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyKyc;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Implementation of the "Owner and Occupier Registration" use-case for the Property aggregate.
/// Owns existence handling, the upsert decision for the assessment row and the transaction boundary;
/// persistence is delegated to <see cref="IPropertyKycRepository"/>, saving to <see cref="IUnitOfWork"/>,
/// and aggregate invariants to <see cref="IPropertyMutationInvariantPolicy"/>.
/// </summary>
public class PropertyKycService : IPropertyKycService
{
    private readonly IPropertyKycRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPropertyMutationInvariantPolicy _invariantPolicy;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _assessmentRepository;
    private readonly IRepository<OwnerTypeMasterEntity, int> _ownerTypeRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<WingEntity, int> _wingRepository;
    private readonly IRepository<RoomWiseSubmissionDetailsEntity, int> _roomWiseRepository;
    private readonly IRepository<CommunicationDetailsEntity, int> _communicationRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyOldRepository;
    private readonly ILogger<PropertyKycService> _logger;
    private readonly IRepository<WingDetailsMastEntity, int> _wingDetailsMastRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<VirtualPropertyTransferHistoryEntity, int> _virtualPropertyTransferHistoryRepository;

    public PropertyKycService(
        IPropertyKycRepository repository,
        IUnitOfWork unitOfWork,
        IPropertyMutationInvariantPolicy invariantPolicy,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyAssessmentEntity, int> assessmentRepository,
        IRepository<OwnerTypeMasterEntity, int> ownerTypeRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<WingEntity, int> wingRepository,
        IRepository<RoomWiseSubmissionDetailsEntity, int> roomWiseRepository,
        IRepository<CommunicationDetailsEntity, int> communicationRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        ILogger<PropertyKycService> logger,
        IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<VirtualPropertyTransferHistoryEntity, int> virtualPropertyTransferHistoryRepository)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _invariantPolicy = invariantPolicy;
        _propertyRepository = propertyRepository;
        _assessmentRepository = assessmentRepository;
        _ownerTypeRepository = ownerTypeRepository;
        _societyRepository = societyRepository;
        _wingRepository = wingRepository;
        _roomWiseRepository = roomWiseRepository;
        _communicationRepository = communicationRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _propertyOldRepository = propertyOldRepository;
        _logger = logger;
        _wingDetailsMastRepository = wingDetailsMastRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _virtualPropertyTransferHistoryRepository = virtualPropertyTransferHistoryRepository;
    }

    public async Task<PropertyKycDetailsCommonDto?> GetKycDetailsCommon(
        PropertyKycDetailsQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var propertyNo = queryParameters.PropertyNo.Trim();

        var partitionNo = string.IsNullOrWhiteSpace(queryParameters.PartitionNo)
            ? null
            : queryParameters.PartitionNo.Trim();

        // Step 1: Fetch only required Property fields.
        // Projection avoids selecting PropertyMastOldId.
        var property = await _propertyRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.WardId == queryParameters.WardId &&
                x.PropertyNo == propertyNo &&
                x.IsActive &&
                !x.MarkedForDeletion &&
                (
                    partitionNo == null
                        ? string.IsNullOrEmpty(x.PartitionNo)
                        : x.PartitionNo == partitionNo
                ))
            .Select(x => new
            {
                x.Id,
                x.PropertyNo,
                x.PartitionNo,
                x.PropertyTypeId,
                x.CategoryId,
                x.PlotNo,
                x.CSN,

                x.OwnerTitle,
                x.OwnerName,
                x.OwnerTitleEnglish,
                x.OwnerNameEnglish,

                x.OccupierTitle,
                x.OccupierName,
                x.OccupierTitleEnglish,
                x.OccupierNameEnglish,

                x.Address,
                x.Location,
                x.AddressEnglish,
                x.LocationEnglish,

                x.FlatOrShopName,
                x.FlatOrShopNameEnglish,
                x.FlatOrShopNo,
                x.FlatOrShopNoEnglish,

                x.MobileNo,
                x.MobileNoRemarkId,
                x.AlternateMobileNo,
                x.OccupierMobileNo,
                x.OccupierMobileNoRemarkId,
                x.EmailId,
                x.PinCode
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
        {
            _logger.LogWarning(
                "Property not found for WardId: {WardId}, PropertyNo: {PropertyNo}, PartitionNo: {PartitionNo}",
                queryParameters.WardId,
                propertyNo,
                partitionNo);

            return null;
        }

        var propertyId = property.Id;

        // Step 2: Assessment details
        var assessment = await _assessmentRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.PropertyId == propertyId &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.OwnerTypeId,
                x.AdharCardNo,
                x.BlockNo,
                x.SurveyRemark
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 3: Owner type
        string? ownerType = null;

        if (assessment?.OwnerTypeId.HasValue == true)
        {
            ownerType = await _ownerTypeRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.Id == assessment.OwnerTypeId.Value &&
                    x.IsActive)
                .Select(x => x.OwnerType)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Step 4: Society and wing
        var wingQuery = _wingDetailsMastRepository.GetQueryable().AsNoTracking().Where(w => w.IsActive && !w.MarkedForDeletion);
        var society = await (
                from x in _societyRepository.GetQueryable().AsNoTracking()
                where x.PropertyId == property.Id && x.IsActive && !x.MarkedForDeletion
                join wdm in wingQuery on x.Id equals wdm.SocietyDetailsMastId into wdmGroup
                from wdm in wdmGroup.DefaultIfEmpty()
                select new
                {
                    x.Id,
                    x.SocietyName,
                    x.SocietyAddress,
                    x.SocietyNameEnglish,
                    x.SocietyAddressEnglish,
                    x.SocietyEmailId,

                    WingId = wdm != null ? (int?)wdm.WingMasterId : null,
                    WingName = wdm != null ? wdm.WingName : null,

                    x.ManagerName,
                    x.ManagerNameEnglish,
                    x.ManagerMobileNo,
                    x.ManagerMobileNoRemarkId,
                    x.ManagerEmailId,

                    x.SecretaryName,
                    x.SecretaryNameEnglish,
                    x.SecretaryMobileNo,
                    x.SecretaryMobileNoRemarkId,
                    x.SecretaryEmailId,

                    x.LandOwnerName,
                    x.LandOwnerNameEnglish,

                    x.BuilderName,
                    x.BuilderNameEnglish,
                    x.BuilderMobileNo,
                    x.BuilderMobileNoRemarkId
                }
            ).FirstOrDefaultAsync(cancellationToken);

        string? wingNo = null;

        if (society?.WingId != null)
        {
            wingNo = await _wingRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.Id == society.WingId.Value &&
                    x.IsActive)
                .Select(x => x.WingNo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Step 5: Plot-area details
        var roomWiseDetails = await _roomWiseRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.PropertyId == propertyId &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.LengthMtr,
                x.WidthMtr,
                x.TotalAreaSqMtr
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Step 6: Communication details
        var issuedBy = await _communicationRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => x.IssuedBy)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 7: Find PropertyMastOld through PropertyMapDetail
        var oldPropertyId = await _propertyMapDetailRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.PropertyIdNew == propertyId &&
                x.IsActive &&
                x.IsCurrent)
            .OrderByDescending(x => x.Id)
            .Select(x => x.PropertyIdOld)
            .FirstOrDefaultAsync(cancellationToken);

        var oldProperty = oldPropertyId > 0
            ? await _propertyOldRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.Id == oldPropertyId &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.OldCSN,
                    x.OldWardNo,
                    x.OldSocietyName
                })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        // Step 8: TypeOfUseId from PropertyDetails
        var typeOfUseId = await _propertyDetailsRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.PropertyId == propertyId &&
                x.IsActive &&
                !x.MarkedForDeletion)
            .OrderBy(x => x.Id)
            .Select(x => (int?)x.TypeOfUseId)
            .FirstOrDefaultAsync(cancellationToken);

        // Step 9: VirtualPropertyTransferHistory active records for PropertyId
        var virtualPropertyTransferHistory = await _virtualPropertyTransferHistoryRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new VirtualPropertyTransferHistoryDto
            {
                Id = x.Id,
                PropertyId = x.PropertyId,
                WardId = x.WardId,
                PropertyNo = x.PropertyNo,
                PartitionNo = x.PartitionNo,
                TransferredWardId = x.TransferredWardId,
                TransferredPropertyNo = x.TransferredPropertyNo,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedDate = x.CreatedDate,
                UpdatedBy = x.UpdatedBy,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync(cancellationToken);

        // Step 10: Count of active records from PropertyMapDetail
        var mapCount = await _propertyMapDetailRepository
            .GetQueryable()
            .AsNoTracking()
            .CountAsync(x => x.PropertyIdNew == propertyId && x.IsActive, cancellationToken);

        // Step 11: Return response
        return new PropertyKycDetailsCommonDto
        {
            PropertyId = property.Id,
            PropertyNo = property.PropertyNo,
            PartitionNo = property.PartitionNo,
            PropertyTypeId = property.PropertyTypeId,
            CategoryId = property.CategoryId,
            PlotNo = property.PlotNo,
            CSN = property.CSN,
            OwnerTypeId = assessment?.OwnerTypeId,
            AdharCardNo = assessment?.AdharCardNo,
            BlockNo = assessment?.BlockNo,
            SurveyRemark = assessment?.SurveyRemark,
            TypeOfUseId = typeOfUseId,
            OwnerType = ownerType,
            OldCSN = oldProperty?.OldCSN,
            OwnerTitle = property.OwnerTitle,
            OwnerName = property.OwnerName,
            OwnerTitleEnglish = property.OwnerTitleEnglish,
            OwnerNameEnglish = property.OwnerNameEnglish,
            OccupierTitle = property.OccupierTitle,
            OccupierName = property.OccupierName,
            OccupierTitleEnglish = property.OccupierTitleEnglish,
            OccupierNameEnglish = property.OccupierNameEnglish,
            Address = property.Address,
            Location = property.Location,
            AddressEnglish = property.AddressEnglish,
            LocationEnglish = property.LocationEnglish,
            FlatOrShopName = property.FlatOrShopName,
            FlatOrShopNameEnglish = property.FlatOrShopNameEnglish,
            FlatOrShopNo = property.FlatOrShopNo,
            FlatOrShopNoEnglish = property.FlatOrShopNoEnglish,
            MobileNo = property.MobileNo,
            MobileNoRemarkId = property.MobileNoRemarkId,
            AlternateMobileNo = property.AlternateMobileNo,
            OccupierMobileNo = property.OccupierMobileNo,
            OccupierMobileNoRemarkId = property.OccupierMobileNoRemarkId,
            EmailId = property.EmailId,
            PinCode = property.PinCode,
            SocietyName = society?.SocietyName,
            SocietyAddress = society?.SocietyAddress,
            SocietyNameEnglish = society?.SocietyNameEnglish,
            SocietyAddressEnglish = society?.SocietyAddressEnglish,
            SocietyEmailId = society?.SocietyEmailId,
            WingId = society?.WingId,
            WingNo = wingNo,
            WingName = society?.WingName,
            ManagerName = society?.ManagerName,
            ManagerNameEnglish = society?.ManagerNameEnglish,
            ManagerMobileNo = society?.ManagerMobileNo,
            ManagerMobileNoId = society?.ManagerMobileNoRemarkId,
            ManagerEmailId = society?.ManagerEmailId,
            SecretaryName = society?.SecretaryName,
            SecretaryNameEnglish = society?.SecretaryNameEnglish,
            SecretaryMobileNo = society?.SecretaryMobileNo,
            SecretaryMobileNoId = society?.SecretaryMobileNoRemarkId,
            SecretaryEmailId = society?.SecretaryEmailId,
            LandOwnerName = society?.LandOwnerName,
            LandOwnerNameEnglish = society?.LandOwnerNameEnglish,
            BuilderName = society?.BuilderName,
            BuilderNameEnglish = society?.BuilderNameEnglish,
            BuilderMobileNo = society?.BuilderMobileNo,
            BuilderMobileNoId = society?.BuilderMobileNoRemarkId,
            SocietyDetailId = society?.Id,
            PlotLength = roomWiseDetails?.LengthMtr != null
                ? (double?)roomWiseDetails.LengthMtr
                : null,
            PlotWidth = roomWiseDetails?.WidthMtr != null
                ? (double?)roomWiseDetails.WidthMtr
                : null,
            TotalArea = roomWiseDetails?.TotalAreaSqMtr != null
                ? (double?)roomWiseDetails.TotalAreaSqMtr
                : null,
            IssuedBy = issuedBy?.ToString(),
            OldWardNo = oldProperty?.OldWardNo,
            OldSocietyName = oldProperty?.OldSocietyName,
            VirtualPropertyTransferHistory = virtualPropertyTransferHistory,
            MapCount = mapCount,
            PropertyIdOld = oldPropertyId > 0 ? oldPropertyId : null,
        };
    }

    public Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetKycDetailsAsync(propertyId, cancellationToken);

    public async Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(
        int propertyId,
        UpdatePropertyKycDetailsDto dto,
        CancellationToken cancellationToken = default)
    {
        // A missing property is reported as null (→ 404).
        var property = await _repository.GetActivePropertyAsync(propertyId, cancellationToken);
        if (property == null) return null;

        // Enforce all Property aggregate write invariants before any state change.
        await _invariantPolicy.EnforceAsync(property, cancellationToken);

        // Single timestamp: consistent across all entity fields in this operation.
        var now = DateTime.Now;

        // Wrap in a transaction: PropertyMast and potentially a new assessment row both save
        // in this operation; both must succeed or roll back atomically (Critical #4 — aggregate invariants).
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            ApplyKycFields(property, dto, now);
            await UpsertAssessmentAsync(propertyId, dto, now, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return await _repository.GetKycDetailsAsync(propertyId, cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Copies every editable KYC field from the DTO onto the entity in a single place.
    /// Using one <paramref name="now"/> value ensures a consistent timestamp.
    /// </summary>
    private static void ApplyKycFields(
        PropertyEntity property,
        UpdatePropertyKycDetailsDto dto,
        DateTime now)
    {
        property.OwnerTitle = dto.OwnerTitle;
        property.OwnerName = dto.OwnerName;
        property.OwnerTitleEnglish = dto.OwnerTitleEnglish;
        property.OwnerNameEnglish = dto.OwnerNameEnglish;
        property.OccupierTitle = dto.OccupierTitle;
        property.OccupierName = dto.OccupierName;
        property.OccupierTitleEnglish = dto.OccupierTitleEnglish;
        property.OccupierNameEnglish = dto.OccupierNameEnglish;
        property.Address = dto.Address;
        property.Location = dto.Location;
        property.AddressEnglish = dto.AddressEnglish;
        property.LocationEnglish = dto.LocationEnglish;
        property.FlatOrShopName = dto.FlatOrShopName;
        property.FlatOrShopNameEnglish = dto.FlatOrShopNameEnglish;
        property.FlatOrShopNo = dto.FlatOrShopNo;
        property.FlatOrShopNoEnglish = dto.FlatOrShopNoEnglish;
        property.MobileNo = dto.MobileNo;
        property.AlternateMobileNo = dto.AlternateMobileNo;
        property.EmailId = dto.EmailId;
        property.PinCode = dto.PinCode;
        property.UpdatedDate = now;
    }

    /// <summary>Updates the assessment row's owner-type/aadhar in place, or inserts one only when that data is supplied.</summary>
    private async Task UpsertAssessmentAsync(
        int propertyId,
        UpdatePropertyKycDetailsDto dto,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var assessmentId = await _repository.GetFirstAssessmentIdAsync(propertyId, cancellationToken);
        bool hasAssessmentData = dto.OwnerTypeId.HasValue || dto.AdharCardNo != null;

        if (assessmentId > 0)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId, cancellationToken);
            if (assessment != null)
            {
                assessment.OwnerTypeId = dto.OwnerTypeId;
                assessment.AdharCardNo = dto.AdharCardNo;
                assessment.UpdatedDate = now;
            }
        }
        else if (hasAssessmentData)
        {
            await _repository.AddAssessmentAsync(new PropertyAssessmentEntity
            {
                PropertyId = propertyId,
                OwnerTypeId = dto.OwnerTypeId,
                AdharCardNo = dto.AdharCardNo,
                IsActive = true,
                MarkedForDeletion = false,
                CreatedDate = now
            }, cancellationToken);
        }
    }
}
