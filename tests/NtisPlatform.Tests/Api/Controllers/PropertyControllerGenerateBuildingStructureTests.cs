using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive test suite for PropertyController.GetGeneratebuildingStructure endpoint
/// Target: 100% code coverage and branch coverage
/// </summary>
public class PropertyControllerGenerateBuildingStructureTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly PropertyController _controller;

    public PropertyControllerGenerateBuildingStructureTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _controller = PropertyControllerTestHelper.CreateController(_mockPropertyService, _mockLogger);
    }

    #region Happy Path Tests

    [Fact]
    public async Task GetGeneratebuildingStructure_WithValidVerticalGeneration_ReturnsOkWithItems()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 101,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        var expectedResult = new List<BuildingGenerateStructureDto>
        {
            new() { WardId = 1, PropertyNo = "P001", WingId = 1, RowNo = 1, FloorNo = 1, UnitNo = 1, FlatNo = "A-101", PartitionNo = "W1", GenerationType = "V" },
            new() { WardId = 1, PropertyNo = "P001", WingId = 1, RowNo = 2, FloorNo = 2, UnitNo = 1, FlatNo = "A-201", PartitionNo = "W2", GenerationType = "V" },
            new() { WardId = 1, PropertyNo = "P001", WingId = 1, RowNo = 3, FloorNo = 3, UnitNo = 1, FlatNo = "A-301", PartitionNo = "W3", GenerationType = "V" }
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Message.Should().Be("3 building structures generated successfully");
        response.Items.Should().NotBeNull();
        response.Items.Should().HaveCount(3);
        response.Items.Should().BeEquivalentTo(expectedResult);

        _mockPropertyService.Verify(s => s.GetGenerateBuildingStructureAsync(
            It.Is<BuildingGenerateDetailsDto>(d => 
                d.WardId == dto.WardId && 
                d.FromFloor == dto.FromFloor && 
                d.ToFloor == dto.ToFloor),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithValidHorizontalGeneration_ReturnsOkWithItems()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P002",
            WingId = 2,
            FromFloor = "1",
            ToFloor = "2",
            NoOfFlatOnOneFloor = 3,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "B",
            GenerationType = "H"
        };

        var expectedResult = new List<BuildingGenerateStructureDto>
        {
            new() { WardId = 1, PropertyNo = "P002", WingId = 2, RowNo = 1, FloorNo = 1, UnitNo = 1, FlatNo = "B-100", PartitionNo = "W1", GenerationType = "H" },
            new() { WardId = 1, PropertyNo = "P002", WingId = 2, RowNo = 2, FloorNo = 1, UnitNo = 2, FlatNo = "B-101", PartitionNo = "W2", GenerationType = "H" }
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Message.Should().Be("2 building structures generated successfully");
        response.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithValidHorizontalCustomGeneration_ReturnsOkWithItems()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P003",
            WingId = 3,
            FromFloor = "2",
            ToFloor = "2", // HC requires FromFloor == ToFloor
            NoOfFlatOnOneFloor = 4,
            FlatStart = 200,
            IncrementedBy = 1,
            Prifix = null,
            GenerationType = "HC"
        };

        var expectedResult = new List<BuildingGenerateStructureDto>
        {
            new() { WardId = 1, PropertyNo = "P003", WingId = 3, RowNo = 1, FloorNo = 2, UnitNo = 1, FlatNo = "200", PartitionNo = "W1", GenerationType = "HC" },
            new() { WardId = 1, PropertyNo = "P003", WingId = 3, RowNo = 2, FloorNo = 2, UnitNo = 2, FlatNo = "201", PartitionNo = "W2", GenerationType = "HC" }
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithValidVerticalCustomGeneration_ReturnsOkWithItems()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P004",
            WingId = 4,
            FromFloor = "1",
            ToFloor = "5",
            NoOfFlatOnOneFloor = 1, // VC requires NoOfFlatOnOneFloor == 1
            FlatStart = 101,
            IncrementedBy = 100,
            Prifix = "C",
            GenerationType = "VC"
        };

        var expectedResult = new List<BuildingGenerateStructureDto>
        {
            new() { WardId = 1, PropertyNo = "P004", WingId = 4, RowNo = 1, FloorNo = 1, UnitNo = 1, FlatNo = "C-101", PartitionNo = "W1", GenerationType = "VC" }
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Items.Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases - Empty Results

    [Fact]
    public async Task GetGeneratebuildingStructure_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P999",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "1",
            NoOfFlatOnOneFloor = 1,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "X",
            GenerationType = "V"
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BuildingGenerateStructureDto>());

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Message.Should().Be("No building structures generated");
        response.Items.Should().NotBeNull();
        response.Items.Should().BeEmpty();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No building structures generated")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithNullResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P999",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "1",
            NoOfFlatOnOneFloor = 1,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "X",
            GenerationType = "V"
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<BuildingGenerateStructureDto>?)null);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Message.Should().Be("No building structures generated");
        response.Items.Should().NotBeNull();
        response.Items.Should().BeEmpty();
    }

    #endregion

    #region Validation Error Tests (400 Bad Request)

    [Theory]
    [InlineData("abc", "3", "From Floor must be a numeric value between 1 and 1000")]
    [InlineData("0", "3", "From Floor must be a numeric value between 1 and 1000")]
    [InlineData("1001", "1002", "From Floor must be a numeric value between 1 and 1000")]
    [InlineData("-5", "3", "From Floor must be a numeric value between 1 and 1000")]
    public async Task GetGeneratebuildingStructure_WithInvalidFromFloor_ReturnsBadRequest(
        string fromFloor, string toFloor, string expectedMessage)
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = fromFloor,
            ToFloor = toFloor,
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation error")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("1", "xyz", "To Floor must be a numeric value between 1 and 1000")]
    [InlineData("1", "0", "To Floor must be a numeric value between 1 and 1000")]
    [InlineData("1", "1001", "To Floor must be a numeric value between 1 and 1000")]
    [InlineData("1", "-10", "To Floor must be a numeric value between 1 and 1000")]
    public async Task GetGeneratebuildingStructure_WithInvalidToFloor_ReturnsBadRequest(
        string fromFloor, string toFloor, string expectedMessage)
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = fromFloor,
            ToFloor = toFloor,
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithFromFloorGreaterThanToFloor_ReturnsBadRequest()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "5",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        var expectedMessage = "From Floor cannot be greater than To Floor";

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithHCAndDifferentFloors_ReturnsBadRequest()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "HC"
        };

        var expectedMessage = "From floor and to floor must be the same for Horizontal Custom generation";

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithVCAndMultipleFlats_ReturnsBadRequest()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 5,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "VC"
        };

        var expectedMessage = "Vertical Custom generation: number of flats on one floor must be 1";

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task GetGeneratebuildingStructure_WithInvalidNoOfFlatOnOneFloor_ReturnsBadRequest(int noOfFlats)
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = noOfFlats,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        var expectedMessage = "Number of flats on one floor must be greater than zero";

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithInvalidWingId_ReturnsBadRequest()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 999,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        var expectedMessage = "Wing with ID 999 not found or is inactive";

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData("X")]
    [InlineData("INVALID")]
    [InlineData("")]
    [InlineData("hc")]  // lowercase (should be validated as case-insensitive)
    public async Task GetGeneratebuildingStructure_WithInvalidGenerationType_ReturnsBadRequest(string generationType)
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = generationType
        };

        var expectedMessage = "Invalid Generation Type. Must be V, VC, H, or HC";

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Be(expectedMessage);
    }

    #endregion

    #region Exception Handling Tests (500 Internal Server Error)

    [Fact]
    public async Task GetGeneratebuildingStructure_WithUnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        var expectedException = new Exception("Database connection failed");

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        
        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An unexpected error occurred while generating building structure.");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error generating building structure")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithNullReferenceException_ReturnsInternalServerError()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NullReferenceException("Object reference not set"));

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        
        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An unexpected error occurred while generating building structure.");
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GetGeneratebuildingStructure_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        var expectedResult = new List<BuildingGenerateStructureDto>
        {
            new() { WardId = 1, PropertyNo = "P001", WingId = 1, RowNo = 1, FloorNo = 1, UnitNo = 1, FlatNo = "A-101", PartitionNo = "W1", GenerationType = "V" }
        };

        var cts = new CancellationTokenSource();

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), cts.Token))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, cts.Token);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockPropertyService.Verify(s => s.GetGenerateBuildingStructureAsync(
            It.IsAny<BuildingGenerateDetailsDto>(), 
            cts.Token), 
            Times.Once);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WhenOperationCancelled_ReturnsInternalServerError()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P001",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "3",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "A",
            GenerationType = "V"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, cts.Token);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("An unexpected error occurred while generating building structure.");
    }

    #endregion

    #region Integration-like Scenarios

    [Fact]
    public async Task GetGeneratebuildingStructure_WithLargeFloorRange_ReturnsOkWithManyItems()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P005",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "100",
            NoOfFlatOnOneFloor = 4,
            FlatStart = 100,
            IncrementedBy = 100,
            Prifix = "T",
            GenerationType = "V"
        };

        // Generate 400 items (100 floors * 4 units)
        var expectedResult = Enumerable.Range(1, 400)
            .Select(i => new BuildingGenerateStructureDto
            {
                WardId = 1,
                PropertyNo = "P005",
                WingId = 1,
                RowNo = i,
                FloorNo = ((i - 1) / 4) + 1,
                UnitNo = ((i - 1) % 4) + 1,
                FlatNo = $"T-{100 + i}",
                PartitionNo = $"W{i}",
                GenerationType = "V"
            })
            .ToList();

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Items.Should().HaveCount(400);
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithNullPrefix_ReturnsOkWithItemsWithoutPrefix()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P006",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "2",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 10,
            Prifix = null,
            GenerationType = "V"
        };

        var expectedResult = new List<BuildingGenerateStructureDto>
        {
            new() { WardId = 1, PropertyNo = "P006", WingId = 1, RowNo = 1, FloorNo = 1, UnitNo = 1, FlatNo = "100", PartitionNo = "W1", GenerationType = "V" },
            new() { WardId = 1, PropertyNo = "P006", WingId = 1, RowNo = 2, FloorNo = 1, UnitNo = 2, FlatNo = "101", PartitionNo = "W2", GenerationType = "V" }
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Items.Should().HaveCount(2);
        response.Items.Should().AllSatisfy(item => item.FlatNo.Should().NotContain("-"));
    }

    [Fact]
    public async Task GetGeneratebuildingStructure_WithEmptyPrefix_ReturnsOkWithItemsWithoutPrefix()
    {
        // Arrange
        var dto = new BuildingGenerateDetailsDto
        {
            WardId = 1,
            PropertyNo = "P007",
            WingId = 1,
            FromFloor = "1",
            ToFloor = "2",
            NoOfFlatOnOneFloor = 2,
            FlatStart = 100,
            IncrementedBy = 10,
            Prifix = string.Empty,
            GenerationType = "H"
        };

        var expectedResult = new List<BuildingGenerateStructureDto>
        {
            new() { WardId = 1, PropertyNo = "P007", WingId = 1, RowNo = 1, FloorNo = 1, UnitNo = 1, FlatNo = "100", PartitionNo = "W1", GenerationType = "H" }
        };

        _mockPropertyService
            .Setup(s => s.GetGenerateBuildingStructureAsync(It.IsAny<BuildingGenerateDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetGeneratebuildingStructure(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingGenerateStructureDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Items.Should().HaveCount(1);
    }

    #endregion

    #region GetBuildingListAsync Tests

    [Fact]
    public async Task GetBuildingListAsync_WithValidWardId_ReturnsOkWithItems()
    {
        // Arrange
        var wardId = 1;
        var expectedResult = new List<BuildingListDto>
        {
            new() { PropertyId = 1, WardNo = "W001", PropertyNo = "P001", CatPropertyCategoryName = "Residential", PartitionNo = "A" },
            new() { PropertyId = 2, WardNo = "W001", PropertyNo = "P002", CatPropertyCategoryName = "Commercial", PartitionNo = "B" }
        };

        _mockPropertyService
            .Setup(s => s.GetBuildingListAsync(wardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetBuildingListAsync(wardId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingListDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Record fetched successfully");
        response.Items.Should().NotBeNull();
        response.Items.Should().HaveCount(2);
        response.Items.Should().BeEquivalentTo(expectedResult);

        _mockPropertyService.Verify(s => s.GetBuildingListAsync(wardId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBuildingListAsync_WithNonExistentWardId_ReturnsNotFound()
    {
        // Arrange
        var wardId = 999;

        _mockPropertyService
            .Setup(s => s.GetBuildingListAsync(wardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<BuildingListDto>?)null);

        // Act
        var result = await _controller.GetBuildingListAsync(wardId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse<BuildingListDto>>().Subject;
        
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("999");
        response.Message.Should().Contain("not found");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("999") && v.ToString()!.Contains("not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBuildingListAsync_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var wardId = 1;
        var expectedException = new Exception("Database connection failed");

        _mockPropertyService
            .Setup(s => s.GetBuildingListAsync(wardId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.GetBuildingListAsync(wardId, CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        
        var response = statusCodeResult.Value.Should().BeOfType<ApiResponse<BuildingListDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("error");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving building details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBuildingListAsync_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var wardId = 1;

        _mockPropertyService
            .Setup(s => s.GetBuildingListAsync(wardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _controller.GetBuildingListAsync(wardId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingListDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Items.Should().NotBeNull();
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBuildingListAsync_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var wardId = 1;
        var cts = new CancellationTokenSource();

        _mockPropertyService
            .Setup(s => s.GetBuildingListAsync(wardId, cts.Token))
            .ReturnsAsync([]);

        // Act
        var result = await _controller.GetBuildingListAsync(wardId, cts.Token);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockPropertyService.Verify(s => s.GetBuildingListAsync(wardId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetBuildingListAsync_WithLargeDataset_ReturnsOkWithAllItems()
    {
        // Arrange
        var wardId = 1;
        var expectedResult = Enumerable.Range(1, 100)
            .Select(i => new BuildingListDto
            {
                PropertyId = i,
                WardNo = "W001",
                PropertyNo = $"P{i:D3}",
                CatPropertyCategoryName = i % 2 == 0 ? "Residential" : "Commercial",
                PartitionNo = $"Part{i}"
            })
            .ToList();

        _mockPropertyService
            .Setup(s => s.GetBuildingListAsync(wardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetBuildingListAsync(wardId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<BuildingListDto>>>().Subject;
        
        response.Success.Should().BeTrue();
        response.Items.Should().HaveCount(100);
    }

    #endregion
}
