using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Extensions;


namespace NtisPlatform.Application.Services;




public class TypeOfUseGroupService : BaseCommonCrudService<TypeOfUseGroupEntity, TypeOfUseGroupDto, CreateTypeOfUseGroupDto, UpdateTypeOfUseGroupDto, TypeOfUseGroupQueryParameters, int>, ITypeOfUseGroupService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepository;

    public TypeOfUseGroupService(
       IRepository<TypeOfUseGroupEntity, int> repository,
       IUnitOfWork unitOfWork,
       IMapper mapper,
       IReferenceValidationService referenceValidator,
       IRepository<TypeOfUseEntity, int> typeOfUseRepository) // <-- Added this parameter
       : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _typeOfUseRepository = typeOfUseRepository;            // <-- Added this assignment
    }



    public override async Task<PagedResult<TypeOfUseGroupDto>> GetAllAsync(
        TypeOfUseGroupQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        // 1. Get standard groups list
        var result = await base.GetAllAsync(queryParameters, cancellationToken);

        // 2. Fetch total count of all types in ptis.TypeOfUseMaster
        var totalTypesCount = await _typeOfUseRepository.GetQueryable()
            .CountAsync(cancellationToken);

        // 3. Create the TOTAL row
        var totalRow = new TypeOfUseGroupDto 
        {
            Id = 0,
            GroupName = "all",
            TypeOfUseGroupCode = "TOTAL",
            CountOfTypes = totalTypesCount,
            GroupIcon = null,
            IsActive = true
        };

        // 4. Append TOTAL row to the list
        var items = new List<TypeOfUseGroupDto>(result.Items);
        items.Add(totalRow);

        result.Items = items;

        return result;
    }




    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        TypeOfUseGroupEntity currentEntity,
        TypeOfUseGroupEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<TypeOfUseGroupEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }




    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        TypeOfUseGroupEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<TypeOfUseGroupEntity>(id, cancellationToken);
    }
}

