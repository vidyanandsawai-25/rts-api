using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.RoleWiseScreenAccessMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application
{
    public class RoleWiseScreenAccessMasterServiceTests
    {
        private readonly Mock<IRepository<RoleWiseScreenAccessMasterEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly RoleWiseScreenAccessMasterService _service;

        public RoleWiseScreenAccessMasterServiceTests()
        {
            _mockRepository = new Mock<IRepository<RoleWiseScreenAccessMasterEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockUnitOfWork
                .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _service = new RoleWiseScreenAccessMasterService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                _mockMapper.Object);
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            // Arrange
            var entity = new RoleWiseScreenAccessMasterEntity
            {
                RoleWiseScreenAccessId = 1,
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = true,
                CanDelete = false,
                HaveFullAccess = false,
                HaveNoAccess = false,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = 1
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<RoleWiseScreenAccessMasterDTO>(It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Returns((RoleWiseScreenAccessMasterEntity e) => new RoleWiseScreenAccessMasterDTO
                {
                    RoleWiseScreenAccessId = e.RoleWiseScreenAccessId,
                    UserRoleId = e.UserRoleId,
                    ScreenId = e.ScreenId,
                    CanView = e.CanView,
                    CanEdit = e.CanEdit,
                    CanDelete = e.CanDelete,
                    HaveFullAccess = e.HaveFullAccess,
                    HaveNoAccess = e.HaveNoAccess,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate,
                    CreatedBy = e.CreatedBy
                });

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.RoleWiseScreenAccessId);
            Assert.Equal(10, result.UserRoleId);
            Assert.Equal(5, result.ScreenId);
            Assert.True(result.CanView);
            Assert.True(result.CanEdit);
            Assert.False(result.CanDelete);
            Assert.False(result.HaveFullAccess);
            Assert.False(result.HaveNoAccess);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RoleWiseScreenAccessMasterEntity?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var entities = new List<RoleWiseScreenAccessMasterEntity>
            {
                new()
                {
                    RoleWiseScreenAccessId = 1,
                    UserRoleId = 10,
                    ScreenId = 5,
                    CanView = true,
                    CanEdit = true,
                    CanDelete = false,
                    HaveFullAccess = false,
                    HaveNoAccess = false,
                    IsActive = true
                },
                new()
                {
                    RoleWiseScreenAccessId = 2,
                    UserRoleId = 11,
                    ScreenId = 6,
                    CanView = true,
                    CanEdit = false,
                    CanDelete = false,
                    HaveFullAccess = false,
                    HaveNoAccess = false,
                    IsActive = true
                }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RoleWiseScreenAccessMasterEntity, RoleWiseScreenAccessMasterDTO>();
            });

            mapperConfig.AssertConfigurationIsValid();
            IMapper mapper = mapperConfig.CreateMapper();

            var service = new RoleWiseScreenAccessMasterService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                mapper);

            var queryParams = new RoleWiseScreenAccessQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
                SearchTerm = null!,
                SortBy = null!
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);

            var items = result.Items.ToList();
            Assert.Equal(2, items.Count);
            Assert.Contains(items, x => x.RoleWiseScreenAccessId == 1);
            Assert.Contains(items, x => x.RoleWiseScreenAccessId == 2);
        }

        [Fact]
        public async Task GetAllAsync_WithFilters_ReturnsFilteredEntities()
        {
            // Arrange
            var entities = new List<RoleWiseScreenAccessMasterEntity>
            {
                new()
                {
                    RoleWiseScreenAccessId = 1,
                    UserRoleId = 10,
                    ScreenId = 5,
                    CanView = true,
                    IsActive = true
                },
                new()
                {
                    RoleWiseScreenAccessId = 2,
                    UserRoleId = 11,
                    ScreenId = 5,
                    CanView = false,
                    IsActive = true
                }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RoleWiseScreenAccessMasterEntity, RoleWiseScreenAccessMasterDTO>();
            });

            IMapper mapper = mapperConfig.CreateMapper();

            var service = new RoleWiseScreenAccessMasterService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                mapper);

            var queryParams = new RoleWiseScreenAccessQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                UserRoleId = 10,
                FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            var items = result.Items.ToList();
            Assert.All(items, item => Assert.Equal(10, item.UserRoleId));
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            // Arrange
            var createDto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = true,
                CanDelete = false,
                HaveFullAccess = false,
                HaveNoAccess = false,
                IsActive = true,
                CreatedBy = 1
            };

            var entities = new List<RoleWiseScreenAccessMasterEntity>();
            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterEntity>(It.IsAny<CreateRoleWiseScreenAccessMasterDto>()))
                .Returns((CreateRoleWiseScreenAccessMasterDto dto) => new RoleWiseScreenAccessMasterEntity
                {
                    RoleWiseScreenAccessId = 1,
                    UserRoleId = dto.UserRoleId,
                    ScreenId = dto.ScreenId,
                    CanView = dto.CanView,
                    CanEdit = dto.CanEdit,
                    CanDelete = dto.CanDelete,
                    HaveFullAccess = dto.HaveFullAccess,
                    HaveNoAccess = dto.HaveNoAccess,
                    IsActive = dto.IsActive,
                    CreatedDate = DateTime.Now,
                    CreatedBy = dto.CreatedBy
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RoleWiseScreenAccessMasterEntity e, CancellationToken _) => e);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterDTO>(It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Returns((RoleWiseScreenAccessMasterEntity e) => new RoleWiseScreenAccessMasterDTO
                {
                    RoleWiseScreenAccessId = e.RoleWiseScreenAccessId,
                    UserRoleId = e.UserRoleId,
                    ScreenId = e.ScreenId,
                    CanView = e.CanView,
                    CanEdit = e.CanEdit,
                    CanDelete = e.CanDelete,
                    HaveFullAccess = e.HaveFullAccess,
                    HaveNoAccess = e.HaveNoAccess,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate,
                    CreatedBy = e.CreatedBy
                });

            // Act
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.UserRoleId);
            Assert.Equal(5, result.ScreenId);
            Assert.True(result.CanView);
            Assert.True(result.CanEdit);
            Assert.False(result.CanDelete);
            Assert.False(result.HaveFullAccess);
            Assert.False(result.HaveNoAccess);
            Assert.True(result.IsActive);
            Assert.NotNull(result.CreatedDate);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_FullAccess_CreatesSuccessfully()
        {
            // Arrange
            var createDto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = true,
                CanDelete = true,
                HaveFullAccess = true,
                HaveNoAccess = false,
                IsActive = true,
                CreatedBy = 1
            };

            var entities = new List<RoleWiseScreenAccessMasterEntity>();
            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterEntity>(It.IsAny<CreateRoleWiseScreenAccessMasterDto>()))
                .Returns((CreateRoleWiseScreenAccessMasterDto dto) => new RoleWiseScreenAccessMasterEntity
                {
                    RoleWiseScreenAccessId = 1,
                    UserRoleId = dto.UserRoleId,
                    ScreenId = dto.ScreenId,
                    CanView = dto.CanView,
                    CanEdit = dto.CanEdit,
                    CanDelete = dto.CanDelete,
                    HaveFullAccess = dto.HaveFullAccess,
                    HaveNoAccess = dto.HaveNoAccess,
                    IsActive = dto.IsActive,
                    CreatedDate = DateTime.Now
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RoleWiseScreenAccessMasterEntity e, CancellationToken _) => e);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterDTO>(It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Returns((RoleWiseScreenAccessMasterEntity e) => new RoleWiseScreenAccessMasterDTO
                {
                    RoleWiseScreenAccessId = e.RoleWiseScreenAccessId,
                    UserRoleId = e.UserRoleId,
                    ScreenId = e.ScreenId,
                    CanView = e.CanView,
                    CanEdit = e.CanEdit,
                    CanDelete = e.CanDelete,
                    HaveFullAccess = e.HaveFullAccess,
                    HaveNoAccess = e.HaveNoAccess,
                    IsActive = e.IsActive
                });

            // Act
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.HaveFullAccess);
            Assert.True(result.CanView);
            Assert.True(result.CanEdit);
            Assert.True(result.CanDelete);
            Assert.False(result.HaveNoAccess);
        }

        [Fact]
        public async Task CreateAsync_NoAccess_CreatesSuccessfully()
        {
            // Arrange
            var createDto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = false,
                CanEdit = false,
                CanDelete = false,
                HaveFullAccess = false,
                HaveNoAccess = true,
                IsActive = true,
                CreatedBy = 1
            };

            var entities = new List<RoleWiseScreenAccessMasterEntity>();
            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterEntity>(It.IsAny<CreateRoleWiseScreenAccessMasterDto>()))
                .Returns((CreateRoleWiseScreenAccessMasterDto dto) => new RoleWiseScreenAccessMasterEntity
                {
                    RoleWiseScreenAccessId = 1,
                    UserRoleId = dto.UserRoleId,
                    ScreenId = dto.ScreenId,
                    CanView = dto.CanView,
                    CanEdit = dto.CanEdit,
                    CanDelete = dto.CanDelete,
                    HaveFullAccess = dto.HaveFullAccess,
                    HaveNoAccess = dto.HaveNoAccess,
                    IsActive = dto.IsActive
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RoleWiseScreenAccessMasterEntity e, CancellationToken _) => e);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterDTO>(It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Returns((RoleWiseScreenAccessMasterEntity e) => new RoleWiseScreenAccessMasterDTO
                {
                    RoleWiseScreenAccessId = e.RoleWiseScreenAccessId,
                    UserRoleId = e.UserRoleId,
                    ScreenId = e.ScreenId,
                    CanView = e.CanView,
                    CanEdit = e.CanEdit,
                    CanDelete = e.CanDelete,
                    HaveFullAccess = e.HaveFullAccess,
                    HaveNoAccess = e.HaveNoAccess,
                    IsActive = e.IsActive
                });

            // Act
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.HaveNoAccess);
            Assert.False(result.CanView);
            Assert.False(result.CanEdit);
            Assert.False(result.CanDelete);
            Assert.False(result.HaveFullAccess);
        }

        [Fact]
        public async Task CreateAsync_DuplicateRoleScreen_ThrowsInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = false,
                CanDelete = false,
                IsActive = true,
                CreatedBy = 1
            };

            var existingEntities = new List<RoleWiseScreenAccessMasterEntity>
            {
                new()
                {
                    RoleWiseScreenAccessId = 1,
                    UserRoleId = 10,
                    ScreenId = 5,
                    CanView = true,
                    IsActive = true
                }
            };

            var mockQuery = existingEntities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(createDto, CancellationToken.None));

            Assert.Contains("Role-Screen access already exists", exception.Message);
            Assert.Contains("UserRoleId=10", exception.Message);
            Assert.Contains("ScreenId=5", exception.Message);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_AfterSoftDelete_CreatesSuccessfully()
        {
            // Arrange - A soft-deleted record exists (IsActive = false)
            var createDto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = false,
                CanDelete = false,
                IsActive = true,
                CreatedBy = 1
            };

            // Existing soft-deleted record with same UserRoleId and ScreenId
            var existingEntities = new List<RoleWiseScreenAccessMasterEntity>
            {
                new()
                {
                    RoleWiseScreenAccessId = 1,
                    UserRoleId = 10,
                    ScreenId = 5,
                    CanView = true,
                    IsActive = false  // Soft-deleted
                }
            };

            var mockQuery = existingEntities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterEntity>(It.IsAny<CreateRoleWiseScreenAccessMasterDto>()))
                .Returns((CreateRoleWiseScreenAccessMasterDto dto) => new RoleWiseScreenAccessMasterEntity
                {
                    RoleWiseScreenAccessId = 2,
                    UserRoleId = dto.UserRoleId,
                    ScreenId = dto.ScreenId,
                    CanView = dto.CanView,
                    CanEdit = dto.CanEdit,
                    CanDelete = dto.CanDelete,
                    HaveFullAccess = dto.HaveFullAccess,
                    HaveNoAccess = dto.HaveNoAccess,
                    IsActive = dto.IsActive,
                    CreatedDate = DateTime.Now
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RoleWiseScreenAccessMasterEntity e, CancellationToken _) =>
                {
                    e.RoleWiseScreenAccessId = 2;
                    return e;
                });

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterDTO>(It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Returns((RoleWiseScreenAccessMasterEntity e) => new RoleWiseScreenAccessMasterDTO
                {
                    RoleWiseScreenAccessId = e.RoleWiseScreenAccessId,
                    UserRoleId = e.UserRoleId,
                    ScreenId = e.ScreenId,
                    CanView = e.CanView,
                    CanEdit = e.CanEdit,
                    CanDelete = e.CanDelete,
                    HaveFullAccess = e.HaveFullAccess,
                    HaveNoAccess = e.HaveNoAccess,
                    IsActive = e.IsActive
                });

            // Act
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.RoleWiseScreenAccessId);
            Assert.Equal(10, result.UserRoleId);
            Assert.Equal(5, result.ScreenId);
            Assert.True(result.IsActive);

            // Verify that the soft-deleted record didn't prevent creation
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
        {
            // Arrange
            var updateDto = new UpdateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = true,
                CanDelete = true,
                HaveFullAccess = false,
                HaveNoAccess = false,
                IsActive = true,
                UpdatedBy = 2
            };

            var existingEntity = new RoleWiseScreenAccessMasterEntity
            {
                RoleWiseScreenAccessId = 1,
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = false,
                CanDelete = false,
                HaveFullAccess = false,
                HaveNoAccess = false,
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-1),
                CreatedBy = 1
            };

            var entities = new List<RoleWiseScreenAccessMasterEntity>();
            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockMapper.Setup(m => m.Map(It.IsAny<UpdateRoleWiseScreenAccessMasterDto>(), It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Callback<UpdateRoleWiseScreenAccessMasterDto, RoleWiseScreenAccessMasterEntity>((dto, entity) =>
                {
                    entity.UserRoleId = dto.UserRoleId;
                    entity.ScreenId = dto.ScreenId;
                    entity.CanView = dto.CanView;
                    entity.CanEdit = dto.CanEdit;
                    entity.CanDelete = dto.CanDelete;
                    entity.HaveFullAccess = dto.HaveFullAccess;
                    entity.HaveNoAccess = dto.HaveNoAccess;
                    entity.IsActive = dto.IsActive;
                    entity.UpdatedDate = DateTime.Now;
                    entity.UpdatedBy = dto.UpdatedBy;
                })
                .Returns(existingEntity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterDTO>(It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Returns((RoleWiseScreenAccessMasterEntity e) => new RoleWiseScreenAccessMasterDTO
                {
                    RoleWiseScreenAccessId = e.RoleWiseScreenAccessId,
                    UserRoleId = e.UserRoleId,
                    ScreenId = e.ScreenId,
                    CanView = e.CanView,
                    CanEdit = e.CanEdit,
                    CanDelete = e.CanDelete,
                    HaveFullAccess = e.HaveFullAccess,
                    HaveNoAccess = e.HaveNoAccess,
                    IsActive = e.IsActive,
                    UpdatedDate = e.UpdatedDate,
                    UpdatedBy = e.UpdatedBy
                });

            // Act
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.RoleWiseScreenAccessId);
            Assert.Equal(10, result.UserRoleId);
            Assert.Equal(5, result.ScreenId);
            Assert.True(result.CanView);
            Assert.True(result.CanEdit);
            Assert.True(result.CanDelete);
            Assert.NotNull(result.UpdatedDate);
            Assert.Equal(2, result.UpdatedBy);

            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
        {
            // Arrange
            var updateDto = new UpdateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                IsActive = true
            };

            var entities = new List<RoleWiseScreenAccessMasterEntity>();
            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RoleWiseScreenAccessMasterEntity?)null);

            // Act
            var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_DuplicateRoleScreen_ThrowsInvalidOperationException()
        {
            // Arrange
            var updateDto = new UpdateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 11,
                ScreenId = 6,
                CanView = true,
                IsActive = true,
                UpdatedBy = 2
            };

            var existingEntity = new RoleWiseScreenAccessMasterEntity
            {
                RoleWiseScreenAccessId = 1,
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                IsActive = true
            };

            var existingEntities = new List<RoleWiseScreenAccessMasterEntity>
            {
                new()
                {
                    RoleWiseScreenAccessId = 2,
                    UserRoleId = 11,
                    ScreenId = 6,
                    CanView = true,
                    IsActive = true
                }
            };

            var mockQuery = existingEntities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

            Assert.Contains("Role-Screen access already exists", exception.Message);
            Assert.Contains("UserRoleId=11", exception.Message);
            Assert.Contains("ScreenId=6", exception.Message);

            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ToFullAccess_UpdatesAllPermissions()
        {
            // Arrange
            var updateDto = new UpdateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = true,
                CanDelete = true,
                HaveFullAccess = true,
                HaveNoAccess = false,
                IsActive = true,
                UpdatedBy = 2
            };

            var existingEntity = new RoleWiseScreenAccessMasterEntity
            {
                RoleWiseScreenAccessId = 1,
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = false,
                CanDelete = false,
                HaveFullAccess = false,
                HaveNoAccess = false,
                IsActive = true
            };

            var entities = new List<RoleWiseScreenAccessMasterEntity>();
            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockMapper.Setup(m => m.Map(It.IsAny<UpdateRoleWiseScreenAccessMasterDto>(), It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Callback<UpdateRoleWiseScreenAccessMasterDto, RoleWiseScreenAccessMasterEntity>((dto, entity) =>
                {
                    entity.CanView = dto.CanView;
                    entity.CanEdit = dto.CanEdit;
                    entity.CanDelete = dto.CanDelete;
                    entity.HaveFullAccess = dto.HaveFullAccess;
                    entity.HaveNoAccess = dto.HaveNoAccess;
                })
                .Returns(existingEntity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RoleWiseScreenAccessMasterEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map<RoleWiseScreenAccessMasterDTO>(It.IsAny<RoleWiseScreenAccessMasterEntity>()))
                .Returns((RoleWiseScreenAccessMasterEntity e) => new RoleWiseScreenAccessMasterDTO
                {
                    RoleWiseScreenAccessId = e.RoleWiseScreenAccessId,
                    UserRoleId = e.UserRoleId,
                    ScreenId = e.ScreenId,
                    CanView = e.CanView,
                    CanEdit = e.CanEdit,
                    CanDelete = e.CanDelete,
                    HaveFullAccess = e.HaveFullAccess,
                    HaveNoAccess = e.HaveNoAccess,
                    IsActive = e.IsActive
                });

            // Act
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.HaveFullAccess);
            Assert.True(result.CanView);
            Assert.True(result.CanEdit);
            Assert.True(result.CanDelete);
            Assert.False(result.HaveNoAccess);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesSuccessfully()
        {
            // Arrange
            var existingEntity = new RoleWiseScreenAccessMasterEntity
            {
                RoleWiseScreenAccessId = 1,
                UserRoleId = 10,
                ScreenId = 5,
                IsActive = true
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(1, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RoleWiseScreenAccessMasterEntity?)null);

            // Act
            var result = await _service.DeleteAsync(999, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void CreateDto_Validate_FullAccessAndNoAccessMutuallyExclusive()
        {
            // Arrange
            var dto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = true,
                CanDelete = true,
                HaveFullAccess = true,
                HaveNoAccess = true,  // Both true - should fail
                IsActive = true
            };

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Act
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "FullAccess_NoAccess_Mutually_Exclusive");
        }

        [Fact]
        public void CreateDto_Validate_FullAccessRequiresAllPermissions()
        {
            // Arrange
            var dto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = false,  // Should be true for full access
                CanDelete = true,
                HaveFullAccess = true,
                HaveNoAccess = false,
                IsActive = true
            };

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Act
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "FullAccess_Requires_All_Permissions");
        }

        [Fact]
        public void CreateDto_Validate_NoAccessCannotHavePermissions()
        {
            // Arrange
            var dto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,  // Should be false for no access
                CanEdit = false,
                CanDelete = false,
                HaveFullAccess = false,
                HaveNoAccess = true,
                IsActive = true
            };

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Act
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "NoAccess_Cannot_Have_Permissions");
        }

        [Fact]
        public void CreateDto_Validate_AtLeastOnePermissionRequired()
        {
            // Arrange
            var dto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = false,
                CanEdit = false,
                CanDelete = false,
                HaveFullAccess = false,
                HaveNoAccess = false,  // No permissions set - should fail
                IsActive = true
            };

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Act
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "At_Least_One_Permission_Required");
        }

        [Fact]
        public void CreateDto_Validate_ValidPartialPermissions_Succeeds()
        {
            // Arrange
            var dto = new CreateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = true,
                CanDelete = false,
                HaveFullAccess = false,
                HaveNoAccess = false,
                IsActive = true
            };

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Act
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdateDto_Validate_FullAccessAndNoAccessMutuallyExclusive()
        {
            // Arrange
            var dto = new UpdateRoleWiseScreenAccessMasterDto
            {
                UserRoleId = 10,
                ScreenId = 5,
                CanView = true,
                CanEdit = true,
                CanDelete = true,
                HaveFullAccess = true,
                HaveNoAccess = true,  // Both true - should fail
                IsActive = true
            };

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Act
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "FullAccess_NoAccess_Mutually_Exclusive");
        }

        #endregion
    }
}
