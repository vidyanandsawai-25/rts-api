using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs
{
    public class NatureFactorCVMasterBulkDtosTests
    {
        [Fact]
        public void BulkCreateNatureFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkCreateNatureFactorCVMasterDto
            {
                NatureFactors = new List<CreateNatureFactorCVMasterDto>
                {
                    new() { Factor = 1.5m, YearRangeCVId = 2, IsActive = true }
                }
            };

            Assert.Single(dto.NatureFactors);
            Assert.Equal(1.5m, dto.NatureFactors[0].Factor);
            Assert.Equal(2, dto.NatureFactors[0].YearRangeCVId);
            Assert.True(dto.NatureFactors[0].IsActive);
        }

        [Fact]
        public void BulkDeleteNatureFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkDeleteNatureFactorCVMasterDto
            {
                NatureFactorIds = new List<int> { 5, 6 }
            };

            Assert.Equal(new List<int> { 5, 6 }, dto.NatureFactorIds);
        }

        [Fact]
        public void BulkNatureFactorDeleteResultDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkNatureFactorDeleteResultDto
            {
                Success = true,
                Message = "deleted",
                Items = new List<int> { 1, 2 },
                Errors = new List<BulkNatureFactorOperationErrorDto> { new() { NatureFactorId = 1, Message = "err" } }
            };

            Assert.True(dto.Success);
            Assert.Equal("deleted", dto.Message);
            Assert.Equal(new List<int> { 1, 2 }, dto.Items);
            Assert.Single(dto.Errors);
            Assert.Equal(1, dto.Errors[0].NatureFactorId);
        }

        [Fact]
        public void BulkNatureFactorOperationErrorDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkNatureFactorOperationErrorDto
            {
                NatureFactorId = 5,
                Index = 2,
                Message = "msg"
            };

            Assert.Equal(5, dto.NatureFactorId);
            Assert.Equal(2, dto.Index);
            Assert.Equal("msg", dto.Message);
        }

        [Fact]
        public void BulkNatureFactorOperationResultDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkNatureFactorOperationResultDto
            {
                Success = true,
                Message = "ok",
                Items = new List<NatureFactorCVMasterDto> { new() { Id = 1, Factor = 1.1m, YearRangeCVId = 2 } },
                Errors = new List<BulkNatureFactorOperationErrorDto> { new() { NatureFactorId = 1, Message = "err" } }
            };

            Assert.True(dto.Success);
            Assert.Equal("ok", dto.Message);
            Assert.Single(dto.Items);
            Assert.Equal(1, dto.Items[0].Id);
            Assert.Single(dto.Errors);
            Assert.Equal(1, dto.Errors[0].NatureFactorId);
        }

        [Fact]
        public void BulkUpdateNatureFactorCVMasterDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUpdateNatureFactorCVMasterDto
            {
                NatureFactors = new List<BulkUpdateNatureFactorCVMasterItemDto>
                {
                    new() { NatureFactorId = 1, ConstructionTypeId = 2, Factor = 2.0m, YearRangeCVId = 3 }
                }
            };

            Assert.Single(dto.NatureFactors);
            Assert.Equal(1, dto.NatureFactors[0].NatureFactorId);
            Assert.Equal(2, dto.NatureFactors[0].ConstructionTypeId);
        }

        [Fact]
        public void BulkUpdateNatureFactorCVMasterItemDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new BulkUpdateNatureFactorCVMasterItemDto
            {
                NatureFactorId = 1,
                ConstructionTypeId = 2,
                Factor = 2.0m,
                YearRangeCVId = 3
            };

            Assert.Equal(1, dto.NatureFactorId);
            Assert.Equal(2, dto.ConstructionTypeId);
            Assert.Equal(2.0m, dto.Factor);
            Assert.Equal(3, dto.YearRangeCVId);
        }
    }
}
