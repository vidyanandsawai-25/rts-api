using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.RuleCategory
{
    /// <summary>
    /// DTO for retrieving a rule category
    /// </summary>
    public class RuleCategoryDto : BaseDtos
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// DTO for creating a new rule category
    /// </summary>
    public class CreateRuleCategoryDto : CreateBaseDtos
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "CategoryCode_Required")]
        [System.ComponentModel.DataAnnotations.StringLength(50, ErrorMessage = "CategoryCode_MaxLen_50")]
        public string CategoryCode { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "CategoryName_Required")]
        [System.ComponentModel.DataAnnotations.StringLength(200, ErrorMessage = "CategoryName_MaxLen_200")]
        public string CategoryName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(500, ErrorMessage = "Description_MaxLen_500")]
        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;
    }

    /// <summary>
    /// DTO for updating an existing rule category
    /// </summary>
    public class UpdateRuleCategoryDto : UpdateBaseDtos
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "CategoryCode_Required")]
        [System.ComponentModel.DataAnnotations.StringLength(50, ErrorMessage = "CategoryCode_MaxLen_50")]
        public string CategoryCode { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "CategoryName_Required")]
        [System.ComponentModel.DataAnnotations.StringLength(200, ErrorMessage = "CategoryName_MaxLen_200")]
        public string CategoryName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.StringLength(500, ErrorMessage = "Description_MaxLen_500")]
        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;
    }

    /// <summary>
    /// Query parameters for filtering rule categories
    /// </summary>
    public class RuleCategoryQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains)]
        public string? CategoryCode { get; set; }

        [Filterable(FilterOperator.Contains)]
        public string? CategoryName { get; set; }

        [Filterable(FilterOperator.Equals)]
        public bool? IsActive { get; set; }
    }
}
