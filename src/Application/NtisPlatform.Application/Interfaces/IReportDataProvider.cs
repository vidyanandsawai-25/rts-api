namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// One implementation per report type. ProviderCode must match
/// ReportDefinitionEntity.DataProviderCode stored in the DB.
/// Adding a new report = one new class implementing this interface + one DI registration.
/// </summary>
public interface IReportDataProvider
{
    string ProviderCode { get; }
    Task<object> GetDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default);
}