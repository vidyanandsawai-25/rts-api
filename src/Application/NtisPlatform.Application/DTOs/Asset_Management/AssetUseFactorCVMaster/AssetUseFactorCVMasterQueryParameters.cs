using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetUseFactorCVMaster
{
    public class AssetUseFactorCVMasterQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? TypeOfUseId { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? SubTypeOfUseId { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public int? YearRangeCVId { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public bool? IsActive { get; set; }

        [Filterable]
        [Sortable]
        public bool? MarkedForDeletion { get; set; }
    }
}
