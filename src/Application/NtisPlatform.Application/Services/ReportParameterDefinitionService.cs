using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class ReportParameterDefinitionService
    : BaseCommonCrudService<ReportParameterDefinitionEntity, ReportParameterDefinitionDto,
        CreateReportParameterDefinitionDto, UpdateReportParameterDefinitionDto,
        ReportParameterDefinitionQueryParameters, int>,
      IReportParameterDefinitionService
{
    public ReportParameterDefinitionService(
        IRepository<ReportParameterDefinitionEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        ReportParameterDefinitionEntity entity,
        CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.ReportDefinitionId == entity.ReportDefinitionId
                        && x.ParameterKey == entity.ParameterKey, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.ParameterKey), "ReportParam_ParameterKey_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        ReportParameterDefinitionEntity currentEntity,
        ReportParameterDefinitionEntity updatedEntity,
        CancellationToken ct = default)
    {
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        ReportParameterDefinitionEntity entity,
        CancellationToken ct = default)
    {
        return ValidationResult.Success();
    }
}
