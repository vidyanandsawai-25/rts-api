using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RoomWiseMinusData;
using NtisPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Interfaces
{
    public interface IRoomWiseMinusService: ICommonCrudService<RoomWiseMinusDataEntity, RoomWiseMinusDataDto, CreateRoomWiseMinusDataDto, UpdateRoomWiseMinusDataDto,PropertyDetailsQueryParameters, int>
    {

    }
}
