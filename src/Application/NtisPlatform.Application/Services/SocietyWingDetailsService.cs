using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Application.Extensions;

namespace NtisPlatform.Application.Services;

public class SocietyWingDetailsService : BaseCommonCrudService<SocietyWingDetailsEntity, SocietyWingDetailsDto, CreateSocietyWingDetailsDto, UpdateSocietyWingDetailsDto, SocietyWingDetailsQueryParameters, int>, ISocietyWingDetailsService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<SocietyDetailsEntity, int> _societydetails;
    private readonly IRepository<PropertyEntity, int> _propertymast;
    private readonly IRepository<WingEntity, int> _wingrepository;

    public SocietyWingDetailsService(
        IRepository<SocietyWingDetailsEntity, int> repository,
        IRepository<PropertyEntity, int> propertymast,
        IRepository<WingEntity, int> wingrepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator,
        IRepository<SocietyDetailsEntity, int> societydetails,
        IRepository<WingDetailsMastEntity, int>? wingDetailsMastRepo = null)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _societydetails = societydetails;
        _propertymast = propertymast;
        _wingrepository = wingrepository;
        _wingDetailsMastRepo = wingDetailsMastRepo;
    }

    private readonly IRepository<WingDetailsMastEntity, int>? _wingDetailsMastRepo;


    /// <summary>
    /// Creates a new society wing detail record.
    /// If PropertyId is provided but SocietyDetailId is not, it automatically resolves and saves the associated society details first.
    /// </summary>
    /// <param name="createDto">The data transfer object containing the society wing details.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A DTO containing the newly created society wing details.</returns>
    public override async Task<SocietyWingDetailsDto> CreateAsync(CreateSocietyWingDetailsDto createDto, CancellationToken cancellationToken = default)
    {
        if (createDto == null)
        {
            throw new ValidationException("Request data cannot be null.", OperationType.Create);
        }

        var wingQuery = _wingrepository.GetQueryable().Where(x => x.IsActive && x.Id == createDto.WingId.Value);
        var isWingIdValid = createDto.WingId.HasValue && (wingQuery is IAsyncEnumerable<WingEntity>
            ? await wingQuery.AnyAsync(cancellationToken)
            : wingQuery.Any());

        if (!isWingIdValid)
        {
            throw new ValidationException($"WingId {createDto.WingId} does not exist or is inactive.", OperationType.Create);
        }

        var propQuery = _propertymast.GetQueryable().Where(x => x.Id == createDto.PropertyId.Value && x.IsActive)
                                             .Select(x => new
                                             {
                                                 x.Id
                                             });
        var mainproperty = createDto.PropertyId.HasValue
            ? (propQuery is IAsyncEnumerable<object>
                ? await propQuery.FirstOrDefaultAsync(cancellationToken)
                : propQuery.FirstOrDefault())
            : null;

        if (mainproperty == null)
        {
            throw new ValidationException($"PropertyId {createDto.PropertyId} does not exist or is inactive.", OperationType.Create);
        }

        if (!string.IsNullOrEmpty(createDto.FromFloor) && !string.IsNullOrEmpty(createDto.ToFloor))
        {
            if (int.TryParse(createDto.FromFloor, out int fromFloor) && int.TryParse(createDto.ToFloor, out int toFloor))
            {
                if (fromFloor > toFloor)
                {
                    throw new ValidationException("FromFloor cannot be greater than ToFloor.", OperationType.Create);
                }
            }
            else
            {
                throw new ValidationException("FromFloor and ToFloor must be valid numeric values.", OperationType.Create);
            }
        }

        // duplicate check
        bool isSocietydetailsExists = false;
        if (_wingDetailsMastRepo != null)
        {
            var q = (
                from sd in _societydetails.GetQueryable()
                join wdm in _wingDetailsMastRepo.GetQueryable() on sd.Id equals wdm.SocietyDetailsMastId
                where sd.PropertyId == createDto.PropertyId && wdm.WingMasterId == createDto.WingId && wdm.WingName == createDto.NewWingName && sd.IsActive && wdm.IsActive
                select wdm
            );
            isSocietydetailsExists = q is IAsyncEnumerable<WingDetailsMastEntity>
                ? await q.AnyAsync(cancellationToken)
                : q.Any();
        }

        if (!isSocietydetailsExists && _repository != null)
        {
            var q1 = _repository.GetQueryable().Where(x => x.IsActive && x.PropertyId == createDto.PropertyId && x.WingId == createDto.WingId);
            bool hasRep = q1 is IAsyncEnumerable<SocietyWingDetailsEntity> ? await q1.AnyAsync(cancellationToken) : q1.Any();

            var q2 = _societydetails.GetQueryable().Where(sd => sd.IsActive && sd.PropertyId == createDto.PropertyId);
            bool hasSoc = q2 is IAsyncEnumerable<SocietyDetailsEntity> ? await q2.AnyAsync(cancellationToken) : q2.Any();

            isSocietydetailsExists = hasRep || hasSoc;
        }

        if (isSocietydetailsExists)
        {
            throw new ValidationException("Wing Details already exists.", OperationType.Create);
        }


        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = _mapper.Map<SocietyWingDetailsEntity>(createDto);

            if (!createDto.SocietyDetailId.HasValue && createDto.PropertyId.HasValue)
            {
                var societyDetails = _mapper.Map<SocietyDetailsEntity>(createDto);

                await _societydetails.AddAsync(societyDetails, cancellationToken);

                // Ensure SaveChanges is called here OR rely on the UnitOfWork tracking 
                entity.SocietyDetailsMast = societyDetails;
            }

            if (_repository != null)
            {
                await _repository.AddAsync(entity, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return _mapper.Map<SocietyWingDetailsDto>(entity);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing society wing detail record.
    /// If PropertyId is provided but SocietyDetailId is not, it automatically resolves and saves the associated society details first.
    /// </summary>
    public override async Task<SocietyWingDetailsDto?> UpdateAsync(int id, UpdateSocietyWingDetailsDto updateDto, CancellationToken cancellationToken = default)
    {
        // Validate PropertyId if provided
        if (updateDto.PropertyId.HasValue)
        {
            var propertyExists = await _propertymast.GetQueryable()
                .AnyAsync(x => x.Id == updateDto.PropertyId && x.IsActive, cancellationToken);

            if (!propertyExists)
            {
                throw new ValidationException($"PropertyId {updateDto.PropertyId} does not exist or is inactive.", OperationType.Update);
            }
        }

        var isWingIdValid = await _wingrepository.GetQueryable()
              .AnyAsync(x => x.IsActive && x.Id == updateDto.WingId, cancellationToken);

        if (!isWingIdValid)
        {
            throw new ValidationException($"WingId {updateDto.WingId} does not exist or is inactive.", OperationType.Update);
        }

        // FromFloor to ToFloor validation
        if (!string.IsNullOrEmpty(updateDto.FromFloor) && !string.IsNullOrEmpty(updateDto.ToFloor))
        {
            bool isFromValid = int.TryParse(updateDto.FromFloor, out int fromFloor);
            bool isToValid = int.TryParse(updateDto.ToFloor, out int toFloor);

            if (!isFromValid || !isToValid)
            {
                throw new ValidationException("FromFloor and ToFloor must be valid numeric values.", OperationType.Update);
            }

            if (fromFloor > toFloor)
            {
                throw new ValidationException("FromFloor cannot be greater than ToFloor.", OperationType.Update);
            }
        }

        // duplicate check
       if (!updateDto.SocietyDetailId.HasValue)
         {
             throw new ValidationException("SocietyDetailId is required.", OperationType.Update);
         }

       bool isSocietydetailsExists = await _societydetails.GetQueryable()
             .AnyAsync(x => x.PropertyId == updateDto.PropertyId && x.Id == updateDto.SocietyDetailId.Value && x.IsActive, cancellationToken);

        if (!isSocietydetailsExists)
        {
            throw new ValidationException($"SocietyDetailsId {updateDto.SocietyDetailId} does not exist or is inactive.", OperationType.Update);
        }

        // --- Transactional work ---
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = await _repository.GetQueryable().FirstOrDefaultAsync(x => x.SocietyDetailId == id, cancellationToken);
            if (entity == null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return default;
            }

            _mapper.Map(updateDto, entity);

            // Update the SocietyDetails record using the same SocietyDetailId (id)
            var existingSocietyDetails = await _societydetails.GetByIdAsync(id, cancellationToken);
            if (existingSocietyDetails != null)
            {
                _mapper.Map(updateDto, existingSocietyDetails);
                await _societydetails.UpdateAsync(existingSocietyDetails, cancellationToken);
            }


            await _repository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return _mapper.Map<SocietyWingDetailsDto>(entity);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Validates deactivation (IsActive change from true to false) for SocietyWingDetailsEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        SocietyWingDetailsEntity currentEntity,
        SocietyWingDetailsEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<SocietyWingDetailsEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates delete operation for SocietyWingDetailsEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        SocietyWingDetailsEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<SocietyWingDetailsEntity>(id, cancellationToken);
    }

    /// <summary>
    /// Gets a society wing detail record by SocietyDetailId.
    /// Returns joined data from both SocietyDetails and SocietyWingDetails tables.
    /// </summary>
    public override async Task<SocietyWingDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var societyQuery = _societydetails.GetQueryable();
        var wingQuery = _repository.GetQueryable();
        var wingDetailsQuery = _wingDetailsMastRepo != null ? _wingDetailsMastRepo.GetQueryable().Where(w => w.IsActive && !w.MarkedForDeletion) : Enumerable.Empty<WingDetailsMastEntity>().AsQueryable();

        var result = await (from sd in societyQuery
                            join wdm in wingDetailsQuery on sd.Id equals wdm.SocietyDetailsMastId into wdmGroup
                            from wdm in wdmGroup.DefaultIfEmpty()
                            join sw in wingQuery on sd.Id equals sw.SocietyDetailId into swGroup
                            from sw in swGroup.DefaultIfEmpty()
                            where sd.Id == id
                            select new SocietyWingDetailsDto
                            {
                                Id = sw != null ? sw.Id : 0,
                                WingId = wdm != null ? (int?)wdm.WingMasterId : null,
                                PropertyId = sd.PropertyId,
                                SocietyDetailId = sd.Id,
                                FromFloor = sw != null ? sw.FromFloor : null,
                                ToFloor = sw != null ? sw.ToFloor : null,
                                OldWingName = sw != null ? sw.OldWingName : null,
                                NewWingName = sw != null ? sw.NewWingName : (wdm != null ? wdm.WingName : null),
                                NoOfFlat = sw != null ? sw.NoOfFlat : null,
                                NoOfShop = sw != null ? sw.NoOfShop : null,
                                NoOfRowHouse = sw != null ? sw.NoOfRowHouse : null,
                                WingPhoto = sw != null ? sw.WingPhoto : null,
                                BoardPhoto = sw != null ? sw.BoardPhoto : null,
                                IsActive = sw != null ? sw.IsActive : sd.IsActive,
                                CreatedBy = sw != null ? sw.CreatedBy : sd.CreatedBy,
                                CreatedDate = sw != null ? sw.CreatedDate : sd.CreatedDate,
                                UpdatedBy = sw != null ? sw.UpdatedBy : sd.UpdatedBy,
                                UpdatedDate = sw != null ? sw.UpdatedDate : sd.UpdatedDate
                            }).FirstOrDefaultAsync(cancellationToken);

        return result;
    }

    public override async Task<PagedResult<SocietyWingDetailsDto>> GetAllAsync(SocietyWingDetailsQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var societyQuery = _societydetails.GetQueryable();
        var wingQuery = _repository.GetQueryable();
        var wingDetailsQuery = _wingDetailsMastRepo != null ? _wingDetailsMastRepo.GetQueryable().Where(w => w.IsActive && !w.MarkedForDeletion) : Enumerable.Empty<WingDetailsMastEntity>().AsQueryable();

        var query = from sd in societyQuery
                    join sw in wingQuery on sd.Id equals sw.SocietyDetailId into swGroup
                    from sw in swGroup.DefaultIfEmpty()
                    join wdm in wingDetailsQuery on sd.Id equals wdm.SocietyDetailsMastId into wdmGroup
                    from wdm in wdmGroup.OrderByDescending(w => w.CreatedDate).Take(1).DefaultIfEmpty()
                    select new SocietyWingDetailsDto
                    {
                        Id = sw != null ? sw.Id : 0,
                        WingId = wdm != null ? (int?)wdm.WingMasterId : null,
                        PropertyId = sd.PropertyId,
                        SocietyDetailId = sd.Id,
                        FromFloor = sw != null ? sw.FromFloor : null,
                        ToFloor = sw != null ? sw.ToFloor : null,
                        OldWingName = sw != null ? sw.OldWingName : null,
                        NewWingName = sw != null ? sw.NewWingName : (wdm != null ? wdm.WingName : null),
                        NoOfFlat = sw != null ? sw.NoOfFlat : null,
                        NoOfShop = sw != null ? sw.NoOfShop : null,
                        NoOfRowHouse = sw != null ? sw.NoOfRowHouse : null,
                        WingPhoto = sw != null ? sw.WingPhoto : null,
                        BoardPhoto = sw != null ? sw.BoardPhoto : null,
                        IsActive = sw != null ? sw.IsActive : sd.IsActive,
                        CreatedBy = sw != null ? sw.CreatedBy : sd.CreatedBy,
                        CreatedDate = sw != null ? sw.CreatedDate : sd.CreatedDate,
                        UpdatedBy = sw != null ? sw.UpdatedBy : sd.UpdatedBy,
                        UpdatedDate = sw != null ? sw.UpdatedDate : sd.UpdatedDate
                    };

        // Default to active records if IsActive is not explicitly filtered
        if (!queryParameters.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == true);
        }

        // Apply generic logic per project structure
        query = query.ApplyFilters(queryParameters);
        query = query.ApplySearch(queryParameters);
        query = query.ApplySort(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<SocietyWingDetailsDto>(items, totalCount, pageNumber, pageSize);
    }


    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .FirstOrDefaultAsync(x => x.SocietyDetailId == id, cancellationToken);
        if (entity == null || !entity.IsActive)
            return false;

        var validationResult = await ValidateForDeleteAsync(entity.Id, entity, cancellationToken);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed for delete operation";
            throw new ValidationException(firstError, validationResult.ToDictionary(), OperationType.Delete);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Soft delete the SocietyWingDetails record
            entity.IsActive = false;
            await _repository.UpdateAsync(entity, cancellationToken);

            // Soft delete the SocietyDetails record
            var societyDetails = await _societydetails.GetByIdAsync(id, cancellationToken);
            if (societyDetails != null && societyDetails.IsActive)
            {
                societyDetails.IsActive = false;
                societyDetails.MarkedForDeletion = true;
                await _societydetails.UpdateAsync(societyDetails, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
