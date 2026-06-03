using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.ICapitalValueService;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.CapitalValueService;

public class TransMastService : BaseCommonCrudService<TransMastEntity, TransMastDto, CreateTransMastDto, UpdateTransMastDto, TransMastQueryParameters, int>, ITransMastService
{
    public TransMastService(
        IRepository<TransMastEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    public async Task<TransMastDto?> GetByPropertyFinanceYearAndTaxIdAsync(long propertyId, string RVorCV, int financeYearId, int taxId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .Where(x => x.PropertyId == propertyId && x.FinanceYearId == financeYearId && x.TaxId == taxId && x.IsActive && !x.MarkedForDeletion && x.RVorCV == RVorCV)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? null : _mapper.Map<TransMastDto>(entity);
    }

    public async Task<List<TransMastDto>> GetByPropertyIdAsync(int propertyId, string RVorCV, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetQueryable()
             .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion && x.RVorCV == RVorCV)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<TransMastDto>>(entities);
    }

    public async Task DeactivateByPropertyIdAsync(int propertyId,string RVorCV, int? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetQueryable()
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion && x.RVorCV == RVorCV)
            .ToListAsync(cancellationToken);

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
}
