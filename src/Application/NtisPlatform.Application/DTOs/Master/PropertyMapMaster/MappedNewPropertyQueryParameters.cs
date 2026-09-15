using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

/// <summary>
/// Query parameters for retrieving new properties mapped to an Old Property ID.
/// </summary>
public class MappedNewPropertyQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Old Property ID (PropertyMastOld.Id)
    /// </summary>
    public int? OldPropertyId { get; set; }

    /// <summary>
    /// Wing Details ID (PTIS.WingDetailsMast.Id)
    /// </summary>
    public int? WingDetailsId { get; set; }
}
