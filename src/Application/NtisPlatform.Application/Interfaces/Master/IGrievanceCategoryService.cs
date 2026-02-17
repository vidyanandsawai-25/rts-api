using NtisPlatform.Application.DTOs.Master.GrievanceCategoryMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master
{
    /// <summary>
    /// Service interface for Grievance Category Master CRUD operations
    /// </summary>
    public interface IGrievanceCategoryService : ICommonCrudService<GrievanceCategoryEntity, GrievanceCategoryDto, CreateGrievanceCategoryDto, UpdateGrievanceCategoryDto, GrievanceCategoryQueryParameters, int>
    {
    }
}
