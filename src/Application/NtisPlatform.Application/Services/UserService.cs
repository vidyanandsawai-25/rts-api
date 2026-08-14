using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Email;
using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class UserService : BaseCommonCrudService<
        UserEntity,
        UserDto,
        CreateUserDto,
        UpdateUserDto,
        UserQueryParameter,
        int>,
    IUserService
{
    // Configuration constants for email templates
    private const string DefaultCompanyName = "NTIS Platform";

    private readonly ILogger<UserService> _logger;
    private readonly IRepository<UserDepartmentAllocationEntity, int> _departmentMapRepository;
    private readonly IRepository<UserModuleAllocationEntity, int> _moduleAccessRepository;
    private readonly IRepository<UserRoleAllocationEntity, int> _roleAllocationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordGeneratorService _passwordGenerator;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEmailSettingsProvider _emailSettingsProvider;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITwoFactorRecoveryCodeRepository _recoveryCodeRepository;
    private readonly ISecurityAuditService _securityAuditService;

    public UserService(
        IRepository<UserEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UserService> logger,
        IRepository<UserDepartmentAllocationEntity, int> departmentMapRepository,
        IRepository<UserModuleAllocationEntity, int> moduleAccessRepository,
        IRepository<UserRoleAllocationEntity, int> roleAllocationRepository,
        IPasswordHasher passwordHasher,
        IPasswordGeneratorService passwordGenerator,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IEmailSettingsProvider emailSettingsProvider,
        IRefreshTokenRepository refreshTokenRepository,
        ITwoFactorRecoveryCodeRepository recoveryCodeRepository,
        ISecurityAuditService securityAuditService)
        : base(repository, unitOfWork, mapper)
    {
        _logger = logger;
        _departmentMapRepository = departmentMapRepository;
        _moduleAccessRepository = moduleAccessRepository;
        _roleAllocationRepository = roleAllocationRepository;
        _passwordHasher = passwordHasher;
        _passwordGenerator = passwordGenerator;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _emailSettingsProvider = emailSettingsProvider;
        _refreshTokenRepository = refreshTokenRepository;
        _recoveryCodeRepository = recoveryCodeRepository;
        _securityAuditService = securityAuditService;
    }

    // GET BY ID

    public override async Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dto = await _repository
            .GetQueryable()
            .Where(u => u.Id == id)
            .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
            return null;

        await EnrichUserWithRelatedDataAsync(dto, cancellationToken);
        return dto;
    }

    // GET ALL (paged)

    public override async Task<PagedResult<UserDto>> GetAllAsync(UserQueryParameter queryParameters, CancellationToken cancellationToken = default)
    {
        var pagedResult = await base.GetAllAsync(queryParameters, cancellationToken);

        if (pagedResult?.Items == null || !pagedResult.Items.Any())
            return pagedResult!;

        var userIds = pagedResult.Items.Select(u => u.Id).ToList();

        var departments = await _departmentMapRepository
            .GetQueryable()
            .Include(d => d.Department)
            .Where(d => userIds.Contains(d.UserId))
            .ToListAsync(cancellationToken);

        var moduleAccess = await _moduleAccessRepository
            .GetQueryable()
            .Include(m => m.Department)
            .Include(m => m.Module)
            .Where(m => userIds.Contains(m.UserId))
            .ToListAsync(cancellationToken);

        var roleAllocations = await _roleAllocationRepository
            .GetQueryable()
            .Include(r => r.Department)
            .Include(r => r.UserRole)
            .Where(r => userIds.Contains(r.UserId))
            .ToListAsync(cancellationToken);

        var departmentsByUser = departments.GroupBy(d => d.UserId).ToDictionary(g => g.Key, g => g.ToList());
        var moduleAccessByUser = moduleAccess.GroupBy(m => m.UserId).ToDictionary(g => g.Key, g => g.ToList());
        var roleAllocByUser = roleAllocations.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var user in pagedResult.Items)
        {
            user.Departments = departmentsByUser.TryGetValue(user.Id, out var depts)
                ? _mapper.Map<List<UserDepartmentAllocationDto>>(depts) : [];
            user.ModuleAccess = moduleAccessByUser.TryGetValue(user.Id, out var modules)
                ? _mapper.Map<List<UserModuleAllocationDto>>(modules) : [];
            user.RoleAllocations = roleAllocByUser.TryGetValue(user.Id, out var roles)
                ? _mapper.Map<List<UserRoleAllocationDto>>(roles) : [];
        }

        return pagedResult;
    }

    // CREATE
    // Password is auto-generated internally — never accepted from or returned to the client.
    // MustChangePassword is always forced true so the user must set their own password on first login.
    // Sends welcome email with temporary password after successful user creation.

    public override async Task<UserDto> CreateAsync(CreateUserDto createDto, CancellationToken cancellationToken = default)
    {
        await ValidateDuplicateUserAsync(createDto.UserName, createDto.UserCode, excludeUserId: null, cancellationToken);

        // Generate temporary password and hash it
        var temporaryPassword = _passwordGenerator.Generate();
        
        var userEntity = _mapper.Map<UserEntity>(createDto);
        userEntity.PasswordHash = _passwordHasher.HashPassword(temporaryPassword);
        userEntity.MustChangePassword = true;
        userEntity.PasswordChangedAt = DateTime.Now;
        userEntity.CreatedDate = DateTime.Now;
        userEntity.CreatedBy = createDto.CreatedBy;

        await _repository.AddAsync(userEntity, cancellationToken);

        // Stage allocations for insert — EF Core's change tracker will handle the UserId FK
        // automatically when SaveChanges is called, even though userEntity.Id is still 0.
        // The entire aggregate (user + allocations) is committed atomically in a single transaction.
        await SaveDepartmentAllocationsAsync(userEntity, createDto.Departments, createDto.CreatedBy, cancellationToken);
        await SaveModuleAllocationsAsync(userEntity, createDto.ModuleAccess, createDto.CreatedBy, cancellationToken);
        await SaveRoleAllocationsAsync(userEntity, createDto.RoleAllocations, createDto.CreatedBy, cancellationToken);

        // Single SaveChanges — commits user + all allocations atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created: {UserId} - {UserName}", userEntity.Id, userEntity.UserName);

        // Send welcome email with temporary password (only if email is provided)
        if (!string.IsNullOrWhiteSpace(userEntity.Email))
        {
            try
            {
                await SendWelcomeEmailAsync(userEntity, temporaryPassword, cancellationToken);
                _logger.LogInformation("Welcome email sent to {Email} for user {UserId}", userEntity.Email, userEntity.Id);
            }
            catch (Exception ex)
            {
                // Log error but don't fail user creation if email fails
                _logger.LogError(ex, "Failed to send welcome email to {Email} for user {UserId}", userEntity.Email, userEntity.Id);
            }
        }
        else
        {
            _logger.LogWarning("No email address provided for user {UserId}, welcome email not sent", userEntity.Id);
        }

        var dto = _mapper.Map<UserDto>(userEntity);
        await EnrichUserWithRelatedDataAsync(dto, cancellationToken);
        return dto;
    }

    // UPDATE (profile only)
    // IsActive excluded from profile update — use Activate/Deactivate endpoints.
    // Diff-patch allocations instead of delete-and-recreate.
    // Profile + allocation changes committed in one SaveChangesAsync.

    public override async Task<UserDto?> UpdateAsync(int id, UpdateUserDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        await ValidateDuplicateUserAsync(updateDto.UserName, updateDto.UserCode, excludeUserId: id, cancellationToken);

        var originalCreatedBy = entity.CreatedBy;
        var originalCreatedDate = entity.CreatedDate;

        _mapper.Map(updateDto, entity);

        entity.CreatedBy = originalCreatedBy;
        entity.CreatedDate = originalCreatedDate;
        entity.UpdatedBy = updateDto.UpdatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(entity, cancellationToken);

        await PatchDepartmentAllocationsAsync(entity.Id, updateDto.Departments, updateDto.UpdatedBy, cancellationToken);
        await PatchModuleAllocationsAsync(entity.Id, updateDto.ModuleAccess, updateDto.UpdatedBy, cancellationToken);
        await PatchRoleAllocationsAsync(entity.Id, updateDto.RoleAllocations, updateDto.UpdatedBy, cancellationToken);

        // Single save — profile + allocation changes committed atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User profile updated: {UserId} - {UserName}", entity.Id, entity.UserName);

        var dto = _mapper.Map<UserDto>(entity);
        await EnrichUserWithRelatedDataAsync(dto, cancellationToken);
        return dto;
    }

    // DELETE / DEACTIVATE
    // Important: This is a soft-delete operation that preserves historical data.
    // User: Sets IsActive = false + MarkedForDeletion = true (queued for permanent removal by cleanup job)
    // Allocations: Sets IsActive = false (preserved as historical authorization records)
    // 
    // Use case: Removing user access while maintaining audit trail and authorization history.
    // For complete data removal, use the nightly cleanup task that processes MarkedForDeletion records.

    public async Task<bool> DeleteAsync(int id, DeleteUserDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        // Deactivate all allocation records (preserves history for audit/compliance)
        // Pass UpdatedBy to track who performed the deletion
        await DeactivateAllocationHistoryAsync(id, dto.UpdatedBy, cancellationToken);

        // Soft-delete user (sets IsActive = false + MarkedForDeletion = true)
        // User record remains in DB for audit trail until cleanup job runs
        await _repository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User soft-deleted and marked for cleanup, allocations deactivated: {UserId} by {DeletedBy}", id, dto.UpdatedBy);
        return true;
    }

    // Keep base implementation for backward compatibility (non-audited deletion)
    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        // Deactivate allocations without audit trail (legacy behavior)
        await DeactivateAllocationHistoryAsync(id, deletedBy: null, cancellationToken);

        await _repository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("User deleted without audit trail (legacy path): {UserId}", id);
        return true;
    }

    // ACTIVATE / DEACTIVATE / RESET PASSWORD

    public async Task<UserSecurityStatusDto?> DeactivateUserAsync(int id, DeactivateUserDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.IsActive = false;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User deactivated: {UserId}", id);
        return _mapper.Map<UserSecurityStatusDto>(entity);
    }

    public async Task<UserSecurityStatusDto?> ActivateUserAsync(int id, ActivateUserDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.IsActive = true;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User activated: {UserId}", id);
        return _mapper.Map<UserSecurityStatusDto>(entity);
    }

    // RESET PASSWORD
    // System auto-generates the new password — never accepted from or returned to the client.
    // MustChangePassword always forced true.

    public async Task<UserSecurityStatusDto?> ResetPasswordAsync(int id, ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.PasswordHash = _passwordHasher.HashPassword(_passwordGenerator.Generate());
        entity.MustChangePassword = true;
        entity.PasswordChangedAt = DateTime.Now;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset for user: {UserId}", id);
        return _mapper.Map<UserSecurityStatusDto>(entity);
    }

    // REQUIRE / UNREQUIRE 2FA
    // Sets or clears the admin policy flag only — never touches the user's own enrollment state.
    // Enabling 2FA itself still has to happen on the user's own device.

    public async Task<UserSecurityStatusDto?> RequireTwoFactorAsync(int id, RequireTwoFactorDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.TwoFactorRequired = true;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _securityAuditService.RecordAsync(SecurityAuditEventType.TwoFactorRequiredByAdmin, id, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Two-factor authentication required for user: {UserId}", id);
        return _mapper.Map<UserSecurityStatusDto>(entity);
    }

    public async Task<UserSecurityStatusDto?> UnrequireTwoFactorAsync(int id, UnrequireTwoFactorDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.TwoFactorRequired = false;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _securityAuditService.RecordAsync(SecurityAuditEventType.TwoFactorUnrequiredByAdmin, id, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Two-factor authentication requirement removed for user: {UserId}", id);
        return _mapper.Map<UserSecurityStatusDto>(entity);
    }

    // ADMIN RESET 2FA
    // Account-recovery path (e.g. lost device): clears the user's current enrollment and recovery
    // codes and invalidates their sessions, without requiring any code from the user — same end
    // state as their own self-service reset/disable, just triggered by an administrator instead.

    public async Task<UserSecurityStatusDto?> AdminResetTwoFactorAsync(int id, AdminResetTwoFactorDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.TwoFactorEnabled = false;
        entity.TwoFactorSecretEncrypted = null;
        entity.TwoFactorEnabledAt = null;
        entity.SecurityStamp = Guid.NewGuid().ToString("N");
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _recoveryCodeRepository.RevokeAllActiveAsync(id, cancellationToken);
        await _refreshTokenRepository.RevokeAllUserTokensAsync(id, cancellationToken);

        await _securityAuditService.RecordAsync(SecurityAuditEventType.TwoFactorAdminReset, id, success: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Two-factor authentication reset by administrator for user: {UserId}", id);
        return _mapper.Map<UserSecurityStatusDto>(entity);
    }

    // PRIVATE: allocation save helpers (used by CreateAsync)

    // PRIVATE: allocation save helpers (used by CreateAsync)
    // Uses existing AutoMapper profiles:
    //   UserDepartmentAllocationCreateDto -> UserDepartmentAllocationEntity
    //   UserModuleAllocationCreateDto     -> UserModuleAllocationEntity
    //   UserRoleAllocationCreateDto       -> UserRoleAllocationEntity
    // User navigation property and audit fields are set after mapping.
    // Setting the navigation property (instead of just UserId) allows EF Core's change tracker
    // to automatically propagate the generated UserId when the user is saved in the same transaction.

    private async Task SaveDepartmentAllocationsAsync(UserEntity user, List<UserDepartmentAllocationCreateDto>? departments, int? auditUserId, CancellationToken cancellationToken)
    {
        if (departments?.Any() != true) return;

        foreach (var dept in departments)
        {
            var entity = _mapper.Map<UserDepartmentAllocationEntity>(dept);
            if (entity == null)
                throw new InvalidOperationException("AutoMapper returned null for UserDepartmentAllocationEntity. Check mapping configuration.");

            entity.User = user; // Set navigation property for EF Core relationship fix-up
            entity.CreatedBy = auditUserId;
            entity.CreatedDate = DateTime.Now;
            await _departmentMapRepository.AddAsync(entity, cancellationToken);
        }

        _logger.LogInformation("Staged {Count} dept allocations for user (Id will be set on SaveChanges)", departments.Count);
    }

    private async Task SaveModuleAllocationsAsync(UserEntity user, List<UserModuleAllocationCreateDto>? moduleAccess, int? auditUserId, CancellationToken cancellationToken)
    {
        if (moduleAccess?.Any() != true) return;

        foreach (var module in moduleAccess)
        {
            var entity = _mapper.Map<UserModuleAllocationEntity>(module);
            if (entity == null)
                throw new InvalidOperationException("AutoMapper returned null for UserModuleAllocationEntity. Check mapping configuration.");

            entity.User = user; // Set navigation property for EF Core relationship fix-up
            entity.CreatedBy = auditUserId;
            entity.CreatedDate = DateTime.Now;
            await _moduleAccessRepository.AddAsync(entity, cancellationToken);
        }

        _logger.LogInformation("Staged {Count} module allocations for user (Id will be set on SaveChanges)", moduleAccess.Count);
    }

    private async Task SaveRoleAllocationsAsync(UserEntity user, List<UserRoleAllocationCreateDto>? roleAllocations, int? auditUserId, CancellationToken cancellationToken)
    {
        if (roleAllocations?.Any() != true) return;

        foreach (var role in roleAllocations)
        {
            var entity = _mapper.Map<UserRoleAllocationEntity>(role);
            if (entity == null)
                throw new InvalidOperationException("AutoMapper returned null for UserRoleAllocationEntity. Check mapping configuration.");

            entity.User = user; // Set navigation property for EF Core relationship fix-up
            entity.CreatedBy = auditUserId;
            entity.CreatedDate = DateTime.Now;
            await _roleAllocationRepository.AddAsync(entity, cancellationToken);
        }

        _logger.LogInformation("Staged {Count} role allocations for user (Id will be set on SaveChanges)", roleAllocations.Count);
    }

    // PRIVATE: diff-patch helpers (used by UpdateAsync)
    // New rows   -> mapped via CreateDto profile, then UserId/audit set after
    // Removed    -> IsActive = false, UpdatedBy/UpdatedDate stamped directly on existing entity
    // Reactivated -> IsActive = true, UpdatedBy/UpdatedDate stamped directly on existing entity

    private async Task PatchDepartmentAllocationsAsync(int userId, List<UserDepartmentAllocationCreateDto>? incoming, int? auditUserId, CancellationToken cancellationToken)
    {
        var existing = await _departmentMapRepository.GetQueryable()
            .Where(d => d.UserId == userId).ToListAsync(cancellationToken);
        var incomingIds = incoming?.Select(d => d.DepartmentId).ToHashSet() ?? [];

        // Deactivate removed rows
        foreach (var row in existing.Where(e => !incomingIds.Contains(e.DepartmentId)))
        {
            row.IsActive = false;
            row.UpdatedBy = auditUserId;
            row.UpdatedDate = DateTime.Now;
            await _departmentMapRepository.UpdateAsync(row, cancellationToken);
        }

        var existingIds = existing.Select(e => e.DepartmentId).ToHashSet();

        // Add new rows via mapper
        foreach (var dept in incoming?.Where(d => !existingIds.Contains(d.DepartmentId)) ?? [])
        {
            var entity = _mapper.Map<UserDepartmentAllocationEntity>(dept);
            if (entity == null)
                throw new InvalidOperationException("AutoMapper returned null for UserDepartmentAllocationEntity. Check mapping configuration.");

            entity.UserId = userId;
            entity.CreatedBy = auditUserId;
            entity.CreatedDate = DateTime.Now;
            await _departmentMapRepository.AddAsync(entity, cancellationToken);
        }

        // Re-activate previously deactivated rows
        foreach (var row in existing.Where(e => incomingIds.Contains(e.DepartmentId) && !e.IsActive))
        {
            row.IsActive = true;
            row.UpdatedBy = auditUserId;
            row.UpdatedDate = DateTime.Now;
            await _departmentMapRepository.UpdateAsync(row, cancellationToken);
        }
    }

    private async Task PatchModuleAllocationsAsync(int userId, List<UserModuleAllocationCreateDto>? incoming, int? auditUserId, CancellationToken cancellationToken)
    {
        var existing = await _moduleAccessRepository.GetQueryable()
            .Where(m => m.UserId == userId).ToListAsync(cancellationToken);
        var incomingKeys = incoming?.Select(m => (m.DepartmentId, m.ModuleId)).ToHashSet() ?? [];

        // Deactivate removed rows
        foreach (var row in existing.Where(e => !incomingKeys.Contains((e.DepartmentId, e.ModuleId))))
        {
            row.IsActive = false;
            row.UpdatedBy = auditUserId;
            row.UpdatedDate = DateTime.Now;
            await _moduleAccessRepository.UpdateAsync(row, cancellationToken);
        }

        var existingKeys = existing.Select(e => (e.DepartmentId, e.ModuleId)).ToHashSet();

        // Add new rows via mapper
        foreach (var module in incoming?.Where(m => !existingKeys.Contains((m.DepartmentId, m.ModuleId))) ?? [])
        {
            var entity = _mapper.Map<UserModuleAllocationEntity>(module);
            if (entity == null)
                throw new InvalidOperationException("AutoMapper returned null for UserModuleAllocationEntity. Check mapping configuration.");

            entity.UserId = userId;
            entity.CreatedBy = auditUserId;
            entity.CreatedDate = DateTime.Now;
            await _moduleAccessRepository.AddAsync(entity, cancellationToken);
        }

        // Re-activate previously deactivated rows
        foreach (var row in existing.Where(e => incomingKeys.Contains((e.DepartmentId, e.ModuleId)) && !e.IsActive))
        {
            row.IsActive = true;
            row.UpdatedBy = auditUserId;
            row.UpdatedDate = DateTime.Now;
            await _moduleAccessRepository.UpdateAsync(row, cancellationToken);
        }
    }

    private async Task PatchRoleAllocationsAsync(int userId, List<UserRoleAllocationCreateDto>? incoming, int? auditUserId, CancellationToken cancellationToken)
    {
        var existing = await _roleAllocationRepository.GetQueryable()
            .Where(r => r.UserId == userId).ToListAsync(cancellationToken);
        var incomingKeys = incoming?.Select(r => (r.DepartmentId, r.UserRoleId)).ToHashSet() ?? [];

        // Deactivate removed rows
        foreach (var row in existing.Where(e => !incomingKeys.Contains((e.DepartmentId, e.UserRoleId))))
        {
            row.IsActive = false;
            row.UpdatedBy = auditUserId;
            row.UpdatedDate = DateTime.Now;
            await _roleAllocationRepository.UpdateAsync(row, cancellationToken);
        }

        var existingKeys = existing.Select(e => (e.DepartmentId, e.UserRoleId)).ToHashSet();

        // Add new rows via mapper
        foreach (var role in incoming?.Where(r => !existingKeys.Contains((r.DepartmentId, r.UserRoleId))) ?? [])
        {
            var entity = _mapper.Map<UserRoleAllocationEntity>(role);
            if (entity == null)
                throw new InvalidOperationException("AutoMapper returned null for UserRoleAllocationEntity. Check mapping configuration.");

            entity.UserId = userId;
            entity.CreatedBy = auditUserId;
            entity.CreatedDate = DateTime.Now;
            await _roleAllocationRepository.AddAsync(entity, cancellationToken);
        }

        // Re-activate previously deactivated rows
        foreach (var row in existing.Where(e => incomingKeys.Contains((e.DepartmentId, e.UserRoleId)) && !e.IsActive))
        {
            row.IsActive = true;
            row.UpdatedBy = auditUserId;
            row.UpdatedDate = DateTime.Now;
            await _roleAllocationRepository.UpdateAsync(row, cancellationToken);
        }
    }

    // PRIVATE: deactivate allocation history (preserves audit trail)
    // Allocation rows are HISTORICAL RECORDS, not pure join tables.
    // They track authorization changes over time with full audit fields.
    // Deactivation (IsActive = false) preserves:
    //   - Who granted the allocation (CreatedBy)
    //   - When it was granted (CreatedDate)
    //   - Who revoked it (UpdatedBy)
    //   - When it was revoked (UpdatedDate)
    // This supports compliance, audit reports, and authorization history tracking.

    private async Task DeactivateAllocationHistoryAsync(int userId, int? deletedBy, CancellationToken cancellationToken)
    {
        var depts = await _departmentMapRepository.GetQueryable()
            .Where(d => d.UserId == userId && d.IsActive).ToListAsync(cancellationToken);
        foreach (var d in depts)
        {
            d.IsActive = false;
            d.UpdatedBy = deletedBy;
            d.UpdatedDate = DateTime.Now;
            await _departmentMapRepository.UpdateAsync(d, cancellationToken);
        }

        var modules = await _moduleAccessRepository.GetQueryable()
            .Where(m => m.UserId == userId && m.IsActive).ToListAsync(cancellationToken);
        foreach (var m in modules) 
        { 
            m.IsActive = false;
            m.UpdatedBy = deletedBy;
            m.UpdatedDate = DateTime.Now;
            await _moduleAccessRepository.UpdateAsync(m, cancellationToken); 
        }

        var roles = await _roleAllocationRepository.GetQueryable()
            .Where(r => r.UserId == userId && r.IsActive).ToListAsync(cancellationToken);
        foreach (var r in roles) 
        { 
            r.IsActive = false;
            r.UpdatedBy = deletedBy;
            r.UpdatedDate = DateTime.Now;
            await _roleAllocationRepository.UpdateAsync(r, cancellationToken); 
        }

        _logger.LogInformation("Deactivated allocations for user {UserId}: {DeptCount} departments, {ModuleCount} modules, {RoleCount} roles (UpdatedBy: {UpdatedBy})", 
            userId, depts.Count, modules.Count, roles.Count, deletedBy);
    }

    // PRIVATE: enrich DTO with child allocations

    private async Task EnrichUserWithRelatedDataAsync(UserDto dto, CancellationToken cancellationToken)
    {
        // Sequential awaits — EF Core DbContext is not thread-safe.
        // Task.WhenAll runs queries concurrently on the same context instance
        // which causes "A second operation was started" errors.
        var departments = await _departmentMapRepository
            .GetQueryable().Include(d => d.Department)
            .Where(d => d.UserId == dto.Id).ToListAsync(cancellationToken);

        var moduleAccess = await _moduleAccessRepository
            .GetQueryable().Include(m => m.Department).Include(m => m.Module)
            .Where(m => m.UserId == dto.Id).ToListAsync(cancellationToken);

        var roleAllocs = await _roleAllocationRepository
            .GetQueryable().Include(r => r.Department).Include(r => r.UserRole)
            .Where(r => r.UserId == dto.Id).ToListAsync(cancellationToken);

        dto.Departments = _mapper.Map<List<UserDepartmentAllocationDto>>(departments);
        dto.ModuleAccess = _mapper.Map<List<UserModuleAllocationDto>>(moduleAccess);
        dto.RoleAllocations = _mapper.Map<List<UserRoleAllocationDto>>(roleAllocs);
    }

    // PRIVATE: duplicate check

    private async Task ValidateDuplicateUserAsync(string userName, string? userCode, int? excludeUserId, CancellationToken cancellationToken)
    {
        var queryable = _repository.GetQueryable();
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(userCode))
        {
            var q = queryable.Where(u => u.UserCode == userCode);
            if (excludeUserId.HasValue) q = q.Where(u => u.Id != excludeUserId.Value);
            if (await q.AnyAsync(cancellationToken))
            {
                errors.Add($"UserCode '{userCode}' already exists");
                _logger.LogWarning("Duplicate UserCode: {UserCode}", userCode);
            }
        }

        var unQ = queryable.Where(u => u.UserName.ToUpper() == userName.ToUpper());
        if (excludeUserId.HasValue) unQ = unQ.Where(u => u.Id != excludeUserId.Value);
        if (await unQ.AnyAsync(cancellationToken))
        {
            errors.Add($"Username '{userName}' is already taken");
            _logger.LogWarning("Duplicate Username: {UserName}", userName);
        }

        if (errors.Any())
            throw new InvalidOperationException(string.Join("; ", errors));
    }

    /// <summary>
    /// Sends welcome email with temporary password to newly created user
    /// </summary>
    private async Task SendWelcomeEmailAsync(UserEntity user, string temporaryPassword, CancellationToken cancellationToken)
    {
        // Get email settings (includes LoginUrl from config)
        var emailSettings = await _emailSettingsProvider.GetEmailSettingsAsync(cancellationToken);

        // Prepare template placeholders (CompanyName can be overridden in appsettings if needed)
        var placeholders = new Dictionary<string, string>
        {
            { "UserName", user.UserName },
            { "Email", user.Email ?? string.Empty },
            { "TemporaryPassword", temporaryPassword },
            { "LoginUrl", emailSettings.LoginUrl ?? "#" },
            { "CompanyName", DefaultCompanyName }
        };

        // Load and process template
        var emailBody = await _emailTemplateService.GetTemplateAsync("WelcomeEmail", placeholders, cancellationToken);

        // Build email request
        var emailRequest = new EmailRequest
        {
            ToEmail = user.Email!,
            ToName = $"{user.FirstName} {user.LastName}".Trim(),
            Subject = "Welcome to NTIS Platform - Your Account Details",
            Body = emailBody,
            IsHtml = true
        };

        // Send email
        await _emailService.SendEmailAsync(emailRequest, cancellationToken);
    }
}