using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Interfaces.Rules;
using NtisPlatform.Core.Models.Rules;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories.Rules;

public class RuleFieldsRepository : IRuleFieldsRepository
{
    private readonly ApplicationDbContext _context;

    public RuleFieldsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RuleFieldDetailsDto>> GetByFieldIdAsync(int RuleScopeId, CancellationToken cancellationToken = default)
    {
        var result = await (from rs in _context.RuleScopeFieldMapping
                            join rf in _context.RulesField on rs.RulesFieldId equals rf.Id
                            join fc in _context.FieldConfiguration on rf.Id equals fc.RulesFieldId into fcGroup
                            from fc in fcGroup.DefaultIfEmpty()
                            where rs.RuleScopeId == RuleScopeId
                                && rs.IsActive
                                && rf.IsActive
                                && (fc == null || fc.IsActive)
                            select new RuleFieldDetailsDto
                            {
                                RulesFieldId = rf.Id,
                                RuleScopeId = rs.RuleScopeId ?? 0,
                                FieldName = rf.FieldName,
                                FieldType = rf.FieldType,
                                DatabaseColumnName = rf.DatabaseColumnName,
                                DataType = fc != null ? fc.DataType : string.Empty,
                                HasApiSource = fc != null && fc.HasApiSource,
                                HasStaticValues = fc != null && fc.HasStaticValues,
                                IsRequired = fc != null && fc.IsRequired,
                                ApiEndpoint = fc != null ? fc.ApiEndpoint ?? string.Empty : string.Empty,
                                InputType = fc != null ? fc.InputType : string.Empty,
                                ApiMethod = fc != null ? fc.ApiMethod ?? string.Empty : string.Empty,
                                ApiParameters = fc != null ? fc.ApiParameters ?? string.Empty : string.Empty,
                                ApiResponseMapping = fc != null ? fc.ApiResponseMapping ?? string.Empty : string.Empty,
                                StaticValuesJson = fc != null ? fc.StaticValuesJson ?? string.Empty : string.Empty,
                                DefaultValue = fc != null ? fc.DefaultValue ?? string.Empty : string.Empty,
                                ValidationRegex = fc != null ? fc.ValidationRegex ?? string.Empty : string.Empty,
                                MinValue = fc != null && fc.MinValue.HasValue ? (double)fc.MinValue.Value : 0,
                                MaxValue = fc != null && fc.MaxValue.HasValue ? (double)fc.MaxValue.Value : 0,
                                MinLength = fc != null && fc.MinLength.HasValue ? fc.MinLength.Value : 0,
                                MaxLength = fc != null && fc.MaxLength.HasValue ? fc.MaxLength.Value : 0,
                                DisplayOrder = rs.DisplayOrder
                            })
                           .ToListAsync(cancellationToken);

        return result;
    }
}
