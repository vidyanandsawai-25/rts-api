using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Global Property Service - Used across all features
/// Provides property search, lookup, and master data functionality
/// </summary>
public class PropertyService
    : BaseCommonCrudService<PropertyEntity, PropertyDto, CreatePropertyDto, UpdatePropertyDto, PropertyQueryParameters, int>,
      IPropertyService
{
    private readonly IPropertyRepository _propertyRepository;

    public PropertyService(
        IRepository<PropertyEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPropertyRepository propertyRepository)
        : base(repository, unitOfWork, mapper)
    {
        _propertyRepository = propertyRepository;
    }

    public async Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetBasicDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.UpdateBasicDetailsAsync(propertyId, dto, cancellationToken);
    }

    public async Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetSocietyDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(int propertyId, UpdatePropertySocietyDetailsDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.UpdateSocietyDetailsAsync(propertyId, dto, cancellationToken);
    }

    public async Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetKycDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(int propertyId, UpdatePropertyKycDetailsDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.UpdateKycDetailsAsync(propertyId, dto, cancellationToken);
    }

    public async Task<PropertyOldDetailsDto?> UpdateOldDetailsAsync(int propertyId, UpdatePropertyOldDetailsDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.UpdateOldDetailsAsync(propertyId, dto, cancellationToken);
    }

    public async Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetOldDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetTaxDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetTaxDetailsCVAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetOldTaxesDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyOldTaxesDetailsDto?> UpdateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.UpdateOldTaxesDetailsAsync(propertyId, dto, cancellationToken);
    }

    public async Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetFloorDetailsOldAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyDetailsOldDto?> GetFloorDetailsOldByIdAsync(int propertyId, int floorId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetFloorDetailsOldByIdAsync(propertyId, floorId, cancellationToken);
    }

    public async Task<PropertyDetailsOldDto?> AddFloorDetailsOldAsync(int propertyId, AddPropertyDetailsOldDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.AddFloorDetailsOldAsync(propertyId, dto, cancellationToken);
    }

    public async Task<PropertyDetailsOldDto?> UpdateFloorDetailsOldAsync(int propertyId, int floorId, UpdatePropertyDetailsOldDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.UpdateFloorDetailsOldAsync(propertyId, floorId, dto, cancellationToken);
    }

    public async Task<bool> DeleteFloorDetailsOldAsync(int propertyId, int floorId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.DeleteFloorDetailsOldAsync(propertyId, floorId, cancellationToken);
    }

    public async Task<PropertyTaxApartmentDetailsDto?> GetApartmentPropertyTaxDetailsAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetApartmentPropertyTaxDetailsAsync(dto, cancellationToken);
    }

    public async Task<PropertyTaxApartmentDetailsCVDto?> GetApartmentPropertyTaxDetailsCVAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetApartmentPropertyTaxDetailsCVAsync(dto, cancellationToken);
    }

    public async Task<List<BuildingGenerateStructureDto>?> GetGenerateBuildingStructureAsync(BuildingGenerateDetailsDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetGenerateBuildingStructureAsync(dto, cancellationToken);
    }
	
	public async Task<RangeResult<CreateNewPropertyResponseDto>> CreatePropertiesFromRangeAsync(RangeCreateRequest<CreateNewPropertyDto> request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Template == null)
            return new RangeResult<CreateNewPropertyResponseDto>(SuccessCount: 0, FailedCount: 0, Results: [], Errors: new List<string> { "Template cannot be null." });

        var rangeValues = RangeGenerator.GenerateRangeValues(request.RangeFrom, request.RangeTo, request.Prefix, request.Suffix);
        var results = new List<CreateNewPropertyResponseDto>();
        var errors = new List<string>();
        var sequenceNo = request.StartSequenceNo;
        var processedCount = 0;

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            for (int i = 0; i < rangeValues.Count; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    errors.Add($"Operation cancelled at Row {i + 1}.");
                    break;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(rangeValues[i]))
                    {
                        errors.Add($"Row {i + 1}: Generated property name is empty or null.");
                        break;
                    } 

                    request.Template?.PropertyNo = $"{rangeValues[i]}";
                    request.Template?.PropertySeqNo = Convert.ToInt32(rangeValues[i]);

                    if (request.Template == null)
                    {
                        errors.Add($"Row {i + 1}: Template is null.");
                        break;
                    }

                    var res = await _propertyRepository.CreateNewPropertyAsync(request.Template, ct);
                    processedCount++;

                    if (res != null && res.Success)
                    {
                        results.Add(res);
                    }
                    else if (res != null && !res.Success)
                    {
                        for (int existCount = i; existCount < rangeValues.Count; existCount++)
                        {
                            if (await _propertyRepository.IsPropertyExists(request.Template.WardId, rangeValues[existCount], null))
                            {
                                errors.Add($"{rangeValues[existCount]} : {res.Message ?? "Unknown error"}");
                            }
                            else 
                            {
                                errors.Add($"Row {existCount + 1} : {res.Message ?? "Unknown error"}");
                                break;
                            }
                        }
                        break;
                    }
                    else
                    {
                        errors.Add($"Row {i + 1} ({rangeValues[i]}): Repository returned null response.");
                        break;
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    errors.Add($"Row {i + 1} ({rangeValues[i]}): Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    break;
                }
                catch (OperationCanceledException ocEx)
                {
                    errors.Add($"Row {i + 1} ({rangeValues[i]}): Operation cancelled: {ocEx.Message}");
                    break;
                }
                catch (ArgumentException argEx)
                {
                    errors.Add($"Row {i + 1} ({rangeValues[i]}): Invalid argument: {argEx.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {i + 1} ({rangeValues[i]}): {ex.GetType().Name}: {ex.Message}");
                    break;
                }
            }

            if (errors.Count > 0)
            {
                try
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                }
                catch (Exception rollbackEx)
                {
                    errors.Add($"Rollback error: {rollbackEx.Message}");
                }

                return new RangeResult<CreateNewPropertyResponseDto>(
                    SuccessCount: 0,
                    FailedCount: rangeValues.Count,
                    Results: [],
                    Errors: errors
                );
            }
            await _unitOfWork.CommitTransactionAsync(ct);

            return new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: results.Count,
                FailedCount: 0,
                Results: results,
                Errors: null
            );
        }
        //Unexpected exception during loop - rollback
        catch (Exception ex)
        {
            try
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
            }
            catch (Exception rollbackEx)
            {
                errors.Add($"Rollback error: {rollbackEx.Message}");
            }

            errors.Add($"Unexpected transaction error: {ex.GetType().Name}: {ex.Message}");

            return new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 0,
                FailedCount: rangeValues.Count,
                Results: [],
                Errors: errors
            );
        }
    }
 public async Task<BulkResult<CreateBulkPropertyResponseDto>?> BulkCreateAsync(CreateBulkPropertyDto[] items, CancellationToken ct)
    {
        if (items.Length == 0)
        {
            return new BulkResult<CreateBulkPropertyResponseDto>(0, 0, []);
        }

        var results = new List<CreateBulkPropertyResponseDto>();
        var errors = new List<string>();

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];

                if (string.IsNullOrWhiteSpace(item.PropertyNo))
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return new BulkResult<CreateBulkPropertyResponseDto>(
                        0,
                        items.Length,
                        [],
                        [$"{i}: PropertyNo is required."]
                    );
                }
                    var res = await _propertyRepository.CreateBulkPropertyAsync(item, ct);
                    if (res == null || !res.Success)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return new BulkResult<CreateBulkPropertyResponseDto>(
                            0,
                            items.Length,
                            [],
                            [$"{i}: {res?.Message ?? "Unknown error"}"]
                        );
                    }

                    results.Add(res);
                }
            

            await _unitOfWork.CommitTransactionAsync(ct);

            return new BulkResult<CreateBulkPropertyResponseDto>(
                results.Count,
                0,
                results,
                null
            );
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return new BulkResult<CreateBulkPropertyResponseDto>(
                0,
                items.Length,
                [],
                [$"Transaction failed: {ex.Message}"]
            );
        }
    }
}