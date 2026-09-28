using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.PropertyMapMaster;

/// <summary>
/// Query parameters for retrieving mapped properties filtered society-wise or wing-wise.
/// </summary>
public class PropertyMapSocietyQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// Society Detail ID (PTIS.SocietyDetailsMast.Id)
    /// </summary>
    public int? SocietyDetailId { get; set; }

    /// <summary>
    /// Wing Details ID (PTIS.WingDetailsMast.Id)
    /// </summary>
    public int? WingDetailsId { get; set; }
}
