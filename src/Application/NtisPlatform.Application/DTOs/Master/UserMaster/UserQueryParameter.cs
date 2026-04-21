using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.UserMaster
{
    using NtisPlatform.Application.Attributes;
    using NtisPlatform.Application.Enums;

    public class UserQueryParameter : BaseQueryParameters
    {
        /// <summary>
        /// Filter/search by username
        /// </summary>
        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? UserName { get; set; }

        /// <summary>
        /// Filter/search by first name
        /// </summary>
        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? FirstName { get; set; }

        /// <summary>
        /// Filter/search by middle name
        /// </summary>
        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? MiddleName { get; set; }

        /// <summary>
        /// Filter/search by last name
        /// </summary>
        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? LastName { get; set; }

        /// <summary>
        /// Filter/search by mobile number
        /// </summary>
        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? MobileNo { get; set; }
    }
}
