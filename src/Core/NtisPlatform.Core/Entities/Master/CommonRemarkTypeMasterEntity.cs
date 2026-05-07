namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a common remark type entity that stores remark type information.
/// </summary>
public class CommonRemarkTypeMasterEntity : BaseEntity
{
    public string RemarkTypeName { get; set; } = string.Empty;
}
