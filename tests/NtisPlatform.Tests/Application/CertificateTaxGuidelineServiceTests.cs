using System.Globalization;
using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.CertificateTaxGuideline;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class CertificateTaxGuidelineServiceTests
{
    private readonly Mock<IRepository<CertificateTaxGuidelineEntity, int>> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<IReferenceValidationService> _referenceValidator;
    private readonly CertificateTaxGuidelineService _service;

    public CertificateTaxGuidelineServiceTests()
    {
        _repository = new Mock<IRepository<CertificateTaxGuidelineEntity, int>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();
        _referenceValidator = new Mock<IReferenceValidationService>();

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new CertificateTaxGuidelineService(
            _repository.Object,
            _unitOfWork.Object,
            _mapper.Object,
            _referenceValidator.Object);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var existing = CreateEntity(1, isActive: true);
        var updateDto = new UpdateCertificateTaxGuidelineDto
        {
            GuidelineCode = existing.GuidelineCode,
            GuidelineName = existing.GuidelineName,
            IsActive = false,
            DataType = "VARCHAR",
            GuidelineValue = "New Value"
        };

        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _repository.Setup(r => r.GetQueryable()).Returns(new List<CertificateTaxGuidelineEntity> { existing }.BuildMock());
        _referenceValidator.Setup(v => v.ValidateReferencesAsync<CertificateTaxGuidelineEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));

        _mapper.Setup(m => m.Map(updateDto, existing))
            .Returns(existing)
            .Callback<UpdateCertificateTaxGuidelineDto, CertificateTaxGuidelineEntity>((dto, entity) => entity.IsActive = dto.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var existing = CreateEntity(1);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _referenceValidator.Setup(v => v.ValidateReferencesAsync<CertificateTaxGuidelineEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_NoReferences_Succeeds()
    {
        var existing = CreateEntity(1);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _referenceValidator.Setup(v => v.ValidateReferencesAsync<CertificateTaxGuidelineEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _repository.Setup(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
    }

    [Theory]
    [InlineData("BIT", "true", true)]
    [InlineData("BIT", "1", true)]
    [InlineData("INT", "42", 42)]
    [InlineData("DECIMAL", "12.34", 12.34)]
    [InlineData("VARCHAR", "hello", "hello")]
    public async Task GetGuidelineValueAsync_ParsesCorrectly(string dataType, string rawValue, object expectedParsed)
    {
        var existing = new CertificateTaxGuidelineEntity
        {
            GuidelineCode = "TEST_CODE",
            DataType = dataType,
            GuidelineValue = rawValue,
            IsActive = true
        };

        _repository.Setup(r => r.GetQueryable()).Returns(new List<CertificateTaxGuidelineEntity> { existing }.BuildMock());

        var result = await _service.GetGuidelineValueAsync("TEST_CODE", CancellationToken.None);

        if (dataType == "DECIMAL")
        {
            Assert.Equal(Convert.ToDecimal(expectedParsed, CultureInfo.InvariantCulture), Assert.IsType<decimal>(result));
        }
        else if (expectedParsed is int expectedInt)
        {
            Assert.Equal(expectedInt, Convert.ToInt32(result));
        }
        else
        {
            Assert.Equal(expectedParsed, result);
        }
    }

    private static CertificateTaxGuidelineEntity CreateEntity(int id, bool isActive = true)
    {
        return new CertificateTaxGuidelineEntity
        {
            Id = id,
            GuidelineCode = $"G{id}",
            GuidelineName = $"Guideline {id}",
            IsActive = isActive,
            DataType = "VARCHAR",
            GuidelineValue = "Val",
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };
    }
}
