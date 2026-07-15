using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Modules are created/edited/deleted exclusively through the separate report-admin tool;
/// ReportModulesController only exposes GetAll/GetById from the full CRUD surface below.
/// </summary>
public interface IReportModuleService
    : ICommonCrudService<ReportModuleEntity, ReportModuleDto,
        CreateReportModuleDto, UpdateReportModuleDto,
        ReportModuleQueryParameters, int>
{
}
