using Xunit;
using Moq;
using AutoMapper;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Application.DTOs.Master.ConfigCategoryMaster;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace NtisPlatform.Tests.Application
{
    public class ConfigCategoryMasterServiceTest
    {
        private readonly Mock<IRepository<ConfigCategoryMasterEntity, int>> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ConfigCategoryMasterService _service;

        public ConfigCategoryMasterServiceTest()
        {
            _repositoryMock = new Mock<IRepository<ConfigCategoryMasterEntity, int>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _service = new ConfigCategoryMasterService(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenEntityExists()
        {
            // Arrange
            var entity = new ConfigCategoryMasterEntity { Id = 1 };
            var dto = new ConfigCategoryMasterDto { Id = 1 };
            _repositoryMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<ConfigCategoryMasterDto>(entity)).Returns(dto);

            // Act
            ConfigCategoryMasterDto? result = await _service.GetByIdAsync(1, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenEntityDoesNotExist()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync((ConfigCategoryMasterEntity?)null);

            ConfigCategoryMasterDto? result = await _service.GetByIdAsync(2, default);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_CallsRepositoryAndUnitOfWork()
        {
            var createDto = new CreateConfigCategoryMasterDto();
            var entity = new ConfigCategoryMasterEntity();
            var dto = new ConfigCategoryMasterDto();

            _mapperMock.Setup(m => m.Map<ConfigCategoryMasterEntity>(createDto)).Returns(entity);
            _repositoryMock.Setup(r => r.AddAsync(entity, default)).ReturnsAsync(entity);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<ConfigCategoryMasterDto>(entity)).Returns(dto);

            ConfigCategoryMasterDto? result = await _service.CreateAsync(createDto);

            _repositoryMock.Verify(r => r.AddAsync(entity, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntity_WhenExists()
        {
            var updateDto = new UpdateConfigCategoryMasterDto();
            var entity = new ConfigCategoryMasterEntity { Id = 1 };
            var dto = new ConfigCategoryMasterDto { Id = 1 };
            _repositoryMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(entity);
            _repositoryMock.Setup(r => r.UpdateAsync(entity, default)).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map(updateDto, entity)).Returns(entity);
            _mapperMock.Setup(m => m.Map<ConfigCategoryMasterDto>(entity)).Returns(dto);

            ConfigCategoryMasterDto? result = await _service.UpdateAsync(1, updateDto);

            _repositoryMock.Verify(r => r.GetByIdAsync(1, default), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(entity, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNull_WhenEntityDoesNotExist()
        {
            var updateDto = new UpdateConfigCategoryMasterDto();
            _repositoryMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((ConfigCategoryMasterEntity?)null);

            ConfigCategoryMasterDto? result = await _service.UpdateAsync(99, updateDto);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_DeletesEntity_WhenExists()
        {
            var entity = new ConfigCategoryMasterEntity { Id = 1 };
            _repositoryMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(entity);
            _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<ConfigCategoryMasterEntity>(), default)).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await _service.DeleteAsync(1);

            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<ConfigCategoryMasterEntity>(), default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenEntityDoesNotExist()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync((ConfigCategoryMasterEntity?)null);

            bool result = await _service.DeleteAsync(2);

            Assert.False(result);
        }
    }
}
