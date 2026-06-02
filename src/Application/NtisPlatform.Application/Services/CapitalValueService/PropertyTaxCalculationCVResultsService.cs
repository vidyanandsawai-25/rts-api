using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.ICapitalValueService;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.CapitalValueService;

public class PropertyTaxCalculationCVResultsService : BaseCommonCrudService<PropertyTaxCalculationCVResultsEntity, PropertyTaxCalculationCVResultsDto, CreatePropertyTaxCalculationCVResultsDto, UpdatePropertyTaxCalculationCVResultsDto, PropertyTaxCalculationCVResultsQueryParameters, long>, IPropertyTaxCalculationCVResultsService
{
    public PropertyTaxCalculationCVResultsService(
        IRepository<PropertyTaxCalculationCVResultsEntity, long> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    public async Task<List<PropertyTaxCalculationCVResultsDto>> GetByPropertyIdAsync(long propertyId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetQueryable()
            .Include(x => x.FloorFactorCVMaster)
            .Include(x => x.AgeFactorCVMaster)
            .Include(x => x.NatureFactorCVMaster)
            .Include(x => x.UseFactorCVMaster)
            .Include(x => x.TaxMaster)
            .Include(x => x.PropertyMast).ThenInclude(p => p.FlagMaster)
            .Where(x => x.PropertyId == propertyId && x.IsActive && x.MarkedForDeletion == false)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<PropertyTaxCalculationCVResultsDto>>(entities);

        // Map actual factor values from related entities
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var dto = dtos[i];

            // Use the property's FlagMaster.Lift flag to select the correct floor factor
            var hasLift = entity.PropertyMast?.FlagMaster?.FirstOrDefault(f => f.IsActive)?.Lift ?? false;
            dto.FloorFactor = entity.FloorFactorCVMaster != null
                ? (double?)(hasLift
                    ? entity.FloorFactorCVMaster.FactorWithLift 
                    : entity.FloorFactorCVMaster.FactorWithoutLift)
                : null;
            dto.AgeFactor = entity.AgeFactorCVMaster != null ? (double?)entity.AgeFactorCVMaster.Factor : null;
            dto.NTBFactor = entity.NatureFactorCVMaster != null ? (double?)entity.NatureFactorCVMaster.Factor : null;
            dto.UseFactor = entity.UseFactorCVMaster != null ? (double?)entity.UseFactorCVMaster.Factor : null;

            // ← ADDED: Map TaxName from TaxMaster
            dto.TaxName = entity.TaxMaster?.TaxName;
        }

        return dtos;
    }

    public async Task<List<PropertyTaxCalculationCVResultsDto>> GetByPropertyDetailsIdAsync(int propertyDetailsId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetQueryable()
            .Include(x => x.FloorFactorCVMaster)
            .Include(x => x.AgeFactorCVMaster)
            .Include(x => x.NatureFactorCVMaster)
            .Include(x => x.UseFactorCVMaster)
            .Include(x => x.TaxMaster)
            .Include(x => x.PropertyMast).ThenInclude(p => p.FlagMaster)
            .Where(x => x.PropertyDetailsId == propertyDetailsId && x.IsActive && x.MarkedForDeletion == false)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<PropertyTaxCalculationCVResultsDto>>(entities);

        // Map actual factor values from related entities
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var dto = dtos[i];

            // Use the property's FlagMaster.Lift flag to select the correct floor factor
            var hasLift = entity.PropertyMast?.FlagMaster?.FirstOrDefault(f => f.IsActive)?.Lift ?? false;
            dto.FloorFactor = entity.FloorFactorCVMaster != null
                ? (double?)(hasLift
                    ? entity.FloorFactorCVMaster.FactorWithLift 
                    : entity.FloorFactorCVMaster.FactorWithoutLift)
                : null;
            dto.AgeFactor = entity.AgeFactorCVMaster != null ? (double?)entity.AgeFactorCVMaster.Factor : null;
            dto.NTBFactor = entity.NatureFactorCVMaster != null ? (double?)entity.NatureFactorCVMaster.Factor : null;
            dto.UseFactor = entity.UseFactorCVMaster != null ? (double?)entity.UseFactorCVMaster.Factor : null;

            // ← ADDED: Map TaxName from TaxMaster
            dto.TaxName = entity.TaxMaster?.TaxName;
        }

        return dtos;
    }

    public async Task<bool> ExistsAsync(long propertyId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetQueryable()
            .AnyAsync(x => x.PropertyId == propertyId && x.IsActive && x.MarkedForDeletion == false, cancellationToken);
    }

    public async Task DeactivateByPropertyDetailsIdAsync(int propertyDetailsId, int? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetQueryable()
            .Where(x => x.PropertyDetailsId == propertyDetailsId && x.IsActive && x.MarkedForDeletion == false).ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            entity.IsActive = false;
            entity.MarkedForDeletion = true;
            entity.MarkedForDeletionDate = DateTime.Now;
            entity.UpdatedDate = DateTime.Now;
            entity.UpdatedBy = updatedBy;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetCVInputHashAsync(int propertyDetailsId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetQueryable() .Where(x => x.PropertyDetailsId == propertyDetailsId && x.IsActive && x.MarkedForDeletion == false)
            .Select(x => x.CVInputHash).FirstOrDefaultAsync(cancellationToken);
    }
}
