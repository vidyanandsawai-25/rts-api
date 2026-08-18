using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using OperationType = NtisPlatform.Application.Enums.OperationType;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveRuleEvidenceConditionService : BaseCommonCrudService<RetrospectiveRuleEvidenceConditionEntity, RetrospectiveRuleEvidenceConditionDto, CreateRetrospectiveRuleEvidenceConditionDto, UpdateRetrospectiveRuleEvidenceConditionDto, RetrospectiveRuleEvidenceConditionQueryParameters, int>, IRetrospectiveRuleEvidenceConditionService
{
    private readonly IRepository<EvidenceTypeMasterEntity, int> _evidenceTypeRepository;

    public RetrospectiveRuleEvidenceConditionService(
        IRepository<RetrospectiveRuleEvidenceConditionEntity, int> repository,
        IRepository<EvidenceTypeMasterEntity, int> evidenceTypeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _evidenceTypeRepository = evidenceTypeRepository;
    }

    public async Task<List<RetrospectiveRuleEvidenceConditionStateDto>> GetEvidenceStateForRuleAsync(
        int ruleId, CancellationToken cancellationToken = default)
    {
        var evidenceTypes = await _evidenceTypeRepository.GetQueryable()
            .Where(e => e.IsActive)
            .OrderBy(e => e.DisplayOrder)
            .ToListAsync(cancellationToken);

        var selectedStateByEvidenceTypeId = await _repository.GetQueryable()
            .Where(c => c.RuleId == ruleId && c.IsActive)
            .ToDictionaryAsync(c => c.EvidenceTypeId, c => c.EvidenceState, cancellationToken);

        return evidenceTypes.Select(e => new RetrospectiveRuleEvidenceConditionStateDto
        {
            EvidenceTypeId = e.Id,
            EvidenceCode = e.EvidenceCode,
            EvidenceName = e.EvidenceName,
            DisplayOrder = e.DisplayOrder,
            SelectedState = selectedStateByEvidenceTypeId.TryGetValue(e.Id, out var state) ? state : null
        }).ToList();
    }

    public async Task<List<RetrospectiveRuleEvidenceConditionStateDto>> SetEvidenceStateForRuleAsync(
        int ruleId, SetRetrospectiveRuleEvidenceConditionStateDto request, CancellationToken cancellationToken = default)
    {
        var overlap = request.AvailableEvidenceTypeIds.Intersect(request.UnavailableEvidenceTypeIds).ToList();
        if (overlap.Count > 0)
        {
            var validationResult = ValidationResult.Failure(
                nameof(request.UnavailableEvidenceTypeIds),
                $"Evidence type(s) {string.Join(", ", overlap)} cannot be checked in both the Available and Unavailable panels.");
            throw new ValidationException(
                "An evidence type cannot be both available and unavailable for the same rule.",
                validationResult.ToDictionary(), OperationType.Update);
        }

        var desired = request.AvailableEvidenceTypeIds.Select(id => (EvidenceTypeId: id, State: "AVAILABLE"))
            .Concat(request.UnavailableEvidenceTypeIds.Select(id => (EvidenceTypeId: id, State: "UNAVAILABLE")))
            .ToList();
        var desiredIds = desired.Select(d => d.EvidenceTypeId).ToHashSet();

        var existingConditions = await _repository.GetQueryable()
            .Where(c => c.RuleId == ruleId && c.IsActive)
            .ToListAsync(cancellationToken);
        var existingByEvidenceTypeId = existingConditions.ToDictionary(c => c.EvidenceTypeId);

        foreach (var (evidenceTypeId, state) in desired)
        {
            if (existingByEvidenceTypeId.TryGetValue(evidenceTypeId, out var existing))
            {
                if (existing.EvidenceState != state)
                {
                    existing.EvidenceState = state;
                    existing.UpdatedBy = request.UpdatedBy;
                    existing.UpdatedDate = DateTime.Now;
                    await _repository.UpdateAsync(existing, cancellationToken);
                }
            }
            else
            {
                await _repository.AddAsync(new RetrospectiveRuleEvidenceConditionEntity
                {
                    RuleId = ruleId,
                    EvidenceTypeId = evidenceTypeId,
                    EvidenceState = state,
                    IsActive = true,
                    CreatedBy = request.UpdatedBy,
                    CreatedDate = DateTime.Now
                }, cancellationToken);
            }
        }

        foreach (var existing in existingConditions.Where(c => !desiredIds.Contains(c.EvidenceTypeId)))
        {
            existing.IsActive = false;
            existing.UpdatedBy = request.UpdatedBy;
            existing.UpdatedDate = DateTime.Now;
            await _repository.UpdateAsync(existing, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetEvidenceStateForRuleAsync(ruleId, cancellationToken);
    }
}
