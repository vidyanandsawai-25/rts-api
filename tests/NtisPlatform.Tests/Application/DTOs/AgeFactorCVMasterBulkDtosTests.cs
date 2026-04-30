using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Master.AgeFactorCVMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs
{
    public class AgeFactorCVMasterBulkDtosTests
    {
        [Fact]
        public void BulkAgeFactorDeleteResultDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkAgeFactorDeleteResultDto
            {
                Success = true,
                Message = "deleted",
                Items = new List<int> { 1, 2 },
                Errors = new List<BulkAgeFactorOperationErrorDto> { new() { Id = 1, Message = "err" } }
            };

            Assert.True(dto.Success);
            Assert.Equal("deleted", dto.Message);
            Assert.Equal(new List<int> { 1, 2 }, dto.Items);
            Assert.Single(dto.Errors);
            Assert.Equal(1, dto.Errors[0].Id);
        }

        [Fact]
        public void BulkAgeFactorOperationErrorDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkAgeFactorOperationErrorDto
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
        public void BulkAgeFactorOperationResultDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkAgeFactorOperationResultDto
            {
                Success = true,
                Message = "ok",
                Items = new List<AgeFactorCVMasterDto> { new() { ConstructionTypeId = 1, AgeFrom = 2, AgeTo = 3, Factor = 1.1m, YearRangeCVId = 4 } },
                Errors = new List<BulkAgeFactorOperationErrorDto> { new() { Id = 1, Message = "err" } }
            };

            Assert.True(dto.Success);
            Assert.Equal("ok", dto.Message);
            Assert.Single(dto.Items);
            Assert.Equal(1, dto.Items[0].ConstructionTypeId);
            Assert.Single(dto.Errors);
            Assert.Equal(1, dto.Errors[0].Id);
        }

        [Fact]
        public void BulkCreateAgeFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkCreateAgeFactorCVMasterDto
            {
                AgeFactors = new List<CreateAgeFactorCVMasterDto>
                {
                    new() { ConstructionTypeId = 1, AgeFrom = 10, AgeTo = 20, Factor = 1.5m, YearRangeCVId = 2 }
                }
            };

            Assert.Single(dto.AgeFactors);
            Assert.Equal(1, dto.AgeFactors[0].ConstructionTypeId);
        }

        [Fact]
        public void BulkDeleteAgeFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkDeleteAgeFactorCVMasterDto
            {
                Ids = new List<int> { 5, 6 }
            };

            Assert.Equal(new List<int> { 5, 6 }, dto.Ids);
        }

        [Fact]
        public void BulkUpdateAgeFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUpdateAgeFactorCVMasterDto
            {
                AgeFactors = new List<BulkUpdateAgeFactorCVMasterItemDto>
                {
                    new() { Id = 1, ConstructionTypeId = 2, AgeFrom = 10, AgeTo = 20, Factor = 2.0m, YearRangeCVId = 3 }
                }
            };

            Assert.Single(dto.AgeFactors);
            Assert.Equal(1, dto.AgeFactors[0].Id);
            Assert.Equal(2, dto.AgeFactors[0].ConstructionTypeId);
        }

        [Fact]
        public void BulkUpdateAgeFactorCVMasterItemDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUpdateAgeFactorCVMasterItemDto
            {
                Id = 1,
                ConstructionTypeId = 2,
                AgeFrom = 10,
                AgeTo = 20,
                Factor = 2.0m,
                YearRangeCVId = 3
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal(2, dto.ConstructionTypeId);
            Assert.Equal(10, dto.AgeFrom);
            Assert.Equal(20, dto.AgeTo);
            Assert.Equal(2.0m, dto.Factor);
            Assert.Equal(3, dto.YearRangeCVId);
        }
    }
}
