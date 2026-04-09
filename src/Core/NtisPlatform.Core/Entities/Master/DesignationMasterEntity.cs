namespace NtisPlatform.Core.Entities;

/// <summary>
/// Entity representing a designation in the system
/// </summary>
public class DesignationMasterEntity : BaseEntity
{
    /// <summary>
    /// Unique identifier for the designation
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique code for the designation
    /// </summary>
    public string? DesignationCode { get; set; } 

    /// <summary>
    /// Name of the designation
    /// </summary>
    public string? DesignationName { get; set; }

    /// <summary>
    /// Name of the designation in local language
    /// </summary>
    public string? DesignationLocal { get; set; }

    /// <summary>
    /// Description of the designation
    /// </summary>
    public string? DesignationDescription { get; set; }

}
