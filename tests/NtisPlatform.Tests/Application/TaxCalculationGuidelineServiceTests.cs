using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class TaxCalculationGuidelineServiceTests
{
    private readonly Mock<IRepository<TaxCalculationGuidelineEntity, int>> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<IReferenceValidationService> _referenceValidator;
    private readonly TaxCalculationGuidelineService _service;

    public TaxCalculationGuidelineServiceTests()
    {
        _repository = new Mock<IRepository<TaxCalculationGuidelineEntity, int>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();
        _referenceValidator = new Mock<IReferenceValidationService>();

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new TaxCalculationGuidelineService(
            _repository.Object,
            _unitOfWork.Object,
            _mapper.Object,
            _referenceValidator.Object);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var existing = CreateEntity(1, isActive: true);
        var updateDto = new UpdateTaxCalculationGuidelineDto
        {
            GuidelineCode = existing.GuidelineCode,
            GuidelineName = existing.GuidelineName,
            IsActive = false,
            DatePriority1 = "RETROSPECTIVE",
            DatePriority2 = "ELECTRIC_BILL",
            DatePriority3 = "CC",
            DatePriority4 = "OC",
            IgnoreCCToOCIfWithinType = "MONTHS",
            ElectricBillDateRule = "NO_TAX",
            NoDateRule = "DEFAULT_RETROSPECTIVE",
            FloorCertificatePriority = "PROPERTY_OVERRIDES_FLOOR",
            ProrationMethod = "FULL_YEAR",
            TaxPersistenceMode = "PROPERTY_AGGREGATED"
        };

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.GetQueryable()).Returns(new List<TaxCalculationGuidelineEntity> { existing }.BuildMock());
        _referenceValidator.Setup(v => v.ValidateReferencesAsync<TaxCalculationGuidelineEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));

        _mapper.Setup(m => m.Map(updateDto, existing))
            .Returns(existing)
            .Callback<UpdateTaxCalculationGuidelineDto, TaxCalculationGuidelineEntity>((dto, entity) => entity.IsActive = dto.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var existing = CreateEntity(1);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _referenceValidator.Setup(v => v.ValidateReferencesAsync<TaxCalculationGuidelineEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_NoReferences_Succeeds()
    {
        var existing = CreateEntity(1);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _referenceValidator.Setup(v => v.ValidateReferencesAsync<TaxCalculationGuidelineEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _repository.Setup(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
    }

    private static TaxCalculationGuidelineEntity CreateEntity(int id, bool isActive = true)
    {
        return new TaxCalculationGuidelineEntity
        {
            Id = id,
            GuidelineCode = $"G{id}",
            GuidelineName = $"Guideline {id}",
            IsActive = isActive,
            DatePriority1 = "RETROSPECTIVE",
            DatePriority2 = "ELECTRIC_BILL",
            DatePriority3 = "CC",
            DatePriority4 = "OC",
            IgnoreCCToOCIfWithinType = "MONTHS",
            ElectricBillDateRule = "NO_TAX",
            NoDateRule = "DEFAULT_RETROSPECTIVE",
            FloorCertificatePriority = "PROPERTY_OVERRIDES_FLOOR",
            ProrationMethod = "FULL_YEAR",
            TaxPersistenceMode = "PROPERTY_AGGREGATED",
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };
    }
}
