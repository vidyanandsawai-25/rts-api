using AutoMapper;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings.RuleEngineMappings
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
