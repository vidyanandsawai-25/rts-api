using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Helpers.AutomationDashboard
{
    public class TypeBreakdown
    {

    }
    /// <summary>
    /// Assessment status breakdown result
    /// </summary>
    public class AssessmentStatusBreakdown
    {
        public StructureUnitCount Assessed { get; set; } = new();
        public StructureUnitCount Unassessed { get; set; } = new();
        public StructureUnitCount NewlyAssessedFound { get; set; } = new();
        public StructureUnitCount AssessmentInProcess { get; set; } = new();
    }

    /// <summary>
    /// Structure and unit count
    /// </summary>
    public class StructureUnitCount
    {
        public int StatusId { get; set; }
        public int StructureCount { get; set; }
        public int UnitCount { get; set; }
    }
    /// <summary>
    /// Property type breakdown result
    /// </summary>
    public class PropertyTypeBreakdown
    {
        public int Residential { get; set; }
        public int NonResidential { get; set; }
        public int Mixed { get; set; }
        public int PublicUtility { get; set; }
        public int UnderConstruction { get; set; }
    }

    /// <summary>
    /// Represents a group of property uses
    /// </summary>
    public sealed record PropertyUseGroup(List<string> Types, List<string> Codes);

}
