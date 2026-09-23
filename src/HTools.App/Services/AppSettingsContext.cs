using HTools.Core.Models;
using HTools.Core.Services;

namespace HTools.App.Services;

/// <summary>Holds the one loaded <see cref="AppSettings"/> instance for the app's lifetime and persists edits.</summary>
public sealed class AppSettingsContext
{
    private readonly IAppSettingsStore _store;

    public AppSettingsContext(IAppSettingsStore store)
    {
        _store = store;
        Current = store.Load();
    }

    public AppSettings Current { get; }

    public void Save() => _store.Save(Current);
}
