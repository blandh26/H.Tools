using HTools.Core.Models;
using HTools.Core.Services;

namespace HTools.Core.Tests;

public class AppSettingsStoreTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"htools-tests-{Guid.NewGuid():N}", "settings.db");
        var store = new AppSettingsStore(path);

        var settings = store.Load();

        Assert.Equal("zh-CN", settings.Language);
        Assert.False(settings.StartWithWindows);
        Assert.Empty(settings.ClipboardSlots);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"htools-tests-{Guid.NewGuid():N}", "settings.db");
        var store = new AppSettingsStore(path);

        var original = new AppSettings
        {
            Language = "en-US",
            StartWithWindows = true,
            ClipboardSlots = [new ClipboardSlot { Index = 2, Content = "hello", UpdatedAt = new DateTime(2026, 1, 1) }],
        };

        store.Save(original);
        var loaded = store.Load();

        Assert.Equal(original.Language, loaded.Language);
        Assert.Equal(original.StartWithWindows, loaded.StartWithWindows);
        Assert.Single(loaded.ClipboardSlots);
        Assert.Equal("hello", loaded.ClipboardSlots[0].Content);

        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsServerAndMouseEffectSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"htools-tests-{Guid.NewGuid():N}", "settings.db");
        var store = new AppSettingsStore(path);

        var original = new AppSettings();
        original.MouseEffect.Shape = "Arrow";
        original.MockServer.Port = 6001;
        original.MockServer.ResponseBody = "custom";
        original.KestrelServer.StaticFile.RootFolder = @"C:\site";
        original.KestrelServer.MockApi.Rules.Add(new MockApiRuleSettings { Method = "POST", Path = "/x", StatusCode = 201, Body = "{}" });
        original.KestrelServer.Proxy.TargetUrl = "http://example.com";
        original.KestrelServer.Latency.DelayMs = 2500;

        store.Save(original);
        var loaded = store.Load();

        Assert.Equal("Arrow", loaded.MouseEffect.Shape);
        Assert.Equal(6001, loaded.MockServer.Port);
        Assert.Equal("custom", loaded.MockServer.ResponseBody);
        Assert.Equal(@"C:\site", loaded.KestrelServer.StaticFile.RootFolder);
        Assert.Single(loaded.KestrelServer.MockApi.Rules);
        Assert.Equal("POST", loaded.KestrelServer.MockApi.Rules[0].Method);
        Assert.Equal("http://example.com", loaded.KestrelServer.Proxy.TargetUrl);
        Assert.Equal(2500, loaded.KestrelServer.Latency.DelayMs);

        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileIsCorrupt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"htools-tests-{Guid.NewGuid():N}", "settings.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "this is not a LiteDB file");

        var settings = new AppSettingsStore(path).Load();

        Assert.Equal("zh-CN", settings.Language);

        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }
}
