using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IReportDefinitionService
    : ICommonCrudService<ReportDefinitionEntity, ReportDefinitionDto,
        CreateReportDefinitionDto, UpdateReportDefinitionDto,
        ReportDefinitionQueryParameters, int>
{
}
