using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.EmployeeType;
using NtisPlatform.Application.Interfaces.Master;
using Xunit;

namespace NtisPlatform.Tests.Application
{
    public class EmployeeTypeControllerTests
    {
        private readonly Mock<IEmployeeType> _serviceMock;
        private readonly Mock<ILogger<EmployeeTypeController>> _loggerMock;
        private readonly EmployeeTypeController _controller;

        public EmployeeTypeControllerTests()
        {
            _serviceMock = new Mock<IEmployeeType>();
            _loggerMock = new Mock<ILogger<EmployeeTypeController>>();
            _controller = new EmployeeTypeController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult()
        {
            var query = new UserEmployeeTypeQueryParameterDto();
            var expectedItems = new List<EmployeeTypeDto> { new EmployeeTypeDto { Id = 1, EmployeeType = "TestType" } };
            var expected = new NtisPlatform.Application.Models.PagedResult<EmployeeTypeDto>
            {
                Items = expectedItems,
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 10
            };
            _serviceMock.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await _controller.GetAll(query, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsType<NtisPlatform.Application.Models.PagedResult<EmployeeTypeDto>>(okResult.Value);
            Assert.Single(actual.Items);
            Assert.Equal(expectedItems.First().Id, actual.Items.First().Id);
            Assert.Equal(expectedItems.First().EmployeeType, actual.Items.First().EmployeeType);
            _serviceMock.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult()
        {
            var expected = new EmployeeTypeDto { Id = 1, EmployeeType = "TestType" };
            _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await _controller.GetById(1, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsType<EmployeeTypeDto>(okResult.Value);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.EmployeeType, actual.EmployeeType);
            _serviceMock.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_ReturnsCreatedResult()
        {
            var dto = new CreateEmployeeTypeDto { EmployeeType = "TestType" };
            var expected = new EmployeeTypeDto { Id = 1, EmployeeType = "TestType" };
            _serviceMock.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await _controller.Create(dto, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<EmployeeTypeDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Record inserted successfully", response.Message);
            Assert.NotNull(response.Items);
            Assert.Equal(expected.Id, response.Items.Id);
            Assert.Equal(expected.EmployeeType, response.Items.EmployeeType);
            _serviceMock.Verify(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsOkResult()
        {
            var dto = new UpdateEmployeeTypeDto { EmployeeType = "UpdatedType" };
            var expected = new EmployeeTypeDto { Id = 1, EmployeeType = "UpdatedType" };
            _serviceMock.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await _controller.Update(1, dto, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<EmployeeTypeDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Record updated successfully", response.Message);
            Assert.NotNull(response.Items);
            Assert.Equal(expected.Id, response.Items.Id);
            Assert.Equal(expected.EmployeeType, response.Items.EmployeeType);
            _serviceMock.Verify(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsOkObjectResult()
        {
            _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _controller.Delete(1, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<EmployeeTypeDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Record marked for deletion", response.Message);
            _serviceMock.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
