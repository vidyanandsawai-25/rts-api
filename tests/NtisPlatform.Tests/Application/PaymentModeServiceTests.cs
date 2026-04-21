using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.PaymentMode;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Tests.Application
{
    public class PaymentModeServiceTests
    {
        private readonly Mock<IRepository<PaymentModeEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly PaymentModeService _service;

        public PaymentModeServiceTests()
        {
            _mockRepository = new Mock<IRepository<PaymentModeEntity, int>>();
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

            _service = new PaymentModeService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            var entity = new PaymentModeEntity
            {
                Id = 1,
                Code = "UPI",
                PaymentModeName = "UPI Payment",
                Type = "Online",
                Category = "Digital",
                Description = "Payment through UPI",
                ChargeType = "Percentage",
                TransactionCharge = 5,
                IsActive = true
            };

            var expectedDto = new PaymentModeDto
            {
                Id = 1,
                Code = "UPI",
                PaymentModeName = "UPI Payment",
                Type = "Online",
                Category = "Digital",
                Description = "Payment through UPI",
                ChargeType = "Percentage",
                TransactionCharge = 5,
                IsActive = true
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper
                .Setup(m => m.Map<PaymentModeDto>(It.IsAny<PaymentModeEntity>()))
                .Returns(expectedDto);

            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(expectedDto.Id, result!.Id);
            Assert.Equal(expectedDto.Code, result.Code);
            Assert.Equal(expectedDto.PaymentModeName, result.PaymentModeName);
            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_AddsAndReturnsDto()
        {
            var createDto = new CreatePaymentModeDto
            {
                Code = "CARD",
                PaymentModeName = "Card Payment",
                Type = "POS",
                Category = "Digital",
                Description = "Payment through card",
                ChargeType = "Fixed",
                TransactionCharge = 2,
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<PaymentModeEntity>(It.IsAny<CreatePaymentModeDto>()))
                .Returns((CreatePaymentModeDto src) => new PaymentModeEntity
                {
                    Code = src.Code,
                    PaymentModeName = src.PaymentModeName,
                    Type = src.Type,
                    Category = src.Category,
                    Description = src.Description,
                    ChargeType = src.ChargeType,
                    TransactionCharge = src.TransactionCharge,
                    IsActive = src.IsActive,
                    CreatedBy = src.CreatedBy
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<PaymentModeEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentModeEntity b, CancellationToken _) => b);

            var returnedDto = new PaymentModeDto
            {
                Id = 0,
                Code = createDto.Code,
                PaymentModeName = createDto.PaymentModeName,
                Type = createDto.Type,
                Category = createDto.Category,
                Description = createDto.Description,
                ChargeType = createDto.ChargeType,
                TransactionCharge = createDto.TransactionCharge,
                IsActive = createDto.IsActive
            };

            _mockMapper
                .Setup(m => m.Map<PaymentModeDto>(It.IsAny<PaymentModeEntity>()))
                .Returns(returnedDto);

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(createDto.Code, result.Code);
            Assert.Equal(createDto.PaymentModeName, result.PaymentModeName);
            _mockRepository.Verify(
                r => r.AddAsync(
                    It.Is<PaymentModeEntity>(b =>
                        b.Code == createDto.Code &&
                        b.PaymentModeName == createDto.PaymentModeName),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExistingId_UpdatesAndReturnsDto()
        {
            var existing = new PaymentModeEntity
            {
                Id = 2,
                Code = "CASH",
                PaymentModeName = "Cash Payment",
                Type = "Offline",
                Category = "Manual",
                Description = "Cash payment",
                ChargeType = "Fixed",
                TransactionCharge = 0,
                IsActive = true
            };

            var updateDto = new UpdatePaymentModeDto
            {
                Code = "NETBANK",
                PaymentModeName = "Net Banking",
                Type = "Online",
                Category = "Banking",
                Description = "Payment through net banking",
                ChargeType = "Percentage",
                TransactionCharge = 3,
                IsActive = true,
                UpdatedBy = 1
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdatePaymentModeDto>(), It.IsAny<PaymentModeEntity>()))
                .Returns((UpdatePaymentModeDto src, PaymentModeEntity dest) =>
                {
                    dest.Code = src.Code;
                    dest.PaymentModeName = src.PaymentModeName;
                    dest.Type = src.Type;
                    dest.Category = src.Category;
                    dest.Description = src.Description;
                    dest.ChargeType = src.ChargeType;
                    dest.TransactionCharge = src.TransactionCharge;
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;
                    return dest;
                });

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<PaymentModeEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var expectedDto = new PaymentModeDto
            {
                Id = 2,
                Code = updateDto.Code,
                PaymentModeName = updateDto.PaymentModeName,
                Type = updateDto.Type,
                Category = updateDto.Category,
                Description = updateDto.Description,
                ChargeType = updateDto.ChargeType,
                TransactionCharge = updateDto.TransactionCharge,
                IsActive = updateDto.IsActive
            };

            _mockMapper
                .Setup(m => m.Map<PaymentModeDto>(It.IsAny<PaymentModeEntity>()))
                .Returns(expectedDto);

            var result = await _service.UpdateAsync(2, updateDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(updateDto.Code, result!.Code);
            Assert.Equal(updateDto.PaymentModeName, result.PaymentModeName);
            _mockRepository.Verify(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(
                r => r.UpdateAsync(
                    It.Is<PaymentModeEntity>(b =>
                        b.Code == updateDto.Code &&
                        b.PaymentModeName == updateDto.PaymentModeName),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_DeletesAndReturnsTrue()
        {
            var existing = new PaymentModeEntity
            {
                Id = 3,
                Code = "DELETE",
                PaymentModeName = "To Delete"
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _mockRepository
                .Setup(r => r.DeleteAsync(3, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(3, CancellationToken.None);

            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            _mockRepository
                .Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentModeEntity?)null);

            var result = await _service.DeleteAsync(4, CancellationToken.None);

            Assert.False(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentModeEntity?)null);
            var result = await _service.GetByIdAsync(999, CancellationToken.None);
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task UpdateAsync_NonExistingId_ReturnsNull()
        {
            var updateDto = new UpdatePaymentModeDto
            {
                Code = "UPD",
                PaymentModeName = "Updated Payment Mode",
                Type = "Online",
                Category = "Digital",
                Description = "Updated description",
                ChargeType = "Fixed",
                TransactionCharge = 1,
                IsActive = true,
                UpdatedBy = 1
            };
            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaymentModeEntity?)null);
            var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PaymentModeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
