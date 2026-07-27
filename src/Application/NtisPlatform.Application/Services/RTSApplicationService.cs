using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSApplicationService : BaseCommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSFieldValueDto, RTSApplicationQueryParameters, int>, IRTSApplicationService
{
    private readonly IRTSCitizenSessionService _sessionService;

    public RTSApplicationService(
        IRepository<RTSApplicationDetailsEntity, int> repository,
        IRTSCitizenSessionService sessionService,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(repository, unitOfWork, mapper)
    {
        _sessionService = sessionService;
    }

    public override async Task<RTSApplicationDetailsDto> CreateAsync(CreateRTSApplicationDetailsDto createDto, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(createDto.SessionId))
        {
            var validationResult = await _sessionService.ValidateAndUpdateSessionAsync(createDto.SessionId, cancellationToken);
            if (!validationResult.Success)
            {
                throw new UnauthorizedAccessException($"CitizenSession_{validationResult.Message}");
            }
        }

        var entity = _mapper.Map<RTSApplicationDetailsEntity>(createDto);
        entity.ApplicationStatus = string.IsNullOrWhiteSpace(createDto.ApplicationStatus) || createDto.ApplicationStatus == "string" ? "Submitted" : createDto.ApplicationStatus;

        if (createDto.FieldValues?.Any() == true)
        {
            entity.FieldValueData = createDto.FieldValues
                .Select(f =>
                {
                    var field = _mapper.Map<RTSFieldValueEntity>(f);
                    field.CreatedBy = createDto.CreatedBy;
                    return field;
                })
                .ToList();
        }

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<RTSApplicationDetailsDto>(entity);
    }


    //}
    //public override async Task<RTSApplicationDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    //{
    //    var query = _repository.GetQueryable().Where(x => !x.MarkedForDeletion && x.Id == id).AsQueryable();


    //    var totalCount = await query.CountAsync(cancellationToken);

    //    //var pageNumber = queryParameters.PageNumber < 1 ? 1 : queryParameters.PageNumber;
    //    //var pageSize = queryParameters.PageSize < 1 ? 10 : queryParameters.PageSize;

    //    // 2. Include child table (FieldValueData) & fetch entities
    //    var entities = await query
    //        .Include(x => x.FieldValueData)
    //        //.Skip((pageNumber - 1) * pageSize)
    //        //.Take(pageSize)
    //        .ToListAsync(cancellationToken);

    //    // 3. Map entities to DTOs
    //    var items = _mapper.Map<List<RTSApplicationDetailsDto>>(entities);

    //    return new PagedResult<RTSApplicationDetailsDto>(items, totalCount);

    //}




    public async Task<PagedResult<RTSApplicationDashboardResponseDto>> GetAllDashboardApplicationAsync(
    RTSApplicationQueryParameters queryParameters,
    CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable().Where(x => !x.MarkedForDeletion).AsQueryable();

        if (queryParameters.DepartmentId > 0)
            query = query.Where(x => x.DepartmentId == queryParameters.DepartmentId);
        if (queryParameters.ServiceId > 0)
            query = query.Where(x => x.ServiceId == queryParameters.ServiceId);
        if (!string.IsNullOrWhiteSpace(queryParameters.ApplicationNo))
            query = query.Where(x => x.ApplicationNo == queryParameters.ApplicationNo);
        if (!string.IsNullOrWhiteSpace(queryParameters.ApplicationStatus))
            query = query.Where(x => x.ApplicationStatus == queryParameters.ApplicationStatus);

        var totalCount = await query.CountAsync(cancellationToken);

        var dashboard = await query
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalApplications = g.Count(),
                Pending = g.Count(x => x.ApplicationStatus == "Pending"),
                Approved = g.Count(x => x.ApplicationStatus == "Approved"),
                Rejected = g.Count(x => x.ApplicationStatus == "Rejected"),
                Reverted = g.Count(x => x.ApplicationStatus == "Reverted"),
                TodayApplications = g.Count(x => x.CreatedDate == DateTime.Today),
                //InProgress = g.Count(x => x.ApplicationStatus == "Pending"),
                OverdueApplications = g.Count(x =>
                 x.ApplicationStatus != "Approved" &&
                 x.ApplicationStatus != "Rejected" &&
                 x.Service.Sla != null &&
                 x.Service.Sla.Contains(" ") &&
                 x.CreatedDate.HasValue &&
                 x.CreatedDate.Value.AddDays(Convert.ToInt32(x.Service.Sla.Substring(0, x.Service.Sla.IndexOf(" ")))) < DateTime.Today),
                 DueToday = g.Count(x =>
                 x.ApplicationStatus != "Approved" &&
                 x.ApplicationStatus != "Rejected" &&
                 x.Service.Sla != null &&
                 x.Service.Sla.Contains(" ") &&
                 x.CreatedDate.HasValue &&
                 x.CreatedDate.Value.AddDays(Convert.ToInt32(x.Service.Sla.Substring(0, x.Service.Sla.IndexOf(" ")))).Date == DateTime.Today)

            }).SingleOrDefaultAsync(cancellationToken);

       
        var pageNumber = queryParameters.PageNumber < 1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize < 1 ? 10 : queryParameters.PageSize;

        var items = await query
            .OrderByDescending(x => x.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RTSApplicationDashboardDetailsDto
            {
                Id = x.Id,
                DepartmentId = x.DepartmentId,
                ServiceId = x.ServiceId,
                ApplicationNo = x.ApplicationNo,
                ApplicationStatus = x.ApplicationStatus,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate,
                SessionId = x.SessionId, 
                OwnerId = x.OwnerId,
                DepartmentName = x.Department.DepartmentName,
                ServiceName = x.Service.ServiceName,
                Sla = x.Service.Sla,

                ApplicantDetails = x.FieldValueData
                .Where(fv => fv.FieldDefinition != null)
                .Where(fv => fv.FieldDefinition!.FieldGroup ==
                    x.FieldValueData
                        .Where(f => f.FieldDefinition != null)
                        .Select(f => f.FieldDefinition!.FieldGroup)
                        .FirstOrDefault())
                .Select(fv => new ApplicantFieldDto
                {
                    FieldLabel = fv.FieldDefinition!.FieldLabel,
                    FieldValue = fv.TextValue
                })
                .ToList()
            }).ToListAsync(cancellationToken);

                

       var result = new RTSApplicationDashboardResponseDto
       {
           Dashboard = new RTSApplicationDashboardCountsDto
           {
               TotalApplications = dashboard?.TotalApplications ?? 0,
               Pending = dashboard?.Pending ?? 0,
               Approved = dashboard?.Approved ?? 0,
               Rejected = dashboard?.Rejected ?? 0,
               Reverted = dashboard?.Reverted ?? 0,
               TodayApplications = dashboard?.TodayApplications ?? 0,
               //InProgress = dashboard?.InProgress ?? 0,
               OverdueApplications = dashboard?.OverdueApplications ?? 0,
               DueToday = dashboard?.DueToday ?? 0,
           },
           Applications = items

       };

        return new PagedResult<RTSApplicationDashboardResponseDto>(
            new List<RTSApplicationDashboardResponseDto> { result },
            totalCount,
            pageNumber,
            pageSize);
    }
}
