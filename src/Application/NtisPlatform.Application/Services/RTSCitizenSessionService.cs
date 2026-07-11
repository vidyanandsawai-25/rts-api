using System;
using System.Linq;
using AutoMapper;
using NtisPlatform.Application.DTOs.RTSCitizenSession;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSCitizenSessionService:BaseCommonCrudService<RTSCitizenSessionEntity, RTSCitizenSessionDto, CreateRTSCitizenSessionDto, UpdateRTSCitizenSessionDto, RTSCitizenSessionQueryParameters, int>, IRTSCitizenSessionService
{
    public RTSCitizenSessionService(IRepository<RTSCitizenSessionEntity, int> repository,IMapper mapper,IUnitOfWork unitOfWork):base(repository, unitOfWork, mapper)
    {
    }

    public async Task<RTSCitizenSessionValidationResultDto> ValidateAndUpdateSessionAsync(string sessionId, CancellationToken ct)
    {
        var sessions = await _repository.GetAsync(s => s.SessionId == sessionId, ct);
        var session = sessions.FirstOrDefault();

        if (session == null)
        {
            return new RTSCitizenSessionValidationResultDto
            {
                Success = false,
                Message = "SessionNotFound"
            };
        }

        if (!session.IsActive)
        {
            return new RTSCitizenSessionValidationResultDto
            {
                Success = false,
                Message = "SessionInactive",
                Session = _mapper.Map<RTSCitizenSessionDto>(session)
            };
        }

        var lastActivity = session.LastActivityTime ?? session.LoginTime;
        if (DateTime.UtcNow.Subtract(lastActivity).TotalMinutes > 30)
        {
            session.IsActive = false;
            session.LogoutTime = lastActivity;
            await _repository.UpdateAsync(session, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new RTSCitizenSessionValidationResultDto
            {
                Success = false,
                Message = "SessionExpired",
                Session = _mapper.Map<RTSCitizenSessionDto>(session)
            };
        }

        session.LastActivityTime = DateTime.UtcNow;
        await _repository.UpdateAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new RTSCitizenSessionValidationResultDto
        {
            Success = true,
            Message = "SessionActive",
            Session = _mapper.Map<RTSCitizenSessionDto>(session)
        };
    }
}
