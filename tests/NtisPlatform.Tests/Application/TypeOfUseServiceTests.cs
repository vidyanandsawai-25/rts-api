using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class TypeOfUseServiceTests
{
    private readonly Mock<IRepository<TypeOfUseEntity, string>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TypeOfUseService _service;

    public TypeOfUseServiceTests()
    {
        _mockRepository = new Mock<IRepository<TypeOfUseEntity, string>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        // Service is calling SaveChangesAsync (NOT transactions), so setup SaveChangesAsync.
        // If your SaveChangesAsync returns Task (not Task<int>), change ReturnsAsync(1) to Returns(Task.CompletedTask).
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Optional: keep these setups if your interface has them (harmless even if not called)
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new TypeOfUseService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new TypeOfUseEntity
        {
            TypeOfUseID = "R",
            Description = "Residential",
            DescriptionEnglish = "Residential",
            Type = "R",
            GroupID = "R",
            SearchKey = "Alt+D",
            Sequence = 1,
            IsSociety = true,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync("R", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<TypeOfUseDto>(It.IsAny<TypeOfUseEntity>()))
            .Returns(new TypeOfUseDto
            {
                TypeOfUseID = "R",
                Description = "Residential",
                DescriptionEnglish = "Residential",
                Type = "R",
                GroupID = "R",
                SearchKey = "Alt+D",
                Sequence = 1,
                IsSociety = true,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

        // Act
        var result = await _service.GetByIdAsync("R");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("R", result.TypeOfUseID);
        Assert.Equal("Residential", result.Description);
        Assert.Equal("Residential", result.DescriptionEnglish);
        Assert.Equal("R", result.Type);
        Assert.Equal("R", result.GroupID);
        Assert.Equal("Alt+D", result.SearchKey);
        Assert.Equal(1, result.Sequence);
        Assert.True(result.IsSociety);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync("ZZZZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseEntity?)null);

        // Act
        var result = await _service.GetByIdAsync("ZZZZ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<TypeOfUseEntity>
        {
            new() { TypeOfUseID = "R", Description = "R", DescriptionEnglish = "Residential", Type="R", GroupID="R", SearchKey="Alt+D",Sequence=1,IsSociety=true, IsActive=true,CreatedBy=31, CreatedDate = DateTime.Now, UpdatedBy=31, UpdatedDate=DateTime.Now },
            new() { TypeOfUseID = "C", Description = "C", DescriptionEnglish = "Commercial", Type="C", GroupID="C", SearchKey="Alt+C",Sequence=2,IsSociety=true,IsActive=true, CreatedBy=31, CreatedDate = DateTime.Now, UpdatedBy=31, UpdatedDate=DateTime.Now }
        };

        var mockQuery = entities.BuildMock(); // async IQueryable
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TypeOfUseEntity, TypeOfUseDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TypeOfUseService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new TypeOfUseQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SearchTerm = null!,
            SortBy = null!
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.TypeOfUseID == "R");
        Assert.Contains(items, x => x.TypeOfUseID == "C");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateTypeOfUseDto
        {
            TypeOfUseID = "R",
            Description = "Residential",
            DescriptionEnglish = "Residential",
            Type = "R",
            GroupID = "R",
            SearchKey = "Alt+D",
            Sequence = 1,
            IsSociety = true,
            IsActive = true,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<TypeOfUseEntity>(It.IsAny<CreateTypeOfUseDto>()))
            .Returns((CreateTypeOfUseDto dto) => new TypeOfUseEntity
            {
                TypeOfUseID = "R",
                Description = "Residential",
                DescriptionEnglish = "Residential",
                Type = "R",
                GroupID = "R",
                SearchKey = "Alt+D",
                Sequence = 1,
                IsSociety = true,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = 31,
                UpdatedDate = DateTime.Now,
                UpdatedBy = 31
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<TypeOfUseDto>(It.IsAny<TypeOfUseEntity>()))
            .Returns((TypeOfUseEntity e) => new TypeOfUseDto
            {
                TypeOfUseID = "R",
                Description = "Residential",
                DescriptionEnglish = "Residential",
                Type = "R",
                GroupID = "R",
                SearchKey = "Alt+D",
                Sequence = 1,
                IsSociety = true,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("R", result.TypeOfUseID);
        Assert.Equal("Residential", result.Description);
        Assert.Equal("Residential", result.DescriptionEnglish);
        Assert.Equal("R", result.Type);
        Assert.Equal("R", result.GroupID);
        Assert.Equal("Alt+D", result.SearchKey);
        Assert.Equal(1, result.Sequence);
        Assert.True(result.IsSociety);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        // Service calls SaveChangesAsync (based on your test output)
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Not called by service (based on your test output)
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseDto
        {
            Description = "Residential",
            DescriptionEnglish = "Residential",
            Type = "R",
            GroupID = "R",
            SearchKey = "Alt+D",
            Sequence = 1,
            IsSociety = true,
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new TypeOfUseEntity
        {
            TypeOfUseID = "R",
            Description = "Old Residential",
            DescriptionEnglish = "Old Residential",
            Type = "RR",
            GroupID = "RR",
            SearchKey = "Alt+B",
            Sequence = 1,
            IsSociety = true,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("R", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTypeOfUseDto>(), It.IsAny<TypeOfUseEntity>()))
            .Callback((UpdateTypeOfUseDto src, TypeOfUseEntity dest) =>
            {
                dest.Description = src.Description;
                dest.DescriptionEnglish = src.DescriptionEnglish;
                dest.Type = src.Type;
                dest.GroupID = src.GroupID;
                dest.SearchKey = src.SearchKey;
                dest.Sequence = src.Sequence;
                dest.IsSociety = src.IsSociety;
            });

        // Act
        await _service.UpdateAsync("R", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync("R", It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("R", existingEntity.TypeOfUseID);
        Assert.Equal("Residential", existingEntity.Description);
        Assert.Equal("Residential", existingEntity.DescriptionEnglish);
        Assert.Equal("R", existingEntity.Type);
        Assert.Equal("R", existingEntity.GroupID);
        Assert.Equal("Alt+D", existingEntity.SearchKey);
        Assert.Equal(1, existingEntity.Sequence);
        Assert.True(existingEntity.IsSociety);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseDto
        {
            Description = "Residential",
            DescriptionEnglish = "Residential",
            Type = "R",
            GroupID = "R",
            SearchKey = "Alt+D",
            Sequence = 1,
            IsSociety = true,
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("ZZZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseEntity?)null);

        // Act
        await _service.UpdateAsync("ZZZ", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = "ZZZ";

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = "R";

        var existingEntity = new TypeOfUseEntity
        {
            TypeOfUseID = idToDelete,
            Description = "Old Residential",
            DescriptionEnglish = "Old Residential",
            Type = "RR",
            GroupID = "RR",
            SearchKey = "Alt+B",
            Sequence = 1,
            IsSociety = true,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

}

