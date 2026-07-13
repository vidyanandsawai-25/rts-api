using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class ReportDefinitionService
    : BaseCommonCrudService<ReportDefinitionEntity, ReportDefinitionDto,
        CreateReportDefinitionDto, UpdateReportDefinitionDto,
        ReportDefinitionQueryParameters, int>,
      IReportDefinitionService
{
    public ReportDefinitionService(
        IRepository<ReportDefinitionEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(
        ReportDefinitionEntity entity,
        CancellationToken ct = default)
    {
        var duplicate = await _repository.GetQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.ReportCode == entity.ReportCode, ct);

        return duplicate
            ? ValidationResult.Failure(nameof(entity.ReportCode), "Report_ReportCode_Duplicate")
            : ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        ReportDefinitionEntity currentEntity,
        ReportDefinitionEntity updatedEntity,
        CancellationToken ct = default)
    {
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        ReportDefinitionEntity entity,
        CancellationToken ct = default)
    {
        return ValidationResult.Success();
    }
}
