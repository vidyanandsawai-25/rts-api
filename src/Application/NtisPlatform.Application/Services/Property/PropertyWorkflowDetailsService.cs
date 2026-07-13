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
    private readonly IUserRepository _userRepository;

    public PropertyWorkflowDetailsService(
        IRepository<PropertyWorkflowDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPropertyWorkflowDetailsRepository workflowDetailsRepository,
        IUserRepository userRepository)
        : base(repository, unitOfWork, mapper)
    {
        _workflowDetailsRepository = workflowDetailsRepository;
        _userRepository = userRepository;
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

        var dto = _mapper.Map<PropertyWorkflowDetailsDto>(entity);
        await ResolveCreatedByNameAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<List<PropertyWorkflowDetailsDto>> GetByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var entities = await _workflowDetailsRepository.GetByPropertyIdAsync(propertyId, cancellationToken);
        var dtos = _mapper.Map<List<PropertyWorkflowDetailsDto>>(entities);
        foreach (var dto in dtos)
            await ResolveCreatedByNameAsync(dto, cancellationToken);
        return dtos;
    }

    public async Task<PropertyWorkflowDetailsDto?> GetCurrentByPropertyNoAsync(string propertyid, CancellationToken cancellationToken = default)
    {
        var entity = await _workflowDetailsRepository.GetCurrentByPropertyNoAsync(propertyid, cancellationToken);
        if (entity is null) return null;

        var dto = _mapper.Map<PropertyWorkflowDetailsDto>(entity);
        await ResolveCreatedByNameAsync(dto, cancellationToken);
        return dto;
    }

    /// <summary>
    /// Looks up the user by <see cref="PropertyWorkflowDetailsDto.CreatedBy"/> ID
    /// and sets <see cref="PropertyWorkflowDetailsDto.CreatedByName"/> to
    /// "FirstName MiddleName LastName" (null parts are skipped).
    /// </summary>
    private async Task ResolveCreatedByNameAsync(PropertyWorkflowDetailsDto dto, CancellationToken cancellationToken)
    {
        if (dto.CreatedBy is null) return;

        var user = await _userRepository.GetByIdAsync(dto.CreatedBy.Value, cancellationToken);
        if (user is null) return;

        dto.CreatedByName = string.Join(" ",
            new[] { user.FirstName, user.MiddleName, user.LastName }
            .Where(n => !string.IsNullOrWhiteSpace(n)));
    }
}
