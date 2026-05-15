using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Moq;
using NtisPlatform.Api.Localization;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using System.Globalization;
using Xunit;

namespace NtisPlatform.Tests.Api.Localization;

public class DbServiceStringLocalizerTests
{
    private static DbServiceStringLocalizer Build(
        out Mock<ILocalizationService> svc,
        out Mock<IHttpContextAccessor> http,
        string? culture = "hi",
        string resource = "ValidationMessages")
    {
        svc = new Mock<ILocalizationService>();
        http = new Mock<IHttpContextAccessor>();
        var ctx = new DefaultHttpContext();
        if (culture != null)
            ctx.Items[HttpContextKeys.CurrentLanguage] = culture;
        http.Setup(h => h.HttpContext).Returns(ctx);
        return new DbServiceStringLocalizer(svc.Object, resource, http.Object);
    }

    [Fact]
    public void Indexer_ResolvesTranslationFromService()
    {
        var localizer = Build(out var svc, out _);
        svc.Setup(s => s.GetTranslation("ValidationMessages", "hi", "FloorID_Required"))
            .Returns("तल आईडी आवश्यक है");

        var result = localizer["FloorID_Required"];

        Assert.Equal("FloorID_Required", result.Name);
        Assert.Equal("तल आईडी आवश्यक है", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void Indexer_MarksResourceNotFound_WhenServiceReturnsKey()
    {
        var localizer = Build(out var svc, out _);
        svc.Setup(s => s.GetTranslation("ValidationMessages", "hi", "Missing")).Returns("Missing");

        var result = localizer["Missing"];

        Assert.True(result.ResourceNotFound);
        Assert.Equal("Missing", result.Value);
    }

    [Fact]
    public void Indexer_FallsBackToEnglish_WhenHttpContextItemMissing()
    {
        var localizer = Build(out var svc, out _, culture: null);
        svc.Setup(s => s.GetTranslation("ValidationMessages", "en", "FloorID_Required")).Returns("Floor required");

        var result = localizer["FloorID_Required"];

        Assert.Equal("Floor required", result.Value);
        svc.Verify(s => s.GetTranslation("ValidationMessages", "en", "FloorID_Required"), Times.Once);
    }

    [Fact]
    public void Indexer_WithNullHttpContext_FallsBackToEnglish()
    {
        var svc = new Mock<ILocalizationService>();
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        svc.Setup(s => s.GetTranslation("R", "en", "K")).Returns("V");

        var localizer = new DbServiceStringLocalizer(svc.Object, "R", http.Object);
        var result = localizer["K"];

        Assert.Equal("V", result.Value);
    }

    [Fact]
    public void IndexerWithArguments_FormatsResult()
    {
        var localizer = Build(out var svc, out _);
        svc.Setup(s => s.GetTranslation("ValidationMessages", "hi", "MaxLen")).Returns("Max length is {0}");

        var result = localizer["MaxLen", 5];

        Assert.Equal("Max length is 5", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void GetAllStrings_ReturnsEmpty()
    {
        var localizer = Build(out _, out _);

        Assert.Empty(localizer.GetAllStrings(includeParentCultures: true));
        Assert.Empty(localizer.GetAllStrings(includeParentCultures: false));
    }

    [Fact]
    public void WithCulture_ReturnsSelf()
    {
        var localizer = Build(out _, out _);

        var withHindi = localizer.WithCulture(new CultureInfo("hi-IN"));

        Assert.Same(localizer, withHindi);
    }

    [Fact]
    public void Factory_Create_FromType_ReturnsLocalizerForTypeName()
    {
        var svc = new Mock<ILocalizationService>();
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());
        svc.Setup(s => s.GetTranslation(typeof(SampleResource).Name, "en", "K")).Returns("hit");

        var factory = new DbServiceStringLocalizerFactory(svc.Object, http.Object);
        var localizer = factory.Create(typeof(SampleResource));

        Assert.Equal("hit", localizer["K"].Value);
    }

    [Fact]
    public void Factory_Create_FromBaseName_ReturnsLocalizerForBaseName()
    {
        var svc = new Mock<ILocalizationService>();
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());
        svc.Setup(s => s.GetTranslation("CustomResource", "en", "K")).Returns("hit");

        var factory = new DbServiceStringLocalizerFactory(svc.Object, http.Object);
        var localizer = factory.Create("CustomResource", location: "ignored");

        Assert.Equal("hit", localizer["K"].Value);
    }

    private sealed class SampleResource { }
}
