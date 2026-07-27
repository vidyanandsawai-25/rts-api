using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Rules
{
    public class RateLookupApplicatorTests
    {
        private readonly Mock<IRepository<RateEntity, int>> _mockRateRepo;
        private readonly RateLookupApplicator _applicator;

        public RateLookupApplicatorTests()
        {
            _mockRateRepo = new Mock<IRepository<RateEntity, int>>();
            _applicator = new RateLookupApplicator(_mockRateRepo.Object, NullLogger<RateLookupApplicator>.Instance);
        }

        [Fact]
        public void CanHandle_MatchesRateLookupVariants()
        {
            Assert.True(_applicator.CanHandle("RateLookup"));
            Assert.True(_applicator.CanHandle("Rate Lookup"));
            Assert.True(_applicator.CanHandle("rate_lookup"));
            Assert.False(_applicator.CanHandle("Decrease %"));
        }

        [Fact]
        public async Task Apply_LooksUpRateAndAppliesPercentage()
        {
            // GIVEN Master Rate table has Residential (TypeOfUseGroupId = 1) in TaxZone 1, ConstructionType 2 with Rate = 500
            var rates = new List<RateEntity>
            {
                new RateEntity
                {
                    Id = 1,
                    TypeOfUseGroupId = 1,
                    TaxZoneId = 1,
                    ConstructionTypeId = 2,
                    YearRangeRVId = 1,
                    RateSquareMeter = 500m,
                    IsActive = true
                }
            }.AsQueryable();

            _mockRateRepo.Setup(r => r.GetQueryable()).Returns(rates);

            // GIVEN Input dictionary (current calculation context)
            var inputDict = new Dictionary<string, object>
            {
                ["TaxZone"] = 1,
                ["Construction Type"] = 2,
                ["YearRangeRVId"] = 1
            };

            // GIVEN Rule Context (asking to lookup Residential RateTypeOfUseGroupId = 1)
            var ruleContext = new Dictionary<string, object>
            {
                ["effectType"] = "RateLookup",
                ["value"] = "10",
                ["RateTypeOfUseGroupId"] = "1"
            };

            _applicator.SetInputDictionary(inputDict);
            _applicator.SetLookupContext(ruleContext);

            // WHEN Apply(baseRate = 0, effectValue = 10) is called
            decimal result = await _applicator.Apply(0m, 10m);

            // THEN 500 * (10 / 100) = 50.00
            Assert.Equal(50m, result);
            Assert.Equal(500m, _applicator.ReferenceRate);
        }

        [Fact]
        public void RuleJsonBuilder_Build_RateLookup_PreservesRateTypeOfUseGroupId()
        {
            var conditions = @"{ ""logicalOperator"": ""AND"", ""conditions"": [ { ""fieldId"": ""TypeOfUseGroupId"", ""operator"": ""EQUALS"", ""value"": 5 } ] }";
            var effectJson = @"{ ""effectType"": ""RateLookup"", ""value"": ""10"", ""RateTypeOfUseGroupId"": 1 }";

            var ruleJson = RuleJsonBuilder.Build("Commercial Parking Rule", "RULE-PARK-001", true, "RV", conditions, effectJson);

            Assert.Contains("RateLookup", ruleJson);
            Assert.Contains("RateTypeOfUseGroupId", ruleJson);
        }
    }
}
