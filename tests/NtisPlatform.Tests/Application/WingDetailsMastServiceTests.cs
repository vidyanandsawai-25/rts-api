using System.Linq;
using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.WingDetailsMast;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class WingDetailsMastServiceTests
{
    private readonly Mock<IRepository<WingDetailsMastEntity, int>> _mockRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository;
    private readonly Mock<IRepository<WingEntity, int>> _mockWingRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyMastOldRepository;
    private readonly Mock<IRepository<PropertyPhotoEntity, int>> _mockPropertyPhotoRepository;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly WingDetailsMastService _service;

    public WingDetailsMastServiceTests()
    {
        _mockRepository = new Mock<IRepository<WingDetailsMastEntity, int>>();
        _mockSocietyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockWingRepository = new Mock<IRepository<WingEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockPropertyMastOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyPhotoRepository = new Mock<IRepository<PropertyPhotoEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<WingDetailsMastMappingProfile>();
            cfg.AddProfile<SocietyWingDetailsMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<WingDetailsMastEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _service = new WingDetailsMastService(
            _mockRepository.Object,
            _mockSocietyRepository.Object,
            _mockWingRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockPropertyMastOldRepository.Object,
            _mockPropertyPhotoRepository.Object,
            _mockReferenceValidator.Object,
            _mockUnitOfWork.Object,
            _mapper);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenRecordExists()
    {
        var entities = new List<WingDetailsMastEntity>
        {
            new WingDetailsMastEntity
            {
                Id = 1,
                SocietyDetailsMastId = 10,
                WingMasterId = 5,
                WingName = "Wing A",
                IsActive = true,
                MarkedForDeletion = false
            }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Wing A", result.WingName);
        Assert.Equal(10, result.SocietyDetailsMastId);
        Assert.Equal(5, result.WingMasterId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRecordDoesNotExist()
    {
        var entities = new List<WingDetailsMastEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllWithOldDetailsAsync_ReturnsPagedResult_WithOldWingDetails()
    {
        var entities = new List<WingDetailsMastEntity>
        {
            new WingDetailsMastEntity
            {
                Id = 100,
                SocietyDetailsMastId = 1,
                WingMasterId = 2,
                WingName = "A-Wing",
                IsActive = true,
                MarkedForDeletion = false,
                SocietyWingDetails = new SocietyWingDetailsEntity
                {
                    Id = 10,
                    PropertyId = 50,
                    SocietyDetailId = 1,
                    WingId = 2,
                    IsActive = true
                }
            }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var pmdList = new List<PropertyMapDetailEntity>
        {
            new PropertyMapDetailEntity
            {
                Id = 1,
                PropertyIdNew = 50,
                PropertyIdOld = 200,
                Status = "DRAFT",
                IsActive = true
            }
        };
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(pmdList.BuildMock());

        var pmoList = new List<PropertyMastOldEntity>
        {
            new PropertyMastOldEntity
            {
                Id = 200,
                OldSocietyName = "Old Society",
                OldWardNo = "W-01",
                OldAddress = "123 Old Road",
                OldWing = "Wing-1",
                OldFlatOrShopNumber = "101",
                OldFloor = "1"
            }
        };
        _mockPropertyMastOldRepository.Setup(r => r.GetQueryable()).Returns(pmoList.BuildMock());

        var photos = new List<PropertyPhotoEntity>();
        _mockPropertyPhotoRepository.Setup(r => r.GetQueryable()).Returns(photos.BuildMock());

        var queryParameters = new WingDetailsMastQueryParameters { PageNumber = 1, PageSize = 10 };
        var response = await _service.GetAllWithOldDetailsAsync(queryParameters);

        Assert.NotNull(response);
        Assert.Single(response.WingDetailsMast);
        Assert.Equal(100, response.WingDetailsMast[0].Id);
        Assert.Equal("A-Wing", response.WingDetailsMast[0].WingName);
        Assert.Single(response.OldWingDetails);
        Assert.Equal("Old Society", response.OldWingDetails[0].OldSocietyName);
        Assert.Equal("W-01", response.OldWingDetails[0].OldWardNo);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationException_WhenSocietyWingDetailsIsNull()
    {
        var dto = new CreateWingDetailsMastDto
        {
            SocietyDetailsMastId = 1,
            WingMasterId = 1,
            SocietyWingDetails = null
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("Society wing details are required", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationException_WhenMismatchedSocietyOrWingIds()
    {
        var dto = new CreateWingDetailsMastDto
        {
            SocietyDetailsMastId = 1,
            WingMasterId = 1,
            SocietyWingDetails = new CreateSocietyWingDetailsDto
            {
                SocietyDetailId = 2,
                WingId = 1
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("do not match", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationException_WhenSocietyDoesNotExist()
    {
        var dto = new CreateWingDetailsMastDto
        {
            SocietyDetailsMastId = 999,
            WingMasterId = 1,
            SocietyWingDetails = new CreateSocietyWingDetailsDto
            {
                SocietyDetailId = 999,
                WingId = 1
            }
        };

        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());
        _mockWingRepository.Setup(r => r.GetQueryable()).Returns(new List<WingEntity> { new WingEntity { Id = 1 } }.BuildMock());

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("specified society does not exist", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationException_WhenDuplicateWingExistsForSociety()
    {
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity> { new SocietyDetailsEntity { Id = 1 } }.BuildMock());
        _mockWingRepository.Setup(r => r.GetQueryable()).Returns(new List<WingEntity> { new WingEntity { Id = 2 } }.BuildMock());

        var existingWings = new List<WingDetailsMastEntity>
        {
            new WingDetailsMastEntity
            {
                Id = 10,
                SocietyDetailsMastId = 1,
                WingMasterId = 2,
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingWings.BuildMock());

        var dto = new CreateWingDetailsMastDto
        {
            SocietyDetailsMastId = 1,
            WingMasterId = 2,
            SocietyWingDetails = new CreateSocietyWingDetailsDto
            {
                SocietyDetailId = 1,
                WingId = 2
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("selected wing already exists", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationException_WhenFromFloorIsGreaterThanToFloor()
    {
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity> { new SocietyDetailsEntity { Id = 1 } }.BuildMock());
        _mockWingRepository.Setup(r => r.GetQueryable()).Returns(new List<WingEntity> { new WingEntity { Id = 2 } }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());

        var dto = new CreateWingDetailsMastDto
        {
            SocietyDetailsMastId = 1,
            WingMasterId = 2,
            SocietyWingDetails = new CreateSocietyWingDetailsDto
            {
                SocietyDetailId = 1,
                WingId = 2,
                FromFloor = "10",
                ToFloor = "2"
            }
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("FromFloor cannot be greater than ToFloor", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_SuccessfullyCreatesRecord_AndNormalizesPropertyId()
    {
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity> { new SocietyDetailsEntity { Id = 1 } }.BuildMock());
        _mockWingRepository.Setup(r => r.GetQueryable()).Returns(new List<WingEntity> { new WingEntity { Id = 2 } }.BuildMock());
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());

        WingDetailsMastEntity? addedEntity = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WingDetailsMastEntity>(), It.IsAny<CancellationToken>()))
            .Callback<WingDetailsMastEntity, CancellationToken>((entity, _) => { entity.Id = 100; addedEntity = entity; })
            .ReturnsAsync((WingDetailsMastEntity entity, CancellationToken _) => entity);

        var dto = new CreateWingDetailsMastDto
        {
            SocietyDetailsMastId = 1,
            WingMasterId = 2,
            WingName = "Block C",
            SecretaryName = "John Doe",
            ManagerName = "Jane Smith",
            SocietyWingDetails = new CreateSocietyWingDetailsDto
            {
                SocietyDetailId = 1,
                WingId = 2,
                PropertyId = 0, // <= 0, should normalize to null
                FromFloor = "1",
                ToFloor = "5",
                NoOfFlat = 20
            }
        };

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(addedEntity);
        Assert.NotNull(addedEntity.SocietyWingDetails);
        Assert.Null(addedEntity.SocietyWingDetails.PropertyId); // PropertyId <= 0 normalized to null
        Assert.Equal("1", addedEntity.SocietyWingDetails.FromFloor);
        Assert.Equal("5", addedEntity.SocietyWingDetails.ToFloor);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidationException_WhenRecordDoesNotExist()
    {
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());

        var dto = new UpdateWingDetailsMastDto
        {
            WingName = "Updated Name"
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(999, dto));
        Assert.Contains("record does not exist", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_SuccessfullyUpdatesRecord_AndNormalizesPropertyId()
    {
        var existingEntity = new WingDetailsMastEntity
        {
            Id = 10,
            SocietyDetailsMastId = 1,
            WingMasterId = 2,
            WingName = "Old Block Name",
            IsActive = true,
            MarkedForDeletion = false,
            SocietyWingDetails = new SocietyWingDetailsEntity
            {
                Id = 5,
                SocietyDetailId = 1,
                WingId = 2,
                FromFloor = "1",
                ToFloor = "3",
                IsActive = true
            }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity> { existingEntity }.BuildMock());
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WingDetailsMastEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var updateDto = new UpdateWingDetailsMastDto
        {
            WingName = "New Block Name",
            SecretaryName = "New Secretary",
            SocietyWingDetails = new UpdateSocietyWingDetailsDto
            {
                FromFloor = "1",
                ToFloor = "10",
                NoOfFlat = 40
            }
        };

        var result = await _service.UpdateAsync(10, updateDto);

        Assert.Equal("New Block Name", existingEntity.WingName);
        Assert.Equal("New Secretary", existingEntity.SecretaryName);
        Assert.Equal("10", existingEntity.SocietyWingDetails!.ToFloor);
        Assert.Equal(40, existingEntity.SocietyWingDetails.NoOfFlat);
    }

    [Fact]
    public async Task DeleteAsync_SuccessfullySoftDeletes_WhenNoActivePropertiesExist()
    {
        var existingEntity = new WingDetailsMastEntity
        {
            Id = 10,
            SocietyDetailsMastId = 1,
            WingMasterId = 2,
            IsActive = true,
            MarkedForDeletion = false,
            SocietyWingDetails = new SocietyWingDetailsEntity
            {
                Id = 5,
                IsActive = true
            }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity> { existingEntity }.BuildMock());
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WingDetailsMastEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var success = await _service.DeleteAsync(10);

        Assert.True(success);
        Assert.False(existingEntity.IsActive);
        Assert.True(existingEntity.MarkedForDeletion);
        Assert.NotNull(existingEntity.MarkedForDeletionDate);
        Assert.False(existingEntity.SocietyWingDetails!.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsValidationException_WhenActivePropertiesAreAssociated()
    {
        var existingEntity = new WingDetailsMastEntity
        {
            Id = 10,
            SocietyDetailsMastId = 1,
            WingMasterId = 2,
            IsActive = true,
            MarkedForDeletion = false,
            Properties = new List<PropertyEntity>
            {
                new PropertyEntity { Id = 101, IsActive = true, MarkedForDeletion = false }
            }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity> { existingEntity }.BuildMock());

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(10));
        Assert.Contains("active properties associated with it", ex.Message);
    }

    private static PropertyPhotoEntity CreateTestPhoto(int id, int wingDetailId, string photoTypeCode, DocumentBindingEntity binding, int displayOrder)
    {
        var photoType = new PropertyPhotoTypeEntity
        {
            PhotoTypeCode = photoTypeCode
        };
        typeof(BaseEntity).GetProperty("Id")!.SetValue(photoType, id);

        var photo = PropertyPhotoEntity.CreateWithDetails(
            propertyId: null,
            photoTypeId: photoType.Id,
            entityType: "W",
            societyDetailId: null,
            wingDetailId: wingDetailId,
            documentBindingId: binding.Id,
            displayOrder: displayOrder);

        typeof(BaseEntity).GetProperty("Id")!.SetValue(photo, id);
        typeof(PropertyPhotoEntity).GetProperty("PhotoType")!.SetValue(photo, photoType);
        typeof(PropertyPhotoEntity).GetProperty("DocumentBinding")!.SetValue(photo, binding);

        return photo;
    }

    [Fact]
    public async Task GetAllAsync_ClassifiesPhotosIntoWingPhotosAndBoardPhotosCorrectly()
    {
        var entities = new List<WingDetailsMastEntity>
        {
            new WingDetailsMastEntity
            {
                Id = 1,
                SocietyDetailsMastId = 10,
                WingMasterId = 5,
                WingName = "Wing A",
                IsActive = true,
                MarkedForDeletion = false,
                SocietyWingDetails = new SocietyWingDetailsEntity
                {
                    Id = 100,
                    SocietyDetailId = 10,
                    WingId = 5,
                    IsActive = true
                }
            }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var document = new DocumentEntity { Id = 50, DocumentGuid = Guid.NewGuid(), FileName = "test.jpg", FileSizeBytes = 1024, StoragePath = "/path" };
        var binding = new DocumentBindingEntity { Id = 20, Document = document, IsActive = true, MarkedForDeletion = false, IsPrimaryDocument = true };

        var photos = new List<PropertyPhotoEntity>
        {
            CreateTestPhoto(1, 1, "WP", binding, 1),
            CreateTestPhoto(2, 1, "BP", binding, 2),
            CreateTestPhoto(3, 1, "OTHER", binding, 3)
        };
        _mockPropertyPhotoRepository.Setup(r => r.GetQueryable()).Returns(photos.BuildMock());

        var queryParameters = new WingDetailsMastQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(queryParameters);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        var dto = result.Items.First();
        Assert.NotNull(dto.SocietyWingDetails);
        Assert.Single(dto.SocietyWingDetails.WingPhotos);
        Assert.Equal("WP", dto.SocietyWingDetails.WingPhotos[0].PhotoTypeCode);
        Assert.Single(dto.SocietyWingDetails.BoardPhotos);
        Assert.Equal("BP", dto.SocietyWingDetails.BoardPhotos[0].PhotoTypeCode);
    }
}
