using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Master.CSNDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application
{
    public class RateMasterForCVServiceTest
    {
        private readonly Mock<IRepository<RateMasterForCVEntity, int>> _mockRepository;
        private readonly Mock<IRepository<CSNDetailsEntity, int>> _mockCsnDetailsRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHardDeleteCleanupService> _mockHardDeleteCleanupService;
        private readonly RateMasterForCVService _service;

        public RateMasterForCVServiceTest()
        {
            _mockRepository = new Mock<IRepository<RateMasterForCVEntity, int>>();
            _mockCsnDetailsRepository = new Mock<IRepository<CSNDetailsEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockHardDeleteCleanupService = new Mock<IHardDeleteCleanupService>();

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockUnitOfWork
                .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHardDeleteCleanupService
                .Setup(h => h.ForceHardDeleteAsync<CSNDetailsEntity, int>(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _service = new RateMasterForCVService(
                _mockRepository.Object,
                _mockCsnDetailsRepository.Object,
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockHardDeleteCleanupService.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCSNDetails()
        {
            var entity = new RateMasterForCVEntity
            {
                Id = 43,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 5000.00m,
                IsActive = true,
                CSNDetails = new List<CSNDetailsEntity>
                {
                    new()
                    {
                        Id = 101,
                        RateMasterCVId = 43,
                        CSN = "COM-004",
                        IsActive = true
                    },
                    new()
                    {
                        Id = 102,
                        RateMasterCVId = 43,
                        CSN = "COM-005",
                        IsActive = true
                    }
                }
            };

            var entities = new List<RateMasterForCVEntity> { entity };
            _mockRepository
                .Setup(r => r.GetQueryable())
                .Returns(entities.BuildMock());

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    Id = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupCVId = e.TypeOfUseGroupCVId,
                    FloorGroupId = e.FloorGroupId,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    RateAmount = e.RateAmount,
                    IsActive = e.IsActive,
                    CSNDetails = e.CSNDetails.Select(c => new CSNDetailsDto
                    {
                        Id = c.Id,
                        RateMasterCVId = c.RateMasterCVId,
                        CSN = c.CSN ?? string.Empty,
                        IsActive = c.IsActive
                    }).ToList()
                });

            var result = await _service.GetByIdAsync(43, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(43, result!.Id);
            Assert.Equal(16, result.SubZoneId);
            Assert.Equal(7, result.TypeOfUseGroupCVId);
            Assert.Equal(2, result.FloorGroupId);
            Assert.Equal(5, result.AssessmentYearRangeId);
            Assert.Equal(5000.00m, result.RateAmount);
            Assert.Equal(2, result.CSNDetails.Count);
            Assert.Contains(result.CSNDetails, x => x.CSN == "COM-004");
            Assert.Contains(result.CSNDetails, x => x.CSN == "COM-005");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var entities = new List<RateMasterForCVEntity>();

            _mockRepository
                .Setup(r => r.GetQueryable())
                .Returns(entities.BuildMock());

            var result = await _service.GetByIdAsync(999, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_CreatesParentAndSplitCSNDetails()
        {
            var createDto = new CreateRateMasterForCVDto
            {
                IsActive = true,
                CreatedBy = 1,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 6850.75m,
                CSNDetails = new List<CreateCSNDetailsDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedBy = 1,
                        CSN = "COM-001, COM-002"
                    }
                }
            };

            var savedEntity = new RateMasterForCVEntity
            {
                Id = 43,
                IsActive = true,
                CreatedBy = 1,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 6850.75m,
                CSNDetails = new List<CSNDetailsEntity>()
            };

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVEntity>(It.IsAny<CreateRateMasterForCVDto>()))
                .Returns(savedEntity);

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateMasterForCVEntity e, CancellationToken _) =>
                {
                    e.Id = 43;
                    return e;
                });

            _mockMapper
                .Setup(m => m.Map<CSNDetailsEntity>(It.IsAny<CreateCSNDetailsDto>()))
                .Returns((CreateCSNDetailsDto dto) => new CSNDetailsEntity
                {
                    CSN = dto.CSN,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy
                });

            var reloadEntity = new RateMasterForCVEntity
            {
                Id = 43,
                IsActive = true,
                CreatedBy = 1,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 6850.75m,
                CSNDetails = new List<CSNDetailsEntity>
                {
                    new() { Id = 1, RateMasterCVId = 43, CSN = "COM-001", IsActive = true },
                    new() { Id = 2, RateMasterCVId = 43, CSN = "COM-002", IsActive = true }
                }
            };

            _mockRepository
                .Setup(r => r.GetQueryable())
                .Returns(new List<RateMasterForCVEntity> { reloadEntity }.BuildMock());

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    Id = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupCVId = e.TypeOfUseGroupCVId,
                    FloorGroupId = e.FloorGroupId,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    RateAmount = e.RateAmount,
                    IsActive = e.IsActive,
                    CSNDetails = e.CSNDetails.Select(c => new CSNDetailsDto
                    {
                        Id = c.Id,
                        RateMasterCVId = c.RateMasterCVId,
                        CSN = c.CSN ?? string.Empty,
                        IsActive = c.IsActive
                    }).ToList()
                });

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(43, result.Id);
            Assert.Equal(16, result.SubZoneId);
            Assert.Equal(7, result.TypeOfUseGroupCVId);
            Assert.Equal(2, result.FloorGroupId);
            Assert.Equal(5, result.AssessmentYearRangeId);
            Assert.Equal(6850.75m, result.RateAmount);
            Assert.Equal(2, result.CSNDetails.Count);

            _mockRepository.Verify(
                r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.Is<CSNDetailsEntity>(x =>
                    x.RateMasterCVId == 43 && x.CSN == "COM-001"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.Is<CSNDetailsEntity>(x =>
                    x.RateMasterCVId == 43 && x.CSN == "COM-002"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockUnitOfWork.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateAsync_ExistingEntity_DeletesOldCSNDetailsAndAddsNewCSNDetails()
        {
            var existingEntity = new RateMasterForCVEntity
            {
                Id = 43,
                IsActive = true,
                CreatedBy = 1,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 5000.00m,
                CSNDetails = new List<CSNDetailsEntity>
                {
                    new() { Id = 201, RateMasterCVId = 43, CSN = "OLD-COM-001", IsActive = true },
                    new() { Id = 202, RateMasterCVId = 43, CSN = "OLD-COM-002", IsActive = true }
                }
            };

            var updateDto = new UpdateRateMasterForCVDto
            {
                IsActive = true,
                UpdatedBy = 1,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 5500.00m,
                CSNDetails = new List<UpdateCSNDetailsDto>
                {
                    new()
                    {
                        IsActive = true,
                        UpdatedBy = 1,
                        RateMasterCVId = 43,
                        CSN = "COM-004, COM-005"
                    }
                }
            };

            _mockRepository
                .SetupSequence(r => r.GetQueryable())
                .Returns(new List<RateMasterForCVEntity> { existingEntity }.BuildMock())
                .Returns(new List<RateMasterForCVEntity> { existingEntity }.BuildMock());

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRateMasterForCVDto>(), It.IsAny<RateMasterForCVEntity>()))
                .Callback((UpdateRateMasterForCVDto src, RateMasterForCVEntity dest) =>
                {
                    dest.SubZoneId = src.SubZoneId;
                    dest.TypeOfUseGroupCVId = src.TypeOfUseGroupCVId;
                    dest.FloorGroupId = src.FloorGroupId;
                    dest.AssessmentYearRangeId = src.AssessmentYearRangeId;
                    dest.RateAmount = src.RateAmount;
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;
                    dest.UpdatedDate = DateTime.Now;
                });

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockCsnDetailsRepository
                .Setup(r => r.GetQueryable())
                .Returns(existingEntity.CSNDetails.BuildMock());

            _mockMapper
                .Setup(m => m.Map<CSNDetailsEntity>(It.IsAny<UpdateCSNDetailsDto>()))
                .Returns((UpdateCSNDetailsDto dto) => new CSNDetailsEntity
                {
                    CSN = dto.CSN,
                    IsActive = dto.IsActive,
                    UpdatedBy = dto.UpdatedBy
                });

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    Id = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupCVId = e.TypeOfUseGroupCVId,
                    FloorGroupId = e.FloorGroupId,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    RateAmount = e.RateAmount,
                    IsActive = e.IsActive
                });

            var result = await _service.UpdateAsync(43, updateDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(43, result!.Id);
            Assert.Equal(5500.00m, result.RateAmount);

            _mockRepository.Verify(
                r => r.UpdateAsync(It.Is<RateMasterForCVEntity>(x =>
                    x.Id == 43 &&
                    x.RateAmount == 5500.00m),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockHardDeleteCleanupService.Verify(
                h => h.ForceHardDeleteAsync<CSNDetailsEntity, int>(
                    201,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockHardDeleteCleanupService.Verify(
                h => h.ForceHardDeleteAsync<CSNDetailsEntity, int>(
                    202,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.Is<CSNDetailsEntity>(x =>
                    x.RateMasterCVId == 43 && x.CSN == "COM-004"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.Is<CSNDetailsEntity>(x =>
                    x.RateMasterCVId == 43 && x.CSN == "COM-005"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockUnitOfWork.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
        {
            var updateDto = new UpdateRateMasterForCVDto
            {
                IsActive = true,
                UpdatedBy = 1,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 5500.00m,
                CSNDetails = new List<UpdateCSNDetailsDto>
                {
                    new() { CSN = "COM-004" }
                }
            };

            _mockRepository
                .Setup(r => r.GetQueryable())
                .Returns(new List<RateMasterForCVEntity>().BuildMock());

            var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

            Assert.Null(result);

            _mockRepository.Verify(
                r => r.UpdateAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.IsAny<CSNDetailsEntity>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _mockUnitOfWork.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesChildCSNDetailsThenParent()
        {
            var rateMasterId = 43;

            var csnDetails = new List<CSNDetailsEntity>
            {
                new() { Id = 301, RateMasterCVId = rateMasterId, CSN = "COM-004", IsActive = true },
                new() { Id = 302, RateMasterCVId = rateMasterId, CSN = "COM-005", IsActive = true }
            };

            _mockCsnDetailsRepository
                .Setup(r => r.GetQueryable())
                .Returns(csnDetails.BuildMock());

            _mockCsnDetailsRepository
                .Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockRepository
                .Setup(r => r.DeleteAsync(rateMasterId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(rateMasterId, CancellationToken.None);

            Assert.True(result);

            _mockCsnDetailsRepository.Verify(
                r => r.DeleteAsync(301, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCsnDetailsRepository.Verify(
                r => r.DeleteAsync(302, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockRepository.Verify(
                r => r.DeleteAsync(rateMasterId, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockUnitOfWork.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BulkCreateAsync_ValidItems_CreatesParentsAndCSNDetails()
        {
            var items = new[]
            {
                new CreateRateMasterForCVDto
                {
                    IsActive = true,
                    CreatedBy = 1,
                    SubZoneId = 16,
                    TypeOfUseGroupCVId = 7,
                    FloorGroupId = 2,
                    AssessmentYearRangeId = 5,
                    RateAmount = 6850.75m,
                    CSNDetails = new List<CreateCSNDetailsDto>
                    {
                        new() { IsActive = true, CreatedBy = 1, CSN = "COM-001, COM-002" }
                    }
                },
                new CreateRateMasterForCVDto
                {
                    IsActive = true,
                    CreatedBy = 1,
                    SubZoneId = 10,
                    TypeOfUseGroupCVId = 6,
                    FloorGroupId = 1,
                    AssessmentYearRangeId = 2,
                    RateAmount = 2800.00m,
                    CSNDetails = new List<CreateCSNDetailsDto>
                    {
                        new() { IsActive = true, CreatedBy = 1, CSN = "RES-101, RES-102" }
                    }
                }
            };

            var generatedId = 42;

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVEntity>(It.IsAny<CreateRateMasterForCVDto>()))
                .Returns((CreateRateMasterForCVDto dto) => new RateMasterForCVEntity
                {
                    Id = 0,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy,
                    SubZoneId = dto.SubZoneId,
                    TypeOfUseGroupCVId = dto.TypeOfUseGroupCVId,
                    FloorGroupId = dto.FloorGroupId,
                    AssessmentYearRangeId = dto.AssessmentYearRangeId,
                    RateAmount = dto.RateAmount
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateMasterForCVEntity entity, CancellationToken _) =>
                {
                    entity.Id = ++generatedId;
                    return entity;
                });

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    Id = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupCVId = e.TypeOfUseGroupCVId,
                    FloorGroupId = e.FloorGroupId,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    RateAmount = e.RateAmount,
                    IsActive = e.IsActive
                });

            var result = await _service.BulkCreateAsync(items, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);

            _mockRepository.Verify(
                r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.IsAny<CSNDetailsEntity>(), It.IsAny<CancellationToken>()),
                Times.Exactly(4));
        }

        [Fact]
        public async Task BulkCreateAsync_WhenOneItemFails_ReturnsPartialFailureResult()
        {
            var items = new[]
            {
        new CreateRateMasterForCVDto
        {
            IsActive = true,
            CreatedBy = 1,
            SubZoneId = 16,
            TypeOfUseGroupCVId = 7,
            FloorGroupId = 2,
            AssessmentYearRangeId = 5,
            RateAmount = 6850.75m,
            CSNDetails = new List<CreateCSNDetailsDto>
            {
                new() { IsActive = true, CreatedBy = 1, CSN = "COM-001" }
            }
        },
        new CreateRateMasterForCVDto
        {
            IsActive = true,
            CreatedBy = 1,
            SubZoneId = 10,
            TypeOfUseGroupCVId = 6,
            FloorGroupId = 1,
            AssessmentYearRangeId = 2,
            RateAmount = 2800.00m,
            CSNDetails = new List<CreateCSNDetailsDto>
            {
                new() { IsActive = true, CreatedBy = 1, CSN = "RES-101" }
            }
        }
    };

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVEntity>(It.IsAny<CreateRateMasterForCVDto>()))
                .Returns((CreateRateMasterForCVDto dto) => new RateMasterForCVEntity
                {
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy,
                    SubZoneId = dto.SubZoneId,
                    TypeOfUseGroupCVId = dto.TypeOfUseGroupCVId,
                    FloorGroupId = dto.FloorGroupId,
                    AssessmentYearRangeId = dto.AssessmentYearRangeId,
                    RateAmount = dto.RateAmount
                });

            var addCallCount = 0;

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateMasterForCVEntity entity, CancellationToken _) =>
                {
                    addCallCount++;

                    if (addCallCount == 2)
                    {
                        throw new Exception("Create failed");
                    }

                    entity.Id = 43;
                    return entity;
                });

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    Id = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupCVId = e.TypeOfUseGroupCVId,
                    FloorGroupId = e.FloorGroupId,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    RateAmount = e.RateAmount,
                    IsActive = e.IsActive
                });

            var result = await _service.BulkCreateAsync(items, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(1, result.FailedCount);
            Assert.Single(result.Results);
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors, e => e.Contains("Create failed"));

            _mockRepository.Verify(
                r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.IsAny<CSNDetailsEntity>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _mockUnitOfWork.Verify(
                u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            _mockUnitOfWork.Verify(
                u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            _mockUnitOfWork.Verify(
                u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task BulkUpdateAsync_DuplicateCSNInSamePayload_InsertsOnlyOneBecauseOfDistinct()
        {
            var item = new BulkUpdateItem<int, UpdateRateMasterForCVDto>(43, new UpdateRateMasterForCVDto
            {
                IsActive = true,
                UpdatedBy = 1,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 5000.00m,
                CSNDetails = new List<UpdateCSNDetailsDto>
                {
                    new()
                    {
                        IsActive = true,
                        UpdatedBy = 1,
                        RateMasterCVId = 43,
                        CSN = "COM-004, COM-004"
                    }
                }
            });

            var entity = new RateMasterForCVEntity
            {
                Id = 43,
                IsActive = true,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 6850.75m
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(43, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRateMasterForCVDto>(), It.IsAny<RateMasterForCVEntity>()))
                .Callback((UpdateRateMasterForCVDto src, RateMasterForCVEntity dest) =>
                {
                    dest.SubZoneId = src.SubZoneId;
                    dest.TypeOfUseGroupCVId = src.TypeOfUseGroupCVId;
                    dest.FloorGroupId = src.FloorGroupId;
                    dest.AssessmentYearRangeId = src.AssessmentYearRangeId;
                    dest.RateAmount = src.RateAmount;
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;
                    dest.UpdatedDate = DateTime.Now;
                });

            _mockCsnDetailsRepository
                .Setup(r => r.GetQueryable())
                .Returns(new List<CSNDetailsEntity>().BuildMock());

            _mockMapper
                .Setup(m => m.Map<CSNDetailsEntity>(It.IsAny<UpdateCSNDetailsDto>()))
                .Returns((UpdateCSNDetailsDto dto) => new CSNDetailsEntity
                {
                    IsActive = dto.IsActive,
                    UpdatedBy = dto.UpdatedBy,
                    CSN = dto.CSN
                });

            _mockMapper
                .Setup(m => m.Map<List<RateMasterForCVDto>>(It.IsAny<List<RateMasterForCVEntity>>()))
                .Returns((List<RateMasterForCVEntity> list) => list.Select(e => new RateMasterForCVDto
                {
                    Id = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupCVId = e.TypeOfUseGroupCVId,
                    FloorGroupId = e.FloorGroupId,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    RateAmount = e.RateAmount,
                    IsActive = e.IsActive
                }).ToList());

            var result = await _service.BulkUpdateAsync(
                new[] { item },
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.Is<CSNDetailsEntity>(x =>
                    x.RateMasterCVId == 43 && x.CSN == "COM-004"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BulkUpdateAsync_DuplicateCSNAcrossSeparateDtos_InsertsDuplicateBecauseNoCrossDtoDistinct()
        {
            var item = new BulkUpdateItem<int, UpdateRateMasterForCVDto>(43, new UpdateRateMasterForCVDto
            {
                IsActive = true,
                UpdatedBy = 1,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 5000.00m,
                CSNDetails = new List<UpdateCSNDetailsDto>
        {
            new()
            {
                IsActive = true,
                UpdatedBy = 1,
                RateMasterCVId = 43,
                CSN = "COM-004"
            },
            new()
            {
                IsActive = true,
                UpdatedBy = 1,
                RateMasterCVId = 43,
                CSN = "COM-004"
            }
        }
            });

            var entity = new RateMasterForCVEntity
            {
                Id = 43,
                IsActive = true,
                SubZoneId = 16,
                TypeOfUseGroupCVId = 7,
                FloorGroupId = 2,
                AssessmentYearRangeId = 5,
                RateAmount = 6850.75m
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(43, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRateMasterForCVDto>(), It.IsAny<RateMasterForCVEntity>()))
                .Callback((UpdateRateMasterForCVDto src, RateMasterForCVEntity dest) =>
                {
                    dest.SubZoneId = src.SubZoneId;
                    dest.TypeOfUseGroupCVId = src.TypeOfUseGroupCVId;
                    dest.FloorGroupId = src.FloorGroupId;
                    dest.AssessmentYearRangeId = src.AssessmentYearRangeId;
                    dest.RateAmount = src.RateAmount;
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;
                    dest.UpdatedDate = DateTime.Now;
                });

            _mockCsnDetailsRepository
                .Setup(r => r.GetQueryable())
                .Returns(new List<CSNDetailsEntity>().BuildMock());

            _mockMapper
                .Setup(m => m.Map<CSNDetailsEntity>(It.IsAny<UpdateCSNDetailsDto>()))
                .Returns((UpdateCSNDetailsDto dto) => new CSNDetailsEntity
                {
                    IsActive = dto.IsActive,
                    UpdatedBy = dto.UpdatedBy,
                    CSN = dto.CSN
                });

            _mockMapper
                .Setup(m => m.Map<List<RateMasterForCVDto>>(It.IsAny<List<RateMasterForCVEntity>>()))
                .Returns((List<RateMasterForCVEntity> list) => list.Select(e => new RateMasterForCVDto
                {
                    Id = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupCVId = e.TypeOfUseGroupCVId,
                    FloorGroupId = e.FloorGroupId,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    RateAmount = e.RateAmount,
                    IsActive = e.IsActive
                }).ToList());

            var result = await _service.BulkUpdateAsync(new[] { item }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);

            _mockCsnDetailsRepository.Verify(
                r => r.AddAsync(It.Is<CSNDetailsEntity>(x =>
                    x.RateMasterCVId == 43 && x.CSN == "COM-004"),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task BulkDeleteAsync_ExistingIds_DeletesChildCSNDetailsThenParents()
        {
            var ids = new[] { 43, 44 };

            var csnDetails = new List<CSNDetailsEntity>
            {
                new() { Id = 401, RateMasterCVId = 43, CSN = "COM-004", IsActive = true },
                new() { Id = 402, RateMasterCVId = 43, CSN = "COM-005", IsActive = true },
                new() { Id = 403, RateMasterCVId = 44, CSN = "RES-101", IsActive = true }
            };

            _mockRepository
                .Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockCsnDetailsRepository
                .Setup(r => r.GetQueryable())
                .Returns(csnDetails.BuildMock());

            _mockCsnDetailsRepository
                .Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.BulkDeleteAsync(ids, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);

            _mockCsnDetailsRepository.Verify(
                r => r.DeleteAsync(401, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCsnDetailsRepository.Verify(
                r => r.DeleteAsync(402, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockCsnDetailsRepository.Verify(
                r => r.DeleteAsync(403, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockRepository.Verify(
                r => r.DeleteAsync(43, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockRepository.Verify(
                r => r.DeleteAsync(44, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockUnitOfWork.Verify(
                u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            _mockUnitOfWork.Verify(
                u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}