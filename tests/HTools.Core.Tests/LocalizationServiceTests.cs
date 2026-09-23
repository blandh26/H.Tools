using HTools.Core.Services;

namespace HTools.Core.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void Translate_ReturnsValueForCurrentLanguage()
    {
        var loc = new LocalizationService("zh-CN");

        Assert.Equal("设置", loc.Translate("Settings.Title"));
    }

    [Fact]
    public void SetLanguage_SwitchesTranslationsAndRaisesEvent()
    {
        var loc = new LocalizationService("zh-CN");
        var raised = false;
        loc.LanguageChanged += (_, _) => raised = true;

        loc.SetLanguage("en-US");

        Assert.True(raised);
        Assert.Equal("en-US", loc.CurrentLanguage);
        Assert.Equal("Settings", loc.Translate("Settings.Title"));
    }

    [Fact]
    public void Translate_FallsBackToKey_WhenMissingEverywhere()
    {
        var loc = new LocalizationService("zh-CN");

        Assert.Equal("Not.A.Real.Key", loc.Translate("Not.A.Real.Key"));
    }

    [Fact]
    public void Constructor_FallsBackToEnglish_WhenInitialLanguageUnsupported()
    {
        var loc = new LocalizationService("fr-FR");

        Assert.Equal("en-US", loc.CurrentLanguage);
    }

    [Fact]
    public void SupportedLanguages_IncludesAllFourLanguages()
    {
        var loc = new LocalizationService();

        var codes = loc.SupportedLanguages.Select(l => l.Code).ToArray();

        Assert.Equal(["zh-CN", "en-US", "ja-JP", "ko-KR"], codes);
    }
}
