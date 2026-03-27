using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.PaymentMode;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Services;

public class PaymentModeService : BaseCommonCrudService<PaymentModeEntity, PaymentModeDto, CreatePaymentModeDto, UpdatePaymentModeDto, PaymentModeQueryParameters, int>,
      IPaymentModeService
{
    public PaymentModeService(
        IRepository<PaymentModeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
