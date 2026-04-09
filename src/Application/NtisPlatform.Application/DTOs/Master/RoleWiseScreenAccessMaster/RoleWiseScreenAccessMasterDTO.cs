using NtisPlatform.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RoleWiseScreenAccessMaster
{
    public class RoleWiseScreenAccessMasterDTO
    {
        public int Id { get; set; }
        public int UserRoleId { get; set; }
        public int ScreenId { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool HaveFullAccess { get; set; }
        public bool HaveNoAccess { get; set; }
        public bool IsActive { get; set; }
        
        // Audit fields for transparency
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class CreateRoleWiseScreenAccessMasterDto : CreateBaseDtos, IValidatableObject
    {
        [Required(ErrorMessage = "UserRoleId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "UserRoleId_Must_Be_Positive")]
        public int UserRoleId { get; set; }

        [Required(ErrorMessage = "ScreenId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "ScreenId_Must_Be_Positive")]
        public int ScreenId { get; set; }

        public bool CanView { get; set; } = false;

        public bool CanEdit { get; set; } = false;

        public bool CanDelete { get; set; } = false;

        public bool HaveFullAccess { get; set; } = false;

        public bool HaveNoAccess { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Full access and no access are mutually exclusive
            if (HaveFullAccess && HaveNoAccess)
            {
                yield return new ValidationResult(
                    "FullAccess_NoAccess_Mutually_Exclusive",
                    new[] { nameof(HaveFullAccess), nameof(HaveNoAccess) }
                );
            }

            // If have full access, all permissions should be true
            if (HaveFullAccess && (!CanView || !CanEdit || !CanDelete))
            {
                yield return new ValidationResult(
                    "FullAccess_Requires_All_Permissions",
                    new[] { nameof(HaveFullAccess) }
                );
            }

            // If have no access, all permissions should be false
            if (HaveNoAccess && (CanView || CanEdit || CanDelete))
            {
                yield return new ValidationResult(
                    "NoAccess_Cannot_Have_Permissions",
                    new[] { nameof(HaveNoAccess) }
                );
            }

            // At least one permission or explicit access level should be set
            if (!HaveFullAccess && !HaveNoAccess && !CanView && !CanEdit && !CanDelete)
            {
                yield return new ValidationResult(
                    "At_Least_One_Permission_Required",
                    new[] { nameof(CanView), nameof(CanEdit), nameof(CanDelete) }
                );
            }
        }
    }

    public class UpdateRoleWiseScreenAccessMasterDto : UpdateBaseDtos, IValidatableObject
    {
        [Required(ErrorMessage = "UserRoleId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "UserRoleId_Must_Be_Positive")]
        public int UserRoleId { get; set; }

        [Required(ErrorMessage = "ScreenId_Required")]
        [Range(1, int.MaxValue, ErrorMessage = "ScreenId_Must_Be_Positive")]
        public int ScreenId { get; set; }

        public bool CanView { get; set; } = false;

        public bool CanEdit { get; set; } = false;

        public bool CanDelete { get; set; } = false;

        public bool HaveFullAccess { get; set; } = false;

        public bool HaveNoAccess { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Full access and no access are mutually exclusive
            if (HaveFullAccess && HaveNoAccess)
            {
                yield return new ValidationResult(
                    "FullAccess_NoAccess_Mutually_Exclusive",
                    new[] { nameof(HaveFullAccess), nameof(HaveNoAccess) }
                );
            }

            // If have full access, all permissions should be true
            if (HaveFullAccess && (!CanView || !CanEdit || !CanDelete))
            {
                yield return new ValidationResult(
                    "FullAccess_Requires_All_Permissions",
                    new[] { nameof(HaveFullAccess) }
                );
            }

            // If have no access, all permissions should be false
            if (HaveNoAccess && (CanView || CanEdit || CanDelete))
            {
                yield return new ValidationResult(
                    "NoAccess_Cannot_Have_Permissions",
                    new[] { nameof(HaveNoAccess) }
                );
            }

            // At least one permission or explicit access level should be set
            if (!HaveFullAccess && !HaveNoAccess && !CanView && !CanEdit && !CanDelete)
            {
                yield return new ValidationResult(
                    "At_Least_One_Permission_Required",
                    new[] { nameof(CanView), nameof(CanEdit), nameof(CanDelete) }
                );
            }
        }
    }
}
