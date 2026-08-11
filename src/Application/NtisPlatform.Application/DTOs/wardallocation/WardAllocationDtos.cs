using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.wardallocation;

/// <summary>
/// Ward allocation DTO for read operations.
/// </summary>
public class WardAllocationDto : BaseDtos
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmpCode { get; set; }

    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public int ModuleId { get; set; }
    public string? ModuleName { get; set; }

    public int ZoneId { get; set; }
    public string? ZoneNo { get; set; }

    public int WardId { get; set; }
    public string? WardNo { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for creating new ward allocation records.
/// </summary>
public class CreateWardAllocationDto : CreateBaseDtos
{
    [Required(ErrorMessage = "WardAllocation_UserId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_UserId_Invalid")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "WardAllocation_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_DepartmentId_Invalid")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "WardAllocation_ModuleId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_ModuleId_Invalid")]
    public int ModuleId { get; set; }

    [Required(ErrorMessage = "WardAllocation_ZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_ZoneId_Invalid")]
    public int ZoneId { get; set; }

    [Required(ErrorMessage = "WardAllocation_WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_WardId_Invalid")]
    public int WardId { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating existing ward allocation records.
/// </summary>
public class UpdateWardAllocationDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "WardAllocation_Id_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_Id_Invalid")]
    public int Id { get; set; }

    [Required(ErrorMessage = "WardAllocation_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_DepartmentId_Invalid")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "WardAllocation_ModuleId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_ModuleId_Invalid")]
    public int ModuleId { get; set; }

    [Required(ErrorMessage = "WardAllocation_ZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_ZoneId_Invalid")]
    public int ZoneId { get; set; }

    [Required(ErrorMessage = "WardAllocation_WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_WardId_Invalid")]
    public int WardId { get; set; }

    public bool IsActive { get; set; }
}

/// <summary>
/// Employee dropdown DTO.
/// </summary>
public class WardAllocationEmployeeDto
{
    public int UserId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmpCode { get; set; }
}

/// <summary>
/// Module dropdown DTO.
/// </summary>
public class WardAllocationModuleDto
{
    public int ModuleId { get; set; }
    public string? ModuleCode { get; set; }
    public string? ModuleName { get; set; }

    public int DepartmentId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Zone dropdown DTO.
/// </summary>
public class WardAllocationZoneDto
{
    public int ZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public string? ZoneName { get; set; }
}

/// <summary>
/// Ward dropdown DTO.
/// </summary>
public class WardAllocationWardDto
{
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public string? WardName { get; set; }
    public int ZoneId { get; set; }
}

/// <summary>
/// Zone-Ward allocation group for flexible allocation.
/// </summary>
public class ZoneWardAllocationDto
{
    [Required(ErrorMessage = "WardAllocation_ZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_ZoneId_Invalid")]
    public int ZoneId { get; set; }

    [Required(ErrorMessage = "WardAllocation_WardIds_Required")]
    [MinLength(1, ErrorMessage = "WardAllocation_WardIds_AtLeastOne")]
    public List<int> WardIds { get; set; } = new();
}

/// <summary>
/// Unified DTO for creating ward allocations.
/// Supports: Single/Multiple zones with Single/Multiple wards each.
/// </summary>
public class CreateFlexibleWardAllocationDto : CreateBaseDtos
{
    [Required(ErrorMessage = "WardAllocation_UserId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_UserId_Invalid")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "WardAllocation_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_DepartmentId_Invalid")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "WardAllocation_ModuleId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_ModuleId_Invalid")]
    public int ModuleId { get; set; }

    [Required(ErrorMessage = "WardAllocation_Allocations_Required")]
    [MinLength(1, ErrorMessage = "WardAllocation_Allocations_AtLeastOne")]
    public List<ZoneWardAllocationDto> Allocations { get; set; } = new();

    public bool IsActive { get; set; } = true;
}

public class UpdateFlexibleWardAllocationDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "WardAllocation_UserId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_UserId_Invalid")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "WardAllocation_DepartmentId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_DepartmentId_Invalid")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "WardAllocation_ModuleId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "WardAllocation_ModuleId_Invalid")]
    public int ModuleId { get; set; }

    [Required(ErrorMessage = "WardAllocation_Allocations_Required")]
    [MinLength(1, ErrorMessage = "WardAllocation_Allocations_AtLeastOne")]
    public List<ZoneWardAllocationDto> Allocations { get; set; } = new();

    public bool IsActive { get; set; } = true;
}

public class WardAllocationDepartmentDto
{
    public int DepartmentId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
}

public class UserAllocatedZoneWardDto
{
    public int ZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public string? ZoneName { get; set; }
    public List<UserAllocatedWardDto> Wards { get; set; } = new();
}

public class UserAllocatedWardDto
{
    public int WardId { get; set; }
    public string? WardNo { get; set; }
}

public class AllocatedZoneByUserDto
{
    public int ModuleId { get; set; }
    public string? ModuleName { get; set; }

    public int ZoneId { get; set; }
    public string? ZoneNo { get; set; }
    public string? ZoneName { get; set; }
}

public class AllocatedWardByUserDto
{
    public int ModuleId { get; set; }
    public string? ModuleName { get; set; }

    public int ZoneId { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
}