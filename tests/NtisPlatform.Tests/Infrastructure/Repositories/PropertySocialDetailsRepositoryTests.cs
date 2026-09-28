using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories.Property;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

public class PropertySocialDetailsRepositoryTests
{
    [Fact]
    public async Task GetSocialDetailsByFiltersAsync_ReturnsDirectScopedMatchesBeforeHierarchyFallback()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        const int societyDetailId = 1510588;
        const int wingDetailId = 1517645;
        const int socialAttributeId = 17;

        context.Set<SocialAttributeEntity>().Add(new SocialAttributeEntity
        {
            Id = socialAttributeId,
            SocialAttributeCode = "HAS_TEST_ATTRIBUTE",
            SocialAttributeName = "Has test attribute",
            DataType = "bool",
            IsActive = true
        });

        context.Set<SocietyDetailsEntity>().Add(new SocietyDetailsEntity
        {
            Id = societyDetailId,
            PropertyId = 500,
            IsActive = true
        });

        context.Set<WingDetailsMastEntity>().Add(new WingDetailsMastEntity
        {
            Id = wingDetailId,
            SocietyDetailsMastId = societyDetailId,
            WingMasterId = 10,
            IsActive = true
        });

        context.PropertyMast.AddRange(
            new PropertyEntity
            {
                Id = 101,
                WardId = 1,
                TaxZoneId = 1,
                WingDetailId = wingDetailId,
                IsActive = true
            },
            new PropertyEntity
            {
                Id = 102,
                WardId = 1,
                TaxZoneId = 1,
                WingDetailId = wingDetailId,
                IsActive = true
            },
            new PropertyEntity
            {
                Id = 103,
                WardId = 1,
                TaxZoneId = 1,
                WingDetailId = 999,
                IsActive = true
            });

        context.Set<PropertySocialDetailsEntity>().AddRange(
            new PropertySocialDetailsEntity
            {
                Id = 1,
                PropertyId = 101,
                SocialAttributeId = socialAttributeId,
                WingDetailId = wingDetailId,
                SocietyDetailId = societyDetailId,
                BitValue = true,
                IsActive = true
            },
            new PropertySocialDetailsEntity
            {
                Id = 2,
                PropertyId = 102,
                SocialAttributeId = socialAttributeId,
                WingDetailId = wingDetailId,
                SocietyDetailId = societyDetailId,
                BitValue = false,
                IsActive = true
            },
            new PropertySocialDetailsEntity
            {
                Id = 3,
                PropertyId = 103,
                SocialAttributeId = socialAttributeId,
                BitValue = true,
                IsActive = true
            });

        await context.SaveChangesAsync();

        var repository = new PropertySocialDetailsRepository(context);

        var result = await repository.GetSocialDetailsByFiltersAsync(
            socialAttributeId,
            societyDetailId,
            wingDetailId);

        Assert.Equal(new[] { 101, 102 }, result.Select(x => x.PropertyId).OrderBy(x => x));
        Assert.All(result, item => Assert.Equal(socialAttributeId, item.SocialAttributeId));
        Assert.All(result, item => Assert.Equal(wingDetailId, item.WingDetailId));
        Assert.All(result, item => Assert.Equal(societyDetailId, item.SocietyDetailId));
    }

    [Fact]
    public async Task GetSocialDetailsByFiltersAsync_UsesPropertyWingWhenNoDirectScopedMatchesExist()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        const int societyDetailId = 1510588;
        const int wingDetailId = 1517645;
        const int socialAttributeId = 17;

        context.Set<SocialAttributeEntity>().Add(new SocialAttributeEntity
        {
            Id = socialAttributeId,
            SocialAttributeCode = "HAS_TEST_ATTRIBUTE",
            SocialAttributeName = "Has test attribute",
            DataType = "bool",
            IsActive = true
        });

        context.Set<SocietyDetailsEntity>().Add(new SocietyDetailsEntity
        {
            Id = societyDetailId,
            PropertyId = 500,
            IsActive = true
        });

        context.Set<WingDetailsMastEntity>().Add(new WingDetailsMastEntity
        {
            Id = wingDetailId,
            SocietyDetailsMastId = societyDetailId,
            WingMasterId = 10,
            IsActive = true
        });

        context.PropertyMast.AddRange(
            new PropertyEntity
            {
                Id = 101,
                WardId = 1,
                TaxZoneId = 1,
                WingDetailId = wingDetailId,
                IsActive = true
            },
            new PropertyEntity
            {
                Id = 102,
                WardId = 1,
                TaxZoneId = 1,
                WingDetailId = wingDetailId,
                IsActive = true
            });

        context.Set<PropertySocialDetailsEntity>().AddRange(
            new PropertySocialDetailsEntity
            {
                Id = 1,
                PropertyId = 101,
                SocialAttributeId = socialAttributeId,
                BitValue = true,
                IsActive = true
            },
            new PropertySocialDetailsEntity
            {
                Id = 2,
                PropertyId = 102,
                SocialAttributeId = socialAttributeId,
                BitValue = false,
                IsActive = true
            });

        await context.SaveChangesAsync();

        var repository = new PropertySocialDetailsRepository(context);

        var result = await repository.GetSocialDetailsByFiltersAsync(
            socialAttributeId,
            societyDetailId,
            wingDetailId);

        Assert.Equal(new[] { 101, 102 }, result.Select(x => x.PropertyId).OrderBy(x => x));
        Assert.All(result, item => Assert.Equal(socialAttributeId, item.SocialAttributeId));
    }
}
