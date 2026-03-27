using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.PaymentMode;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPaymentModeService
    : ICommonCrudService<PaymentModeEntity, PaymentModeDto, CreatePaymentModeDto, UpdatePaymentModeDto, PaymentModeQueryParameters, int>
{
}

