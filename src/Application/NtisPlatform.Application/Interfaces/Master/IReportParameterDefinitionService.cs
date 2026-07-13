using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IReportParameterDefinitionService
    : ICommonCrudService<ReportParameterDefinitionEntity, ReportParameterDefinitionDto,
        CreateReportParameterDefinitionDto, UpdateReportParameterDefinitionDto,
        ReportParameterDefinitionQueryParameters, int>
{
}
