using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class ReferenceValidationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ReferenceValidationService _service;

    public ReferenceValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new ReferenceValidationService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task ValidateReferencesAsync_ReturnsSuccess_WhenNoConfiguredValidator()
    {
        // PropertyEntity has no configured validator
        var result = await _service.ValidateReferencesAsync<PropertyEntity>(1);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateReferencesAsync_ReturnsSuccess_WhenNoReferences()
    {
        // Zone with no Ward referrers
        var result = await _service.ValidateReferencesAsync<ZoneEntity>(99);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateReferencesAsync_ReturnsFailure_WhenReferenced()
    {
        // Add a Ward that references zone 5
        _context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W1", ZoneId = 5 });
        await _context.SaveChangesAsync();

        var result = await _service.ValidateReferencesAsync<ZoneEntity>(5);

        Assert.False(result.IsValid);
        Assert.Contains("Ward Master", result.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task ValidateReferencesAsync_ListsAllReferencingTables()
    {
        // Ward with referrers in multiple tables
        _context.BlockMasters.Add(new BlockMasterEntity { Id = 1, WardId = 7 });
        _context.PropertyMast.Add(new PropertyEntity { Id = 1, WardId = 7 });
        await _context.SaveChangesAsync();

        var result = await _service.ValidateReferencesAsync<WardEntity>(7);

        Assert.False(result.IsValid);
        Assert.Contains("Block Master", result.Errors.Single().ErrorMessage);
        Assert.Contains("Property Mast", result.Errors.Single().ErrorMessage);
    }
}
