using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Tests.Application;

#region ActiveTaxesEntity Tests

public class ActiveTaxesEntityTests
{
    [Fact]
    public void ActiveTaxesEntity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new ActiveTaxesEntity
        {
            Id = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            DisplayOrder = 1,
            TaxOnUnit = true,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("GeneralTax", entity.TaxName);
        Assert.Equal("General Tax", entity.TaxNameAlias);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.TaxOnUnit);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void ActiveTaxesEntity_TaxNameAlias_CanBeNull()
    {
        var entity = new ActiveTaxesEntity
        {
            Id = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = null
        };

        Assert.Null(entity.TaxNameAlias);
    }

    [Fact]
    public void ActiveTaxesEntity_TaxName_CanBeNull()
    {
        var entity = new ActiveTaxesEntity
        {
            Id = 1,
            TaxName = null
        };

        Assert.Null(entity.TaxName);
    }

    [Fact]
    public void ActiveTaxesEntity_InheritsFromCommonBaseEntity()
    {
        var entity = new ActiveTaxesEntity();
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }
}

#endregion

#region ActiveTaxesDto Tests

public class ActiveTaxesDtoTests
{
    [Fact]
    public void ActiveTaxesDto_Properties_GetSet_WorksCorrectly()
    {
        var dto = new ActiveTaxesDto
        {
            Id = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            DisplayOrder = 1,
            TaxOnUnit = true,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("GeneralTax", dto.TaxName);
        Assert.Equal("General Tax", dto.TaxNameAlias);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.TaxOnUnit);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void ActiveTaxesDto_InheritsFromBaseDtos()
    {
        var dto = new ActiveTaxesDto();
        Assert.IsAssignableFrom<BaseDtos>(dto);
    }
}

#endregion

#region CreateActiveTaxesDto Tests

public class CreateActiveTaxesDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreateActiveTaxesDto_ValidData_PassesValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            DisplayOrder = 1,
            TaxOnUnit = false,
            CreatedBy = 1
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateActiveTaxesDto_TaxNameMaxLength_FailsValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = new string('X', 201),
            TaxNameAlias = "Alias"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_TaxName_MaxLen_200");
    }

    [Fact]
    public void CreateActiveTaxesDto_TaxNameAliasMaxLength_FailsValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = new string('Y', 201)
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_TaxNameAlias_MaxLen_200");
    }

    [Fact]
    public void CreateActiveTaxesDto_DisplayOrderRange_FailsValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            DisplayOrder = 0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_DisplayOrder_Range");
    }

    [Fact]
    public void CreateActiveTaxesDto_InheritsFromCreateCommonBaseDtos()
    {
        var dto = new CreateActiveTaxesDto();
        Assert.IsAssignableFrom<CreateBaseDtos>(dto);
    }
}

#endregion

#region UpdateActiveTaxesDto Tests

public class UpdateActiveTaxesDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UpdateActiveTaxesDto_ValidData_PassesValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax Updated",
            DisplayOrder = 2,
            TaxOnUnit = true,
            UpdatedBy = 1,
            IsActive = true
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_TaxNameMaxLength_FailsValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = new string('X', 201),
            TaxNameAlias = "Alias"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_InheritsFromUpdateCommonBaseDtos()
    {
        var dto = new UpdateActiveTaxesDto();
        Assert.IsAssignableFrom<UpdateBaseDtos>(dto);
    }
}

#endregion

#region ActiveTaxesService Tests

public class ActiveTaxesServiceTests
{
    private readonly Mock<IRepository<ActiveTaxesEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ActiveTaxesService _service;

    public ActiveTaxesServiceTests()
    {
        _mockRepository = new Mock<IRepository<ActiveTaxesEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new ActiveTaxesService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new ActiveTaxesEntity
        {
            Id = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            DisplayOrder = 1,
            TaxOnUnit = false,
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<ActiveTaxesDto>(It.IsAny<ActiveTaxesEntity>()))
            .Returns(new ActiveTaxesDto
            {
                Id = 1,
                TaxName = "GeneralTax",
                TaxNameAlias = "General Tax",
                DisplayOrder = 1,
                TaxOnUnit = false
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("GeneralTax", result.TaxName);
        Assert.Equal("General Tax", result.TaxNameAlias);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<ActiveTaxesEntity>
        {
            new() { Id = 1, TaxName = "GeneralTax", TaxNameAlias = "General", DisplayOrder = 1, TaxOnUnit = false, IsActive = true },
            new() { Id = 2, TaxName = "RoadCess", TaxNameAlias = "Road", DisplayOrder = 2, TaxOnUnit = true, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ActiveTaxesMappingProfile>();
        });
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new ActiveTaxesService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var qp = new ActiveTaxesQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.Id == 1);
        Assert.Contains(result.Items, x => x.Id == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            DisplayOrder = 1,
            TaxOnUnit = false
        };

        _mockMapper
            .Setup(m => m.Map<ActiveTaxesEntity>(It.IsAny<CreateActiveTaxesDto>()))
            .Returns((CreateActiveTaxesDto dto) => new ActiveTaxesEntity
            {
                TaxName = dto.TaxName,
                TaxNameAlias = dto.TaxNameAlias,
                DisplayOrder = dto.DisplayOrder,
                TaxOnUnit = dto.TaxOnUnit,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<ActiveTaxesDto>(It.IsAny<ActiveTaxesEntity>()))
            .Returns((ActiveTaxesEntity e) => new ActiveTaxesDto
            {
                Id = e.Id,
                TaxName = e.TaxName,
                TaxNameAlias = e.TaxNameAlias,
                DisplayOrder = e.DisplayOrder,
                TaxOnUnit = e.TaxOnUnit
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("GeneralTax", result.TaxName);
        Assert.Equal("General Tax", result.TaxNameAlias);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax Updated",
            DisplayOrder = 2,
            TaxOnUnit = true
        };

        var existingEntity = new ActiveTaxesEntity
        {
            Id = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            DisplayOrder = 1,
            TaxOnUnit = false,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateActiveTaxesDto>(), It.IsAny<ActiveTaxesEntity>()))
            .Callback((UpdateActiveTaxesDto src, ActiveTaxesEntity dest) =>
            {
                dest.TaxNameAlias = src.TaxNameAlias;
                dest.DisplayOrder = src.DisplayOrder;
                dest.TaxOnUnit = src.TaxOnUnit;
            });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("General Tax Updated", existingEntity.TaxNameAlias);
        Assert.Equal(2, existingEntity.DisplayOrder);
        Assert.True(existingEntity.TaxOnUnit);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        var updateDto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "Alias"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesEntity?)null);

        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesEntity?)null);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        var idToDelete = 1;

        var existingEntity = new ActiveTaxesEntity
        {
            Id = idToDelete,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
