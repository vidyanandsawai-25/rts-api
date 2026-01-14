using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FilterableAttribute : Attribute
{
    public FilterOperator Operator { get; set; } = FilterOperator.Equals;
    public string? EntityProperty { get; set; }

    public FilterableAttribute()
    {
    }

    public FilterableAttribute(FilterOperator operatorType)
    {
        Operator = operatorType;
    }
}
