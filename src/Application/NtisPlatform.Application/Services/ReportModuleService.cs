using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Report;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class ReportModuleService
    : BaseCommonCrudService<ReportModuleEntity, ReportModuleDto,
        CreateReportModuleDto, UpdateReportModuleDto,
        ReportModuleQueryParameters, int>,
      IReportModuleService
{
    public ReportModuleService(
        IRepository<ReportModuleEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }

    // LogoBase64 (Convert.ToBase64String) can't be translated to SQL by the EF Core provider, so
    // returning a query the base class doesn't consider reference-equal forces GetAllAsync's
    // in-memory _mapper.Map fallback instead of ProjectTo — same trick WaterConnectionSizeService
    // uses for its own non-SQL-translatable projection.
    protected override IQueryable<ReportModuleEntity> ApplyIncludes(IQueryable<ReportModuleEntity> query)
        => query.AsNoTracking();
}
