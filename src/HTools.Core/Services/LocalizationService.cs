using System.Reflection;
using System.Text.Json;

namespace HTools.Core.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string FallbackLanguage = "en-US";
    private const string ResourceFolderMarker = "Resources.Lang.";

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase);

    public LocalizationService(string initialLanguage = "zh-CN")
    {
        LoadEmbeddedTranslations();

        SupportedLanguages =
        [
            new LanguageOption("zh-CN", "中文"),
            new LanguageOption("en-US", "English"),
            new LanguageOption("ja-JP", "日本語"),
            new LanguageOption("ko-KR", "한국어"),
        ];

        CurrentLanguage = _translations.ContainsKey(initialLanguage) ? initialLanguage : FallbackLanguage;
    }

    public IReadOnlyList<LanguageOption> SupportedLanguages { get; }

    public string CurrentLanguage { get; private set; }

    public event EventHandler? LanguageChanged;

    public void SetLanguage(string languageCode)
    {
        if (!_translations.ContainsKey(languageCode) || string.Equals(languageCode, CurrentLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CurrentLanguage = languageCode;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Translate(string key)
    {
        if (_translations.TryGetValue(CurrentLanguage, out var map) && map.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_translations.TryGetValue(FallbackLanguage, out var fallbackMap) && fallbackMap.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public string this[string key] => Translate(key);

    private void LoadEmbeddedTranslations()
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var markerIndex = resourceName.IndexOf(ResourceFolderMarker, StringComparison.Ordinal);
            if (markerIndex < 0 || !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = resourceName[(markerIndex + ResourceFolderMarker.Length)..];
            var languageCode = fileName[..^".json".Length];

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
            if (map is not null)
            {
                _translations[languageCode] = map;
            }
        }
    }
}
