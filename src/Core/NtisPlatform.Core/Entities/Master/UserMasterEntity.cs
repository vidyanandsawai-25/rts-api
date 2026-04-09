using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// User entity mapped to existing CORE.UserMaster table
/// </summary>
[Table("UserMaster", Schema = "CORE")]
public class UserMasterEntity : BaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? UserNameNormalized { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? UserCode { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(15)]
    public string? MobileNo { get; set; }

    [MaxLength(15)]
    public string? AlternateMobileNo { get; set; }

    [MaxLength(100)]
    public string? Mail { get; set; }

    /// <summary>
    /// Bcrypt hashed password - stored in PasswordHash column
    /// NOTE: Add this column to your UserMaster table if not exists:
    /// ALTER TABLE [CORE].[UserMaster] ADD [PasswordHash] NVARCHAR(255) NULL;
    /// </summary>
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    public bool MustChangePassword { get; set; }

    public int? UserRoleID { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    public DateTime? LockedUntilAt { get; set; }

    public int? FailedLoginCount { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int? EmployeeTypeID { get; set; }
}
