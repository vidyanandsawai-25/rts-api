using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Master.UseFactorCVMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs
{
    public class UseFactorCVMasterBulkDtosTests
    {
        [Fact]
        public void BulkCreateUseFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkCreateUseFactorCVMasterDto
            {
                UseFactors = new List<CreateUseFactorCVMasterDto>
                {
                    new() { TypeOfUseId = 1, SubTypeOfUseId = 2, Factor = 1.5m, YearRangeCVId = 3, IsActive = true }
                }
            };

            Assert.Single(dto.UseFactors);
            Assert.Equal(1, dto.UseFactors[0].TypeOfUseId);
        }

        [Fact]
        public void BulkDeleteUseFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkDeleteUseFactorCVMasterDto
            {
                Ids = new List<int> { 5, 6 }
            };

            Assert.Equal(new List<int> { 5, 6 }, dto.Ids);
        }

        [Fact]
        public void BulkUpdateUseFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUpdateUseFactorCVMasterDto
            {
                UseFactors = new List<BulkUpdateUseFactorCVMasterItemDto>
                {
                    new() { Id = 1, TypeOfUseId = 2, SubTypeOfUseId = 3, Factor = 2.0m, YearRangeCVId = 4 }
                }
            };

            Assert.Single(dto.UseFactors);
            Assert.Equal(1, dto.UseFactors[0].Id);
            Assert.Equal(2, dto.UseFactors[0].TypeOfUseId);
        }

        [Fact]
        public void BulkUpdateUseFactorCVMasterItemDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUpdateUseFactorCVMasterItemDto
            {
                Id = 1,
                TypeOfUseId = 2,
                SubTypeOfUseId = 3,
                Factor = 2.0m,
                YearRangeCVId = 4
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal(2, dto.TypeOfUseId);
            Assert.Equal(3, dto.SubTypeOfUseId);
            Assert.Equal(2.0m, dto.Factor);
            Assert.Equal(4, dto.YearRangeCVId);
        }

        [Fact]
        public void BulkUseFactorDeleteResultDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUseFactorDeleteResultDto
            {
                Success = true,
                Message = "deleted",
                Items = new List<int> { 1, 2 },
                Errors = new List<BulkUseFactorOperationErrorDto> { new() { Id = 1, Message = "err" } }
            };

            Assert.True(dto.Success);
            Assert.Equal("deleted", dto.Message);
            Assert.Equal(new List<int> { 1, 2 }, dto.Items);
            Assert.Single(dto.Errors);
            Assert.Equal(1, dto.Errors[0].Id);
        }

        [Fact]
        public void BulkUseFactorOperationErrorDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUseFactorOperationErrorDto
            {
                Id = 5,
                Index = 2,
                Message = "msg"
            };

            Assert.Equal(5, dto.Id);
            Assert.Equal(2, dto.Index);
            Assert.Equal("msg", dto.Message);
        }

        [Fact]
        public void BulkUseFactorOperationResultDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUseFactorOperationResultDto
            {
                Success = true,
                Message = "ok",
                Items = new List<UseFactorCVMasterDto> { new() { TypeOfUseId = 1, SubTypeOfUseId = 2, Factor = 1.1m, YearRangeCVId = 3 } },
                Errors = new List<BulkUseFactorOperationErrorDto> { new() { Id = 1, Message = "err" } }
            };

            Assert.True(dto.Success);
            Assert.Equal("ok", dto.Message);
            Assert.Single(dto.Items);
            Assert.Equal(1, dto.Items[0].TypeOfUseId);
            Assert.Single(dto.Errors);
            Assert.Equal(1, dto.Errors[0].Id);
        }
    }
}
