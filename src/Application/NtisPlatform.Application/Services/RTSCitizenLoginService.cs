using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.CitizenLoginDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.PropertyTax;
using NtisPlatform.Core.Interfaces;
using System.Linq.Expressions;

namespace NtisPlatform.Application.Services;

public class RTSCitizenLoginService: BaseCommonCrudService<RTSPropertyMastEntity, PropertyDto, CreatePropertyDto, UpdatePropertyDto, PropertyQueryParameters, int>, IRTSCitizenLoginService
{
    public RTSCitizenLoginService(
        IRepository<RTSPropertyMastEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
    public override async Task<PagedResult<PropertyDto>> GetAllAsync(
    PropertyQueryParameters queryParams,
    CancellationToken cancellationToken = default)
    {
        
        Expression<Func<RTSPropertyMastEntity, bool>>? filter = null;

        if (!string.IsNullOrWhiteSpace(queryParams.MobileNo))
        {
            var mobile = queryParams.MobileNo.Trim();

            filter = p =>
                p.MobileNo != null &&
                p.MobileNo.Contains(mobile);
        }
        else if (!string.IsNullOrWhiteSpace(queryParams.UnicdeAddress))
        {
            var upic = queryParams.UnicdeAddress.Trim();

            filter = p =>
                p.UnicdeAddress != null &&
                p.UnicdeAddress.Contains(upic);
        }
        else if (!string.IsNullOrWhiteSpace(queryParams.NewZoneNo) &&
                 !string.IsNullOrWhiteSpace(queryParams.NewWardNo) &&
                 !string.IsNullOrWhiteSpace(queryParams.NewPropertyNo))
        {
            var zoneNo = queryParams.NewZoneNo.Trim();
            var wardNo = queryParams.NewWardNo.Trim();
            var propertyNo = queryParams.NewPropertyNo.Trim();

            filter = p =>
                p.NewZoneNo == zoneNo &&
                p.NewWardNo == wardNo &&
                p.NewPropertyNo == propertyNo;
        }

        if (filter == null)
        {
            return new PagedResult<PropertyDto>(
                new List<PropertyDto>(),
                0,
                queryParams.PageNumber,
                queryParams.PageSize
            );
        }

        Expression<Func<RTSPropertyMastEntity, PropertyDto>> select = p => new PropertyDto
        {
            Id = p.Id,
            OwnerID = p.OwnerID,
            MobileNo = p.MobileNo,
            UnicdeAddress = p.UnicdeAddress,
            NewZoneNo = p.NewZoneNo,
            NewWardNo = p.NewWardNo,
            NewPropertyNo = p.NewPropertyNo,
            NewPartitionNo = p.NewPartitionNo,
            OldPropertyNo = p.OldPropertyNo,
            OwnerFirstName = p.OwnerFirstName,
            MarathiSocietyName = p.MarathiSocietyName,
            MarathiOwnerPatta = p.MarathiOwnerPatta,
            MarathiOwnerDukanFlatNo = p.MarathiOwnerDukanFlatNo
        };

        var query = _repository.GetQueryable().Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.OwnerID)
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(select)
            .ToListAsync(cancellationToken);

        return new PagedResult<PropertyDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);

    }
}
