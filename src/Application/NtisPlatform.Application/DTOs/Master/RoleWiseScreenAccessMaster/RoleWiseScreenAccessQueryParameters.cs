using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.RoleWiseScreenAccessMaster
{
    public class RoleWiseScreenAccessQueryParameters : BaseQueryParameters
    {
        [Filterable]
        [Sortable]
        public int? RoleWiseScreenAccessId { get; set; }

        [Filterable]
        [Sortable]
        public int? UserRoleId { get; set; }

        [Filterable]
        [Sortable]
        public int? ScreenId { get; set; }

        [Filterable]
        [Sortable]
        public bool? CanView { get; set; }

        [Filterable]
        [Sortable]
        public bool? CanEdit { get; set; }

        [Filterable]
        [Sortable]
        public bool? CanDelete { get; set; }

        [Filterable]
        [Sortable]
        public bool? HaveFullAccess { get; set; }

        [Filterable]
        [Sortable]
        public bool? HaveNoAccess { get; set; }
    }
}
