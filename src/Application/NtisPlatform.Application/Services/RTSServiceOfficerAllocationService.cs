using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.RTS;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSServiceOfficerAllocationService : IRTSServiceOfficerAllocationService
{
    private readonly IRepository<RTSServiceOfficerAllocationEntity, int> _allocationRepository;
    private readonly IRepository<RTSServiceEntity, int> _serviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RTSServiceOfficerAllocationService> _logger;

    public RTSServiceOfficerAllocationService(
        IRepository<RTSServiceOfficerAllocationEntity, int> allocationRepository,
        IRepository<RTSServiceEntity, int> serviceRepository,
        IUnitOfWork unitOfWork,
        ILogger<RTSServiceOfficerAllocationService> logger)
    {
        _allocationRepository = allocationRepository;
        _serviceRepository = serviceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<RTSServiceOfficerAllocationDto>> GetOfficersByServiceIdAsync(int serviceId, CancellationToken ct = default)
    {
        return await _allocationRepository.GetQueryable()
            .AsNoTracking()
            .Include(a => a.Service)
            .Where(a => a.ServiceId == serviceId && a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.ZoneName)
            .Select(a => new RTSServiceOfficerAllocationDto
            {
                Id = a.Id,
                ServiceId = a.ServiceId,
                ServiceName = a.Service != null ? a.Service.ServiceName : null,
                ZoneId = a.ZoneId,
                ZoneName = a.ZoneName,
                ZoneNameLocal = a.ZoneNameLocal,
                OfficerName = a.OfficerName,
                OfficerNameLocal = a.OfficerNameLocal,
                Designation = a.Designation,
                DesignationLocal = a.DesignationLocal,
                MobileNo = a.MobileNo,
                Email = a.Email,
                OfficeAddress = a.OfficeAddress,
                OfficeAddressLocal = a.OfficeAddressLocal,
                OfficerRole = a.OfficerRole,
                DisplayOrder = a.DisplayOrder,
                IsActive = a.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<List<RTSServiceOfficerAllocationDto>> GetAllAllocationsAsync(int? serviceId = null, int? zoneId = null, CancellationToken ct = default)
    {
        var query = _allocationRepository.GetQueryable()
            .AsNoTracking()
            .Include(a => a.Service)
            .Where(a => a.IsActive);

        if (serviceId.HasValue && serviceId.Value > 0)
        {
            query = query.Where(a => a.ServiceId == serviceId.Value);
        }

        if (zoneId.HasValue && zoneId.Value > 0)
        {
            query = query.Where(a => a.ZoneId == zoneId.Value);
        }

        return await query
            .OrderBy(a => a.ServiceId)
            .ThenBy(a => a.DisplayOrder)
            .Select(a => new RTSServiceOfficerAllocationDto
            {
                Id = a.Id,
                ServiceId = a.ServiceId,
                ServiceName = a.Service != null ? a.Service.ServiceName : null,
                ZoneId = a.ZoneId,
                ZoneName = a.ZoneName,
                ZoneNameLocal = a.ZoneNameLocal,
                OfficerName = a.OfficerName,
                OfficerNameLocal = a.OfficerNameLocal,
                Designation = a.Designation,
                DesignationLocal = a.DesignationLocal,
                MobileNo = a.MobileNo,
                Email = a.Email,
                OfficeAddress = a.OfficeAddress,
                OfficeAddressLocal = a.OfficeAddressLocal,
                OfficerRole = a.OfficerRole,
                DisplayOrder = a.DisplayOrder,
                IsActive = a.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<RTSServiceOfficerAllocationDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var a = await _allocationRepository.GetQueryable()
            .AsNoTracking()
            .Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

        if (a == null) return null;

        return new RTSServiceOfficerAllocationDto
        {
            Id = a.Id,
            ServiceId = a.ServiceId,
            ServiceName = a.Service != null ? a.Service.ServiceName : null,
            ZoneId = a.ZoneId,
            ZoneName = a.ZoneName,
            ZoneNameLocal = a.ZoneNameLocal,
            OfficerName = a.OfficerName,
            OfficerNameLocal = a.OfficerNameLocal,
            Designation = a.Designation,
            DesignationLocal = a.DesignationLocal,
            MobileNo = a.MobileNo,
            Email = a.Email,
            OfficeAddress = a.OfficeAddress,
            OfficeAddressLocal = a.OfficeAddressLocal,
            OfficerRole = a.OfficerRole,
            DisplayOrder = a.DisplayOrder,
            IsActive = a.IsActive
        };
    }

    public async Task<RTSServiceOfficerAllocationDto> CreateAllocationAsync(CreateRTSServiceOfficerAllocationDto dto, int? userId = null, CancellationToken ct = default)
    {
        var entity = new RTSServiceOfficerAllocationEntity
        {
            ServiceId = dto.ServiceId,
            ZoneId = dto.ZoneId,
            ZoneName = dto.ZoneName.Trim(),
            ZoneNameLocal = dto.ZoneNameLocal?.Trim(),
            OfficerName = dto.OfficerName.Trim(),
            OfficerNameLocal = dto.OfficerNameLocal?.Trim(),
            Designation = dto.Designation.Trim(),
            DesignationLocal = dto.DesignationLocal?.Trim(),
            MobileNo = dto.MobileNo.Trim(),
            Email = dto.Email?.Trim(),
            OfficeAddress = dto.OfficeAddress?.Trim(),
            OfficeAddressLocal = dto.OfficeAddressLocal?.Trim(),
            OfficerRole = !string.IsNullOrWhiteSpace(dto.OfficerRole) ? dto.OfficerRole : "DesignatedOfficer",
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedBy = userId,
            CreatedDate = DateTime.Now
        };

        await _allocationRepository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct) ?? new RTSServiceOfficerAllocationDto
        {
            Id = entity.Id,
            ServiceId = entity.ServiceId,
            ZoneId = entity.ZoneId,
            ZoneName = entity.ZoneName,
            ZoneNameLocal = entity.ZoneNameLocal,
            OfficerName = entity.OfficerName,
            OfficerNameLocal = entity.OfficerNameLocal,
            Designation = entity.Designation,
            DesignationLocal = entity.DesignationLocal,
            MobileNo = entity.MobileNo,
            Email = entity.Email,
            OfficeAddress = entity.OfficeAddress,
            OfficeAddressLocal = entity.OfficeAddressLocal,
            OfficerRole = entity.OfficerRole,
            DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive
        };
    }

    public async Task<RTSServiceOfficerAllocationDto?> UpdateAllocationAsync(int id, UpdateRTSServiceOfficerAllocationDto dto, int? userId = null, CancellationToken ct = default)
    {
        var entity = await _allocationRepository.GetByIdAsync(id, ct);
        if (entity == null) return null;

        entity.ZoneId = dto.ZoneId;
        entity.ZoneName = dto.ZoneName.Trim();
        entity.ZoneNameLocal = dto.ZoneNameLocal?.Trim();
        entity.OfficerName = dto.OfficerName.Trim();
        entity.OfficerNameLocal = dto.OfficerNameLocal?.Trim();
        entity.Designation = dto.Designation.Trim();
        entity.DesignationLocal = dto.DesignationLocal?.Trim();
        entity.MobileNo = dto.MobileNo.Trim();
        entity.Email = dto.Email?.Trim();
        entity.OfficeAddress = dto.OfficeAddress?.Trim();
        entity.OfficeAddressLocal = dto.OfficeAddressLocal?.Trim();
        entity.OfficerRole = !string.IsNullOrWhiteSpace(dto.OfficerRole) ? dto.OfficerRole : entity.OfficerRole;
        entity.DisplayOrder = dto.DisplayOrder;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = userId;
        entity.UpdatedDate = DateTime.Now;

        await _allocationRepository.UpdateAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<bool> DeleteAllocationAsync(int id, int? userId = null, CancellationToken ct = default)
    {
        var entity = await _allocationRepository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        entity.IsActive = false;
        entity.UpdatedBy = userId;
        entity.UpdatedDate = DateTime.Now;

        await _allocationRepository.UpdateAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
