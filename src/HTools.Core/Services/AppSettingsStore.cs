using HTools.Core.Models;
using LiteDB;

namespace HTools.Core.Services;

public interface IAppSettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}

/// <summary>
/// Persists the single <see cref="AppSettings"/> object graph (language, autostart, theme, clipboard
/// slots, and every tool's own sub-settings) as one document in a local LiteDB file, rather than as
/// JSON. LiteDB serializes plain POCOs the same way System.Text.Json did — no attributes needed on
/// <see cref="AppSettings"/> or any of its nested settings classes — so this is a drop-in replacement
/// at the storage layer only; nothing above <see cref="IAppSettingsStore"/> changes.
/// </summary>
public sealed class AppSettingsStore : IAppSettingsStore
{
    private const string CollectionName = "settings";

    // There's only ever one settings document; a fixed id keeps Load/Save both pointed at it without
    // needing an Id property bolted onto the otherwise-persistence-agnostic AppSettings model.
    private const int DocumentId = 1;

    private readonly string _databasePath;

    public AppSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HTools",
            "settings.db"))
    {
    }

    public AppSettingsStore(string databasePath)
    {
        _databasePath = databasePath;
    }

    public AppSettings Load()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
            using var db = new LiteDatabase(_databasePath);
            var collection = db.GetCollection<AppSettings>(CollectionName);
            return collection.FindById(DocumentId) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<AppSettings>(CollectionName);
        collection.Upsert(DocumentId, settings);
    }
}
