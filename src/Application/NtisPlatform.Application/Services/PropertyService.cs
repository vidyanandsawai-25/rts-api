using AutoMapper;
using NtisPlatform.Application.DTOs.Property;
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

    public async Task<PropertyDetailsOldListDto?> UpdateFloorDetailsOldAsync(int propertyId, UpdatePropertyDetailsOldListDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.UpdateFloorDetailsOldAsync(propertyId, dto, cancellationToken);
    }
}




