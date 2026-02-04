using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.DTOs
{
    public class TypeOfUseGroupQueryParameters: BaseQueryParameters
    {
        [Filterable]
        [Sortable]
        [Searchable]
        public string? TypeOfUseGroupID { get; set; }

        [Filterable]
        [Sortable]
        [Searchable]
        public string? GroupName { get; set; }
    }
}
