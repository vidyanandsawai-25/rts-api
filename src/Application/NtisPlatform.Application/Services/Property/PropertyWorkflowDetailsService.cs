using AutoMapper;
using NtisPlatform.Application.DTOs.Property.PropertyWorkflowDetails;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Property;

public class PropertyWorkflowDetailsService
    : BaseCommonCrudService<PropertyWorkflowDetailsEntity, PropertyWorkflowDetailsDto, CreatePropertyWorkflowDetailsDto, UpdatePropertyWorkflowDetailsDto, PropertyWorkflowDetailsQueryParameters, int>,
      IPropertyWorkflowDetailsService
{
    private readonly IPropertyWorkflowDetailsRepository _workflowDetailsRepository;

    public PropertyWorkflowDetailsService(
        IRepository<PropertyWorkflowDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPropertyWorkflowDetailsRepository workflowDetailsRepository)
        : base(repository, unitOfWork, mapper)
    {
        _workflowDetailsRepository = workflowDetailsRepository;
    }

    /// <summary>
    /// Creates a new workflow detail record, setting CurrentStatus=true on the new row
    /// and CurrentStatus=false on all previous rows for the same PropertyId.
    /// </summary>
    public override async Task<PropertyWorkflowDetailsDto> CreateAsync(CreatePropertyWorkflowDetailsDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<PropertyWorkflowDetailsEntity>(createDto);
        entity.CurrentStatus = true;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _workflowDetailsRepository.ResetCurrentStatusAsync(entity.PropertyId, cancellationToken);

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return _mapper.Map<PropertyWorkflowDetailsDto>(entity);
    }

    public async Task<List<PropertyWorkflowDetailsDto>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var entities = await _workflowDetailsRepository.GetByPropertyIdAsync(propertyId, cancellationToken);
        return _mapper.Map<List<PropertyWorkflowDetailsDto>>(entities);
    }

    public async Task<PropertyWorkflowDetailsDto?> GetCurrentByPropertyNoAsync(string propertyNo, CancellationToken cancellationToken = default)
    {
        var entity = await _workflowDetailsRepository.GetCurrentByPropertyNoAsync(propertyNo, cancellationToken);
        return entity is null ? null : _mapper.Map<PropertyWorkflowDetailsDto>(entity);
    }
}
