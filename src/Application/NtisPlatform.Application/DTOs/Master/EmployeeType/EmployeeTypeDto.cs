using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.EmployeeType
{
    public class EmployeeTypeDto : BaseDtos
    {
        public string? EmployeeType { get; set; }
    }
    public class CreateEmployeeTypeDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "EmployeeType_Required")]
        [StringLength(100, ErrorMessage = "EmployeeType_MaxLen_100")]
        public string EmployeeType { get; set; } = string.Empty;
    }

    public class UpdateEmployeeTypeDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "EmployeeType_Required")]
        [StringLength(100, ErrorMessage = "EmployeeType_MaxLen_100")]
        public string EmployeeType { get; set; } = string.Empty;
    }

    public class UserEmployeeTypeQueryParameterDto : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains)]
        public string? EmployeeType { get; set; }
        [Filterable(FilterOperator.Equals)]
        public bool? IsActive { get; set; }
    }
}
