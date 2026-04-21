using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.EmployeeType;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application
{
    public class EmployeeTypeServiceTests
    {
        private readonly Mock<IRepository<EmployeeTypeEntity, int>> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;

        public EmployeeTypeServiceTests()
        {
            _repositoryMock = new Mock<IRepository<EmployeeTypeEntity, int>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsPagedResult()
        {
            var entities = new List<EmployeeTypeEntity>
            {
                new EmployeeTypeEntity { Id = 1, EmployeeType = "Permanent" },
                new EmployeeTypeEntity { Id = 2, EmployeeType = "Contract" }
            };
            var mockQuery = entities.BuildMock();
            _repositoryMock.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EmployeeTypeEntity, EmployeeTypeDto>();
            });
            var mapper = mapperConfig.CreateMapper();
            var service = new EmployeeTypeService(_repositoryMock.Object, _unitOfWorkMock.Object, mapper);

            var query = new UserEmployeeTypeQueryParameterDto { PageNumber = 1, PageSize = 10 };
            var result = await service.GetAllAsync(query, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            var entity = new EmployeeTypeEntity { Id = 1, EmployeeType = "Permanent" };
            _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<EmployeeTypeDto>(entity)).Returns(new EmployeeTypeDto { Id = 1, EmployeeType = "Permanent" });
            var service = new EmployeeTypeService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
            var result = await service.GetByIdAsync(1, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Permanent", result.EmployeeType);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((EmployeeTypeEntity?)null);
            var service = new EmployeeTypeService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
            var result = await service.GetByIdAsync(999, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_AddsEntity()
        {
            var dto = new CreateEmployeeTypeDto { EmployeeType = "Permanent" };
            var entity = new EmployeeTypeEntity { Id = 1, EmployeeType = "Permanent" };
            var resultDto = new EmployeeTypeDto { Id = 1, EmployeeType = "Permanent" };
            _mapperMock.Setup(m => m.Map<EmployeeTypeEntity>(dto)).Returns(entity);
            _repositoryMock.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<EmployeeTypeDto>(entity)).Returns(resultDto);
            var service = new EmployeeTypeService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
            var result = await service.CreateAsync(dto, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Permanent", result.EmployeeType);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntity()
        {
            var dto = new UpdateEmployeeTypeDto { EmployeeType = "Permanent" };
            var entity = new EmployeeTypeEntity { Id = 1, EmployeeType = "Permanent" };
            var resultDto = new EmployeeTypeDto { Id = 1, EmployeeType = "Permanent" };
            _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map(dto, entity)).Returns(entity);
            _repositoryMock.Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<EmployeeTypeDto>(entity)).Returns(resultDto);
            var service = new EmployeeTypeService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
            var result = await service.UpdateAsync(1, dto, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Permanent", result.EmployeeType);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingId_ReturnsNull()
        {
            var dto = new UpdateEmployeeTypeDto();
            _repositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((EmployeeTypeEntity?)null);
            var service = new EmployeeTypeService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
            var result = await service.UpdateAsync(999, dto, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_DeletesEntity()
        {
            var entity = new EmployeeTypeEntity { Id = 1, EmployeeType = "Permanent" };
            _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _repositoryMock.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            var service = new EmployeeTypeService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
            var result = await service.DeleteAsync(1, CancellationToken.None);
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((EmployeeTypeEntity?)null);
            var service = new EmployeeTypeService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
            var result = await service.DeleteAsync(999, CancellationToken.None);
            Assert.False(result);
        }

    }
}
