namespace NtisPlatform.Application.DTOs.PropertyComparison;

public class PropertyComparisonDto
{
    public string OldPropertyIds { get; set; } = string.Empty;
    public int NewPropertyId { get; set; }
    public AreaComparisonDto Area { get; set; } = new();
    public ChangeOfUseDto ChangeOfUse { get; set; } = new();
    public ValueComparisonDto RV { get; set; } = new();
    public ValueComparisonDto ALV { get; set; } = new();
    public ValueComparisonDto Tax { get; set; } = new();
}

public class AreaComparisonDto
{
    private decimal _old;
    private decimal _new;

    public decimal Old
    {
        get => _old;
        set => _old = decimal.Round(value, 2);
    }

    public decimal New
    {
        get => _new;
        set => _new = decimal.Round(value, 2);
    }

    public decimal Change => decimal.Round(_new - _old, 2);
    public string Unit { get; set; } = "SqMeter";
}

public class ChangeOfUseDto
{
    public bool HasChanged { get; set; }
    public string OldUse { get; set; } = string.Empty;
    public string NewUse { get; set; } = string.Empty;
}

public class ValueComparisonDto
{
    private decimal _old;
    private decimal _new;

    public decimal Old
    {
        get => _old;
        set => _old = decimal.Round(value, 2);
    }

    public decimal New
    {
        get => _new;
        set => _new = decimal.Round(value, 2);
    }

    public decimal Change => decimal.Round(_new - _old, 2);
    public decimal ChangePercent => _old > 0 ? decimal.Round((_new - _old) / _old * 100, 2) : 0;
}
