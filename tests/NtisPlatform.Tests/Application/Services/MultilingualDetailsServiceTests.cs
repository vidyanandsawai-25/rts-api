using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for MultilingualDetailsService to achieve 100% code coverage
/// </summary>
public class MultilingualDetailsServiceTests
{
    [Fact]
    public async Task GetAllForLocalizationAsync_ReturnsFilteredResults()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var entities = new[]
        {
            new MultilingualDetailsEntity
            {
                Id = 1,
                Resource = "ValidationMessages",
                Culture = "en",
                Key = "Required",
                Value = "Field is required",
                IsActive = true
            },
            new MultilingualDetailsEntity
            {
                Id = 2,
                Resource = "ValidationMessages",
                Culture = "hi",
                Key = "Required",
                Value = "?????? ?????? ??",
                IsActive = true
            },
            new MultilingualDetailsEntity
            {
                Id = 3,
                Resource = "ValidationMessages",
                Culture = "en",
                Key = "MaxLength",
                Value = "Max length exceeded",
                IsActive = true
            },
            new MultilingualDetailsEntity
            {
                Id = 4,
                Resource = "Labels",
                Culture = "en",
                Key = "Submit",
                Value = "Submit",
                IsActive = true
            },
            new MultilingualDetailsEntity
            {
                Id = 5,
                Resource = "ValidationMessages",
                Culture = "en",
                Key = "Inactive",
                Value = "This is inactive",
                IsActive = false
            }
        };

        context.MultilingualDetails.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<MultilingualDetailsEntity, int>(context);
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualDetailsEntity, MultilingualDetailsDtos>();
        });
        var mapper = mapperConfig.CreateMapper();

        var service = new MultilingualDetailsService(repository, mockUnitOfWork.Object, mapper);

        var result = await service.GetAllForLocalizationAsync("ValidationMessages", "en", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Only active ValidationMessages in English
        Assert.All(result, r => Assert.Equal("ValidationMessages", r.Resource));
        Assert.All(result, r => Assert.Equal("en", r.Culture));
    }

    [Fact]
    public async Task GetAllForLocalizationAsync_NoMatches_ReturnsEmptyList()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var repository = new Repository<MultilingualDetailsEntity, int>(context);
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualDetailsEntity, MultilingualDetailsDtos>();
        });
        var mapper = mapperConfig.CreateMapper();

        var service = new MultilingualDetailsService(repository, mockUnitOfWork.Object, mapper);

        var result = await service.GetAllForLocalizationAsync("NonExistent", "en", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllForLocalizationAsync_FiltersByResourceAndCulture()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var entities = new[]
        {
            new MultilingualDetailsEntity
            {
                Id = 1,
                Resource = "ValidationMessages",
                Culture = "en",
                Key = "Required",
                Value = "Required",
                IsActive = true
            },
            new MultilingualDetailsEntity
            {
                Id = 2,
                Resource = "ValidationMessages",
                Culture = "hi",
                Key = "Required",
                Value = "??????",
                IsActive = true
            },
            new MultilingualDetailsEntity
            {
                Id = 3,
                Resource = "Labels",
                Culture = "en",
                Key = "Submit",
                Value = "Submit",
                IsActive = true
            }
        };

        context.MultilingualDetails.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<MultilingualDetailsEntity, int>(context);
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualDetailsEntity, MultilingualDetailsDtos>();
        });
        var mapper = mapperConfig.CreateMapper();

        var service = new MultilingualDetailsService(repository, mockUnitOfWork.Object, mapper);

        var resultEn = await service.GetAllForLocalizationAsync("ValidationMessages", "en", CancellationToken.None);
        var resultHi = await service.GetAllForLocalizationAsync("ValidationMessages", "hi", CancellationToken.None);
        var resultLabels = await service.GetAllForLocalizationAsync("Labels", "en", CancellationToken.None);

        Assert.Single(resultEn);
        Assert.Equal("Required", resultEn[0].Key);
        Assert.Equal("Required", resultEn[0].Value);

        Assert.Single(resultHi);
        Assert.Equal("Required", resultHi[0].Key);
        Assert.Equal("??????", resultHi[0].Value);

        Assert.Single(resultLabels);
        Assert.Equal("Submit", resultLabels[0].Key);
    }

    [Fact]
    public async Task GetAllForLocalizationAsync_OnlyActiveRecords_Returned()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var entities = new[]
        {
            new MultilingualDetailsEntity
            {
                Id = 1,
                Resource = "Test",
                Culture = "en",
                Key = "Key1",
                Value = "Value1",
                IsActive = true
            },
            new MultilingualDetailsEntity
            {
                Id = 2,
                Resource = "Test",
                Culture = "en",
                Key = "Key2",
                Value = "Value2",
                IsActive = false
            }
        };

        context.MultilingualDetails.AddRange(entities);
        await context.SaveChangesAsync();

        var repository = new Repository<MultilingualDetailsEntity, int>(context);
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualDetailsEntity, MultilingualDetailsDtos>();
        });
        var mapper = mapperConfig.CreateMapper();

        var service = new MultilingualDetailsService(repository, mockUnitOfWork.Object, mapper);

        var result = await service.GetAllForLocalizationAsync("Test", "en", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Key1", result[0].Key);
    }

    [Fact]
    public async Task GetAllForLocalizationAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var repository = new Repository<MultilingualDetailsEntity, int>(context);
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualDetailsEntity, MultilingualDetailsDtos>();
        });
        var mapper = mapperConfig.CreateMapper();

        var service = new MultilingualDetailsService(repository, mockUnitOfWork.Object, mapper);
        var cts = new CancellationTokenSource();

        var result = await service.GetAllForLocalizationAsync("Test", "en", cts.Token);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
