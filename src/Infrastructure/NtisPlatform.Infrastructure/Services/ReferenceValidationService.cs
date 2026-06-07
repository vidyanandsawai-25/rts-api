using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using System.Linq.Expressions;


namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Centralized service for validating entity references.
/// Checks if an entity is referenced by other entities before allowing deactivation or deletion.
/// </summary>
public class ReferenceValidationService : IReferenceValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, Func<int, CancellationToken, Task<(bool hasReferences, string errorMessage)>>> _validators;

    private static readonly Dictionary<Type, List<(string TableName, Func<ApplicationDbContext, int, IQueryable<object>> Query)>> _referenceConfig;

    static ReferenceValidationService()
    {
        var config = new ReferenceValidationConfiguration();
        config.ForEntity<AssessmentYearRangeCVEntity>()
            .CheckReferences(
                ("Age Factor CV Master", (ctx, id) => ctx.AgeFactorCVMasters.Where(a => a.YearRangeCVId == id).Cast<object>()),
                ("Floor Factor CV Master", (ctx, id) => ctx.FloorFactorCVMasters.Where(f => f.YearRangeCVId == id).Cast<object>()),
                ("Nature Factor CV Master", (ctx, id) => ctx.NatureFactorCVMasters.Where(n => n.YearRangeCVId == id).Cast<object>()),
                ("Tax Percentage Master CV", (ctx, id) => ctx.TaxPercentageMasterCVs.Where(t => t.YearRangeCVId == id).Cast<object>()),
                ("Use Factor CV Master", (ctx, id) => ctx.UseFactorCVMaster.Where(u => u.YearRangeCVId == id).Cast<object>())
            );
        config.ForEntity<SubFloorEntity>()
            .CheckReferences(
                ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(p => p.SubFloorId == id).Cast<object>()),
                ("Property Details Reassessment", (ctx, id) => ctx.PropertyDetailsReassessment.Where(r => r.SubFloorId == id).Cast<object>())
            );
        config.ForEntity<ConstructionTypeEntity>()
            .CheckReferences(
                ("Rates", (ctx, id) => ctx.RateEntity.Where(r => r.ConstructionTypeId == id).Cast<object>()),
                ("Age Factors", (ctx, id) => ctx.AgeFactorCVMasters.Where(a => a.ConstructionTypeId == id).Cast<object>()),
                ("Nature Factors", (ctx, id) => ctx.NatureFactorCVMasters.Where(n => n.ConstructionTypeId == id).Cast<object>()),
                ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(p => p.ConstructionTypeId == id).Cast<object>()),
                ("Depreciation Master", (ctx, id) => ctx.DepreciationMaster.Where(p => p.ConstructionTypeId == id).Cast<object>()),
                ("Property Details Reassessment", (ctx, id) => ctx.PropertyDetailsReassessment.Where(r => r.ConstructionTypeId == id).Cast<object>())
            );
        config.ForEntity<TaxZoneEntity>()
            .CheckReferences(
                ("Rates", (ctx, id) => ctx.RateEntity.Where(r => r.TaxZoneId == id).Cast<object>()),
                ("Properties", (ctx, id) => ctx.PropertyMast.Where(p => p.TaxZoneId == id).Cast<object>())
            );
        config.ForEntity<FloorEntity>()
            .CheckReferences(
                ("Floor Factors", (ctx, id) => ctx.FloorFactorCVMasters.Where(f => f.FloorId == id).Cast<object>()),
                ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(p => p.FloorId == id).Cast<object>()),
                ("Property Details Reassessment", (ctx, id) => ctx.PropertyDetailsReassessment.Where(r => r.FloorId == id).Cast<object>()),
                ("Rates", (ctx, id) => ctx.RateEntity.Where(r => r.FloorId == id).Cast<object>())
            );
        config.ForEntity<AssessmentYearRangeEntity>()
            .CheckReferences(
                ("Depreciation Master", (ctx, id) => ctx.DepreciationMaster.Where(d => d.YearRangeRVId == id).Cast<object>()),
                ("Rate Master", (ctx, id) => ctx.RateEntity.Where(r => r.YearRangeRVId == id).Cast<object>()),
                ("Tax Percentage Master RV", (ctx, id) => ctx.TaxPercentageMasterRVs.Where(t => t.YearRangeRVId == id).Cast<object>())
            );
        config.ForEntity<RateSectionEntity>()
            .CheckReferences(
                ("Rate Section Details", (ctx, id) => ctx.RateSectionDetails.Where(d => d.RateSectionId == id).Cast<object>()),
                ("Rate Master", (ctx, id) => ctx.RateEntity.Where(r => r.RateSectionId == id).Cast<object>())              
            );
        config.ForEntity<WardEntity>()
           .CheckReferences(
               ("Block Master", (ctx, id) => ctx.BlockMasters.Where(d => d.WardId == id).Cast<object>()),
               ("Property Mast", (ctx, id) => ctx.PropertyMast.Where(r => r.WardId == id).Cast<object>()),
               ("Rate Section Details", (ctx, id) => ctx.RateSectionDetails.Where(r => r.WardId == id).Cast<object>())
           );
        config.ForEntity<ZoneEntity>()
          .CheckReferences(
              ("Ward Master", (ctx, id) => ctx.WardMaster.Where(d => d.ZoneId == id).Cast<object>())        
          );
        config.ForEntity<TypeOfUseGroupEntity>()
         .CheckReferences(
             ("Rate Master", (ctx, id) => ctx.RateEntity.Where(d => d.TypeOfUseGroupId == id).Cast<object>()),
             ("Type Of Use Master", (ctx, id) => ctx.TypeOfUse.Where(d => d.TypeOfUseGroupId == id).Cast<object>())
         );
        config.ForEntity<TypeOfUseEntity>()
         .CheckReferences(
             ("Parking Type Master", (ctx, id) => ctx.ParkingTypeMaster.Where(d => d.TypeOfUseId == id).Cast<object>()),
             ("Property Description And TypeOfUseValidation", (ctx, id) => ctx.PropertyDescriptionAndTypeOfUseValidations.Where(d => d.TypeOfUseId == id).Cast<object>()),
             ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(d => d.TypeOfUseId == id).Cast<object>()),
             ("Property Details Reassessment", (ctx, id) => ctx.PropertyDetailsReassessment.Where(d => d.TypeOfUseId == id).Cast<object>()),
             ("SubType Of Use Master", (ctx, id) => ctx.SubTypeOfUse.Where(d => d.TypeOfUseId == id).Cast<object>()),
             ("Tax PercentageMaster CV", (ctx, id) => ctx.TaxPercentageMasterCVs.Where(d => d.TypeOfUseId == id).Cast<object>()),
             ("Tax PercentageMaster RV", (ctx, id) => ctx.TaxPercentageMasterRVs.Where(d => d.TypeOfUseId == id).Cast<object>()),
             ("Use Factor CV Master", (ctx, id) => ctx.UseFactorCVMaster.Where(d => d.TypeOfUseId == id).Cast<object>())
         );
       config.ForEntity<SubTypeOfUseEntity>()
        .CheckReferences(         
           ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(d => d.SubTypeOfUseId == id).Cast<object>()),
           ("Property Details Reassessment", (ctx, id) => ctx.PropertyDetailsReassessment.Where(d => d.SubTypeOfUseId == id).Cast<object>()),           
           ("Use Factor CV Master", (ctx, id) => ctx.UseFactorCVMaster.Where(d => d.SubTypeOfUseId == id).Cast<object>())
        );

        config.ForEntity<SocietyDetailsEntity>()
        .CheckReferences(
           ("Property Master", (ctx, id) => ctx.PropertyMast.Where(p => p.SocietyDetailId == id).Cast<object>())
        );

        config.ForEntity<AssetCategoryEntity>()
        .CheckReferences(
            ("Asset Type Master", (ctx, id) => ctx.Set<AssetTypeEntity>().Where(a => a.CategoryId == id).Cast<object>()),
            ("Asset Document Definition", (ctx, id) => ctx.AssetDocumentDefinitions.Where(d => d.AssetCategoryId == id).Cast<object>()),
            ("Asset Field Definition", (ctx, id) => ctx.AssetFieldDefinitions.Where(f => f.AssetCategoryId == id).Cast<object>())
        );
        // Inventory Item Category - referenced by InventoryItemName and InventoryItemCondition
        config.ForEntity<InventoryItemCategoryEntity>()
         .CheckReferences(
            ("Inventory Item Name Master", (ctx, id) => ctx.InventoryItemName.Where(i => i.InventoryItemCategoryId == id && i.IsActive).Cast<object>()),
            ("Inventory Item Condition Master", (ctx, id) => ctx.InventoryItemCondition.Where(i => i.InventoryItemCategoryId == id && i.IsActive).Cast<object>())
         );
        // Inventory Item Name - referenced by InventoryItemModel
        config.ForEntity<InventoryItemNameEntity>()
          .CheckReferences(
              ("Inventory Item Model Master", (ctx, id) => ctx.InventoryItemModelMaster.Where(i => i.InventoryItemNameId == id && i.IsActive).Cast<object>())
          );

        config.ForEntity<ScreenEntity>()
        .CheckReferences(
            ("Screen Form Section Master", (ctx, id) => ctx.ScreenFormSectionMaster.Where(s => s.ScreenId == id && s.IsActive).Cast<object>()),
            ("Screen Form Field Master", (ctx, id) => ctx.ScreenFormFieldMaster.Where(f => f.ScreenId == id && f.IsActive).Cast<object>()),
            ("Child Screens", (ctx, id) => ctx.AssetScreen.Where(s => s.ParentScreenId == id && s.IsActive).Cast<object>())
        );

        // ScreenFormSection - referenced by ScreenFormField and child sections
        config.ForEntity<ScreenFormSectionMasterEntity>()
            .CheckReferences(
                ("Screen Form Field Master", (ctx, id) => ctx.ScreenFormFieldMaster.Where(f => f.SectionId == id && f.IsActive).Cast<object>()),
                ("Child Sections", (ctx, id) => ctx.ScreenFormSectionMaster.Where(s => s.ParentSectionId == id && s.IsActive).Cast<object>())
            );

        // ScreenFormField - referenced by child fields
        config.ForEntity<ScreenFormFieldMasterEntity>()
            .CheckReferences(
                ("Child Fields", (ctx, id) => ctx.ScreenFormFieldMaster.Where(f => f.ParentFieldId == id && f.IsActive).Cast<object>())
            );

        config.ForEntity<PropertyAssessmentStatusEntity>()
            .CheckReferences(
                ("Property Master", (ctx, id) => ctx.PropertyMast.Where(p => p.PropertyAssessmentStatusId == id).Cast<object>())
            );		
			
        config.ForEntity<AssetTypeEntity>()
        .CheckReferences(
            ("Asset Document Definition", (ctx, id) => ctx.AssetDocumentDefinitions.Where(d => d.AssetTypeId == id).Cast<object>()),
            ("Asset Field Definition", (ctx, id) => ctx.AssetFieldDefinitions.Where(f => f.AssetTypeId == id).Cast<object>())
        );

        config.ForEntity<AssetAuthorityMasterEntity>()
        .CheckReferences(
            ("Asset Organization Master", (ctx, id) => ctx.AssetOrganizationMasters.Where(o => o.AuthorityId == id).Cast<object>())
        );

        _referenceConfig = config.Build();
    }

    public ReferenceValidationService(ApplicationDbContext context)
    {
        _context = context;
        _validators = new Dictionary<Type, Func<int, CancellationToken, Task<(bool, string)>>>();

        foreach (var kvp in _referenceConfig)
        {
            _validators[kvp.Key] = async (id, ct) =>
            {
                var referencingTables = new List<string>();
                foreach (var (tableName, queryFunc) in kvp.Value)
                {
                    var any = await queryFunc(_context, id).AnyAsync(ct);
                    if (any)
                        referencingTables.Add(tableName);
                }
                if (referencingTables.Any())
                {
                    var entityName = kvp.Key.Name.Replace("Entity", string.Empty);
                    return (true, $"Cannot deactivate/delete this {entityName} because it is referenced in: {string.Join(", ", referencingTables)}");
                }
                return (false, string.Empty);
            };
        }
    }

    public async Task<ValidationResult> ValidateReferencesAsync<TEntity>(int entityId, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity
    {
        var entityType = typeof(TEntity);
        if (!_validators.TryGetValue(entityType, out var validator))
        {
            // No validation configured for this entity type - allow operation
            return ValidationResult.Success();
        }
        var (hasReferences, errorMessage) = await validator(entityId, cancellationToken);
        if (hasReferences)
        {
            return ValidationResult.Failure(errorMessage);
        }
        return ValidationResult.Success();
    }

    /// <summary>
    /// retrieves all foreign key constraints for an entity.
    /// </summary>    
    
    public async Task<List<string>> GetReferencingTablesWithDataAsync<TEntity, TKey>(
    TKey referencedId,
    string referencedColumnName = "Id",
    CancellationToken cancellationToken = default) // Added cancellationToken parameter
    where TEntity : class
    {
        var model = _context.Model;

        var entityType = model.FindEntityType(typeof(TEntity));

        if (entityType == null)
        {
            return new List<string>
        {
            $"Entity type '{typeof(TEntity).Name}' not found."
        };
        }

        var referencingTables = new List<string>();

        // Get all FK relationships referencing this entity
        var foreignKeys = entityType.GetReferencingForeignKeys()
            .Where(fk => fk.PrincipalKey.Properties
                .Any(p => p.Name == referencedColumnName))
            .ToList();

        foreach (var fk in foreignKeys)
        {
            var dependentEntityType = fk.DeclaringEntityType;

            var tableName = dependentEntityType.GetTableName();

            if (string.IsNullOrWhiteSpace(tableName))
                continue;

            // FK property in child entity
            var foreignKeyProperty = fk.Properties.Single();

            var foreignKeyPropertyName = foreignKeyProperty.Name;

            // CLR type of dependent entity
            var clrType = dependentEntityType.ClrType;

            // DbSet<TEntity>
            var dbSet = (IQueryable)_context.GetType()
                .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                .MakeGenericMethod(clrType)
                .Invoke(_context, null)!;

            // x =>
            // EF.Property<TProperty>(x, foreignKeyPropertyName) == referencedId

            var parameter = Expression.Parameter(clrType, "x");

            var propertyType =
                Nullable.GetUnderlyingType(foreignKeyProperty.ClrType)
                ?? foreignKeyProperty.ClrType;

            var propertyMethod = typeof(EF)
                .GetMethod(nameof(EF.Property))!
                .MakeGenericMethod(foreignKeyProperty.ClrType);

            var propertyExpression = Expression.Call(
                propertyMethod,
                parameter,
                Expression.Constant(foreignKeyPropertyName));

            // Convert the referencedId to the appropriate property type
            var convertedId = Convert.ChangeType(
                referencedId,
                propertyType);

            // Ensure the constant matches the exact FK CLR type
            // If the FK property type is nullable, wrap the constant in Expression.Convert
            var constantExpression = Expression.Constant(
                convertedId,
                propertyType); // Use the non-nullable propertyType here

            // Wrap in Expression.Convert if the FK property type is nullable
            var equalExpression = Expression.Equal(
                propertyExpression,
                foreignKeyProperty.ClrType.IsGenericType &&
                foreignKeyProperty.ClrType.GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? Expression.Convert(constantExpression, foreignKeyProperty.ClrType) // Convert to nullable type if needed
                    : constantExpression);

            var lambda = Expression.Lambda(
                equalExpression,
                parameter);

            // AnyAsync<TEntity>(source, predicate, cancellationToken)

            var anyAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m =>
                    m.Name == nameof(EntityFrameworkQueryableExtensions.AnyAsync)
                    && m.GetParameters().Length == 3)
                .MakeGenericMethod(clrType);

            var task = (Task<bool>)anyAsyncMethod.Invoke(
                null,
                new object[]
                {
                dbSet,
                lambda,
                cancellationToken 
                })!;

            var exists = await task;

            if (exists)
            {
                referencingTables.Add(tableName);
            }
        }

        return referencingTables
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }
}