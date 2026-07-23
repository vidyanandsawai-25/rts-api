using AutoMapper;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Mappings;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;
using NtisPlatform.Application.DTOs.Master;

namespace NtisPlatform.Tests.Application
{
    public class GSTServiceTests
    {
        private readonly Mock<IRepository<GSTMasterEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IMapper _mapper;
        private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
        private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
        private readonly GSTService _service;

        public GSTServiceTests()
        {
            _mockRepository = new Mock<IRepository<GSTMasterEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockReferenceValidator = new Mock<IReferenceValidationService>();
            _mockCleanupService = new Mock<IHardDeleteCleanupService>();
            _mockReferenceValidator
                .Setup(x => x.ValidateReferencesAsync<GSTMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<GSTMappingProfile>();
            },
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _service = new GSTService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);
        }

        #region Entity Tests

        [Fact]
        public void Entity_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var entity = new GSTMasterEntity
            {
                Id = 1,
                TaxCode = "GST18",
                TaxName = "GST 18%",
                TaxPercentage = 18.00m,
                EffectiveFromDate = date,
                EffectiveToDate = date.AddYears(1),
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = date,
                UpdatedBy = 2,
                UpdatedDate = date
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal("GST18", entity.TaxCode);
            Assert.Equal("GST 18%", entity.TaxName);
            Assert.Equal(18.00m, entity.TaxPercentage);
            Assert.Equal(date, entity.EffectiveFromDate);
            Assert.Equal(date.AddYears(1), entity.EffectiveToDate);
            Assert.True(entity.IsActive);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(date, entity.CreatedDate);
            Assert.Equal(2, entity.UpdatedBy);
            Assert.Equal(date, entity.UpdatedDate);
        }

        [Fact]
        public void Entity_DefaultValues_AreCorrect()
        {
            var entity = new GSTMasterEntity();
            Assert.Equal(0, entity.Id);
            Assert.Equal(string.Empty, entity.TaxCode);
            Assert.Equal(string.Empty, entity.TaxName);
            Assert.Equal(0m, entity.TaxPercentage);
            Assert.True(entity.IsActive);
            Assert.Null(entity.CreatedBy);
            Assert.Null(entity.CreatedDate);
            Assert.Null(entity.UpdatedBy);
            Assert.Null(entity.UpdatedDate);
        }

        #endregion

        #region DTO Tests

        [Fact]
        public void Dto_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var dto = new GSTDto
            {
                Id = 1,
                TaxCode = "GST18",
                TaxName = "GST 18%",
                TaxPercentage = 18.00m,
                EffectiveFromDate = date,
                EffectiveToDate = date.AddYears(1),
                IsActive = true,
                CreatedDate = date,
                UpdatedDate = date
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal("GST18", dto.TaxCode);
            Assert.Equal("GST 18%", dto.TaxName);
            Assert.Equal(18.00m, dto.TaxPercentage);
            Assert.Equal(date, dto.EffectiveFromDate);
            Assert.Equal(date.AddYears(1), dto.EffectiveToDate);
            Assert.True(dto.IsActive);
            Assert.Equal(date, dto.CreatedDate);
            Assert.Equal(date, dto.UpdatedDate);
        }

        [Fact]
        public void CreateDto_ValidData_PassesValidation()
        {
            var dto = new CreateGSTDto
            {
                TaxCode = "GST18",
                TaxName = "GST 18%",
                TaxPercentage = 18.00m,
                EffectiveFromDate = DateTime.Now,
                IsActive = true,
                CreatedBy = 1
            };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(null, "GST_TaxCode_Required")]
        [InlineData("", "GST_TaxCode_Required")]
        public void CreateDto_InvalidTaxCode_FailsValidation(string? taxCode, string expectedError)
        {
            var dto = new CreateGSTDto 
            { 
                TaxCode = taxCode!, 
                TaxName = "GST 18%", 
                TaxPercentage = 18m, 
                EffectiveFromDate = DateTime.Now 
            };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.False(isValid);
            Assert.Contains(results, v => v.ErrorMessage == expectedError);
        }

        [Fact]
        public void CreateDto_TaxCodeTooLong_FailsValidation()
        {
            var dto = new CreateGSTDto
            {
                TaxCode = new string('A', 51),
                TaxName = "GST 18%",
                TaxPercentage = 18m,
                EffectiveFromDate = DateTime.Now
            };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.False(isValid);
            Assert.Contains(results, v => v.ErrorMessage == "GST_TaxCode_MaxLen_50");
        }

        [Fact]
        public void CreateDto_TaxNameTooLong_FailsValidation()
        {
            var dto = new CreateGSTDto
            {
                TaxCode = "GST18",
                TaxName = new string('A', 101),
                TaxPercentage = 18m,
                EffectiveFromDate = DateTime.Now
            };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.False(isValid);
            Assert.Contains(results, v => v.ErrorMessage == "GST_TaxName_MaxLen_100");
        }

        [Fact]
        public void CreateDto_TaxPercentageOutOfRange_FailsValidation()
        {
            var dto = new CreateGSTDto
            {
                TaxCode = "GST18",
                TaxName = "GST 18%",
                TaxPercentage = 150m,
                EffectiveFromDate = DateTime.Now
            };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.False(isValid);
            Assert.Contains(results, v => v.ErrorMessage == "GST_TaxPercentage_Range");
        }

        [Fact]
        public void UpdateDto_ValidData_PassesValidation()
        {
            var dto = new UpdateGSTDto
            {
                TaxCode = "GST18",
                TaxName = "GST 18% Updated",
                TaxPercentage = 18.00m,
                EffectiveFromDate = DateTime.Now,
                IsActive = true,
                UpdatedBy = 1
            };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.True(isValid);
            Assert.Empty(results);
        }

        #endregion

        #region QueryParameters Tests

        [Fact]
        public void QueryParameters_Properties_WorkCorrectly()
        {
            var qp = new GSTQueryParameters
            {
                TaxCode = "GST18",
                TaxName = "GST",
                TaxPercentage = 18m,
                IsActive = true,
                PageNumber = 2,
                PageSize = 20,
                SearchTerm = "Test",
                SortBy = "TaxName"
            };
            Assert.Equal("GST18", qp.TaxCode);
            Assert.Equal("GST", qp.TaxName);
            Assert.Equal(18m, qp.TaxPercentage);
            Assert.True(qp.IsActive);
            Assert.Equal(2, qp.PageNumber);
            Assert.Equal(20, qp.PageSize);
            Assert.Equal("Test", qp.SearchTerm);
            Assert.Equal("TaxName", qp.SortBy);
        }

        #endregion

        #region Service CRUD Tests

        [Fact]
        public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
        {
            var entity = new GSTMasterEntity
            {
                Id = 1,
                TaxCode = "GST18",
                TaxName = "GST 18%",
                TaxPercentage = 18m,
                IsActive = true
            };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            var result = await _service.GetByIdAsync(1, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("GST18", result.TaxCode);
            Assert.Equal("GST 18%", result.TaxName);
        }

        [Fact]
        public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((GSTMasterEntity?)null);
            var result = await _service.GetByIdAsync(999, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task Service_GetAllAsync_ReturnsAllEntities()
        {
            var entities = new List<GSTMasterEntity>
            {
                new() { Id = 1, TaxCode = "GST18", TaxName = "GST 18%", TaxPercentage = 18m, IsActive = true },
                new() { Id = 2, TaxCode = "GST5", TaxName = "GST 5%", TaxPercentage = 5m, IsActive = true }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
            var qp = new GSTQueryParameters { PageNumber = 1, PageSize = 10 };
            var result = await _service.GetAllAsync(qp, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            var createDto = new CreateGSTDto
            {
                TaxCode = "GST18",
                TaxName = "GST 18%",
                TaxPercentage = 18m,
                EffectiveFromDate = DateTime.Now,
                IsActive = true,
                CreatedBy = 1
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<GSTMasterEntity>().BuildMockDbSet().Object);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<GSTMasterEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GSTMasterEntity e, CancellationToken _) => { e.Id = 1; return e; });
            var result = await _service.CreateAsync(createDto, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("GST18", result.TaxCode);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
        {
            var existingEntity = new GSTMasterEntity
            {
                Id = 1,
                TaxCode = "GST18",
                TaxName = "GST 18%",
                TaxPercentage = 18m,
                IsActive = true
            };
            var updateDto = new UpdateGSTDto
            {
                TaxCode = "GST18",
                TaxName = "GST 18% Updated",
                TaxPercentage = 18m,
                EffectiveFromDate = DateTime.Now,
                IsActive = true
            };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<GSTMasterEntity> { existingEntity }.BuildMockDbSet().Object);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<GSTMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal("GST 18% Updated", result.TaxName);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
        {
            var entity = new GSTMasterEntity { Id = 1, TaxCode = "GST18" };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<GSTMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var result = await _service.DeleteAsync(1, CancellationToken.None);
            Assert.True(result);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Controller Tests

        [Fact]
        public async Task Controller_GetAll_ReturnsOk()
        {
            var qp = new GSTQueryParameters();
            var pagedResult = new PagedResult<GSTDto>(new List<GSTDto>(), 0, 1, 10);
            var serviceMock = new Mock<IGSTService>();
            var loggerMock = new Mock<ILogger<GSTController>>();
            serviceMock.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);
            var ctrl = new GSTController(serviceMock.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, loggerMock.Object);
            var result = await ctrl.GetAll(qp, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Controller_GetById_ExistingId_ReturnsOk()
        {
            var serviceMock = new Mock<IGSTService>();
            var loggerMock = new Mock<ILogger<GSTController>>();
            serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new GSTDto { Id = 1 });
            var ctrl = new GSTController(serviceMock.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, loggerMock.Object);
            var result = await ctrl.GetById(1, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Controller_GetById_NonExistingId_ReturnsNotFound()
        {
            var serviceMock = new Mock<IGSTService>();
            var loggerMock = new Mock<ILogger<GSTController>>();
            serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((GSTDto?)null);
            var ctrl = new GSTController(serviceMock.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, loggerMock.Object);
            var result = await ctrl.GetById(999, CancellationToken.None);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Controller_Purge_ReturnsOk()
        {
            var serviceMock = new Mock<IGSTService>();
            var loggerMock = new Mock<ILogger<GSTController>>();
            _mockCleanupService.Setup(c => c.ForceHardDeleteAsync<GSTMasterEntity, int>(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var ctrl = new GSTController(serviceMock.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, loggerMock.Object);
            var result = await ctrl.Purge(1, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Controller_Create_ValidDto_ReturnsOk()
        {
            var serviceMock = new Mock<IGSTService>();
            var loggerMock = new Mock<ILogger<GSTController>>();
            serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateGSTDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GSTDto { Id = 1 });
            var ctrl = new GSTController(serviceMock.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, loggerMock.Object);
            var result = await ctrl.Create(new CreateGSTDto { TaxCode = "GST18", TaxName = "GST 18%", TaxPercentage = 18m, EffectiveFromDate = DateTime.Now }, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Controller_Update_ExistingId_ReturnsOk()
        {
            var serviceMock = new Mock<IGSTService>();
            var loggerMock = new Mock<ILogger<GSTController>>();
            serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateGSTDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GSTDto { Id = 1 });
            var ctrl = new GSTController(serviceMock.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, loggerMock.Object);
            var result = await ctrl.Update(1, new UpdateGSTDto { TaxCode = "GST18", TaxName = "GST 18% Updated", TaxPercentage = 18m, EffectiveFromDate = DateTime.Now }, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Controller_Delete_ExistingId_ReturnsOk()
        {
            var serviceMock = new Mock<IGSTService>();
            var loggerMock = new Mock<ILogger<GSTController>>();
            serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var ctrl = new GSTController(serviceMock.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, loggerMock.Object);
            var result = await ctrl.Delete(1, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void GSTMasterEntity_Properties_Coverage()
        {
            var date = DateTime.Now;
            var entity = new GSTMasterEntity
            {
                MarkedForDeletion = true,
                MarkedForDeletionDate = date
            };
            Assert.True(entity.MarkedForDeletion);
            Assert.Equal(date, entity.MarkedForDeletionDate);
        }

        #endregion
    }
}
