using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Services.CommonDetails;

/// <summary>
/// The entity a bulk update targets: its CLR type and the column used to match a property.
/// </summary>
/// <param name="EntityType">EF entity type backing the configured reference table.</param>
/// <param name="KeyProperty">
/// Column used to select the rows for a property — <c>Id</c> for the property table itself,
/// <c>PropertyId</c> for child tables (which may have several rows per property).
/// </param>
public readonly record struct BulkUpdateTarget(Type EntityType, string KeyProperty);

/// <summary>
/// The single place that maps a <c>BulkUpdateMaster.ReferenceTableName</c> to the entity to update.
/// Centralizing this here keeps the table names out of the data-access code and replaces what used to
/// be a hardcoded allow-list plus three duplicated table-dispatch switches in the service.
/// The set of keys also doubles as the allow-list: a reference table absent from this map is rejected.
/// </summary>
public static class BulkUpdateTargetRegistry
{
    private static readonly IReadOnlyDictionary<string, BulkUpdateTarget> Map =
        new Dictionary<string, BulkUpdateTarget>(StringComparer.OrdinalIgnoreCase)
        {
            ["PTIS.PropertyMast"]        = new(typeof(PropertyEntity),           "Id"),
            ["PTIS.SocietyDetailsMast"]  = new(typeof(SocietyDetailsEntity),     "PropertyId"),
            ["PTIS.PropertyMastDetails"] = new(typeof(PropertyAssessmentEntity), "PropertyId"),
            ["PTIS.PropertyDetails"]     = new(typeof(PropertyDetailsEntity),    "PropertyId"),
            ["PTIS.PropertySocialDetails"] = new(typeof(PropertySocialDetailsEntity), "PropertyId"),
        };

    /// <summary>
    /// Resolves the target entity/key for a reference table name. Returns false for any table not in
    /// the map (the caller turns that into a clear "unrecognized table" error).
    /// </summary>
    public static bool TryResolve(string referenceTableName, out BulkUpdateTarget target) =>
        Map.TryGetValue(referenceTableName, out target);

    /// <summary>
    /// True when the reference table is keyed by its own <c>Id</c> (the property table itself) rather
    /// than by a <c>PropertyId</c> foreign key — i.e. the property row is its own update source.
    /// </summary>
    public static bool IsPropertyKeyedById(in BulkUpdateTarget target) =>
        string.Equals(target.KeyProperty, "Id", StringComparison.OrdinalIgnoreCase);
}   
