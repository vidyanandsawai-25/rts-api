namespace NtisPlatform.Application.DTOs.Master.DepartmentLicenceDetails;

/// <summary>
/// Department Licence Details DTO for read operations
/// </summary>
public class DepartmentLicenceDetailsDto : CommonBaseDtos
{
    public int LicenceDetailsId { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime? LicenceStartDate { get; set; }
    public DateTime? LicenceEndDate { get; set; }
    public string? LicenceDuration { get; set; } 
}

/// <summary>
/// DTO for creating new Department Licence Details
/// </summary>
public class CreateDepartmentLicenceDetailsDto : CreateCommonBaseDtos
{
    public int? DepartmentId { get; set; }
    public DateTime? LicenceStartDate { get; set; }
    public DateTime? LicenceEndDate { get; set; }
    public string? LicenceDuration { get; set; } 
}

/// <summary>
/// DTO for updating existing Department Licence Details
/// </summary>
public class UpdateDepartmentLicenceDetailsDto : UpdateCommonBaseDtos
{
    public int? DepartmentId { get; set; }
    public DateTime? LicenceStartDate { get; set; }
    public DateTime? LicenceEndDate { get; set; }
    public string? LicenceDuration { get; set; }     
}
