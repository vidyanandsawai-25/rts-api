using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.PropertyChangeCategory;

public class PropertyChangeCategoryQueryParameters : BaseQueryParameters
{
    public int PropertyId { get; set; }
}
