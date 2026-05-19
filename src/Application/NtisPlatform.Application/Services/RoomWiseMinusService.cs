using AutoMapper;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.DTOs.RoomWiseMinusData;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Services
{
    public class RoomWiseMinusService : BaseCommonCrudService<RoomWiseMinusDataEntity, RoomWiseMinusDataDto, CreateRoomWiseMinusDataDto, UpdateRoomWiseMinusDataDto, PropertyDetailsQueryParameters, int>, IRoomWiseMinusService
    {
        public RoomWiseMinusService(
            IRepository<RoomWiseMinusDataEntity, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : base(repository, unitOfWork, mapper)
        {

        }
    }
}
