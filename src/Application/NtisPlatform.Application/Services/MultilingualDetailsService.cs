using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class MultilingualDetailsService
    : BaseCommonCrudService<
        MultilingualDetailsEntity,
        MultilingualDetailsDtos,
        CreateMultilingualDetailsDtos,
        UpdateMultilingualDetailsDtos,
        MultilingualDetailsQueryParameters,
        int>,
      IMultilingualDetailsService
{
    private readonly IRepository<MultilingualDetailsEntity, int> _repository;
    private readonly IMapper _mapper;

    public MultilingualDetailsService(
        IRepository<MultilingualDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<MultilingualDetailsDtos>> GetAllForLocalizationAsync(
        string resource,
        string culture,
        CancellationToken ct)
    {
        var query = _repository.GetQueryable();

        var rows = await query.AsNoTracking()
            .Where(x => x.Resource == resource && x.Culture == culture)
            .ToListAsync(ct);

        return _mapper.Map<List<MultilingualDetailsDtos>>(rows);
    }
}

