using AutoMapper;
using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Mappings.Rules
{
    /// <summary>
    /// AutoMapper profile for RuleVersionHistory entity and DTOs
    /// </summary>
    public class RuleVersionHistoryMappingProfile : Profile
    {
        public RuleVersionHistoryMappingProfile()
        {
            // Entity to DTO mapping
            CreateMap<RuleVersionHistoryEntity, RuleVersionHistoryDto>();
        }
    }
}
